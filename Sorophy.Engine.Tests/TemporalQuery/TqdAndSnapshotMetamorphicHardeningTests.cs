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
using Sorophy.Engine.Diff;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalQuery;

public sealed class TqdAndSnapshotMetamorphicHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Metamorphic Timeline")
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

    // Property 1: TQD.At(T) == Graph.CreateSnapshot(T)
    [Fact]
    public void Metamorphic_1_TqdAt_Equals_GraphCreateSnapshot()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = new SorophyEntity { Name = "Node" };
        graph.AddEntity(e);

        var coord = CreateTime(schema, "50");
        var snapGraph = graph.CreateSnapshot(coord);
        var snapTqd = graph.TemporalQuery.At(coord);

        Assert.Equal(snapGraph.SnapshotTime, snapTqd.SnapshotTime);
        Assert.Equal(snapGraph.EntityCount, snapTqd.EntityCount);
        Assert.Equal(snapGraph.RelationshipCount, snapTqd.RelationshipCount);
        Assert.False(SorophyGraphDiff.Compare(snapGraph, snapTqd).HasChanges);
    }

    // Property 2: Repeating the same query produces equivalent results
    [Fact]
    public void Metamorphic_2_RepeatQuery_ProducesIdenticalResults()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var coord = CreateTime(schema, "100");

        var snap1 = graph.TemporalQuery.At(coord);
        var snap2 = graph.TemporalQuery.At(coord);

        Assert.False(SorophyGraphDiff.Compare(snap1, snap2).HasChanges);
    }

    // Property 3: Deep isolation: mutating graph after snapshot does not alter snapshot
    [Fact]
    public void Metamorphic_3_SnapshotIsolation_MutatingGraphLeavesSnapshotUnchanged()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "OriginalName" };
        entity.Tags.Add("Tag1");
        entity.Documents["doc.md"] = new SorophyEntityDocument("doc.md", "OriginalContent");
        entity.Properties["Prop"] = new SorophyProperty
        {
            Name = "Prop",
            Value = new SorophyValue(SorophyValueType.List, new List<object?> { "item1" })
        };
        graph.AddEntity(entity);

        var snapshot = graph.CreateSnapshot(CreateTime(schema, "10"));

        // Mutate source entity
        entity.Name = "MutatedName";
        entity.Tags.Add("Tag2");
        entity.Documents["doc.md"] = new SorophyEntityDocument("doc.md", "MutatedContent");
        ((List<object?>)entity.Properties["Prop"].Value.Value!).Add("item2");

        // Verify snapshot state remains untouched
        var snapEntity = snapshot.GetEntity(entity.Id)!;
        Assert.Equal("OriginalName", snapEntity.Name);
        Assert.Single(snapEntity.Tags);
        Assert.Equal("OriginalContent", snapEntity.Documents["doc.md"].Content);
        Assert.Single((List<object?>)snapEntity.Properties["Prop"].Value.Value!);
    }

    // Property 4: Unrelated graph mutations do not affect query results on other entities
    [Fact]
    public void Metamorphic_4_UnrelatedMutations_DoNotChangeIsolatedEntityLookups()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var targetEntity = new SorophyEntity { Name = "IsolatedTarget" };
        graph.AddEntity(targetEntity);

        var coord = CreateTime(schema, "10");
        var lookupBefore = graph.TemporalQuery.GetEntityAt(targetEntity.Id, coord);

        // Add lots of unrelated entities and relationships
        for (int i = 0; i < 50; i++)
        {
            graph.AddEntity(new SorophyEntity { Name = $"Noise_{i}" });
        }

        var lookupAfter = graph.TemporalQuery.GetEntityAt(targetEntity.Id, coord);

        Assert.NotNull(lookupBefore);
        Assert.NotNull(lookupAfter);
        Assert.Equal(lookupBefore.Name, lookupAfter.Name);
    }

    // Property 5: Event overload matches direct coordinate overload
    [Fact]
    public void Metamorphic_5_EventOverload_MatchesDirectCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var coord = CreateTime(schema, "75");

        var eventEntity = new SorophyEntity
        {
            Type = "Event",
            OccurredAt = coord
        };
        graph.AddEntity(eventEntity);

        var snapEvent = graph.TemporalQuery.At(eventEntity);
        var snapTime = graph.TemporalQuery.At(coord);

        Assert.False(SorophyGraphDiff.Compare(snapEvent, snapTime).HasChanges);
    }

    // Property 6: [T, T] interval contains facts exactly at T
    [Fact]
    public void Metamorphic_6_PointInterval_ContainsFactsExactlyAtCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s.Id, t.Id, "E1", CreateTime(schema, "10")));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "E2", CreateTime(schema, "20")));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "E3", CreateTime(schema, "30")));

        var factsAt20 = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "20"), CreateTime(schema, "20"));

        Assert.Single(factsAt20);
        Assert.Equal("E1", factsAt20[0].Type);
        Assert.Equal(CreateTime(schema, "20"), factsAt20[0].At);
    }

    // Property 7: History queries never fabricate facts
    [Fact]
    public void Metamorphic_7_HistoryQueries_NeverFabricateFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var rel = new SorophyRelationship { SourceId = s.Id, TargetId = t.Id, Type = "Direct" };
        graph.AddRelationship(rel);

        var facts = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "0"), CreateTime(schema, "100"));
        Assert.Empty(facts);
    }

    // Property 8: Provenance queries return exactly matching EventEntityId facts
    [Fact]
    public void Metamorphic_8_ProvenanceQueries_ReturnOnlyMatchingFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        var e1 = new SorophyEntity { Type = "Event", OccurredAt = CreateTime(schema, "10") };
        var e2 = new SorophyEntity { Type = "Event", OccurredAt = CreateTime(schema, "20") };
        graph.AddEntity(s);
        graph.AddEntity(t);
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s.Id, t.Id, "Step1", CreateTime(schema, "10"), eventEntityId: e1.Id));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", CreateTime(schema, "20"), eventEntityId: e2.Id));

        var e1Facts = graph.TemporalQuery.GetFactsByEvent(e1.Id);
        Assert.Single(e1Facts);
        Assert.Equal("Step1", e1Facts[0].Type);
        Assert.Equal(e1.Id, e1Facts[0].EventEntityId);
    }

    // Property 9: Interval subsumption [T1, T2] subset [T0, T3]
    [Fact]
    public void Metamorphic_9_IntervalSubsumption_SubintervalIsSubsetOfSuperinterval()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        for (int i = 1; i <= 10; i++)
        {
            if (i == 1)
                executor.Execute(graph, new SorophyRelationshipCreation(relId, s.Id, t.Id, $"T{i}", CreateTime(schema, $"{i * 10}")));
            else
                executor.Execute(graph, new SorophyRelationshipTypeChange(relId, $"T{i}", CreateTime(schema, $"{i * 10}")));
        }

        // Subinterval [30, 60]
        var subFacts = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "30"), CreateTime(schema, "60"));

        // Superinterval [10, 100]
        var superFacts = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "10"), CreateTime(schema, "100"));

        Assert.Equal(4, subFacts.Count);
        Assert.Equal(10, superFacts.Count);
        Assert.All(subFacts, f => Assert.Contains(f, superFacts));
    }

    // Property 10: Disjoint interval partition
    [Fact]
    public void Metamorphic_10_DisjointIntervalPartition_CombinesToCompleteSet()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s.Id, t.Id, "E1", CreateTime(schema, "10")));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "E2", CreateTime(schema, "20")));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "E3", CreateTime(schema, "30")));

        var p1 = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "10"), CreateTime(schema, "19"));
        var p2 = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "20"), CreateTime(schema, "30"));
        var full = graph.TemporalQuery.GetFactsInInterval(CreateTime(schema, "10"), CreateTime(schema, "30"));

        Assert.Equal(full.Count, p1.Count + p2.Count);
    }

    // Property 11: Reverted state A -> B -> A produces identical snapshots
    [Fact]
    public void Metamorphic_11_RevertedState_ProducesIdenticalSnapshots()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "Baseline" };
        graph.AddEntity(entity);

        var snapT1 = graph.CreateSnapshot(CreateTime(schema, "10"));

        entity.Name = "Mutated";
        entity.Name = "Baseline";

        var snapT2 = graph.CreateSnapshot(CreateTime(schema, "20"));

        Assert.False(SorophyGraphDiff.Compare(snapT1, snapT2).HasChanges);
    }

    // Property 12: Identical snapshots produce empty Graph Diff
    [Fact]
    public void Metamorphic_12_IdenticalSnapshots_ProduceEmptyGraphDiff()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity { Name = "S" };
        var t = new SorophyEntity { Name = "T" };
        graph.AddEntity(s);
        graph.AddEntity(t);

        var snapA = graph.CreateSnapshot(CreateTime(schema, "10"));
        var snapB = graph.CreateSnapshot(CreateTime(schema, "10"));

        var diff = SorophyGraphDiff.Compare(snapA, snapB);
        Assert.False(diff.HasChanges);
        Assert.Equal(0, diff.TotalChanges);
    }
}
