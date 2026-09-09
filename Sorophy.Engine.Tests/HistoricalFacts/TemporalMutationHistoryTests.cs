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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Diff;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.HistoricalFacts;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.HistoricalFacts;

/// <summary>
/// Authoritative test suite verifying Temporal Mutation History (Property & Relationship Mutations)
/// in Sorophy v2.0.0 "Krono".
/// </summary>
public sealed class TemporalMutationHistoryTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "MutationTimeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, int position)
    {
        return new SorophyTime(schema, position.ToString(), "Year", SorophyTimePrecision.Exact);
    }

    private static SorophyEntity CreateEntity(string name = "Entity", string type = "Default")
    {
        return new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type
        };
    }

    private static SorophyValue ValInt(long v) => new(SorophyValueType.Integer, v);
    private static SorophyValue ValStr(string s) => new(SorophyValueType.String, s);

    // =============================================================
    // 1. ENTITY PROPERTY MUTATIONS
    // =============================================================

    [Fact]
    public void ScenarioA_EntityPropertyMutation_BasicForward_UpdatesEffectiveAndLive()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Hero");

        graph.CreateEntity(e, CreateTime(schema, 100));

        // Mutate property at T=200
        graph.EditAt(CreateTime(schema, 200)).SetEntityProperty(
            e.Id, "level", ValInt(2), "Promoted to level 2");

        // Verify history fact
        var history = graph.EntityHistories[e.Id];
        var propFacts = history.Facts.Where(f => f.Kind == SorophyEntityFactKind.PropertyChanged).ToList();
        Assert.Single(propFacts);

        var fact = propFacts[0];
        Assert.Equal("level", fact.PropertyName);
        Assert.Null(fact.PreviousValue);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(2), fact.NewValue));
        Assert.Equal(CreateTime(schema, 200), fact.At);
        Assert.True(fact.Sequence > 0);

        // Verify effective value at coordinates
        var snapshot150 = graph.TemporalQuery.At(CreateTime(schema, 150));
        Assert.False(snapshot150.GetEntity(e.Id)!.Properties.ContainsKey("level"));

        var snapshot250 = graph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.True(snapshot250.GetEntity(e.Id)!.Properties.ContainsKey("level"));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(2), snapshot250.GetEntity(e.Id)!.Properties["level"].Value));

        // Verify live canonical graph has latest value
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(2), graph.Entities[e.Id].Properties["level"].Value));
    }

    [Fact]
    public void ScenarioB_EntityPropertyRemoval_RecordsFactAndRemovesFromEffectiveSnapshot()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Item");
        e.Properties["durability"] = new SorophyProperty { Name = "durability", Value = ValInt(100) };

        graph.CreateEntity(e, CreateTime(schema, 100));

        // Remove property at T=200
        var removed = graph.EditAt(CreateTime(schema, 200)).RemoveEntityProperty(e.Id, "durability", "Broken");
        Assert.True(removed);

        // Verify history fact
        var history = graph.EntityHistories[e.Id];
        var propFacts = history.Facts.Where(f => f.Kind == SorophyEntityFactKind.PropertyChanged).ToList();
        Assert.Single(propFacts);

        var fact = propFacts[0];
        Assert.Equal("durability", fact.PropertyName);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(100), fact.PreviousValue));
        Assert.Null(fact.NewValue);

        // Snapshot at T=150 still has durability
        var snap150 = graph.TemporalQuery.At(CreateTime(schema, 150));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(100), snap150.GetEntity(e.Id)!.Properties["durability"].Value));

        // Snapshot at T=250 does not have durability
        var snap250 = graph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.False(snap250.GetEntity(e.Id)!.Properties.ContainsKey("durability"));

        // Live canonical graph does not have durability
        Assert.False(graph.Entities[e.Id].Properties.ContainsKey("durability"));
    }

    [Fact]
    public void ScenarioC_EntityPropertyMutation_SameCoordinate_SequenceOrderingPreserved()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Player");
        graph.CreateEntity(e, CreateTime(schema, 100));

        var editor = graph.EditAt(CreateTime(schema, 200));
        editor.SetEntityProperty(e.Id, "rank", ValStr("Bronze"));
        editor.SetEntityProperty(e.Id, "rank", ValStr("Silver"));
        editor.SetEntityProperty(e.Id, "rank", ValStr("Gold"));

        var history = graph.EntityHistories[e.Id];
        var rankFacts = history.Facts.Where(f => f.PropertyName == "rank").ToList();
        Assert.Equal(3, rankFacts.Count);

        // Strictly monotonic sequence ordering
        Assert.True(rankFacts[0].Sequence < rankFacts[1].Sequence);
        Assert.True(rankFacts[1].Sequence < rankFacts[2].Sequence);

        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Gold"), rankFacts[2].NewValue));

        // Point-in-time snapshot at T=200 reflects latest sequential state
        var snap = graph.TemporalQuery.At(CreateTime(schema, 200));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Gold"), snap.GetEntity(e.Id)!.Properties["rank"].Value));

        // Canonical live state reflects Gold
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Gold"), graph.Entities[e.Id].Properties["rank"].Value));
    }

    [Fact]
    public void ScenarioD_EntityPropertyMutation_ArbitraryPastEdit_StitchesImmediateFutureContinuity()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Scholar");
        e.Properties["title"] = new SorophyProperty { Name = "title", Value = ValStr("Apprentice") };
        graph.CreateEntity(e, CreateTime(schema, 100));

        // Author future edits first
        graph.EditAt(CreateTime(schema, 500)).SetEntityProperty(e.Id, "title", ValStr("Master"));
        graph.EditAt(CreateTime(schema, 700)).SetEntityProperty(e.Id, "title", ValStr("Grandmaster"));

        // Now author past edit at T=300
        graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(e.Id, "title", ValStr("Journeyman"));

        var history = graph.EntityHistories[e.Id];
        var facts = history.Facts.Where(f => f.PropertyName == "title").ToList();
        Assert.Equal(3, facts.Count);

        var fact300 = facts.Single(f => f.At.Equals(CreateTime(schema, 300)));
        var fact500 = facts.Single(f => f.At.Equals(CreateTime(schema, 500)));
        var fact700 = facts.Single(f => f.At.Equals(CreateTime(schema, 700)));

        // Verify continuity stitching:
        // fact300: Prev = Apprentice, New = Journeyman
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Apprentice"), fact300.PreviousValue));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Journeyman"), fact300.NewValue));

        // Immediate future fact500: PreviousValue stitched to Journeyman!
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Journeyman"), fact500.PreviousValue));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Master"), fact500.NewValue));

        // Distant future fact700: PreviousValue remains Master (untouched!)
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Master"), fact700.PreviousValue));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Grandmaster"), fact700.NewValue));

        // Verify snapshot reconstructions across timeline
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Apprentice"), graph.TemporalQuery.At(CreateTime(schema, 200)).GetEntity(e.Id)!.Properties["title"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Journeyman"), graph.TemporalQuery.At(CreateTime(schema, 400)).GetEntity(e.Id)!.Properties["title"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Master"), graph.TemporalQuery.At(CreateTime(schema, 600)).GetEntity(e.Id)!.Properties["title"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Grandmaster"), graph.TemporalQuery.At(CreateTime(schema, 800)).GetEntity(e.Id)!.Properties["title"].Value));

        // Canonical live state remains Grandmaster (since T=300 < T=700)
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Grandmaster"), graph.Entities[e.Id].Properties["title"].Value));
    }

    [Fact]
    public void ScenarioE_EntityPropertyRemoval_ArbitraryPastEdit_StitchesImmediateFuture()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Agent");
        e.Properties["badge"] = new SorophyProperty { Name = "badge", Value = ValStr("B1") };
        graph.CreateEntity(e, CreateTime(schema, 100));

        // T=300: remove badge
        graph.EditAt(CreateTime(schema, 300)).RemoveEntityProperty(e.Id, "badge");

        // T=500: set badge to B2
        graph.EditAt(CreateTime(schema, 500)).SetEntityProperty(e.Id, "badge", ValStr("B2"));

        // At T=500, previous value was null (since removed at 300)
        var fact500 = graph.EntityHistories[e.Id].Facts.Single(f => f.At.Equals(CreateTime(schema, 500)));
        Assert.Null(fact500.PreviousValue);

        // Now author past edit at T=400: restore badge to B1.5
        graph.EditAt(CreateTime(schema, 400)).SetEntityProperty(e.Id, "badge", ValStr("B1.5"));

        // Fact at T=500 must now have PreviousValue == B1.5
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("B1.5"), fact500.PreviousValue));
    }

    [Fact]
    public void ScenarioF_EntityProperty_QueryBeforeAnyMutation_ReturnsBaseline()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("StaticItem");
        e.Properties["weight"] = new SorophyProperty { Name = "weight", Value = ValInt(42) };
        graph.CreateEntity(e, CreateTime(schema, 100));

        // No mutation facts authored
        var snap = graph.TemporalQuery.At(CreateTime(schema, 200));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(42), snap.GetEntity(e.Id)!.Properties["weight"].Value));
    }

    // =============================================================
    // 2. RELATIONSHIP PROPERTY & TYPE MUTATIONS
    // =============================================================

    [Fact]
    public void ScenarioG_RelationshipPropertyMutation_BasicForward_UpdatesEffectiveAndLive()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        var props = new Dictionary<string, SorophyProperty>
        {
            ["affinity"] = new SorophyProperty { Name = "affinity", Value = ValInt(10) }
        };
        graph.CreateRelationship(e1.Id, e2.Id, "Colleague", CreateTime(schema, 100), relId, props);

        // Mutate property at T=200
        graph.EditAt(CreateTime(schema, 200)).SetRelationshipProperty(
            relId, "affinity", ValInt(50), "Bond deepened");

        var history = graph.RelationshipHistories[relId];
        var propFact = history.Facts.Single(f => f.Kind == SorophyRelationshipFactKind.PropertyChanged);
        Assert.Equal("affinity", propFact.PropertyName);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(10), propFact.PreviousValue));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(50), propFact.NewValue));

        // Snapshot at T=150 has affinity = 10
        var snap150 = graph.TemporalQuery.At(CreateTime(schema, 150));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(10), snap150.GetRelationship(relId)!.Properties["affinity"].Value));

        // Snapshot at T=250 has affinity = 50
        var snap250 = graph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(50), snap250.GetRelationship(relId)!.Properties["affinity"].Value));

        // Live canonical relationship has affinity = 50
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(50), graph.Relationships[relId].Properties["affinity"].Value));
    }

    [Fact]
    public void ScenarioH_RelationshipPropertyRemoval_RecordsFactAndRemovesFromSnapshot()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        var props = new Dictionary<string, SorophyProperty>
        {
            ["quota"] = new SorophyProperty { Name = "quota", Value = ValInt(500) }
        };
        graph.CreateRelationship(e1.Id, e2.Id, "Trade", CreateTime(schema, 100), relId, props);

        var removed = graph.EditAt(CreateTime(schema, 200)).RemoveRelationshipProperty(relId, "quota");
        Assert.True(removed);

        var snap150 = graph.TemporalQuery.At(CreateTime(schema, 150));
        Assert.True(snap150.GetRelationship(relId)!.Properties.ContainsKey("quota"));

        var snap250 = graph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.False(snap250.GetRelationship(relId)!.Properties.ContainsKey("quota"));

        Assert.False(graph.Relationships[relId].Properties.ContainsKey("quota"));
    }

    [Fact]
    public void ScenarioI_RelationshipTypeChange_PopulatesDualTypes_UpdatesEffective()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        graph.CreateRelationship(e1.Id, e2.Id, "Acquaintance", CreateTime(schema, 100), relId);

        // Change relationship type at T=200
        graph.EditAt(CreateTime(schema, 200)).ChangeRelationshipType(relId, "Friend", "Became friends");

        var history = graph.RelationshipHistories[relId];
        var typeFact = history.Facts.Single(f => f.Kind == SorophyRelationshipFactKind.RelationshipChanged);
        Assert.Equal("Acquaintance", typeFact.PreviousType);
        Assert.Equal("Friend", typeFact.NewType);
        Assert.Equal("Friend", typeFact.Type);

        // Snapshot at T=150: Acquaintance
        var snap150 = graph.TemporalQuery.At(CreateTime(schema, 150));
        Assert.Equal("Acquaintance", snap150.GetRelationship(relId)!.Type);

        // Snapshot at T=250: Friend
        var snap250 = graph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.Equal("Friend", snap250.GetRelationship(relId)!.Type);

        // Live canonical relationship: Friend
        Assert.Equal("Friend", graph.Relationships[relId].Type);
    }

    [Fact]
    public void ScenarioJ_RelationshipMutation_SameCoordinate_SequenceOrderingPreserved()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        graph.CreateRelationship(e1.Id, e2.Id, "Contact", CreateTime(schema, 100), relId);

        var editor = graph.EditAt(CreateTime(schema, 200));
        editor.ChangeRelationshipType(relId, "Friend");
        editor.ChangeRelationshipType(relId, "BestFriend");

        var history = graph.RelationshipHistories[relId];
        var typeFacts = history.Facts.Where(f => f.Kind == SorophyRelationshipFactKind.RelationshipChanged).ToList();
        Assert.Equal(2, typeFacts.Count);
        Assert.True(typeFacts[0].Sequence < typeFacts[1].Sequence);

        var snap = graph.TemporalQuery.At(CreateTime(schema, 200));
        Assert.Equal("BestFriend", snap.GetRelationship(relId)!.Type);
        Assert.Equal("BestFriend", graph.Relationships[relId].Type);
    }

    [Fact]
    public void ScenarioK_RelationshipTypeChange_ArbitraryPastEdit_StitchesImmediateFuture()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        graph.CreateRelationship(e1.Id, e2.Id, "Ally", CreateTime(schema, 100), relId);

        // Future edits first
        graph.EditAt(CreateTime(schema, 500)).ChangeRelationshipType(relId, "Rival");
        graph.EditAt(CreateTime(schema, 700)).ChangeRelationshipType(relId, "Enemy");

        // Past edit at T=300
        graph.EditAt(CreateTime(schema, 300)).ChangeRelationshipType(relId, "Neutral");

        var history = graph.RelationshipHistories[relId];
        var fact300 = history.Facts.Single(f => f.At.Equals(CreateTime(schema, 300)));
        var fact500 = history.Facts.Single(f => f.At.Equals(CreateTime(schema, 500)));
        var fact700 = history.Facts.Single(f => f.At.Equals(CreateTime(schema, 700)));

        Assert.Equal("Ally", fact300.PreviousType);
        Assert.Equal("Neutral", fact300.NewType);

        // Immediate future stitched
        Assert.Equal("Neutral", fact500.PreviousType);
        Assert.Equal("Rival", fact500.NewType);

        // Distant future untouched
        Assert.Equal("Rival", fact700.PreviousType);
        Assert.Equal("Enemy", fact700.NewType);

        // Reconstructions
        Assert.Equal("Ally", graph.TemporalQuery.At(CreateTime(schema, 200)).GetRelationship(relId)!.Type);
        Assert.Equal("Neutral", graph.TemporalQuery.At(CreateTime(schema, 400)).GetRelationship(relId)!.Type);
        Assert.Equal("Rival", graph.TemporalQuery.At(CreateTime(schema, 600)).GetRelationship(relId)!.Type);
        Assert.Equal("Enemy", graph.TemporalQuery.At(CreateTime(schema, 800)).GetRelationship(relId)!.Type);

        Assert.Equal("Enemy", graph.Relationships[relId].Type);
    }

    // =============================================================
    // 3. EMERGENT HISTORICAL FACTS (EHG) & ENTITY TIMELINE
    // =============================================================

    [Fact]
    public void ScenarioM_EmergentHistoricalFacts_ProjectsPropertyChanged_And_RelationshipChanged()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Hero");
        graph.CreateEntity(e, CreateTime(schema, 100));

        graph.EditAt(CreateTime(schema, 200)).SetEntityProperty(e.Id, "power", ValInt(9001));

        var facts = graph.GetEntityHistoricalFacts(e.Id);
        Assert.Equal(2, facts.Count);
        Assert.Equal(SorophyHistoricalFactKind.Created, facts[0].Kind);
        Assert.Equal(SorophyHistoricalFactKind.PropertyChanged, facts[1].Kind);
        Assert.Equal("power", facts[1].PropertyName);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(9001), facts[1].NewValue));
    }

    [Fact]
    public void ScenarioN_EntityTimeline_IncludesDirectAndIncidentRelationshipFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("Hero");
        var e2 = CreateEntity("Villain");
        var e3 = CreateEntity("Unrelated");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));
        graph.CreateEntity(e3, CreateTime(schema, 100));

        // E1 direct mutation
        graph.EditAt(CreateTime(schema, 150)).SetEntityProperty(e1.Id, "level", ValInt(5));

        // Relationship between E1 and E2
        var relId = Guid.NewGuid();
        graph.CreateRelationship(e1.Id, e2.Id, "Rivalry", CreateTime(schema, 120), relId);

        // Mutate relationship
        graph.EditAt(CreateTime(schema, 160)).SetRelationshipProperty(relId, "intensity", ValInt(10));
        graph.EditAt(CreateTime(schema, 180)).ChangeRelationshipType(relId, "Nemesis");

        // Relationship between E2 and E3 (unrelated to E1)
        var unrelatedRelId = Guid.NewGuid();
        graph.CreateRelationship(e2.Id, e3.Id, "Alliance", CreateTime(schema, 130), unrelatedRelId);

        // Query timeline for E1 (using dedicated API)
        var timeline = graph.GetEntityTimelineFacts(e1.Id);

        // Must include:
        // 1. T=100: E1 Created
        // 2. T=120: Rel Created (E1->E2)
        // 3. T=150: E1 PropertyChanged ("level")
        // 4. T=160: Rel PropertyChanged ("intensity")
        // 5. T=180: Rel RelationshipChanged ("Nemesis")
        Assert.Equal(5, timeline.Count);

        Assert.Equal(CreateTime(schema, 100), timeline[0].Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, timeline[0].Kind);
        Assert.Equal(e1.Id, timeline[0].EntityId);

        Assert.Equal(CreateTime(schema, 120), timeline[1].Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, timeline[1].Kind);
        Assert.Equal(relId, timeline[1].RelationshipId);

        Assert.Equal(CreateTime(schema, 150), timeline[2].Time);
        Assert.Equal(SorophyHistoricalFactKind.PropertyChanged, timeline[2].Kind);
        Assert.Equal(e1.Id, timeline[2].EntityId);

        Assert.Equal(CreateTime(schema, 160), timeline[3].Time);
        Assert.Equal(SorophyHistoricalFactKind.PropertyChanged, timeline[3].Kind);
        Assert.Equal(relId, timeline[3].RelationshipId);

        Assert.Equal(CreateTime(schema, 180), timeline[4].Time);
        Assert.Equal(SorophyHistoricalFactKind.RelationshipChanged, timeline[4].Kind);
        Assert.Equal(relId, timeline[4].RelationshipId);
        Assert.Equal("Nemesis", timeline[4].RelationshipType);

        // Verify that without includeRelationships, only direct entity facts returned
        var directOnly = graph.GetEntityHistoricalFacts(e1.Id, includeRelationships: false);
        Assert.Equal(2, directOnly.Count);
        Assert.Equal(SorophyHistoricalFactKind.Created, directOnly[0].Kind);
        Assert.Equal(SorophyHistoricalFactKind.PropertyChanged, directOnly[1].Kind);
    }

    // =============================================================
    // 4. INVARIANTS, ATOMICITY & LIFECYCLE GUARDS
    // =============================================================

    [Fact]
    public void ScenarioO_HardEndpointExistence_PreventsMutationWhenEndpointInactive()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        // Cannot mutate entity property before its creation
        var ex1 = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 50)).SetEntityProperty(e1.Id, "prop", ValInt(1)));
        Assert.Contains("uncreated", ex1.Message);

        // Retire e1 at T=300
        graph.RetireEntity(e1.Id, CreateTime(schema, 300));

        // Cannot mutate entity property after retirement
        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).SetEntityProperty(e1.Id, "prop", ValInt(1)));
        Assert.Contains("retired", ex2.Message);

        // Relationship between e1 and e2
        var relId = Guid.NewGuid();
        graph.CreateRelationship(e1.Id, e2.Id, "Link", CreateTime(schema, 100), relId);

        // Cannot mutate relationship at T=350 because e1 is retired at T=300 (endpoint invariant!)
        var ex3 = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).ChangeRelationshipType(relId, "NewLink"));
        Assert.Contains("does not exist", ex3.Message);
    }

    [Fact]
    public void ScenarioP_RollbackAtomicity_FailedMutationLeavesStateUntouched()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("SafeEntity");
        e.Properties["score"] = new SorophyProperty { Name = "score", Value = ValInt(10) };
        graph.CreateEntity(e, CreateTime(schema, 100));

        var historyBeforeCount = graph.EntityHistories[e.Id].Count;

        // Attempt invalid mutation with empty property name
        Assert.Throws<ArgumentException>(() =>
            graph.EditAt(CreateTime(schema, 200)).SetEntityProperty(e.Id, "   ", ValInt(20)));

        // Verify history completely untouched
        Assert.Equal(historyBeforeCount, graph.EntityHistories[e.Id].Count);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(10), graph.Entities[e.Id].Properties["score"].Value));
    }

    // =============================================================
    // 5. SERIALIZATION & PERSISTENCE ROUND-TRIP
    // =============================================================

    [Fact]
    public void ScenarioQ_LoreSerialization_RoundTrip_PreservesMutationsAndSequences()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("Hero");
        var e2 = CreateEntity("Rival");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var relId = Guid.NewGuid();
        var props = new Dictionary<string, SorophyProperty>
        {
            ["score"] = new SorophyProperty { Name = "score", Value = ValInt(5) }
        };
        graph.CreateRelationship(e1.Id, e2.Id, "Acquaintance", CreateTime(schema, 100), relId, props);

        // Apply entity mutations
        var editor1 = graph.EditAt(CreateTime(schema, 150));
        editor1.SetEntityProperty(e1.Id, "hp", ValInt(100));
        editor1.SetEntityProperty(e1.Id, "hp", ValInt(95));

        // Apply relationship mutations
        var editor2 = graph.EditAt(CreateTime(schema, 200));
        editor2.ChangeRelationshipType(relId, "Friend");
        editor2.SetRelationshipProperty(relId, "score", ValInt(20));

        // Roundtrip via LoreSerializer
        var json = LoreSerializer.Serialize(graph);
        var restoredGraph = LoreSerializer.Deserialize(json);

        // Verify entity facts restored
        Assert.True(restoredGraph.TryGetEntityHistory(e1.Id, out var restoredEHistory));
        Assert.NotNull(restoredEHistory);
        var restoredEPropFacts = restoredEHistory!.Facts.Where(f => f.Kind == SorophyEntityFactKind.PropertyChanged).ToList();
        Assert.Equal(2, restoredEPropFacts.Count);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(95), restoredEPropFacts[1].NewValue));
        Assert.True(restoredEPropFacts[0].Sequence < restoredEPropFacts[1].Sequence);

        // Verify relationship facts restored
        Assert.True(restoredGraph.TryGetRelationshipHistory(relId, out var restoredRHistory));
        Assert.NotNull(restoredRHistory);
        var restoredTypeFacts = restoredRHistory!.Facts.Where(f => f.Kind == SorophyRelationshipFactKind.RelationshipChanged).ToList();
        Assert.Single(restoredTypeFacts);
        Assert.Equal("Acquaintance", restoredTypeFacts[0].PreviousType);
        Assert.Equal("Friend", restoredTypeFacts[0].NewType);

        var restoredRPropFacts = restoredRHistory.Facts.Where(f => f.Kind == SorophyRelationshipFactKind.PropertyChanged).ToList();
        Assert.Single(restoredRPropFacts);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(20), restoredRPropFacts[0].NewValue));

        // Verify snapshots on restored graph
        var snap100 = restoredGraph.TemporalQuery.At(CreateTime(schema, 100));
        Assert.False(snap100.GetEntity(e1.Id)!.Properties.ContainsKey("hp"));
        Assert.Equal("Acquaintance", snap100.GetRelationship(relId)!.Type);

        var snap175 = restoredGraph.TemporalQuery.At(CreateTime(schema, 175));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(95), snap175.GetEntity(e1.Id)!.Properties["hp"].Value));

        var snap250 = restoredGraph.TemporalQuery.At(CreateTime(schema, 250));
        Assert.Equal("Friend", snap250.GetRelationship(relId)!.Type);
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(20), snap250.GetRelationship(relId)!.Properties["score"].Value));

        // Verify EHG on restored graph
        var timeline = restoredGraph.GetEntityTimelineFacts(e1.Id);
        Assert.True(timeline.Count >= 5);
    }
}
