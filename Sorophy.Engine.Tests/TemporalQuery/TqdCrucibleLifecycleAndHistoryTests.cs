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
/// Domain C04 (Relationship Lifecycle Assault) and Domain C05 (Baseline vs Historical State).
/// </summary>
public sealed class TqdCrucibleLifecycleAndHistoryTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    /*
     * =============================================================
     * C04: RELATIONSHIP LIFECYCLE ASSAULT
     * =============================================================
     */

    [Fact]
    public void Lifecycle_CreateModifyTerminate_FullLifecycleReplay()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);
        var t20 = TqdCrucibleTestHelper.CreateTime(_schema, 20);
        var t30 = TqdCrucibleTestHelper.CreateTime(_schema, 30);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s, t, "Initial", t10,
            new Dictionary<string, SorophyProperty>
            {
                ["Val"] = new SorophyProperty { Name = "Val", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
            }));

        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, t20,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Val"] = new SorophyProperty { Name = "Val", Value = new SorophyValue(SorophyValueType.Integer, 2L) }
            }));

        executor.Execute(graph, new SorophyRelationshipTermination(relId, t30));

        var tqd = graph.TemporalQuery;

        // T=5: absent
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 5)));

        // T=10: active, Val = 1
        var s10 = tqd.GetRelationshipAt(relId, t10);
        Assert.NotNull(s10);
        Assert.Equal(1L, s10.Properties["Val"].Value.Value);

        // T=20: active, Val = 2
        var s20 = tqd.GetRelationshipAt(relId, t20);
        Assert.NotNull(s20);
        Assert.Equal(2L, s20.Properties["Val"].Value.Value);

        // T=29: active, Val = 2
        var s29 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 29));
        Assert.NotNull(s29);
        Assert.Equal(2L, s29.Properties["Val"].Value.Value);

        // T=30: terminated (absent)
        Assert.False(tqd.RelationshipExistsAt(relId, t30));

        // T=100: still absent
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 100)));

        // Retired status
        Assert.True(graph.IsRelationshipIdRetired(relId));

        // History intact with 3 facts
        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(3, history.Facts.Count);
    }

    [Fact]
    public void Lifecycle_CreateTypeChangeValidityChangeTerminate()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);
        var t20 = TqdCrucibleTestHelper.CreateTime(_schema, 20);
        var t30 = TqdCrucibleTestHelper.CreateTime(_schema, 30);
        var t40 = TqdCrucibleTestHelper.CreateTime(_schema, 40);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", t10));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", t20));
        executor.Execute(graph, new SorophyRelationshipValidityChange(
            relId, t30, TqdCrucibleTestHelper.CreateTime(_schema, 10), TqdCrucibleTestHelper.CreateTime(_schema, 100)));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t40));

        var tqd = graph.TemporalQuery;

        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(4, history.Facts.Count);

        // Facts in interval [0, 50] returns all 4 facts
        var allFacts = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), TqdCrucibleTestHelper.CreateTime(_schema, 50));
        Assert.Equal(4, allFacts.Count);
        Assert.Equal(t10, allFacts[0].At);
        Assert.Equal(t20, allFacts[1].At);
        Assert.Equal(t30, allFacts[2].At);
        Assert.Equal(t40, allFacts[3].At);
    }

    [Fact]
    public void Lifecycle_ConsecutiveModifications_NeverTerminated()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Live", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        for (long i = 1; i <= 5; i++)
        {
            executor.Execute(graph, new SorophyRelationshipPropertyModification(
                relId, TqdCrucibleTestHelper.CreateTime(_schema, 10 + i * 10),
                propertiesToSet: new Dictionary<string, SorophyProperty>
                {
                    [$"K{i}"] = new SorophyProperty { Name = $"K{i}", Value = new SorophyValue(SorophyValueType.Integer, i) }
                }));
        }

        var tqd = graph.TemporalQuery;

        // Active at T=100 and far future
        Assert.True(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 100)));
        Assert.True(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 99999)));

        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(6, history.Facts.Count);
    }

    [Fact]
    public void Lifecycle_TerminatedRelationshipsDoNotReappearAtFutureCoordinates()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "ShortLived", TqdCrucibleTestHelper.CreateTime(_schema, 10)));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, TqdCrucibleTestHelper.CreateTime(_schema, 20)));

        var tqd = graph.TemporalQuery;

        var futureTimes = new[] { 20, 21, 50, 100, 1000, 1000000 };
        foreach (var time in futureTimes)
        {
            Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, time)));
            Assert.Null(tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, time)));
        }
    }

    /*
     * =============================================================
     * C05: BASELINE VS HISTORICAL STATE
     * =============================================================
     */

    [Fact]
    public void BaselineVsHistory_ZeroFacts_NeverFabricatesHistory()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();

        graph.AddRelationship(new SorophyRelationship { Id = b1, SourceId = s, TargetId = t, Type = "Base1" });
        graph.AddRelationship(new SorophyRelationship { Id = b2, SourceId = s, TargetId = t, Type = "Base2" });

        var tqd = graph.TemporalQuery;
        var t0 = TqdCrucibleTestHelper.CreateTime(_schema, 0);
        var t1000 = TqdCrucibleTestHelper.CreateTime(_schema, 1000);

        // Snapshot contains baseline relationships
        Assert.True(tqd.RelationshipExistsAt(b1, t0));
        Assert.True(tqd.RelationshipExistsAt(b2, t1000));

        // History queries return null/empty — NO fabricated facts
        Assert.Null(tqd.GetRelationshipHistory(b1));
        Assert.Null(tqd.GetRelationshipHistory(b2));
        Assert.Empty(tqd.GetRelationshipFactsInInterval(b1, t0, t1000));
        Assert.Empty(tqd.GetRelationshipFactsInInterval(b2, t0, t1000));
        Assert.Empty(tqd.GetFactsInInterval(t0, t1000));
        Assert.Empty(tqd.GetModifiedRelationshipIds(t0, t1000));
    }

    [Fact]
    public void BaselineVsHistory_MixedBaselineAndEvolved_SeparationPreserved()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var baselineRel = Guid.NewGuid();
        var evolvedRel = Guid.NewGuid();

        graph.AddRelationship(new SorophyRelationship { Id = baselineRel, SourceId = s, TargetId = t, Type = "Baseline" });

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            evolvedRel, s, t, "Evolved", TqdCrucibleTestHelper.CreateTime(_schema, 50)));

        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        // Snapshot at T=100 has both
        Assert.True(tqd.RelationshipExistsAt(baselineRel, t100));
        Assert.True(tqd.RelationshipExistsAt(evolvedRel, t100));

        // Interval facts only has evolvedRel
        var facts = tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t100);
        Assert.Single(facts);
        Assert.Equal(evolvedRel, facts[0].RelationshipId);

        // Modified IDs only contains evolvedRel
        var modified = tqd.GetModifiedRelationshipIds(TqdCrucibleTestHelper.CreateTime(_schema, 0), t100);
        Assert.Single(modified);
        Assert.Contains(evolvedRel, modified);
        Assert.DoesNotContain(baselineRel, modified);
    }

    [Fact]
    public void BaselineVsHistory_DirectCanonicalMutation_DoesNotFabricateHistory()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var rel = new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Direct" };
        graph.AddRelationship(rel);

        // Direct property mutation bypassing evolution executor
        rel.Properties["DirectKey"] = new SorophyProperty
        {
            Name = "DirectKey",
            Value = new SorophyValue(SorophyValueType.String, "DirectValue")
        };

        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        // Live/Snapshot reflects current state
        var snapRel = tqd.GetRelationshipAt(relId, t100);
        Assert.NotNull(snapRel);
        Assert.Equal("DirectValue", snapRel.Properties["DirectKey"].Value.Value);

        // But NO historical facts are created
        Assert.Null(tqd.GetRelationshipHistory(relId));
        Assert.Empty(tqd.GetFactsInInterval(TqdCrucibleTestHelper.CreateTime(_schema, 0), t100));
    }

    [Fact]
    public void BaselineVsHistory_BaselineEvolvedLaterAndTerminated()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = s, TargetId = t, Type = "Orig" });

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 50),
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["E"] = new SorophyProperty { Name = "E", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
            }));
        executor.Execute(graph, new SorophyRelationshipTermination(
            relId, TqdCrucibleTestHelper.CreateTime(_schema, 100)));

        var tqd = graph.TemporalQuery;

        // Prior to T=50: baseline state existed
        Assert.True(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 20)));

        // Between 50 and 100: modified state existed
        var snap75 = tqd.GetRelationshipAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 75));
        Assert.NotNull(snap75);
        Assert.Equal(1L, snap75.Properties["E"].Value.Value);

        // At and after 100: terminated (absent)
        Assert.False(tqd.RelationshipExistsAt(relId, TqdCrucibleTestHelper.CreateTime(_schema, 100)));

        // History contains exactly 2 facts (Mod at 50, Term at 100), no fabricated initial fact
        var history = tqd.GetRelationshipHistory(relId);
        Assert.NotNull(history);
        Assert.Equal(2, history.Facts.Count);
    }
}

