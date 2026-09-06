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
using Sorophy.Engine.TemporalQuery;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalQuery;

/// <summary>
/// Domain C11 (Metamorphic Testing): Formal verification of metamorphic invariants.
/// </summary>
public sealed class TqdCrucibleMetamorphicTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    [Fact]
    public void Metamorphic_Property1_TqdAtEqualsCreateSnapshot()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", TqdCrucibleTestHelper.CreateTime(_schema, 50)));

        var tqd = graph.TemporalQuery;

        var testCoords = new[] { 0, 5, 10, 25, 50, 75, 100 };
        foreach (var c in testCoords)
        {
            var targetTime = TqdCrucibleTestHelper.CreateTime(_schema, c);
            var tqdSnap = tqd.At(targetTime);
            var directSnap = graph.CreateSnapshot(targetTime);

            Assert.Equal(directSnap.SnapshotTime, tqdSnap.SnapshotTime);
            Assert.Equal(directSnap.EntityCount, tqdSnap.EntityCount);
            Assert.Equal(directSnap.RelationshipCount, tqdSnap.RelationshipCount);
            Assert.Equal(directSnap.Entities.Keys.OrderBy(k => k), tqdSnap.Entities.Keys.OrderBy(k => k));
            Assert.Equal(directSnap.Relationships.Keys.OrderBy(k => k), tqdSnap.Relationships.Keys.OrderBy(k => k));
        }
    }

    [Fact]
    public void Metamorphic_Property2_AddingFutureEvolutionDoesNotAlterPastQueries()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        var tqd = graph.TemporalQuery;
        var t20 = TqdCrucibleTestHelper.CreateTime(_schema, 20);

        var snapBefore = tqd.At(t20);
        var factsBefore = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t20);

        // Add future evolution at T=100
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", TqdCrucibleTestHelper.CreateTime(_schema, 100)));

        var snapAfter = tqd.At(t20);
        var factsAfter = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t20);

        Assert.Equal(snapBefore.EntityCount, snapAfter.EntityCount);
        Assert.Equal(snapBefore.RelationshipCount, snapAfter.RelationshipCount);
        Assert.Equal(snapBefore.GetRelationship(relId)!.Type, snapAfter.GetRelationship(relId)!.Type);
        Assert.Equal(factsBefore.Count, factsAfter.Count);
    }

    [Fact]
    public void Metamorphic_Property3_EvolvingUnrelatedRelationshipDoesNotAlterTargetQuery()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var rTarget = Guid.NewGuid();
        var rOther = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rTarget, s, t, "Target", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        executor.Execute(graph, new SorophyRelationshipCreation(rOther, s, t, "Other", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        var tqd = graph.TemporalQuery;
        var t50 = TqdCrucibleTestHelper.CreateTime(_schema, 50);

        var targetBefore = tqd.GetRelationshipAt(rTarget, t50);
        var targetHistoryBefore = tqd.GetRelationshipHistory(rTarget);

        // Mutate and terminate rOther
        executor.Execute(graph, new SorophyRelationshipTypeChange(rOther, "OtherMutated", TqdCrucibleTestHelper.CreateTime(_schema, 30)));
        executor.Execute(graph, new SorophyRelationshipTermination(rOther, TqdCrucibleTestHelper.CreateTime(_schema, 40)));

        var targetAfter = tqd.GetRelationshipAt(rTarget, t50);
        var targetHistoryAfter = tqd.GetRelationshipHistory(rTarget);

        Assert.NotNull(targetBefore);
        Assert.NotNull(targetAfter);
        Assert.Equal(targetBefore.Type, targetAfter.Type);
        Assert.Equal(targetHistoryBefore!.Facts.Count, targetHistoryAfter!.Facts.Count);
    }

    [Fact]
    public void Metamorphic_Property4_AtEventEqualsAtOccurredAt()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var tEvent = TqdCrucibleTestHelper.CreateTime(_schema, 77);
        var eventEntity = TqdCrucibleTestHelper.CreateEventEntity(graph, tEvent, "Conclave");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "ConclavePact", tEvent, eventEntityId: eventEntity.Id));

        var tqd = graph.TemporalQuery;

        var snapFromEvent = tqd.At(eventEntity);
        var snapFromTime = tqd.At(tEvent);

        Assert.Equal(snapFromTime.SnapshotTime, snapFromEvent.SnapshotTime);
        Assert.Equal(snapFromTime.EntityCount, snapFromEvent.EntityCount);
        Assert.Equal(snapFromTime.RelationshipCount, snapFromEvent.RelationshipCount);
        Assert.Equal(snapFromTime.Relationships.Keys.OrderBy(k => k), snapFromEvent.Relationships.Keys.OrderBy(k => k));
    }

    [Fact]
    public void Metamorphic_Property5_ExactIntervalReturnsOnlyMatchingFacts()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();

        for (int i = 1; i <= 5; i++)
        {
            var r = Guid.NewGuid();
            executor.Execute(graph, new SorophyRelationshipCreation(r, s, t, $"Rel_{i}", TqdCrucibleTestHelper.CreateTime(_schema, i * 10)));
        }

        var tqd = graph.TemporalQuery;

        for (int i = 1; i <= 5; i++)
        {
            var targetCoord = TqdCrucibleTestHelper.CreateTime(_schema, i * 10);
            var facts = tqd.GetFactsInInterval(targetCoord, targetCoord);

            Assert.Single(facts);
            Assert.Equal(targetCoord, facts[0].At);
        }
    }

    [Fact]
    public void Metamorphic_Property6_RepeatedIdenticalQueriesProduceIdenticalResults()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", TqdCrucibleTestHelper.CreateTime(_schema, 20)));

        var tqd = graph.TemporalQuery;
        var t15 = TqdCrucibleTestHelper.CreateTime(_schema, 15);

        var snap1 = tqd.At(t15);
        var snap2 = tqd.At(t15);

        Assert.Equal(snap1.EntityCount, snap2.EntityCount);
        Assert.Equal(snap1.RelationshipCount, snap2.RelationshipCount);
        Assert.Equal(snap1.SnapshotTime, snap2.SnapshotTime);
    }

    [Fact]
    public void Metamorphic_Property7_AddingUnrelatedGraphStructureDoesNotChangeUnrelatedQuery()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Fixed", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        var tqd = graph.TemporalQuery;
        var t20 = TqdCrucibleTestHelper.CreateTime(_schema, 20);

        var factsBefore = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t20);

        // Add unrelated entities and baseline relationship
        var u1 = Guid.NewGuid(); var u2 = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = u1, Name = "U1" });
        graph.AddEntity(new SorophyEntity { Id = u2, Name = "U2" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = u1, TargetId = u2, Type = "Unrelated" });

        var factsAfter = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t20);

        Assert.Equal(factsBefore.Count, factsAfter.Count);
        Assert.Equal(factsBefore[0].RelationshipId, factsAfter[0].RelationshipId);
    }

    [Fact]
    public void Metamorphic_Property8_HistoricalQueriesDoNotFabricateFromCurrentState()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var baselineRel = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = baselineRel, SourceId = s, TargetId = t, Type = "PureBaseline" });

        var tqd = graph.TemporalQuery;

        // Current state exists
        Assert.True(graph.ContainsRelationship(baselineRel));

        // But history is null
        Assert.Null(tqd.GetRelationshipHistory(baselineRel));
        Assert.Empty(tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), TqdCrucibleTestHelper.CreateTime(_schema, 1000)));
    }

    [Fact]
    public void Metamorphic_Property9_ProvenanceQueriesOnlyReturnMatchingEventEntityId()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);
        var evA = TqdCrucibleTestHelper.CreateEventEntity(graph, t10, "EvA");
        var evB = TqdCrucibleTestHelper.CreateEventEntity(graph, t10, "EvB");

        var rA = Guid.NewGuid();
        var rB = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rA, s, t, "RelA", t10, eventEntityId: evA.Id));
        executor.Execute(graph, new SorophyRelationshipCreation(rB, s, t, "RelB", t10, eventEntityId: evB.Id));

        var tqd = graph.TemporalQuery;

        var factsA = tqd.GetFactsByEvent(evA);
        Assert.All(factsA, f => Assert.Equal(evA.Id, f.EventEntityId));

        var factsB = tqd.GetFactsByEvent(evB);
        Assert.All(factsB, f => Assert.Equal(evB.Id, f.EventEntityId));
    }

    [Fact]
    public void Metamorphic_Property10_PointInTimeQueryStableAcrossLaterMutations()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Stable", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        var tqd = graph.TemporalQuery;
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);

        Assert.True(tqd.EntityExistsAt(s, t10));
        Assert.True(tqd.RelationshipExistsAt(relId, t10));

        // Add many new entities and relationships
        for (int i = 0; i < 10; i++)
        {
            var dummy = Guid.NewGuid();
            graph.AddEntity(new SorophyEntity { Id = dummy, Name = $"Dummy_{i}" });
        }

        Assert.True(tqd.EntityExistsAt(s, t10));
        Assert.True(tqd.RelationshipExistsAt(relId, t10));
    }

    [Fact]
    public void Metamorphic_Property11_IntervalSubsumption()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();

        for (int i = 1; i <= 10; i++)
        {
            var r = Guid.NewGuid();
            executor.Execute(graph, new SorophyRelationshipCreation(r, s, t, $"Sub_{i}", TqdCrucibleTestHelper.CreateTime(_schema, i * 10)));
        }

        var tqd = graph.TemporalQuery;

        // I_narrow = [30, 70], I_wide = [10, 100]
        var narrowFacts = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 30), TqdCrucibleTestHelper.CreateTime(_schema, 70));
        var wideFacts = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 10), TqdCrucibleTestHelper.CreateTime(_schema, 100));

        // Every fact in narrow must appear in wide
        var wideFactIds = new HashSet<Guid>(wideFacts.Select(f => f.RelationshipId));
        Assert.All(narrowFacts, f => Assert.Contains(f.RelationshipId, wideFactIds));
    }

    [Fact]
    public void Metamorphic_Property12_DisjointIntervalPartitioning()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();

        for (int i = 1; i <= 10; i++)
        {
            var r = Guid.NewGuid();
            executor.Execute(graph, new SorophyRelationshipCreation(r, s, t, $"Part_{i}", TqdCrucibleTestHelper.CreateTime(_schema, i * 10)));
        }

        var tqd = graph.TemporalQuery;

        // Partition [10, 100] into [10, 50] and [51, 100]
        var totalFacts = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 10), TqdCrucibleTestHelper.CreateTime(_schema, 100));
        var part1 = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 10), TqdCrucibleTestHelper.CreateTime(_schema, 50));
        var part2 = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 51), TqdCrucibleTestHelper.CreateTime(_schema, 100));

        Assert.Equal(totalFacts.Count, part1.Count + part2.Count);
    }
}

