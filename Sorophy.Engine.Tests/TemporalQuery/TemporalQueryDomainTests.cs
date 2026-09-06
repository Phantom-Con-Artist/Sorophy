/*
 * Sorophy Engine — a structured knowledge and graph engine
 * Copyright (C) 2026  Subhradeep Sarkar
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.TemporalQuery;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalQuery;

public sealed class TemporalQueryDomainTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            [
                new SorophyTimeUnit(
                    "Tick",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            ]);
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Tick", SorophyTimePrecision.Exact);
    }

    private static SorophyGraph CreateGraphWithEntities(
        out Guid sourceId,
        out Guid targetId)
    {
        var graph = new SorophyGraph();

        sourceId = Guid.NewGuid();
        targetId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = sourceId, Name = "Source", Type = "Character" });
        graph.AddEntity(new SorophyEntity { Id = targetId, Name = "Target", Type = "Location" });

        return graph;
    }

    /*
     * =============================================================
     * A. POINT-IN-TIME SNAPSHOT TESTS
     * =============================================================
     */

    [Fact]
    public void PointInTime_At_ValidTime_MatchesDirectGraphSnapshot()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Connected", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Allied", t200));

        var tqd = graph.TemporalQuery;
        var snapshotFromTqd = tqd.At(t100);
        var directSnapshot = graph.CreateSnapshot(t100);

        Assert.Equal(directSnapshot.SnapshotTime, snapshotFromTqd.SnapshotTime);
        Assert.Equal(directSnapshot.EntityCount, snapshotFromTqd.EntityCount);
        Assert.Equal(directSnapshot.RelationshipCount, snapshotFromTqd.RelationshipCount);
        Assert.Equal(directSnapshot.Entities.Keys.OrderBy(k => k), snapshotFromTqd.Entities.Keys.OrderBy(k => k));
        Assert.Equal(directSnapshot.Relationships.Keys.OrderBy(k => k), snapshotFromTqd.Relationships.Keys.OrderBy(k => k));
    }

    [Fact]
    public void PointInTime_At_EventEntity_UsesOccurredAt()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t150 = CreateTime(schema, "150");

        var eventId = Guid.NewGuid();
        var eventEntity = new SorophyEntity
        {
            Id = eventId,
            Name = "Treaty Signed",
            Type = "Event",
            OccurredAt = t150
        };
        graph.AddEntity(eventEntity);

        var tqd = graph.TemporalQuery;
        var snapshot = tqd.At(eventEntity);

        Assert.NotNull(snapshot);
        Assert.Equal(t150, snapshot.SnapshotTime);
        Assert.True(snapshot.ContainsEntity(eventId));
    }

    [Fact]
    public void PointInTime_At_CoTemporalEvents_ProduceEquivalentSnapshots()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t150 = CreateTime(schema, "150");

        var event1 = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Event Alpha",
            Type = "Event",
            OccurredAt = t150
        };
        var event2 = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Event Beta",
            Type = "Event",
            OccurredAt = t150
        };
        graph.AddEntity(event1);
        graph.AddEntity(event2);

        var tqd = graph.TemporalQuery;
        var snapshot1 = tqd.At(event1);
        var snapshot2 = tqd.At(event2);

        Assert.Equal(snapshot1.SnapshotTime, snapshot2.SnapshotTime);
        Assert.Equal(snapshot1.EntityCount, snapshot2.EntityCount);
        Assert.Equal(snapshot1.RelationshipCount, snapshot2.RelationshipCount);
        Assert.Equal(snapshot1.Entities.Keys.OrderBy(k => k), snapshot2.Entities.Keys.OrderBy(k => k));
    }

    [Fact]
    public void PointInTime_FutureEvolutionExcluded_ExactTimeEvolutionIncluded()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "InitialType", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "EvolvedType", t200));

        var tqd = graph.TemporalQuery;

        // At T=150: before T=200 evolution, should have InitialType
        var snap150 = tqd.At(t150);
        Assert.True(snap150.ContainsRelationship(relId));
        Assert.Equal("InitialType", snap150.GetRelationship(relId)!.Type);

        // At T=200: exact time of evolution, should reflect EvolvedType
        var snap200 = tqd.At(t200);
        Assert.True(snap200.ContainsRelationship(relId));
        Assert.Equal("EvolvedType", snap200.GetRelationship(relId)!.Type);
    }

    [Fact]
    public void PointInTime_TerminationRespected_FutureTerminationNotPremature()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Connected", t100));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t200));

        var tqd = graph.TemporalQuery;

        // At T=150: prior to termination, relationship is active
        Assert.True(tqd.RelationshipExistsAt(relId, t150));

        // At T=200: post-transition termination takes effect, relationship is absent
        Assert.False(tqd.RelationshipExistsAt(relId, t200));
    }

    [Fact]
    public void PointInTime_FutureCreationExcluded()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t50 = CreateTime(schema, "50");
        var t100 = CreateTime(schema, "100");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Connected", t100));

        var tqd = graph.TemporalQuery;

        Assert.False(tqd.RelationshipExistsAt(relId, t50));
        Assert.True(tqd.RelationshipExistsAt(relId, t100));
    }

    [Fact]
    public void PointInTime_BaselineRelationships_PreservedAcrossCoordinates()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t50 = CreateTime(schema, "50");
        var t500 = CreateTime(schema, "500");
        var relId = Guid.NewGuid();

        graph.AddRelationship(new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "BaselineRelation"
        });

        var tqd = graph.TemporalQuery;

        Assert.True(tqd.RelationshipExistsAt(relId, t50));
        Assert.True(tqd.RelationshipExistsAt(relId, t500));
    }

    [Fact]
    public void PointInTime_DanglingRelationships_Pruned()
    {
        var graph = new SorophyGraph();
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = sourceId, Name = "Source", Type = "Character" });
        graph.AddEntity(new SorophyEntity { Id = targetId, Name = "Target", Type = "Location" });

        var relId = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship
        {
            Id = relId,
            SourceId = sourceId,
            TargetId = targetId,
            Type = "Connected"
        });

        // Remove target entity from graph
        graph.RemoveEntity(targetId);

        var tqd = graph.TemporalQuery;

        Assert.False(tqd.RelationshipExistsAt(relId, t100));
        Assert.Null(tqd.GetRelationshipAt(relId, t100));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(sourceId, t100));
    }

    /*
     * =============================================================
     * B. ENTITY QUERY TESTS
     * =============================================================
     */

    [Fact]
    public void Entity_ExistsAt_And_GetEntityAt_ValidAndUnknown()
    {
        var graph = CreateGraphWithEntities(out var s, out _);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var unknownId = Guid.NewGuid();

        var tqd = graph.TemporalQuery;

        Assert.True(tqd.EntityExistsAt(s, t100));
        var entity = tqd.GetEntityAt(s, t100);
        Assert.NotNull(entity);
        Assert.Equal(s, entity.Id);
        Assert.Equal("Source", entity.Name);

        Assert.False(tqd.EntityExistsAt(unknownId, t100));
        Assert.Null(tqd.GetEntityAt(unknownId, t100));

        Assert.False(tqd.EntityExistsAt(Guid.Empty, t100));
        Assert.Null(tqd.GetEntityAt(Guid.Empty, t100));

        Assert.Throws<ArgumentNullException>(() => tqd.EntityExistsAt(s, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetEntityAt(s, null!));
    }

    [Fact]
    public void Entity_EventEntity_HandledAsNormalEntity()
    {
        var graph = new SorophyGraph();
        var schema = CreateSchema();
        var t50 = CreateTime(schema, "50");
        var t100 = CreateTime(schema, "100");

        var eventId = Guid.NewGuid();
        var eventEntity = new SorophyEntity
        {
            Id = eventId,
            Name = "Solstice",
            Type = "Event",
            OccurredAt = t50
        };
        graph.AddEntity(eventEntity);

        var tqd = graph.TemporalQuery;

        Assert.True(tqd.EntityExistsAt(eventId, t100));
        var snapshotEvent = tqd.GetEntityAt(eventId, t100);
        Assert.NotNull(snapshotEvent);
        Assert.True(snapshotEvent.IsEvent);
        Assert.Equal(t50, snapshotEvent.OccurredAt);
    }

    /*
     * =============================================================
     * C. RELATIONSHIP QUERY TESTS
     * =============================================================
     */

    [Fact]
    public void Relationship_ExistsAt_GetAt_Outbound_Inbound()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var relId = Guid.NewGuid();

        graph.AddRelationship(new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "Direct"
        });

        var tqd = graph.TemporalQuery;

        Assert.True(tqd.RelationshipExistsAt(relId, t100));
        var rel = tqd.GetRelationshipAt(relId, t100);
        Assert.NotNull(rel);
        Assert.Equal(relId, rel.Id);
        Assert.Equal(s, rel.SourceId);
        Assert.Equal(t, rel.TargetId);

        var outbound = tqd.GetOutboundRelationshipsAt(s, t100).ToList();
        Assert.Single(outbound);
        Assert.Equal(relId, outbound[0].Id);

        var inbound = tqd.GetInboundRelationshipsAt(t, t100).ToList();
        Assert.Single(inbound);
        Assert.Equal(relId, inbound[0].Id);

        // Unknown entity / relationship
        var unknownId = Guid.NewGuid();
        Assert.False(tqd.RelationshipExistsAt(unknownId, t100));
        Assert.Null(tqd.GetRelationshipAt(unknownId, t100));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(unknownId, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(unknownId, t100));

        // Empty Guid
        Assert.False(tqd.RelationshipExistsAt(Guid.Empty, t100));
        Assert.Null(tqd.GetRelationshipAt(Guid.Empty, t100));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(Guid.Empty, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(Guid.Empty, t100));
    }

    [Fact]
    public void Relationship_RetiredRelationship_Behavior()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Connected", t100));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t200));

        var tqd = graph.TemporalQuery;

        // Point-in-time existence
        Assert.True(tqd.RelationshipExistsAt(relId, t100));
        Assert.False(tqd.RelationshipExistsAt(relId, t200));

        // History remains queryable even when retired
        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(2, history.Facts.Count);
        Assert.True(graph.IsRelationshipIdRetired(relId));
    }

    /*
     * =============================================================
     * D. INTERVAL QUERY TESTS
     * =============================================================
     */

    [Fact]
    public void Interval_InclusiveBoundaries()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", t150));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step3", t200));

        var tqd = graph.TemporalQuery;

        // Exact [100, 200] contains all 3 facts
        var allFacts = tqd.GetFactsInInterval(t100, t200);
        Assert.Equal(3, allFacts.Count);
        Assert.Equal(t100, allFacts[0].At);
        Assert.Equal(t150, allFacts[1].At);
        Assert.Equal(t200, allFacts[2].At);

        // Sub-interval [101, 199] contains only 150
        var t101 = CreateTime(schema, "101");
        var t199 = CreateTime(schema, "199");
        var subFacts = tqd.GetFactsInInterval(t101, t199);
        Assert.Single(subFacts);
        Assert.Equal(t150, subFacts[0].At);

        // Outside interval [201, 300] contains none
        var t201 = CreateTime(schema, "201");
        var t300 = CreateTime(schema, "300");
        Assert.Empty(tqd.GetFactsInInterval(t201, t300));
    }

    [Fact]
    public void Interval_InvertedInterval_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t200 = CreateTime(schema, "200");

        var tqd = graph.TemporalQuery;

        Assert.Throws<ArgumentException>(() => tqd.GetFactsInInterval(t200, t100));
        Assert.Throws<ArgumentException>(() => tqd.GetModifiedRelationshipIds(t200, t100));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipFactsInInterval(Guid.NewGuid(), t200, t100));
    }

    [Fact]
    public void Interval_IncompatibleTemporalCoordinates_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var schemaA = CreateSchema("Timeline A");
        var schemaB = CreateSchema("Timeline B");
        var timeA = CreateTime(schemaA, "100");
        var timeB = CreateTime(schemaB, "200");

        var tqd = graph.TemporalQuery;

        Assert.Throws<ArgumentException>(() => tqd.GetFactsInInterval(timeA, timeB));
        Assert.Throws<ArgumentException>(() => tqd.GetModifiedRelationshipIds(timeA, timeB));
    }

    [Fact]
    public void Interval_GetModifiedRelationshipIds()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t250 = CreateTime(schema, "250");

        var rel1 = Guid.NewGuid();
        var rel2 = Guid.NewGuid();
        var rel3 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rel1, s, t, "Rel1", t100));
        executor.Execute(graph, new SorophyRelationshipCreation(rel2, s, t, "Rel2", t150));
        executor.Execute(graph, new SorophyRelationshipCreation(rel3, s, t, "Rel3", t250));

        var tqd = graph.TemporalQuery;

        var modifiedIn100To200 = tqd.GetModifiedRelationshipIds(t100, CreateTime(schema, "200"));
        Assert.Equal(2, modifiedIn100To200.Count);
        Assert.Contains(rel1, modifiedIn100To200);
        Assert.Contains(rel2, modifiedIn100To200);
        Assert.DoesNotContain(rel3, modifiedIn100To200);
    }

    /*
     * =============================================================
     * E. SAME-TIME FACTS & DETERMINISM
     * =============================================================
     */

    [Fact]
    public void SameTimeFacts_MultipleFactsWithIdenticalAt_ReturnedDeterministically()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");

        var rel1 = Guid.NewGuid();
        var rel2 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rel1, s, t, "Rel1", t100));
        executor.Execute(graph, new SorophyRelationshipCreation(rel2, s, t, "Rel2", t100));

        var tqd = graph.TemporalQuery;

        // Query multiple times: order must be 100% deterministic
        var factsFirstRun = tqd.GetFactsInInterval(t100, t100);
        var factsSecondRun = tqd.GetFactsInInterval(t100, t100);

        Assert.Equal(2, factsFirstRun.Count);
        Assert.Equal(2, factsSecondRun.Count);
        Assert.Equal(factsFirstRun[0].RelationshipId, factsSecondRun[0].RelationshipId);
        Assert.Equal(factsFirstRun[1].RelationshipId, factsSecondRun[1].RelationshipId);

        // Co-temporal check: At coordinates are identical
        Assert.Equal(factsFirstRun[0].At, factsFirstRun[1].At);
    }

    [Fact]
    public void SameTimeFacts_WithinSingleRelationship_PreservesInsertionIndexDeterministically()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "TypeA", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "TypeB", t100));

        var tqd = graph.TemporalQuery;

        var facts = tqd.GetRelationshipFactsInInterval(relId, t100, t100);
        Assert.Equal(2, facts.Count);
        Assert.Equal(t100, facts[0].At);
        Assert.Equal(t100, facts[1].At);
        Assert.Equal("TypeA", facts[0].Type);
        Assert.Equal("TypeA", facts[1].Type); // fact captured state before mutation to TypeB
    }

    /*
     * =============================================================
     * F. RELATIONSHIP HISTORY TESTS
     * =============================================================
     */

    [Fact]
    public void History_GetRelationshipHistory_CompleteRetrieval()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t200 = CreateTime(schema, "200");
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "TypeA", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "TypeB", t200));

        var tqd = graph.TemporalQuery;

        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(relId, history.RelationshipId);
        Assert.Equal(2, history.Facts.Count);
    }

    [Fact]
    public void History_BaselineRelationship_HasNoSyntheticHistory()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t0 = CreateTime(schema, "0");
        var t1000 = CreateTime(schema, "1000");
        var relId = Guid.NewGuid();

        graph.AddRelationship(new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "Baseline"
        });

        var tqd = graph.TemporalQuery;

        // Baseline un-evolved relationship has no recorded history
        Assert.Null(tqd.GetRelationshipHistory(relId));
        Assert.Empty(tqd.GetRelationshipFactsInInterval(relId, t0, t1000));
    }

    /*
     * =============================================================
     * G. PROVENANCE / EVENT QUERY TESTS
     * =============================================================
     */

    [Fact]
    public void Provenance_GetFactsByEvent_GuidAndEntityOverloads()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");

        var eventId = Guid.NewGuid();
        var eventEntity = new SorophyEntity
        {
            Id = eventId,
            Name = "Coronation",
            Type = "Event",
            OccurredAt = t100
        };
        graph.AddEntity(eventEntity);

        var rel1 = Guid.NewGuid();
        var rel2 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rel1, s, t, "Rel1", t100, eventEntityId: eventId));
        executor.Execute(graph, new SorophyRelationshipCreation(rel2, s, t, "Rel2", t100, eventEntityId: eventId));

        var tqd = graph.TemporalQuery;

        var factsByGuid = tqd.GetFactsByEvent(eventId);
        var factsByEntity = tqd.GetFactsByEvent(eventEntity);

        Assert.Equal(2, factsByGuid.Count);
        Assert.Equal(2, factsByEntity.Count);
        Assert.Equal(factsByGuid.Select(f => f.RelationshipId), factsByEntity.Select(f => f.RelationshipId));

        var evolvedByGuid = tqd.GetRelationshipsEvolvedByEvent(eventId);
        var evolvedByEntity = tqd.GetRelationshipsEvolvedByEvent(eventEntity);

        Assert.Equal(2, evolvedByGuid.Count);
        Assert.Equal(2, evolvedByEntity.Count);
        Assert.Contains(rel1, evolvedByGuid);
        Assert.Contains(rel2, evolvedByGuid);
    }

    [Fact]
    public void Provenance_NullEventEntityId_NotFalselyAttributed()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");

        var eventId = Guid.NewGuid();
        var eventEntity = new SorophyEntity
        {
            Id = eventId,
            Name = "Festival",
            Type = "Event",
            OccurredAt = t100
        };
        graph.AddEntity(eventEntity);

        var unanchoredRel = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(unanchoredRel, s, t, "Unanchored", t100, eventEntityId: null));

        var tqd = graph.TemporalQuery;

        var facts = tqd.GetFactsByEvent(eventId);
        Assert.Empty(facts);

        var evolved = tqd.GetRelationshipsEvolvedByEvent(eventId);
        Assert.Empty(evolved);
    }

    [Fact]
    public void Provenance_InvalidNonEventEntity_ThrowsArgumentException()
    {
        var graph = CreateGraphWithEntities(out var s, out _);
        var tqd = graph.TemporalQuery;

        // Source is Type="Character", not "Event"
        Assert.Throws<ArgumentException>(() => tqd.GetFactsByEvent(s));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipsEvolvedByEvent(s));

        var characterEntity = graph.Entities[s];
        Assert.Throws<ArgumentException>(() => tqd.GetFactsByEvent(characterEntity));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipsEvolvedByEvent(characterEntity));
    }

    [Fact]
    public void Provenance_UnknownEventId_ReturnsEmpty()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;
        var unknownId = Guid.NewGuid();

        Assert.Empty(tqd.GetFactsByEvent(unknownId));
        Assert.Empty(tqd.GetRelationshipsEvolvedByEvent(unknownId));
    }

    /*
     * =============================================================
     * H. READ-ONLY / MUTATION ISOLATION TESTS
     * =============================================================
     */

    [Fact]
    public void ReadOnly_SnapshotDoesNotPermitMutationOfCanonicalGraph()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var relId = Guid.NewGuid();

        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "Connected"
        };
        rel.Properties.Add("Key", new SorophyProperty
        {
            Name = "Key",
            Value = new SorophyValue(SorophyValueType.String, "Original")
        });
        graph.AddRelationship(rel);

        var tqd = graph.TemporalQuery;
        var snapshot = tqd.At(t100);
        var snapRel = snapshot.GetRelationship(relId);
        Assert.NotNull(snapRel);

        // Mutating a snapshot property clone does not affect canonical graph
        snapRel.Properties["Key"].Value = new SorophyValue(SorophyValueType.String, "Mutated");
        Assert.Equal("Original", rel.Properties["Key"].Value.Value);
    }

    /*
     * =============================================================
     * I. REPEATED QUERY DETERMINISM TESTS
     * =============================================================
     */

    [Fact]
    public void Determinism_RepeatedQueriesProduceIdenticalResults()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t100 = CreateTime(schema, "100");
        var t200 = CreateTime(schema, "200");

        var eventId = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = eventId, Name = "Event", Type = "Event", OccurredAt = t100 });

        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();
        var r3 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(r1, s, t, "R1", t100, eventEntityId: eventId));
        executor.Execute(graph, new SorophyRelationshipCreation(r2, s, t, "R2", t100, eventEntityId: eventId));
        executor.Execute(graph, new SorophyRelationshipCreation(r3, s, t, "R3", t200));

        var tqd = graph.TemporalQuery;

        var baselineFacts = tqd.GetFactsInInterval(t100, t200);
        var baselineModified = tqd.GetModifiedRelationshipIds(t100, t200);
        var baselineEvolved = tqd.GetRelationshipsEvolvedByEvent(eventId);

        for (int i = 0; i < 20; i++)
        {
            var iterFacts = tqd.GetFactsInInterval(t100, t200);
            var iterModified = tqd.GetModifiedRelationshipIds(t100, t200);
            var iterEvolved = tqd.GetRelationshipsEvolvedByEvent(eventId);

            Assert.Equal(baselineFacts.Count, iterFacts.Count);
            for (int j = 0; j < baselineFacts.Count; j++)
            {
                Assert.Equal(baselineFacts[j].RelationshipId, iterFacts[j].RelationshipId);
                Assert.Equal(baselineFacts[j].At, iterFacts[j].At);
            }

            Assert.Equal(baselineModified, iterModified);
            Assert.Equal(baselineEvolved, iterEvolved);
        }
    }
}
