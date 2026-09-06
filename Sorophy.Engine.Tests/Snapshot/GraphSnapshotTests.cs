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
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Snapshot;

public sealed class GraphSnapshotTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Tick",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
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

    // =============================================================
    // A. BASIC SNAPSHOTS
    // =============================================================

    [Fact]
    public void BasicSnapshot_EmptyGraph_ReturnsEmptySnapshot()
    {
        var graph = new SorophyGraph();
        var schema = CreateSchema();
        var time = CreateTime(schema, "100");

        var snapshot = graph.CreateSnapshot(time);

        Assert.NotNull(snapshot);
        Assert.Equal(time, snapshot.SnapshotTime);
        Assert.Equal(0, snapshot.EntityCount);
        Assert.Equal(0, snapshot.RelationshipCount);
        Assert.Empty(snapshot.Entities);
        Assert.Empty(snapshot.Relationships);
    }

    [Fact]
    public void BasicSnapshot_BaselineEntitiesAndRelationships_AreMaterialized()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var relId = Guid.NewGuid();

        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "Connected"
        };
        rel.Properties["Strength"] = new SorophyProperty
        {
            Name = "Strength",
            Value = new SorophyValue(SorophyValueType.Integer, 5L)
        };
        graph.AddRelationship(rel);

        var schema = CreateSchema();
        var time = CreateTime(schema, "42");

        var snapshot = graph.CreateSnapshot(time);

        Assert.Equal(2, snapshot.EntityCount);
        Assert.Equal(1, snapshot.RelationshipCount);
        Assert.True(snapshot.ContainsEntity(s));
        Assert.True(snapshot.ContainsEntity(t));
        Assert.True(snapshot.ContainsRelationship(relId));

        var snapRel = snapshot.GetRelationship(relId);
        Assert.NotNull(snapRel);
        Assert.Equal("Connected", snapRel!.Type);
        Assert.Equal(5L, snapRel.Properties["Strength"].Value.Value);

        var outRels = snapshot.GetOutboundRelationships(s).ToList();
        Assert.Single(outRels);
        Assert.Equal(relId, outRels[0].Id);

        var inRels = snapshot.GetInboundRelationships(t).ToList();
        Assert.Single(inRels);
        Assert.Equal(relId, inRels[0].Id);
    }

    // =============================================================
    // B. RELATIONSHIP CREATION
    // =============================================================

    [Fact]
    public void Creation_TemporalVisibility_BeforeAtAndAfter()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Alliance", tCreate));

        // Before creation: absent
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "9"));
        Assert.False(snapBefore.ContainsRelationship(relId));
        Assert.Equal(0, snapBefore.RelationshipCount);

        // Exactly at creation: present
        var snapAt = graph.CreateSnapshot(tCreate);
        Assert.True(snapAt.ContainsRelationship(relId));
        Assert.Equal(1, snapAt.RelationshipCount);
        Assert.Equal("Alliance", snapAt.GetRelationship(relId)!.Type);

        // After creation: present
        var snapAfter = graph.CreateSnapshot(CreateTime(schema, "15"));
        Assert.True(snapAfter.ContainsRelationship(relId));
        Assert.Equal(1, snapAfter.RelationshipCount);
    }

    // =============================================================
    // C. RELATIONSHIP MUTATION (PROPERTY MODIFICATION)
    // =============================================================

    [Fact]
    public void PropertyModification_TemporalVisibility_BeforeAtAndAfter()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");
        var tMod = CreateTime(schema, "20");

        var relId = Guid.NewGuid();
        var initialProps = new Dictionary<string, SorophyProperty>
        {
            ["Level"] = new SorophyProperty { Name = "Level", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
        };

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Alliance", tCreate, initialProps));

        var modProps = new Dictionary<string, SorophyProperty>
        {
            ["Level"] = new SorophyProperty { Name = "Level", Value = new SorophyValue(SorophyValueType.Integer, 5L) }
        };
        executor.Execute(graph, new SorophyRelationshipPropertyModification(relId, tMod, propertiesToSet: modProps));

        // Before mutation (between creation and mod): initial property
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "15"));
        Assert.True(snapBefore.ContainsRelationship(relId));
        Assert.Equal(1L, snapBefore.GetRelationship(relId)!.Properties["Level"].Value.Value);

        // Exactly at mutation: post-mutation property visible
        var snapAt = graph.CreateSnapshot(tMod);
        Assert.True(snapAt.ContainsRelationship(relId));
        Assert.Equal(5L, snapAt.GetRelationship(relId)!.Properties["Level"].Value.Value);

        // After mutation: post-mutation property visible
        var snapAfter = graph.CreateSnapshot(CreateTime(schema, "25"));
        Assert.True(snapAfter.ContainsRelationship(relId));
        Assert.Equal(5L, snapAfter.GetRelationship(relId)!.Properties["Level"].Value.Value);
    }

    // =============================================================
    // D. RELATIONSHIP TYPE CHANGE
    // =============================================================

    [Fact]
    public void TypeChange_TemporalVisibility_BeforeAtAndAfter()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");
        var tChange = CreateTime(schema, "20");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Alliance", tCreate));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "War", tChange));

        // Before type change
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "15"));
        Assert.Equal("Alliance", snapBefore.GetRelationship(relId)!.Type);

        // Exactly at type change
        var snapAt = graph.CreateSnapshot(tChange);
        Assert.Equal("War", snapAt.GetRelationship(relId)!.Type);

        // After type change
        var snapAfter = graph.CreateSnapshot(CreateTime(schema, "30"));
        Assert.Equal("War", snapAfter.GetRelationship(relId)!.Type);
    }

    // =============================================================
    // E. RELATIONSHIP VALIDITY CHANGE
    // =============================================================

    [Fact]
    public void ValidityChange_TemporalVisibility_PreservesSemanticValidity()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");
        var tValChange = CreateTime(schema, "25");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s, t, "Treaty", tCreate,
            validFrom: CreateTime(schema, "10"),
            validTill: CreateTime(schema, "50")));

        var newFrom = CreateTime(schema, "15");
        var newTill = CreateTime(schema, "100");
        executor.Execute(graph, new SorophyRelationshipValidityChange(relId, tValChange, newFrom, newTill));

        // Before validity change: historical fact recorded with ValidTill = tValChange
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "20"));
        Assert.Equal(CreateTime(schema, "10"), snapBefore.GetRelationship(relId)!.ValidFrom);
        Assert.Equal(tValChange, snapBefore.GetRelationship(relId)!.ValidTill);

        // At validity change: new validity
        var snapAt = graph.CreateSnapshot(tValChange);
        Assert.Equal(newFrom, snapAt.GetRelationship(relId)!.ValidFrom);
        Assert.Equal(newTill, snapAt.GetRelationship(relId)!.ValidTill);
    }

    // =============================================================
    // F. RELATIONSHIP TERMINATION
    // =============================================================

    [Fact]
    public void Termination_TemporalVisibility_BeforeAtAndAfter()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");
        var tTerm = CreateTime(schema, "30");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Pact", tCreate));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, tTerm));

        // Before termination: present
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "29"));
        Assert.True(snapBefore.ContainsRelationship(relId));

        // Exactly at termination: absent (post-transition semantics)
        var snapAt = graph.CreateSnapshot(tTerm);
        Assert.False(snapAt.ContainsRelationship(relId));

        // After termination: absent
        var snapAfter = graph.CreateSnapshot(CreateTime(schema, "31"));
        Assert.False(snapAfter.ContainsRelationship(relId));
    }

    // =============================================================
    // G. MULTIPLE EVOLUTIONS
    // =============================================================

    [Fact]
    public void MultipleEvolutions_ComplexLifecycleReplay()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var t3 = CreateTime(schema, "30");
        var t4 = CreateTime(schema, "40");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s, t, "Trade", t1,
            new Dictionary<string, SorophyProperty>
            {
                ["Gold"] = new SorophyProperty { Name = "Gold", Value = new SorophyValue(SorophyValueType.Integer, 100L) }
            }));

        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, t2,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Gold"] = new SorophyProperty { Name = "Gold", Value = new SorophyValue(SorophyValueType.Integer, 500L) }
            }));

        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Embargo", t3));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t4));

        // Verify state at each interval:
        // T = 5 (before creation)
        Assert.False(graph.CreateSnapshot(CreateTime(schema, "5")).ContainsRelationship(relId));

        // T = 15 (creation state: Trade, Gold 100)
        var s15 = graph.CreateSnapshot(CreateTime(schema, "15")).GetRelationship(relId);
        Assert.NotNull(s15);
        Assert.Equal("Trade", s15!.Type);
        Assert.Equal(100L, s15.Properties["Gold"].Value.Value);

        // T = 25 (modified state: Trade, Gold 500)
        var s25 = graph.CreateSnapshot(CreateTime(schema, "25")).GetRelationship(relId);
        Assert.NotNull(s25);
        Assert.Equal("Trade", s25!.Type);
        Assert.Equal(500L, s25.Properties["Gold"].Value.Value);

        // T = 35 (type change state: Embargo, Gold 500)
        var s35 = graph.CreateSnapshot(CreateTime(schema, "35")).GetRelationship(relId);
        Assert.NotNull(s35);
        Assert.Equal("Embargo", s35!.Type);
        Assert.Equal(500L, s35.Properties["Gold"].Value.Value);

        // T = 40 (terminated)
        Assert.False(graph.CreateSnapshot(t4).ContainsRelationship(relId));
    }

    // =============================================================
    // H. SAME-COORDINATE EVOLUTION FACTS
    // =============================================================

    [Fact]
    public void SameCoordinate_CreationMutationTermination_ResultsInAbsent()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCoord = CreateTime(schema, "100");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Temp", tCoord));
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, tCoord,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["X"] = new SorophyProperty { Name = "X", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
            }));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, tCoord));

        // Exactly at tCoord: termination is final transition => absent
        var snap = graph.CreateSnapshot(tCoord);
        Assert.False(snap.ContainsRelationship(relId));
    }

    [Fact]
    public void SameCoordinate_CreationAndMutation_ShowsMutatedState()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCoord = CreateTime(schema, "100");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Initial", tCoord));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Updated", tCoord));

        var snap = graph.CreateSnapshot(tCoord);
        Assert.True(snap.ContainsRelationship(relId));
        Assert.Equal("Updated", snap.GetRelationship(relId)!.Type);
    }

    // =============================================================
    // I. EVENT SNAPSHOT OVERLOAD
    // =============================================================

    [Fact]
    public void EventOverload_ValidEvent_DelegatesToOccurredAt()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var eventTime = CreateTime(schema, "50");

        var eventEntity = new SorophyEntity
        {
            Type = "Event",
            Name = "Battle of Five Armies",
            OccurredAt = eventTime
        };
        graph.AddEntity(eventEntity);

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Alliance", eventTime));

        var snapFromEvent = graph.CreateSnapshot(eventEntity);
        var snapFromTime = graph.CreateSnapshot(eventTime);

        Assert.Equal(snapFromTime.EntityCount, snapFromEvent.EntityCount);
        Assert.Equal(snapFromTime.RelationshipCount, snapFromEvent.RelationshipCount);
        Assert.True(snapFromEvent.ContainsRelationship(relId));
        Assert.Equal(eventTime, snapFromEvent.SnapshotTime);
    }

    [Fact]
    public void EventOverload_NonEventEntity_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var schema = CreateSchema();

        var nonEvent = new SorophyEntity
        {
            Type = "Character",
            Name = "Not An Event",
            OccurredAt = CreateTime(schema, "10")
        };

        Assert.Throws<ArgumentException>(() => graph.CreateSnapshot(nonEvent));
    }

    [Fact]
    public void EventOverload_EventWithoutOccurredAt_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();

        var eventWithoutTime = new SorophyEntity
        {
            Type = "Event",
            Name = "Timeless Event",
            OccurredAt = null
        };

        Assert.Throws<ArgumentException>(() => graph.CreateSnapshot(eventWithoutTime));
    }

    [Fact]
    public void EventOverload_NullEvent_ThrowsArgumentNullException()
    {
        var graph = new SorophyGraph();
        Assert.Throws<ArgumentNullException>(() => graph.CreateSnapshot((SorophyEntity)null!));
    }

    // =============================================================
    // J. CO-TEMPORAL EVENTS
    // =============================================================

    [Fact]
    public void CoTemporalEvents_ProduceEquivalentSnapshots()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var sharedTime = CreateTime(schema, "100");

        var eventA = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Type = "Event",
            Name = "Event Alpha",
            OccurredAt = sharedTime
        };
        var eventB = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Type = "Event",
            Name = "Event Beta",
            OccurredAt = sharedTime
        };

        graph.AddEntity(eventA);
        graph.AddEntity(eventB);

        var snapA = graph.CreateSnapshot(eventA);
        var snapB = graph.CreateSnapshot(eventB);
        var snapT = graph.CreateSnapshot(sharedTime);

        Assert.Equal(snapT.EntityCount, snapA.EntityCount);
        Assert.Equal(snapA.EntityCount, snapB.EntityCount);
        Assert.Equal(snapT.RelationshipCount, snapA.RelationshipCount);
        Assert.Equal(snapA.RelationshipCount, snapB.RelationshipCount);

        foreach (var key in snapA.Entities.Keys)
        {
            Assert.True(snapB.ContainsEntity(key));
        }
    }

    // =============================================================
    // K. NO-EVENT TEMPORAL POINTS
    // =============================================================

    [Fact]
    public void NoEventTemporalPoints_SnapshotSucceedsAtArbitraryCoordinates()
    {
        var graph = CreateGraphWithEntities(out _, out _);
        var schema = CreateSchema();

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "99999"));
        Assert.NotNull(snap1);
        Assert.Equal(2, snap1.EntityCount);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "-100"));
        Assert.NotNull(snap2);
        Assert.Equal(2, snap2.EntityCount);
    }

    // =============================================================
    // L. BASELINE STRUCTURES
    // =============================================================

    [Fact]
    public void BaselineStructures_PreExistingHistoryAcrossAllCoordinates()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var relId = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Baseline" });

        var schema = CreateSchema();

        // Baseline structures exist at any arbitrary point in time unless evolved
        var snapPast = graph.CreateSnapshot(CreateTime(schema, "0"));
        Assert.True(snapPast.ContainsRelationship(relId));

        var snapFarFuture = graph.CreateSnapshot(CreateTime(schema, "1000000"));
        Assert.True(snapFarFuture.ContainsRelationship(relId));
    }

    [Fact]
    public void BaselineStructure_EvolvedLater_ExistedPriorToEvolution()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var relId = Guid.NewGuid();
        var baselineRel = new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Friend" };
        baselineRel.Properties["Points"] = new SorophyProperty
        {
            Name = "Points",
            Value = new SorophyValue(SorophyValueType.Integer, 10L)
        };
        graph.AddRelationship(baselineRel);

        var schema = CreateSchema();
        var tMod = CreateTime(schema, "50");

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, tMod,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Points"] = new SorophyProperty { Name = "Points", Value = new SorophyValue(SorophyValueType.Integer, 99L) }
            }));

        // Prior to tMod: baseline property was 10
        var snapBefore = graph.CreateSnapshot(CreateTime(schema, "20"));
        Assert.True(snapBefore.ContainsRelationship(relId));
        Assert.Equal(10L, snapBefore.GetRelationship(relId)!.Properties["Points"].Value.Value);

        // At and after tMod: property is 99
        var snapAfter = graph.CreateSnapshot(CreateTime(schema, "50"));
        Assert.True(snapAfter.ContainsRelationship(relId));
        Assert.Equal(99L, snapAfter.GetRelationship(relId)!.Properties["Points"].Value.Value);
    }

    // =============================================================
    // M. REFERENTIAL INTEGRITY
    // =============================================================

    [Fact]
    public void ReferentialIntegrity_DanglingRelationshipsPrunedFromSnapshot()
    {
        var graph = new SorophyGraph();
        var s = Guid.NewGuid();
        var t = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = s, Name = "S" });
        graph.AddEntity(new SorophyEntity { Id = t, Name = "T" });

        var relId = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Edge" });

        // Removing entity T removes it and its adjacency from graph
        graph.RemoveEntity(t);

        var schema = CreateSchema();
        var snap = graph.CreateSnapshot(CreateTime(schema, "10"));

        // Entity T is absent
        Assert.False(snap.ContainsEntity(t));
        // Relationship is pruned from snapshot to maintain referential integrity
        Assert.False(snap.ContainsRelationship(relId));
        Assert.Empty(snap.GetOutboundRelationships(s));
    }

    // =============================================================
    // N. RETIRED RELATIONSHIP IDS
    // =============================================================

    [Fact]
    public void RetiredIds_TerminatedAfterT_WasActiveAtT()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var tCreate = CreateTime(schema, "10");
        var tTerm = CreateTime(schema, "100");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Temporary", tCreate));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, tTerm));

        // At T = 50, relationship was active (termination had not happened yet)
        var snap50 = graph.CreateSnapshot(CreateTime(schema, "50"));
        Assert.True(snap50.ContainsRelationship(relId));

        // At T = 100, relationship was terminated
        var snap100 = graph.CreateSnapshot(tTerm);
        Assert.False(snap100.ContainsRelationship(relId));
    }

    // =============================================================
    // O. SNAPSHOT ISOLATION (LIVE GRAPH MUTATION)
    // =============================================================

    [Fact]
    public void SnapshotIsolation_LiveGraphMutationsDoNotAffectSnapshot()
    {
        var graph = CreateGraphWithEntities(out var s, out var t);
        var schema = CreateSchema();
        var time = CreateTime(schema, "10");

        var relId = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Initial" });

        var snapshot = graph.CreateSnapshot(time);
        Assert.Equal(2, snapshot.EntityCount);
        Assert.Equal(1, snapshot.RelationshipCount);

        // Mutate live graph: add entity, remove relationship, add relationship
        var newEntity = new SorophyEntity { Id = Guid.NewGuid(), Name = "New" };
        graph.AddEntity(newEntity);
        graph.RemoveRelationship(relId);

        // Existing snapshot must remain completely unaffected
        Assert.Equal(2, snapshot.EntityCount);
        Assert.Equal(1, snapshot.RelationshipCount);
        Assert.True(snapshot.ContainsRelationship(relId));
        Assert.False(snapshot.ContainsEntity(newEntity.Id));
    }

    // =============================================================
    // P. DEEP ISOLATION (MUTABLE DATA STRUCTURES)
    // =============================================================

    [Fact]
    public void DeepIsolation_NestedValueListAndObject_DoNotLeakMutations()
    {
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();

        var nestedList = new List<object?> { "item1", 42L };
        var nestedDict = new Dictionary<string, object?> { ["key1"] = "val1" };

        var entity = new SorophyEntity { Id = entityId, Name = "DeepEntity" };
        entity.Properties["ListProp"] = new SorophyProperty
        {
            Name = "ListProp",
            Value = new SorophyValue(SorophyValueType.List, nestedList)
        };
        entity.Properties["DictProp"] = new SorophyProperty
        {
            Name = "DictProp",
            Value = new SorophyValue(SorophyValueType.Object, nestedDict)
        };
        entity.Tags.Add("TagA");
        entity.Documents.Add("Doc.md", new SorophyEntityDocument("Doc.md", "Original Content"));

        graph.AddEntity(entity);

        var schema = CreateSchema();
        var snapshot = graph.CreateSnapshot(CreateTime(schema, "10"));

        // Mutate live object collections directly
        nestedList.Add("leakedItem");
        nestedDict["key1"] = "mutatedVal";
        entity.Tags.Add("TagB");
        entity.Documents["Doc.md"] = new SorophyEntityDocument("Doc.md", "Mutated Content");
        entity.Properties["ListProp"].Name = "Renamed";

        // Verify snapshot values remained isolated
        var snapEntity = snapshot.GetEntity(entityId);
        Assert.NotNull(snapEntity);

        var snapList = (List<object?>)snapEntity!.Properties["ListProp"].Value.Value!;
        Assert.Equal(2, snapList.Count);
        Assert.Equal("item1", snapList[0]);
        Assert.Equal(42L, snapList[1]);

        var snapDict = (Dictionary<string, object?>)snapEntity.Properties["DictProp"].Value.Value!;
        Assert.Equal("val1", snapDict["key1"]);

        Assert.Single(snapEntity.Tags);
        Assert.Contains("TagA", snapEntity.Tags);
        Assert.DoesNotContain("TagB", snapEntity.Tags);

        Assert.Equal("Original Content", snapEntity.Documents["Doc.md"].Content);
        Assert.Equal("ListProp", snapEntity.Properties["ListProp"].Name);
    }

    // =============================================================
    // Q. DETERMINISM
    // =============================================================

    [Fact]
    public void Determinism_EquivalentStateAndCoordinate_ProducesEquivalentSnapshots()
    {
        var schema = CreateSchema();
        var tCoord = CreateTime(schema, "100");

        var graph1 = CreateGraphWithEntities(out var s1, out var t1);
        var relId1 = Guid.NewGuid();
        var exec1 = new SorophyRelationshipEvolutionExecutor();
        exec1.Execute(graph1, new SorophyRelationshipCreation(relId1, s1, t1, "Edge", tCoord));

        var snap1 = graph1.CreateSnapshot(tCoord);
        var snap2 = graph1.CreateSnapshot(tCoord);

        Assert.Equal(snap1.EntityCount, snap2.EntityCount);
        Assert.Equal(snap1.RelationshipCount, snap2.RelationshipCount);
        Assert.Equal(snap1.GetRelationship(relId1)!.Type, snap2.GetRelationship(relId1)!.Type);
    }
}
