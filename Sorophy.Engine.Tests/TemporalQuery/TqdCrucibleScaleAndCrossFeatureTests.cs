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
/// Domain C16 (Large History / Endurance) and Domain C17 (Cross-Feature Interaction).
/// </summary>
public sealed class TqdCrucibleScaleAndCrossFeatureTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    /*
     * =============================================================
     * C16: LARGE HISTORY / ENDURANCE
     * =============================================================
     */

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Scale_LargeHistories_EnduranceAndDeterministicSorting(int totalFacts)
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var executor = new SorophyRelationshipEvolutionExecutor();

        // Distribute facts across relationships (10 facts per relationship)
        int relCount = Math.Max(1, totalFacts / 10);
        int factsPerRel = totalFacts / relCount;
        var relIds = new List<Guid>();

        for (int r = 0; r < relCount; r++)
        {
            var relId = Guid.NewGuid();
            relIds.Add(relId);
            executor.Execute(graph, new SorophyRelationshipCreation(
                relId, s, t, $"Rel_{r}", TqdCrucibleTestHelper.CreateTime(_schema, 0)));

            for (int f = 1; f < factsPerRel; f++)
            {
                executor.Execute(graph, new SorophyRelationshipPropertyModification(
                    relId, TqdCrucibleTestHelper.CreateTime(_schema, f * 10),
                    propertiesToSet: new Dictionary<string, SorophyProperty>
                    {
                        ["Iteration"] = new SorophyProperty
                        {
                            Name = "Iteration",
                            Value = new SorophyValue(SorophyValueType.Integer, (long)f)
                        }
                    }));
            }
        }

        var tqd = graph.TemporalQuery;

        // Query entire interval
        var allFacts = tqd.GetFactsInInterval(
            TqdCrucibleTestHelper.CreateTime(_schema, 0),
            TqdCrucibleTestHelper.CreateTime(_schema, factsPerRel * 10));

        Assert.Equal(relCount * factsPerRel, allFacts.Count);

        // Verify strict presentation ordering: (At, RelationshipId, FactIndex)
        for (int i = 0; i < allFacts.Count - 1; i++)
        {
            var f1 = allFacts[i];
            var f2 = allFacts[i + 1];

            int timeComp = SorophyTime.Compare(f1.At, f2.At);
            Assert.True(timeComp <= 0);

            if (timeComp == 0)
            {
                int relComp = f1.RelationshipId.CompareTo(f2.RelationshipId);
                Assert.True(relComp <= 0);
            }
        }

        // Point-in-time query in the middle
        var midTime = TqdCrucibleTestHelper.CreateTime(_schema, (factsPerRel * 10) / 2);
        var snapshot = tqd.At(midTime);
        Assert.Equal(relCount, snapshot.RelationshipCount);
    }

    /*
     * =============================================================
     * C17: CROSS-FEATURE INTERACTION
     * =============================================================
     */

    [Fact]
    public void CrossFeature_ProvenanceAndSameTimeAndTerminationAndRetiredHistory()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var triggerEvent = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Cataclysm");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        // 3 operations co-temporal at T=100 anchored by same Event: Creation, Modification, Termination
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "ShortLived", t100, eventEntityId: triggerEvent.Id));
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, t100,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Doom"] = new SorophyProperty { Name = "Doom", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
            },
            eventEntityId: triggerEvent.Id));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t100, eventEntityId: triggerEvent.Id));

        var tqd = graph.TemporalQuery;

        // Post-transition semantics: terminated at T=100 => absent
        Assert.False(tqd.RelationshipExistsAt(relId, t100));

        // Relationship is retired
        Assert.True(graph.IsRelationshipIdRetired(relId));

        // Provenance returns all 3 facts for this retired relationship
        var factsByEvent = tqd.GetFactsByEvent(triggerEvent);
        Assert.Equal(3, factsByEvent.Count);
        Assert.All(factsByEvent, f =>
        {
            Assert.Equal(relId, f.RelationshipId);
            Assert.Equal(triggerEvent.Id, f.EventEntityId);
            Assert.Equal(t100, f.At);
        });

        var evolvedRels = tqd.GetRelationshipsEvolvedByEvent(triggerEvent);
        Assert.Single(evolvedRels);
        Assert.Contains(relId, evolvedRels);
    }

    [Fact]
    public void CrossFeature_BaselineAndEvolvedAndFutureEvolution_MixedTraversal()
    {
        var graph = new SorophyGraph();
        var node = Guid.NewGuid();
        var target1 = Guid.NewGuid();
        var target2 = Guid.NewGuid();
        var target3 = Guid.NewGuid();
        var target4 = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = node, Name = "Node" });
        graph.AddEntity(new SorophyEntity { Id = target1, Name = "Target1" });
        graph.AddEntity(new SorophyEntity { Id = target2, Name = "Target2" });
        graph.AddEntity(new SorophyEntity { Id = target3, Name = "Target3" });
        graph.AddEntity(new SorophyEntity { Id = target4, Name = "Target4" });

        // 2 Baseline relationships
        var rBase1 = Guid.NewGuid();
        var rBase2 = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = rBase1, SourceId = node, TargetId = target1, Type = "Base1" });
        graph.AddRelationship(new SorophyRelationship { Id = rBase2, SourceId = node, TargetId = target2, Type = "Base2" });

        // 1 Evolved creation at T=10
        var rEvolvedPast = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            rEvolvedPast, node, target3, "PastEvolved", TqdCrucibleTestHelper.CreateTime(_schema, 10)));

        // 1 Evolved creation in future at T=50
        var rEvolvedFuture = Guid.NewGuid();
        executor.Execute(graph, new SorophyRelationshipCreation(
            rEvolvedFuture, node, target4, "FutureEvolved", TqdCrucibleTestHelper.CreateTime(_schema, 50)));

        var tqd = graph.TemporalQuery;
        var t25 = TqdCrucibleTestHelper.CreateTime(_schema, 25);

        // At T=25: should see rBase1, rBase2, and rEvolvedPast; rEvolvedFuture must be excluded
        var outboundAt25 = tqd.GetOutboundRelationshipsAt(node, t25).ToList();
        Assert.Equal(3, outboundAt25.Count);
        Assert.Contains(outboundAt25, r => r.Id == rBase1);
        Assert.Contains(outboundAt25, r => r.Id == rBase2);
        Assert.Contains(outboundAt25, r => r.Id == rEvolvedPast);
        Assert.DoesNotContain(outboundAt25, r => r.Id == rEvolvedFuture);
    }

    [Fact]
    public void CrossFeature_MultipleCoTemporalEvents_MultipleRelationships_RepeatedQuerying()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        var ev1 = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Ev1");
        var ev2 = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Ev2");
        var ev3 = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Ev3");

        var executor = new SorophyRelationshipEvolutionExecutor();
        var rels1 = new List<Guid>();
        var rels2 = new List<Guid>();
        var rels3 = new List<Guid>();

        for (int i = 0; i < 3; i++)
        {
            var r1 = Guid.NewGuid(); rels1.Add(r1);
            executor.Execute(graph, new SorophyRelationshipCreation(r1, s, t, $"E1_R{i}", t100, eventEntityId: ev1.Id));

            var r2 = Guid.NewGuid(); rels2.Add(r2);
            executor.Execute(graph, new SorophyRelationshipCreation(r2, s, t, $"E2_R{i}", t100, eventEntityId: ev2.Id));

            var r3 = Guid.NewGuid(); rels3.Add(r3);
            executor.Execute(graph, new SorophyRelationshipCreation(r3, s, t, $"E3_R{i}", t100, eventEntityId: ev3.Id));
        }

        var tqd = graph.TemporalQuery;

        // Snapshots from each co-temporal event are structurally equivalent
        var snap1 = tqd.At(ev1);
        var snap2 = tqd.At(ev2);
        var snap3 = tqd.At(ev3);
        var snapT = tqd.At(t100);

        Assert.Equal(snapT.RelationshipCount, snap1.RelationshipCount);
        Assert.Equal(snap1.RelationshipCount, snap2.RelationshipCount);
        Assert.Equal(snap2.RelationshipCount, snap3.RelationshipCount);

        // Provenance partitions facts cleanly
        var f1 = tqd.GetFactsByEvent(ev1);
        var f2 = tqd.GetFactsByEvent(ev2);
        var f3 = tqd.GetFactsByEvent(ev3);

        Assert.Equal(3, f1.Count);
        Assert.Equal(3, f2.Count);
        Assert.Equal(3, f3.Count);

        Assert.All(f1, f => Assert.Equal(ev1.Id, f.EventEntityId));
        Assert.All(f2, f => Assert.Equal(ev2.Id, f.EventEntityId));
        Assert.All(f3, f => Assert.Equal(ev3.Id, f.EventEntityId));

        var set1 = new HashSet<Guid>(tqd.GetRelationshipsEvolvedByEvent(ev1));
        var set2 = new HashSet<Guid>(tqd.GetRelationshipsEvolvedByEvent(ev2));
        var set3 = new HashSet<Guid>(tqd.GetRelationshipsEvolvedByEvent(ev3));

        // Sets must be mutually disjoint
        Assert.Empty(set1.Intersect(set2));
        Assert.Empty(set2.Intersect(set3));
        Assert.Empty(set1.Intersect(set3));
    }
}

