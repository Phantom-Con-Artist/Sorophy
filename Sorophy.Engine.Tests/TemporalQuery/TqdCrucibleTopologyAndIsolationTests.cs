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
/// Domain C07 (Topology Assault), Domain C08 (Isolation Assault),
/// Domain C09 (Query Repeatability), and Domain C10 (Determinism Assault).
/// </summary>
public sealed class TqdCrucibleTopologyAndIsolationTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    /*
     * =============================================================
     * C07: GRAPH TOPOLOGY ASSAULT
     * =============================================================
     */

    [Fact]
    public void Topology_ChainsCyclesFanInFanOut_PreservesStructureInSnapshot()
    {
        var graph = new SorophyGraph();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        // Linear chain: A -> B -> C -> D
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid(); var d = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = a, Name = "A" });
        graph.AddEntity(new SorophyEntity { Id = b, Name = "B" });
        graph.AddEntity(new SorophyEntity { Id = c, Name = "C" });
        graph.AddEntity(new SorophyEntity { Id = d, Name = "D" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = a, TargetId = b, Type = "Chain" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = b, TargetId = c, Type = "Chain" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = c, TargetId = d, Type = "Chain" });

        // Cycle: X -> Y -> Z -> X
        var x = Guid.NewGuid(); var y = Guid.NewGuid(); var z = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = x, Name = "X" });
        graph.AddEntity(new SorophyEntity { Id = y, Name = "Y" });
        graph.AddEntity(new SorophyEntity { Id = z, Name = "Z" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = x, TargetId = y, Type = "Cycle" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = y, TargetId = z, Type = "Cycle" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = z, TargetId = x, Type = "Cycle" });

        // Fan-in: E1, E2, E3 -> Center
        var center = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = center, Name = "Center" });
        for (int i = 0; i < 3; i++)
        {
            var e = Guid.NewGuid();
            graph.AddEntity(new SorophyEntity { Id = e, Name = $"E{i}" });
            graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e, TargetId = center, Type = "FanIn" });
        }

        // Fan-out: Hub -> F1, F2, F3
        var hub = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = hub, Name = "Hub" });
        for (int i = 0; i < 3; i++)
        {
            var f = Guid.NewGuid();
            graph.AddEntity(new SorophyEntity { Id = f, Name = $"F{i}" });
            graph.AddRelationship(new SorophyRelationship { Id = Guid.NewGuid(), SourceId = hub, TargetId = f, Type = "FanOut" });
        }

        // Isolated entity
        var solo = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = solo, Name = "Solo" });

        var tqd = graph.TemporalQuery;

        // Verify chain
        Assert.Single(tqd.GetOutboundRelationshipsAt(a, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(a, t100));
        Assert.Single(tqd.GetOutboundRelationshipsAt(b, t100));
        Assert.Single(tqd.GetInboundRelationshipsAt(b, t100));

        // Verify cycle
        Assert.Single(tqd.GetOutboundRelationshipsAt(x, t100));
        Assert.Single(tqd.GetInboundRelationshipsAt(x, t100));

        // Verify fan-in
        Assert.Equal(3, tqd.GetInboundRelationshipsAt(center, t100).Count());
        Assert.Empty(tqd.GetOutboundRelationshipsAt(center, t100));

        // Verify fan-out
        Assert.Equal(3, tqd.GetOutboundRelationshipsAt(hub, t100).Count());
        Assert.Empty(tqd.GetInboundRelationshipsAt(hub, t100));

        // Verify isolated entity
        Assert.Empty(tqd.GetOutboundRelationshipsAt(solo, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(solo, t100));
    }

    [Fact]
    public void Topology_EvolutionInOneComponent_DoesNotAffectOtherComponents()
    {
        var graph = new SorophyGraph();
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var c = Guid.NewGuid(); var d = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = a, Name = "A" });
        graph.AddEntity(new SorophyEntity { Id = b, Name = "B" });
        graph.AddEntity(new SorophyEntity { Id = c, Name = "C" });
        graph.AddEntity(new SorophyEntity { Id = d, Name = "D" });

        var rAB = Guid.NewGuid();
        var rCD = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rAB, a, b, "Connected", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        executor.Execute(graph, new SorophyRelationshipCreation(rCD, c, d, "Baseline", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        // Evolve rAB at T=50, rCD remains untouched
        executor.Execute(graph, new SorophyRelationshipTypeChange(rAB, "Allied", TqdCrucibleTestHelper.CreateTime(_schema, 50)));

        var tqd = graph.TemporalQuery;

        var snap25 = tqd.GetRelationshipAt(rCD, TqdCrucibleTestHelper.CreateTime(_schema, 25));
        var snap75 = tqd.GetRelationshipAt(rCD, TqdCrucibleTestHelper.CreateTime(_schema, 75));

        Assert.NotNull(snap25);
        Assert.NotNull(snap75);
        Assert.Equal("Baseline", snap25.Type);
        Assert.Equal("Baseline", snap75.Type);
    }

    /*
     * =============================================================
     * C08: READ-ONLY / ISOLATION ASSAULT
     * =============================================================
     */

    [Fact]
    public void Isolation_MutatingCanonicalGraph_DoesNotAffectPreviousSnapshot()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "Link"
        };
        rel.Properties["OriginalKey"] = new SorophyProperty
        {
            Name = "OriginalKey",
            Value = new SorophyValue(SorophyValueType.String, "BeforeMutation")
        };
        graph.AddRelationship(rel);

        var tqd = graph.TemporalQuery;

        // Materialize snapshot before canonical mutation
        var snapshotBefore = tqd.At(t100);
        Assert.Equal(2, snapshotBefore.EntityCount);
        Assert.Equal(1, snapshotBefore.RelationshipCount);

        // Mutate canonical graph
        var newEntityId = Guid.NewGuid();
        graph.AddEntity(new SorophyEntity { Id = newEntityId, Name = "Intruder" });
        rel.Properties["OriginalKey"].Value = new SorophyValue(SorophyValueType.String, "AfterMutation");
        rel.Type = "MutatedType";

        // Previous snapshot must be 100% unaffected
        Assert.Equal(2, snapshotBefore.EntityCount);
        Assert.Equal(1, snapshotBefore.RelationshipCount);
        Assert.False(snapshotBefore.ContainsEntity(newEntityId));
        var snapRel = snapshotBefore.GetRelationship(relId);
        Assert.NotNull(snapRel);
        Assert.Equal("Link", snapRel.Type);
        Assert.Equal("BeforeMutation", snapRel.Properties["OriginalKey"].Value.Value);
    }

    [Fact]
    public void Isolation_MutatingSnapshot_DoesNotAffectCanonicalGraph()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = s,
            TargetId = t,
            Type = "ProtectedLink"
        };
        rel.Properties["Secret"] = new SorophyProperty
        {
            Name = "Secret",
            Value = new SorophyValue(SorophyValueType.Integer, 42L)
        };
        graph.AddRelationship(rel);

        var tqd = graph.TemporalQuery;
        var snapshot = tqd.At(t100);
        var snapRel = snapshot.GetRelationship(relId);
        Assert.NotNull(snapRel);

        // Attempt mutation on cloned snapshot property
        snapRel.Properties["Secret"].Value = new SorophyValue(SorophyValueType.Integer, 9999L);

        // Canonical graph remains untouched
        Assert.Equal(42L, rel.Properties["Secret"].Value.Value);
    }

    /*
     * =============================================================
     * C09: QUERY REPEATABILITY
     * =============================================================
     */

    [Fact]
    public void Repeatability_IdenticalQueries_100Iterations_ProduceIdenticalResults()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var eventEntity = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "RepeatableEvent");
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(r1, s, t, "R1", t100, eventEntityId: eventEntity.Id));
        executor.Execute(graph, new SorophyRelationshipCreation(r2, s, t, "R2", t200));

        var tqd = graph.TemporalQuery;

        var baselineFacts = tqd.GetFactsInInterval(t100, t200);
        var baselineModified = tqd.GetModifiedRelationshipIds(t100, t200);
        var baselineHistory = tqd.GetRelationshipHistory(r1);
        var baselineEventFacts = tqd.GetFactsByEvent(eventEntity.Id);

        for (int i = 0; i < 100; i++)
        {
            var repeatFacts = tqd.GetFactsInInterval(t100, t200);
            var repeatModified = tqd.GetModifiedRelationshipIds(t100, t200);
            var repeatHistory = tqd.GetRelationshipHistory(r1);
            var repeatEventFacts = tqd.GetFactsByEvent(eventEntity.Id);

            Assert.Equal(baselineFacts.Count, repeatFacts.Count);
            Assert.Equal(baselineModified.Count, repeatModified.Count);
            Assert.Equal(baselineHistory!.Facts.Count, repeatHistory!.Facts.Count);
            Assert.Equal(baselineEventFacts.Count, repeatEventFacts.Count);
        }
    }

    /*
     * =============================================================
     * C10: DETERMINISM ASSAULT
     * =============================================================
     */

    [Fact]
    public void Determinism_SortingInvariant_At_RelationshipId_FactIndex()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var relA = Guid.NewGuid();
        var relB = Guid.NewGuid();
        // Ensure rel1 < rel2 for GUID comparison
        var (rel1, rel2) = relA.CompareTo(relB) < 0 ? (relA, relB) : (relB, relA);

        var executor = new SorophyRelationshipEvolutionExecutor();
        // Add rel2 first, then rel1 (reversed from GUID order)
        executor.Execute(graph, new SorophyRelationshipCreation(rel2, s, t, "Rel2_Fact0", t100));
        executor.Execute(graph, new SorophyRelationshipCreation(rel1, s, t, "Rel1_Fact0", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(rel1, "Rel1_Fact1", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(rel2, "Rel2_Fact1", t200));

        var tqd = graph.TemporalQuery;
        var facts = tqd.GetFactsInInterval(t100, t200);

        Assert.Equal(4, facts.Count);

        // Fact 0: at T=100, rel1 (rel1 < rel2), FactIndex 0
        Assert.Equal(t100, facts[0].At);
        Assert.Equal(rel1, facts[0].RelationshipId);
        Assert.Equal("Rel1_Fact0", facts[0].Type);

        // Fact 1: at T=100, rel1, FactIndex 1
        Assert.Equal(t100, facts[1].At);
        Assert.Equal(rel1, facts[1].RelationshipId);
        Assert.Equal("Rel1_Fact0", facts[1].Type); // captures pre-mutation state

        // Fact 2: at T=100, rel2, FactIndex 0
        Assert.Equal(t100, facts[2].At);
        Assert.Equal(rel2, facts[2].RelationshipId);
        Assert.Equal("Rel2_Fact0", facts[2].Type);

        // Fact 3: at T=200, rel2
        Assert.Equal(t200, facts[3].At);
        Assert.Equal(rel2, facts[3].RelationshipId);
    }
}

