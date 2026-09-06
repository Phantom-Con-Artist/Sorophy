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
/// Domain C02 (Temporal Reconstruction Assault) and Domain C03 (Same-Time / Co-Temporal Chaos).
/// </summary>
public sealed class TqdCrucibleReconstructionTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    /*
     * =============================================================
     * C02: TEMPORAL RECONSTRUCTION ASSAULT
     * =============================================================
     */

    [Fact]
    public void Reconstruction_MultiStageLifecycle_QueryEveryCoordinate()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        // 1. Creation at T=10
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s, t, "Alliance", TqdCrucibleTestHelper.CreateTime(_schema, 10),
            new Dictionary<string, SorophyProperty>
            {
                ["Score"] = new SorophyProperty { Name = "Score", Value = new SorophyValue(SorophyValueType.Integer, 100L) },
                ["Status"] = new SorophyProperty { Name = "Status", Value = new SorophyValue(SorophyValueType.String, "Pending") }
            },
            validFrom: TqdCrucibleTestHelper.CreateTime(_schema, 10),
            validTill: TqdCrucibleTestHelper.CreateTime(_schema, 100)));

        // 2. PropMod at T=20
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 20),
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Status"] = new SorophyProperty { Name = "Status", Value = new SorophyValue(SorophyValueType.String, "Active") }
            }));

        // 3. PropMod at T=30
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 30),
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Score"] = new SorophyProperty { Name = "Score", Value = new SorophyValue(SorophyValueType.Integer, 250L) }
            }));

        // 4. TypeChange at T=40
        executor.Execute(graph, new SorophyRelationshipTypeChange(
            relId, "Federation", TqdCrucibleTestHelper.CreateTime(_schema, 40)));

        // 5. ValidityChange at T=50
        executor.Execute(graph, new SorophyRelationshipValidityChange(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 50),
            newValidFrom: TqdCrucibleTestHelper.CreateTime(_schema, 15),
            newValidTill: TqdCrucibleTestHelper.CreateTime(_schema, 200)));

        // 6. PropMod at T=60
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 60),
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Score"] = new SorophyProperty { Name = "Score", Value = new SorophyValue(SorophyValueType.Integer, 500L) }
            }));

        // 7. TypeChange at T=70
        executor.Execute(graph, new SorophyRelationshipTypeChange(
            relId, "Empire", TqdCrucibleTestHelper.CreateTime(_schema, 70)));

        // 8. Termination at T=80
        executor.Execute(graph, new SorophyRelationshipTermination(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 80)));

        var tqd = graph.TemporalQuery;

        // Query at every coordinate:
        // T=5 (before creation)
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 5)));

        // T=10 (creation)
        var s10 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 10));
        Assert.NotNull(s10);
        Assert.Equal("Alliance", s10.Type);
        Assert.Equal(100L, s10.Properties["Score"].Value.Value);
        Assert.Equal("Pending", s10.Properties["Status"].Value.Value);

        // T=15 (between creation and first mod)
        var s15 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 15));
        Assert.NotNull(s15);
        Assert.Equal("Alliance", s15.Type);
        Assert.Equal(100L, s15.Properties["Score"].Value.Value);
        Assert.Equal("Pending", s15.Properties["Status"].Value.Value);

        // T=20 (first mod: Status = Active)
        var s20 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 20));
        Assert.NotNull(s20);
        Assert.Equal("Active", s20.Properties["Status"].Value.Value);
        Assert.Equal(100L, s20.Properties["Score"].Value.Value);

        // T=30 (second mod: Score = 250)
        var s30 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 30));
        Assert.NotNull(s30);
        Assert.Equal(250L, s30.Properties["Score"].Value.Value);

        // T=40 (type change: Federation)
        var s40 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 40));
        Assert.NotNull(s40);
        Assert.Equal("Federation", s40.Type);
        Assert.Equal(250L, s40.Properties["Score"].Value.Value);

        // T=50 (validity change: 15 to 200)
        var s50 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 50));
        Assert.NotNull(s50);
        Assert.Equal(TqdCrucibleTestHelper.CreateTime(_schema, 15), s50.ValidFrom);
        // Historical state before T=60 mutation is captured with ValidTill = 60
        Assert.Equal(TqdCrucibleTestHelper.CreateTime(_schema, 60), s50.ValidTill);

        // T=60 (third mod: Score = 500)
        var s60 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 60));
        Assert.NotNull(s60);
        Assert.Equal(500L, s60.Properties["Score"].Value.Value);

        // T=70 (second type change: Empire)
        var s70 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 70));
        Assert.NotNull(s70);
        Assert.Equal("Empire", s70.Type);

        // T=79 (immediately before termination)
        var s79 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 79));
        Assert.NotNull(s79);
        Assert.Equal("Empire", s79.Type);
        Assert.Equal(500L, s79.Properties["Score"].Value.Value);

        // T=80 (at termination)
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 80)));

        // T=90 (after termination)
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 90)));
    }

    [Fact]
    public void Reconstruction_DeepHistory_20SequentialModifications()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s, t, "Chain", TqdCrucibleTestHelper.CreateTime(_schema, 0),
            new Dictionary<string, SorophyProperty>
            {
                ["Index"] = new SorophyProperty { Name = "Index", Value = new SorophyValue(SorophyValueType.Integer, 0L) }
            }));

        for (long i = 1; i <= 20; i++)
        {
            executor.Execute(graph, new SorophyRelationshipPropertyModification(
                relId, TqdCrucibleTestHelper.CreateTime(_schema, i * 10),
                propertiesToSet: new Dictionary<string, SorophyProperty>
                {
                    ["Index"] = new SorophyProperty { Name = "Index", Value = new SorophyValue(SorophyValueType.Integer, i) }
                }));
        }

        var tqd = graph.TemporalQuery;

        // Verify intermediate coordinates T = 5, 15, 25, ... 195
        for (long i = 0; i < 20; i++)
        {
            var queryTime = TqdCrucibleTestHelper.CreateTime(_schema, i * 10 + 5);
            var snap = tqd.GetRelationshipAt(relId, queryTime);
            Assert.NotNull(snap);
            Assert.Equal(i, snap.Properties["Index"].Value.Value);
        }

        // Final coordinate T = 205
        var finalSnap = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 205));
        Assert.NotNull(finalSnap);
        Assert.Equal(20L, finalSnap.Properties["Index"].Value.Value);
    }

    /*
     * =============================================================
     * C03: SAME-TIME / CO-TEMPORAL CHAOS
     * =============================================================
     */

    [Fact]
    public void CoTemporal_TwoFactsAtSameCoordinate_ReturnedDeterministically()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(r1, s, t, "Rel1", t100));
        executor.Execute(graph, new SorophyRelationshipCreation(r2, s, t, "Rel2", t100));

        var tqd = graph.TemporalQuery;
        var facts = tqd.GetFactsInInterval(t100, t100);

        Assert.Equal(2, facts.Count);
        Assert.Equal(t100, facts[0].At);
        Assert.Equal(t100, facts[1].At);
    }

    [Fact]
    public void CoTemporal_TenFactsAtSameCoordinate_MixedOperations()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var executor = new SorophyRelationshipEvolutionExecutor();

        for (int i = 0; i < 10; i++)
        {
            var relId = Guid.NewGuid();
            executor.Execute(graph, new SorophyRelationshipCreation(
                relId, s, t, $"Type_{i}", t100,
                new Dictionary<string, SorophyProperty>
                {
                    ["Tag"] = new SorophyProperty { Name = "Tag", Value = new SorophyValue(SorophyValueType.Integer, (long)i) }
                }));
        }

        var tqd = graph.TemporalQuery;
        var facts = tqd.GetFactsInInterval(t100, t100);

        Assert.Equal(10, facts.Count);
        Assert.All(facts, f => Assert.Equal(t100, f.At));

        // Deterministic presentation: repeated calls yield exact same sequence
        for (int run = 0; run < 5; run++)
        {
            var repeat = tqd.GetFactsInInterval(t100, t100);
            for (int j = 0; j < 10; j++)
            {
                Assert.Equal(facts[j].RelationshipId, repeat[j].RelationshipId);
            }
        }
    }

    [Fact]
    public void CoTemporal_FiftyFactsAtSameCoordinate_ScaleAndIntegrity()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t50 = TqdCrucibleTestHelper.CreateTime(_schema, 50);
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relIds = new List<Guid>(50);

        for (int i = 0; i < 50; i++)
        {
            var relId = Guid.NewGuid();
            relIds.Add(relId);
            executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "CoTemporal50", t50));
        }

        var tqd = graph.TemporalQuery;
        var facts = tqd.GetFactsInInterval(t50, t50);

        Assert.Equal(50, facts.Count);
        Assert.All(facts, f => Assert.Equal(t50, f.At));

        var modifiedIds = tqd.GetModifiedRelationshipIds(t50, t50);
        Assert.Equal(50, modifiedIds.Count);
        Assert.All(relIds, id => Assert.Contains(id, modifiedIds));
    }

    [Fact]
    public void CoTemporal_OneHundredFactsAtSameCoordinate_DoubleFactPerRelationship()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var tCoord = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var executor = new SorophyRelationshipEvolutionExecutor();

        // 50 relationships, each with 2 facts at T=100 (creation + type change)
        for (int i = 0; i < 50; i++)
        {
            var relId = Guid.NewGuid();
            executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Initial", tCoord));
            executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Mutated", tCoord));
        }

        var tqd = graph.TemporalQuery;
        var facts = tqd.GetFactsInInterval(tCoord, tCoord);

        Assert.Equal(100, facts.Count);
        Assert.All(facts, f => Assert.Equal(tCoord, f.At));

        // Deterministic presentation check
        var secondQuery = tqd.GetFactsInInterval(tCoord, tCoord);
        Assert.Equal(100, secondQuery.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(facts[i].RelationshipId, secondQuery[i].RelationshipId);
            Assert.Equal(facts[i].Type, secondQuery[i].Type);
        }
    }

    [Fact]
    public void CoTemporal_SameCoordinate_CreationAndTermination_Semantics()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Ephemeral", t100));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t100));

        var tqd = graph.TemporalQuery;

        // Post-transition semantics at T=100: terminated => absent
        Assert.False(tqd.RelationshipExistsAt(relId, t100));
        Assert.Null(tqd.GetRelationshipAt(relId, t100));

        // Prior to T=100: absent
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 99)));

        // History contains both facts
        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(2, history.Facts.Count);
        Assert.Equal(t100, history.Facts[0].At);
        Assert.Equal(t100, history.Facts[1].At);
    }
}
