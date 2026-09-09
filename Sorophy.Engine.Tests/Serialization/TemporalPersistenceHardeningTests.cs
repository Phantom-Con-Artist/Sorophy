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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.HistoricalFacts;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

/// <summary>
/// Hardening test suite verifying that Sorophy Krono temporal graph Save -> Load
/// produces a semantically equivalent graph with lossless temporal reconstruction,
/// historical editing, Canon Check, EHG derivation, and same-coordinate ordering.
/// Uses exclusively established Krono public APIs.
/// </summary>
public sealed class TemporalPersistenceHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "MainTimeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Era", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Named)),
                new SorophyTimeUnit("Year", 1, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric)),
                new SorophyTimeUnit("Day", 2, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(
        SorophyTimeSchema schema,
        string position,
        string unit = "Year")
    {
        return new SorophyTime(schema, position, unit, SorophyTimePrecision.Exact);
    }

    private static SorophyGraph Roundtrip(SorophyGraph graph)
    {
        var json = LoreSerializer.Serialize(graph);
        return LoreSerializer.Deserialize(json);
    }

    private static SorophyValue CreateVal(object val)
    {
        return val switch
        {
            int i => new SorophyValue(SorophyValueType.Integer, (long)i),
            long l => new SorophyValue(SorophyValueType.Integer, l),
            string s => new SorophyValue(SorophyValueType.String, s),
            bool b => new SorophyValue(SorophyValueType.Boolean, b),
            Guid g => new SorophyValue(SorophyValueType.Guid, g),
            _ => throw new ArgumentException($"Unsupported test value type {val?.GetType()}")
        };
    }

    /* =============================================================
     * CORE SCENARIOS (Tests 01 - 12)
     * =============================================================
     */

    [Fact]
    public void Test01_EntityLifecycle_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t200 = CreateTime(schema, "200");

        graph.CreateEntity(new SorophyEntity { Id = entityId, Name = "Hero", Type = "Character" }, t100);
        graph.RetireEntity(entityId, t200);

        var loaded = Roundtrip(graph);

        Assert.Equal(SorophyEntityLifecycleStatus.Uncreated, loaded.GetEntityLifecycleStatus(entityId, CreateTime(schema, "50")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, loaded.GetEntityLifecycleStatus(entityId, t150));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, loaded.GetEntityLifecycleStatus(entityId, t200));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, loaded.GetEntityLifecycleStatus(entityId, CreateTime(schema, "250")));

        var snapActive = loaded.CreateSnapshot(t150);
        Assert.True(snapActive.ContainsEntity(entityId));

        var snapRetired = loaded.CreateSnapshot(CreateTime(schema, "250"));
        Assert.False(snapRetired.ContainsEntity(entityId));

        Assert.True(loaded.TryGetEntityHistory(entityId, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Equal(SorophyEntityFactKind.Created, history.Facts[0].Kind);
        Assert.Equal(SorophyEntityFactKind.Retired, history.Facts[1].Kind);

        // Cannot re-create or re-retire
        Assert.Throws<InvalidOperationException>(() => loaded.CreateEntity(new SorophyEntity { Id = entityId, Name = "Hero", Type = "Character" }, CreateTime(schema, "120")));
        Assert.Throws<InvalidOperationException>(() => loaded.RetireEntity(entityId, CreateTime(schema, "220")));
    }

    [Fact]
    public void Test02_RelationshipLifecycle_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        var t50 = CreateTime(schema, "50");
        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");
        var t200 = CreateTime(schema, "200");

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "Alice", Type = "Person" }, t50);
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "Bob", Type = "Person" }, t50);
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "friend" }, t100);
        graph.RetireRelationship(relId, t200);

        var loaded = Roundtrip(graph);

        Assert.Equal(SorophyRelationshipLifecycleStatus.Uncreated, loaded.GetRelationshipLifecycleStatus(relId, CreateTime(schema, "80")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Active, loaded.GetRelationshipLifecycleStatus(relId, t150));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Retired, loaded.GetRelationshipLifecycleStatus(relId, t200));

        var snapActive = loaded.CreateSnapshot(t150);
        Assert.True(snapActive.ContainsRelationship(relId));

        var snapRetired = loaded.CreateSnapshot(CreateTime(schema, "250"));
        Assert.False(snapRetired.ContainsRelationship(relId));

        Assert.True(loaded.TryGetRelationshipHistory(relId, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Equal(SorophyRelationshipFactKind.Created, history.Facts[0].Kind);
        Assert.Equal(SorophyRelationshipFactKind.Retired, history.Facts[1].Kind);

        Assert.Throws<InvalidOperationException>(() => loaded.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "friend" }, CreateTime(schema, "110")));
        Assert.Throws<InvalidOperationException>(() => loaded.RetireRelationship(relId, CreateTime(schema, "220")));
    }

    [Fact]
    public void Test03_PropertyMutations_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = entityId, Name = "Hero", Type = "Character" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(entityId, "Score", CreateVal(10), CreateTime(schema, "120"));
        graph.SetEntityProperty(entityId, "Score", CreateVal(20), CreateTime(schema, "150"));
        graph.SetEntityProperty(entityId, "Score", CreateVal(30), CreateTime(schema, "180"));

        var loaded = Roundtrip(graph);

        Assert.True(loaded.TryGetEntity(entityId, out var liveEntity));
        Assert.Equal(30L, (long)liveEntity!.Properties["Score"].Value.Value!);

        var s110 = loaded.CreateSnapshot(CreateTime(schema, "110"));
        Assert.False(s110.Entities[entityId].Properties.ContainsKey("Score"));

        var s130 = loaded.CreateSnapshot(CreateTime(schema, "130"));
        Assert.Equal(10L, (long)s130.Entities[entityId].Properties["Score"].Value.Value!);

        var s160 = loaded.CreateSnapshot(CreateTime(schema, "160"));
        Assert.Equal(20L, (long)s160.Entities[entityId].Properties["Score"].Value.Value!);

        var s190 = loaded.CreateSnapshot(CreateTime(schema, "190"));
        Assert.Equal(30L, (long)s190.Entities[entityId].Properties["Score"].Value.Value!);

        Assert.True(loaded.TryGetEntityHistory(entityId, out var history));
        Assert.Equal(4, history!.Count);
        Assert.Equal(1L, history.Facts[0].Sequence);
        Assert.Equal(2L, history.Facts[1].Sequence);
        Assert.Equal(3L, history.Facts[2].Sequence);
        Assert.Equal(4L, history.Facts[3].Sequence);
    }

    [Fact]
    public void Test04_RelationshipPropertyMutations_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "Source", Type = "Node" }, CreateTime(schema, "50"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "Target", Type = "Node" }, CreateTime(schema, "50"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "link" }, CreateTime(schema, "100"));
        graph.SetRelationshipProperty(relId, "Weight", CreateVal(5), CreateTime(schema, "120"));
        graph.SetRelationshipProperty(relId, "Weight", CreateVal(15), CreateTime(schema, "150"));

        var loaded = Roundtrip(graph);

        Assert.True(loaded.TryGetRelationship(relId, out var liveRel));
        Assert.Equal(15L, (long)liveRel!.Properties["Weight"].Value.Value!);

        var s110 = loaded.CreateSnapshot(CreateTime(schema, "110"));
        Assert.False(s110.Relationships[relId].Properties.ContainsKey("Weight"));

        var s130 = loaded.CreateSnapshot(CreateTime(schema, "130"));
        Assert.Equal(5L, (long)s130.Relationships[relId].Properties["Weight"].Value.Value!);

        var s160 = loaded.CreateSnapshot(CreateTime(schema, "160"));
        Assert.Equal(15L, (long)s160.Relationships[relId].Properties["Weight"].Value.Value!);

        Assert.True(loaded.TryGetRelationshipHistory(relId, out var history));
        Assert.Equal(3, history!.Count);
        Assert.Equal(1L, history.Facts[0].Sequence);
        Assert.Equal(2L, history.Facts[1].Sequence);
        Assert.Equal(3L, history.Facts[2].Sequence);
    }

    [Fact]
    public void Test05_RelationshipTypeMutations_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "Source", Type = "Node" }, CreateTime(schema, "50"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "Target", Type = "Node" }, CreateTime(schema, "50"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "ally" }, CreateTime(schema, "100"));
        graph.ChangeRelationshipType(relId, "neutral", CreateTime(schema, "140"));
        graph.ChangeRelationshipType(relId, "hostile", CreateTime(schema, "180"));

        var loaded = Roundtrip(graph);

        Assert.True(loaded.TryGetRelationship(relId, out var liveRel));
        Assert.Equal("hostile", liveRel!.Type);

        var s120 = loaded.CreateSnapshot(CreateTime(schema, "120"));
        Assert.Equal("ally", s120.Relationships[relId].Type);

        var s150 = loaded.CreateSnapshot(CreateTime(schema, "150"));
        Assert.Equal("neutral", s150.Relationships[relId].Type);

        var s190 = loaded.CreateSnapshot(CreateTime(schema, "190"));
        Assert.Equal("hostile", s190.Relationships[relId].Type);

        Assert.True(loaded.TryGetRelationshipHistory(relId, out var history));
        Assert.Equal(3, history!.Count);
        Assert.Equal(1L, history.Facts[0].Sequence);
        Assert.Equal(2L, history.Facts[1].Sequence);
        Assert.Equal(3L, history.Facts[2].Sequence);
    }

    [Fact]
    public void Test06_Removal_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = entityId, Name = "Hero", Type = "Character" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "Other", Type = "Character" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = entityId, TargetId = e2, Type = "link" }, CreateTime(schema, "100"));

        graph.SetEntityProperty(entityId, "Color", CreateVal("Red"), CreateTime(schema, "110"));
        graph.SetEntityProperty(entityId, "Color", CreateVal("Blue"), CreateTime(schema, "130"));
        graph.RemoveEntityProperty(entityId, "Color", CreateTime(schema, "160"));

        graph.SetRelationshipProperty(relId, "Tag", CreateVal("Important"), CreateTime(schema, "120"));
        graph.RemoveRelationshipProperty(relId, "Tag", CreateTime(schema, "170"));

        var loaded = Roundtrip(graph);

        Assert.True(loaded.TryGetEntity(entityId, out var liveEntity));
        Assert.False(liveEntity!.Properties.ContainsKey("Color"));

        Assert.True(loaded.TryGetRelationship(relId, out var liveRel));
        Assert.False(liveRel!.Properties.ContainsKey("Tag"));

        var s120 = loaded.CreateSnapshot(CreateTime(schema, "120"));
        Assert.Equal("Red", (string)s120.Entities[entityId].Properties["Color"].Value.Value!);

        var s140 = loaded.CreateSnapshot(CreateTime(schema, "140"));
        Assert.Equal("Blue", (string)s140.Entities[entityId].Properties["Color"].Value.Value!);

        var s180 = loaded.CreateSnapshot(CreateTime(schema, "180"));
        Assert.False(s180.Entities[entityId].Properties.ContainsKey("Color"));
        Assert.False(s180.Relationships[relId].Properties.ContainsKey("Tag"));

        Assert.True(loaded.TryGetEntityHistory(entityId, out var eHist));
        var removalFact = eHist!.Facts.First(f => f.Kind == SorophyEntityFactKind.PropertyChanged && f.At.Position == "160");
        Assert.Null(removalFact.NewValue);
    }

    [Fact]
    public void Test07_SameCoordinateOrdering_Roundtrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        var t100 = CreateTime(schema, "100");
        var t150 = CreateTime(schema, "150");

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "Node" }, t100);
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "Node" }, t100);
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "ally" }, t100);

        // Same-coordinate entity mutations at T150
        graph.SetEntityProperty(e1, "P1", CreateVal(100), t150);
        graph.SetEntityProperty(e1, "P2", CreateVal(200), t150);
        graph.SetEntityProperty(e1, "P1", CreateVal(150), t150);
        graph.RemoveEntityProperty(e1, "P2", t150);

        // Same-coordinate relationship mutations at T150
        graph.SetRelationshipProperty(relId, "RP1", CreateVal(10), t150);
        graph.ChangeRelationshipType(relId, "neutral", t150);
        graph.SetRelationshipProperty(relId, "RP1", CreateVal(20), t150);

        var preSaveEntityFacts = graph.GetEntityHistoricalFacts(e1);
        var preSaveRelFacts = graph.GetRelationshipHistoricalFacts(relId);
        var preSaveAllFacts = graph.GetHistoricalFacts();

        var loaded = Roundtrip(graph);

        var postLoadEntityFacts = loaded.GetEntityHistoricalFacts(e1);
        var postLoadRelFacts = loaded.GetRelationshipHistoricalFacts(relId);
        var postLoadAllFacts = loaded.GetHistoricalFacts();

        // 1. Exact Sequence preservation
        Assert.Equal(preSaveEntityFacts.Count, postLoadEntityFacts.Count);
        for (int i = 0; i < preSaveEntityFacts.Count; i++)
        {
            Assert.Equal(preSaveEntityFacts[i].Sequence, postLoadEntityFacts[i].Sequence);
            Assert.Equal(preSaveEntityFacts[i].Kind, postLoadEntityFacts[i].Kind);
            Assert.Equal(preSaveEntityFacts[i].PropertyName, postLoadEntityFacts[i].PropertyName);
        }

        Assert.Equal(preSaveRelFacts.Count, postLoadRelFacts.Count);
        for (int i = 0; i < preSaveRelFacts.Count; i++)
        {
            Assert.Equal(preSaveRelFacts[i].Sequence, postLoadRelFacts[i].Sequence);
            Assert.Equal(preSaveRelFacts[i].Kind, postLoadRelFacts[i].Kind);
            Assert.Equal(preSaveRelFacts[i].PropertyName, postLoadRelFacts[i].PropertyName);
            Assert.Equal(preSaveRelFacts[i].NewType, postLoadRelFacts[i].NewType);
        }

        // 2. Exact EHG ordering
        Assert.Equal(preSaveAllFacts.Count, postLoadAllFacts.Count);
        for (int i = 0; i < preSaveAllFacts.Count; i++)
        {
            Assert.Equal(preSaveAllFacts[i], postLoadAllFacts[i]);
        }

        // 3. Exact reconstruction result
        var snapPre = graph.CreateSnapshot(t150);
        var snapPost = loaded.CreateSnapshot(t150);

        Assert.Equal((long)snapPre.Entities[e1].Properties["P1"].Value.Value!, (long)snapPost.Entities[e1].Properties["P1"].Value.Value!);
        Assert.False(snapPost.Entities[e1].Properties.ContainsKey("P2"));
        Assert.Equal(snapPre.Relationships[relId].Type, snapPost.Relationships[relId].Type);
        Assert.Equal((long)snapPre.Relationships[relId].Properties["RP1"].Value.Value!, (long)snapPost.Relationships[relId].Properties["RP1"].Value.Value!);
    }

    [Fact]
    public void Test08_PastTemporalEditAfterLoad()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = entityId, Name = "Hero", Type = "Character" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(entityId, "Level", CreateVal(1), CreateTime(schema, "120"));
        graph.SetEntityProperty(entityId, "Level", CreateVal(5), CreateTime(schema, "180"));

        var loaded = Roundtrip(graph);

        // Perform past edit on loaded graph: insert Level = 3 at T150
        loaded.SetEntityProperty(entityId, "Level", CreateVal(3), CreateTime(schema, "150"));

        Assert.True(loaded.TryGetEntityHistory(entityId, out var history));
        var factAt180 = history!.Facts.First(f => f.At.Position == "180");
        Assert.Equal(3L, (long)factAt180.PreviousValue?.Value!);

        var s130 = loaded.CreateSnapshot(CreateTime(schema, "130"));
        Assert.Equal(1L, (long)s130.Entities[entityId].Properties["Level"].Value.Value!);

        var s160 = loaded.CreateSnapshot(CreateTime(schema, "160"));
        Assert.Equal(3L, (long)s160.Entities[entityId].Properties["Level"].Value.Value!);

        var s190 = loaded.CreateSnapshot(CreateTime(schema, "190"));
        Assert.Equal(5L, (long)s190.Entities[entityId].Properties["Level"].Value.Value!);

        // Ensure subsequent roundtrip remains intact
        var reloaded = Roundtrip(loaded);
        var s160Re = reloaded.CreateSnapshot(CreateTime(schema, "160"));
        Assert.Equal(3L, (long)s160Re.Entities[entityId].Properties["Level"].Value.Value!);
    }

    [Fact]
    public void Test09_CanonCheckEnforcementAfterLoad()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "Node" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "Node" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "link" }, CreateTime(schema, "120"));
        graph.RetireEntity(e2, CreateTime(schema, "200"));

        var loaded = Roundtrip(graph);

        // 1. Cannot create entity before existence or after retirement
        Assert.False(loaded.CanCreateEntity(e1, CreateTime(schema, "80")).IsValid);

        // 2. Cannot retire entity if active relationship exists at retirement
        Assert.False(loaded.CanRetireEntity(e1, CreateTime(schema, "110")).IsValid);

        // 3. Cannot mutate property on uncreated entity
        Assert.False(loaded.CanSetEntityProperty(e1, "P", CreateVal(1), CreateTime(schema, "50")).IsValid);

        // 4. Cannot mutate property on retired entity
        Assert.False(loaded.CanSetEntityProperty(e2, "P", CreateVal(1), CreateTime(schema, "220")).IsValid);

        // 5. Cannot mutate relationship property if rel uncreated or retired
        Assert.False(loaded.CanSetRelationshipProperty(relId, "RP", CreateVal(1), CreateTime(schema, "100")).IsValid);

        // 6. Cannot change relationship type if rel uncreated or retired
        Assert.False(loaded.CanChangeRelationshipType(relId, "newType", CreateTime(schema, "100")).IsValid);

        // 7. Cannot remove property when entity is uncreated, and removing nonexistent property returns false
        Assert.False(loaded.CanRemoveEntityProperty(e1, "NonExistent", CreateTime(schema, "50")).IsValid);
        Assert.False(loaded.RemoveEntityProperty(e1, "NonExistent", CreateTime(schema, "150")));
    }

    [Fact]
    public void Test10_EmergentHistoryGenerator_RoundtripIdentity()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "A", Type = "Person" }, CreateTime(schema, "10"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "B", Type = "Person" }, CreateTime(schema, "20"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "friend" }, CreateTime(schema, "30"));
        graph.SetEntityProperty(e1, "Age", CreateVal(25), CreateTime(schema, "40"));
        graph.SetRelationshipProperty(relId, "Affinity", CreateVal(100), CreateTime(schema, "50"));
        graph.ChangeRelationshipType(relId, "partner", CreateTime(schema, "60"));
        graph.RemoveRelationshipProperty(relId, "Affinity", CreateTime(schema, "70"));
        graph.RetireRelationship(relId, CreateTime(schema, "80"));
        graph.RetireEntity(e2, CreateTime(schema, "90"));

        var preE1 = graph.GetEntityHistoricalFacts(e1, includeRelationships: true);
        var preRel = graph.GetRelationshipHistoricalFacts(relId);
        var preAll = graph.GetHistoricalFacts();

        var loaded = Roundtrip(graph);

        var postE1 = loaded.GetEntityHistoricalFacts(e1, includeRelationships: true);
        var postRel = loaded.GetRelationshipHistoricalFacts(relId);
        var postAll = loaded.GetHistoricalFacts();

        Assert.Equal(preE1.Count, postE1.Count);
        for (int i = 0; i < preE1.Count; i++)
        {
            Assert.Equal(preE1[i], postE1[i]);
        }

        Assert.Equal(preRel.Count, postRel.Count);
        for (int i = 0; i < preRel.Count; i++)
        {
            Assert.Equal(preRel[i], postRel[i]);
        }

        Assert.Equal(preAll.Count, postAll.Count);
        for (int i = 0; i < preAll.Count; i++)
        {
            Assert.Equal(preAll[i], postAll[i]);
        }
    }

    [Fact]
    public void Test11_CanonicalImmutabilityDuringReconstruction()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var relId = Guid.NewGuid();
        var e2 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "Hero", Type = "Character" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "Villain", Type = "Character" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "nemesis" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "HP", CreateVal(100), CreateTime(schema, "110"));
        graph.SetEntityProperty(e1, "HP", CreateVal(50), CreateTime(schema, "150"));
        graph.SetRelationshipProperty(relId, "Hostility", CreateVal(99), CreateTime(schema, "120"));

        var loaded = Roundtrip(graph);

        var jsonBefore = LoreSerializer.Serialize(loaded);

        for (int y = 90; y <= 200; y += 10)
        {
            _ = loaded.CreateSnapshot(CreateTime(schema, y.ToString()));
        }

        var jsonAfter = LoreSerializer.Serialize(loaded);
        Assert.Equal(jsonBefore, jsonAfter);
    }

    [Fact]
    public void Test12_LegacyV1BackwardCompatibility()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = e1, Name = "V1Entity", Type = "Legacy" });
        graph.AddEntity(new SorophyEntity { Id = e2, Name = "V1Target", Type = "Legacy" });
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "v1_link" });

        var hist = graph.GetOrCreateRelationshipHistory(relId);
        hist.Add(new SorophyRelationshipFact(CreateTime(schema, "50"), relId, e1, e2, "v1_link", new Dictionary<string, SorophyProperty>()));

        var loaded = Roundtrip(graph);

        Assert.True(loaded.TryGetEntity(e1, out _));
        Assert.True(loaded.TryGetRelationship(relId, out var loadedRel));
        Assert.Equal("v1_link", loadedRel!.Type);

        // Can now apply Krono operations seamlessly
        loaded.SetEntityProperty(e1, "NewProp", CreateVal(42), CreateTime(schema, "100"));
        var s110 = loaded.CreateSnapshot(CreateTime(schema, "110"));
        Assert.Equal(42L, (long)s110.Entities[e1].Properties["NewProp"].Value.Value!);
    }

    /* =============================================================
     * ADVERSARIAL EDGE CASES (Edge Cases 01 - 20)
     * =============================================================
     */

    [Fact]
    public void EdgeCase01_SequenceGapResilience()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "Type" });
        var hist = graph.GetOrCreateEntityHistory(e1);
        hist.Add(new SorophyEntityFact(CreateTime(schema, "100"), e1, SorophyEntityFactKind.Created, sequence: 10));
        hist.Add(new SorophyEntityFact(CreateTime(schema, "150"), e1, SorophyEntityFactKind.PropertyChanged, propertyName: "A", sequence: 25));

        var loaded = Roundtrip(graph);
        Assert.True(loaded.TryGetEntityHistory(e1, out var loadedHist));
        Assert.Equal(10L, loadedHist!.Facts[0].Sequence);
        Assert.Equal(25L, loadedHist.Facts[1].Sequence);

        // Subsequent mutation assigns maxSeq + 1 (26)
        loaded.SetEntityProperty(e1, "B", CreateVal(1), CreateTime(schema, "200"));
        Assert.Equal(26L, loadedHist.Facts[2].Sequence);
    }

    [Fact]
    public void EdgeCase02_InterleavedEntityAndRelationshipMutationsAtSameTime()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();
        var t = CreateTime(schema, "100");

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, t);
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, t);
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "rel" }, t);

        graph.SetEntityProperty(e1, "P", CreateVal(1), t);
        graph.SetRelationshipProperty(relId, "RP", CreateVal(2), t);

        var loaded = Roundtrip(graph);
        var ehg = loaded.GetHistoricalFacts(t, t);

        Assert.Equal(5, ehg.Count);
        // Entity facts sorted before Relationship facts
        Assert.Equal(SorophyHistoricalFactTarget.Entity, ehg[0].TargetKind);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, ehg[1].TargetKind);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, ehg[2].TargetKind);
        Assert.Equal(SorophyHistoricalFactTarget.Relationship, ehg[3].TargetKind);
        Assert.Equal(SorophyHistoricalFactTarget.Relationship, ehg[4].TargetKind);
    }

    [Fact]
    public void EdgeCase03_DeepHistoryChain()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "DeepEntity", Type = "T" }, CreateTime(schema, "0"));
        for (int i = 1; i <= 50; i++)
        {
            graph.SetEntityProperty(e1, "Counter", CreateVal(i), CreateTime(schema, i.ToString()));
        }

        var loaded = Roundtrip(graph);
        Assert.True(loaded.TryGetEntityHistory(e1, out var hist));
        Assert.Equal(51, hist!.Count);

        for (int i = 0; i < 51; i++)
        {
            Assert.Equal(i + 1, hist.Facts[i].Sequence);
        }

        var snap25 = loaded.CreateSnapshot(CreateTime(schema, "25"));
        Assert.Equal(25L, (long)snap25.Entities[e1].Properties["Counter"].Value.Value!);
    }

    [Fact]
    public void EdgeCase04_EmptyHistoryRoundtrip()
    {
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = e1, Name = "Node1", Type = "T" });
        graph.AddEntity(new SorophyEntity { Id = e2, Name = "Node2", Type = "T" });
        graph.AddRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "link" });

        var loaded = Roundtrip(graph);
        Assert.False(loaded.TryGetEntityHistory(e1, out _));
        Assert.False(loaded.TryGetRelationshipHistory(relId, out _));
    }

    [Fact]
    public void EdgeCase05_ZeroDurationLifecycle()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var t = CreateTime(schema, "100");

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "Ghost", Type = "T" }, t);
        graph.RetireEntity(e1, t);

        var loaded = Roundtrip(graph);
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, loaded.GetEntityLifecycleStatus(e1, t));

        var snap = loaded.CreateSnapshot(t);
        Assert.False(snap.ContainsEntity(e1));
    }

    [Fact]
    public void EdgeCase06_ResaveIdempotency()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "link" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "Prop", CreateVal("Val"), CreateTime(schema, "110"));
        graph.ChangeRelationshipType(relId, "newLink", CreateTime(schema, "120"));

        var json1 = LoreSerializer.Serialize(graph);
        var loaded1 = LoreSerializer.Deserialize(json1);
        var json2 = LoreSerializer.Serialize(loaded1);
        var loaded2 = LoreSerializer.Deserialize(json2);
        var json3 = LoreSerializer.Serialize(loaded2);

        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);
    }

    [Fact]
    public void EdgeCase07_CrossTimelineFacts()
    {
        var schemaA = CreateSchema("TimelineA");
        var schemaB = CreateSchema("TimelineB");
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "Type" });
        var hist = graph.GetOrCreateEntityHistory(e1);
        hist.Add(new SorophyEntityFact(CreateTime(schemaA, "100"), e1, SorophyEntityFactKind.Created, sequence: 1));
        hist.Add(new SorophyEntityFact(CreateTime(schemaB, "50"), e1, SorophyEntityFactKind.PropertyChanged, propertyName: "P", sequence: 2));

        var loaded = Roundtrip(graph);
        Assert.True(loaded.TryGetEntityHistory(e1, out var loadedHist));
        Assert.Equal(2, loadedHist!.Count);
        Assert.Equal("TimelineA", loadedHist.Facts[0].At.Timeline);
        Assert.Equal("TimelineB", loadedHist.Facts[1].At.Timeline);
    }

    [Fact]
    public void EdgeCase08_NullPropertyValueVsRemovedProperty()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "P1", CreateVal("Initial"), CreateTime(schema, "110"));
        graph.RemoveEntityProperty(e1, "P1", CreateTime(schema, "130"));

        var loaded = Roundtrip(graph);
        Assert.True(loaded.TryGetEntityHistory(e1, out var hist));

        var removalFact = hist!.Facts.First(f => f.At.Position == "130");
        Assert.Null(removalFact.NewValue);

        var snap = loaded.CreateSnapshot(CreateTime(schema, "140"));
        Assert.False(snap.Entities[e1].Properties.ContainsKey("P1"));
    }

    [Fact]
    public void EdgeCase09_TypeChangeAndPropertyChangeAtSameCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();
        var t = CreateTime(schema, "100");

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, t);
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, t);
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "base" }, t);

        graph.ChangeRelationshipType(relId, "promoted", t);
        graph.SetRelationshipProperty(relId, "Rank", CreateVal(1), t);

        var loaded = Roundtrip(graph);
        var snap = loaded.CreateSnapshot(t);

        Assert.Equal("promoted", snap.Relationships[relId].Type);
        Assert.Equal(1L, (long)snap.Relationships[relId].Properties["Rank"].Value.Value!);
    }

    [Fact]
    public void EdgeCase10_PropertyChangeOnRetiredRelationshipRejected()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "link" }, CreateTime(schema, "100"));
        graph.RetireRelationship(relId, CreateTime(schema, "150"));

        var loaded = Roundtrip(graph);
        Assert.False(loaded.CanSetRelationshipProperty(relId, "P", CreateVal(1), CreateTime(schema, "160")).IsValid);
        Assert.Throws<InvalidOperationException>(() =>
            loaded.SetRelationshipProperty(relId, "P", CreateVal(1), CreateTime(schema, "160")));
    }

    [Fact]
    public void EdgeCase11_PropertyChangeOnRetiredEntityRejected()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.RetireEntity(e1, CreateTime(schema, "150"));

        var loaded = Roundtrip(graph);
        Assert.False(loaded.CanSetEntityProperty(e1, "P", CreateVal(1), CreateTime(schema, "160")).IsValid);
        Assert.Throws<InvalidOperationException>(() =>
            loaded.SetEntityProperty(e1, "P", CreateVal(1), CreateTime(schema, "160")));
    }

    [Fact]
    public void EdgeCase12_DeserializationUnknownFactKind_Throws()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));

        var json = LoreSerializer.Serialize(graph);
        var badJson = json.Replace("\"Created\"", "\"UnknownKind\"");

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(badJson));
    }

    [Fact]
    public void EdgeCase13_DeserializationMismatchedFactId_Throws()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));

        var json = LoreSerializer.Serialize(graph);
        // Replace only the fact's entityId (the second occurrence of "entityId": "e1")
        var searchStr = $"\"entityId\": \"{e1}\"";
        var firstIdx = json.IndexOf(searchStr, StringComparison.Ordinal);
        var secondIdx = json.IndexOf(searchStr, firstIdx + searchStr.Length, StringComparison.Ordinal);
        var badJson = json.Substring(0, secondIdx) + $"\"entityId\": \"{Guid.NewGuid()}\"" + json.Substring(secondIdx + searchStr.Length);

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(badJson));
    }

    [Fact]
    public void EdgeCase14_DeserializationMissingAtTime_Throws()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));

        var json = LoreSerializer.Serialize(graph);
        var badJson = json.Replace("\"at\":", "\"missing_at\":");

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(badJson));
    }

    [Fact]
    public void EdgeCase15_ContinuityStitchingPastEditPreservesFuture()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "P", CreateVal(10), CreateTime(schema, "120"));
        graph.SetEntityProperty(e1, "P", CreateVal(20), CreateTime(schema, "160"));
        graph.SetEntityProperty(e1, "P", CreateVal(30), CreateTime(schema, "200"));

        var loaded = Roundtrip(graph);

        // Insert at T140: P = 15
        loaded.SetEntityProperty(e1, "P", CreateVal(15), CreateTime(schema, "140"));

        Assert.True(loaded.TryGetEntityHistory(e1, out var hist));
        var factAt160 = hist!.Facts.First(f => f.At.Position == "160");
        Assert.Equal(15L, (long)factAt160.PreviousValue?.Value!);
        Assert.Equal(20L, (long)factAt160.NewValue?.Value!);

        var factAt200 = hist.Facts.First(f => f.At.Position == "200");
        Assert.Equal(20L, (long)factAt200.PreviousValue?.Value!);
        Assert.Equal(30L, (long)factAt200.NewValue?.Value!);
    }

    [Fact]
    public void EdgeCase16_RollbackAtomicityVerification()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "P", CreateVal(10), CreateTime(schema, "120"));

        Assert.True(graph.TryGetEntityHistory(e1, out var hist));
        var initialCount = hist!.Count;

        // Attempt invalid mutation via Canon Check failure: entity doesn't exist at T50
        Assert.Throws<InvalidOperationException>(() =>
            graph.SetEntityProperty(e1, "P", CreateVal(99), CreateTime(schema, "50")));

        // Verify history, count, and live state completely untouched
        Assert.Equal(initialCount, hist.Count);
        Assert.Equal(10L, (long)graph.Entities[e1].Properties["P"].Value.Value!);
    }

    [Fact]
    public void EdgeCase17_MultiPropertyInterleavingAcrossCoordinates()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.SetEntityProperty(e1, "P1", CreateVal("A1"), CreateTime(schema, "110"));
        graph.SetEntityProperty(e1, "P2", CreateVal("B1"), CreateTime(schema, "120"));
        graph.SetEntityProperty(e1, "P1", CreateVal("A2"), CreateTime(schema, "130"));
        graph.SetEntityProperty(e1, "P2", CreateVal("B2"), CreateTime(schema, "140"));

        var loaded = Roundtrip(graph);

        var s125 = loaded.CreateSnapshot(CreateTime(schema, "125"));
        Assert.Equal("A1", (string)s125.Entities[e1].Properties["P1"].Value.Value!);
        Assert.Equal("B1", (string)s125.Entities[e1].Properties["P2"].Value.Value!);

        var s135 = loaded.CreateSnapshot(CreateTime(schema, "135"));
        Assert.Equal("A2", (string)s135.Entities[e1].Properties["P1"].Value.Value!);
        Assert.Equal("B1", (string)s135.Entities[e1].Properties["P2"].Value.Value!);
    }

    [Fact]
    public void EdgeCase18_MultipleRelationshipsBetweenSamePair()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, CreateTime(schema, "100"));

        graph.CreateRelationship(new SorophyRelationship { Id = r1, SourceId = e1, TargetId = e2, Type = "link_one" }, CreateTime(schema, "110"));
        graph.CreateRelationship(new SorophyRelationship { Id = r2, SourceId = e1, TargetId = e2, Type = "link_two" }, CreateTime(schema, "120"));

        graph.ChangeRelationshipType(r1, "link_one_evolved", CreateTime(schema, "130"));

        var loaded = Roundtrip(graph);

        var s125 = loaded.CreateSnapshot(CreateTime(schema, "125"));
        Assert.Equal("link_one", s125.Relationships[r1].Type);
        Assert.Equal("link_two", s125.Relationships[r2].Type);

        var s135 = loaded.CreateSnapshot(CreateTime(schema, "135"));
        Assert.Equal("link_one_evolved", s135.Relationships[r1].Type);
        Assert.Equal("link_two", s135.Relationships[r2].Type);
    }

    [Fact]
    public void EdgeCase19_RetiredRelationshipIdsPreserved()
    {
        var graph = new SorophyGraph();
        var rId = Guid.NewGuid();

        graph.RetireRelationshipId(rId);

        var loaded = Roundtrip(graph);
        Assert.True(loaded.IsRelationshipIdRetired(rId));
        Assert.Throws<InvalidOperationException>(() =>
            loaded.AddRelationship(new SorophyRelationship { Id = rId, SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid(), Type = "T" }));
    }

    [Fact]
    public void EdgeCase20_SnapshotMaterializerConsistency()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var relId = Guid.NewGuid();

        graph.CreateEntity(new SorophyEntity { Id = e1, Name = "E1", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateEntity(new SorophyEntity { Id = e2, Name = "E2", Type = "T" }, CreateTime(schema, "100"));
        graph.CreateRelationship(new SorophyRelationship { Id = relId, SourceId = e1, TargetId = e2, Type = "base" }, CreateTime(schema, "110"));
        graph.SetEntityProperty(e1, "Num", CreateVal(42), CreateTime(schema, "120"));
        graph.ChangeRelationshipType(relId, "advanced", CreateTime(schema, "130"));
        graph.RetireRelationship(relId, CreateTime(schema, "140"));
        graph.RetireEntity(e1, CreateTime(schema, "150"));

        var loaded = Roundtrip(graph);

        var checkpoints = new[] { "90", "100", "115", "125", "135", "145", "155" };
        foreach (var cp in checkpoints)
        {
            var t = CreateTime(schema, cp);
            var pre = graph.CreateSnapshot(t);
            var post = loaded.CreateSnapshot(t);

            Assert.Equal(pre.Entities.Count, post.Entities.Count);
            Assert.Equal(pre.Relationships.Count, post.Relationships.Count);

            foreach (var kvp in pre.Entities)
            {
                Assert.True(post.Entities.TryGetValue(kvp.Key, out var postE));
                Assert.Equal(kvp.Value.Properties.Count, postE!.Properties.Count);
            }

            foreach (var kvp in pre.Relationships)
            {
                Assert.True(post.Relationships.TryGetValue(kvp.Key, out var postR));
                Assert.Equal(kvp.Value.Type, postR!.Type);
                Assert.Equal(kvp.Value.Properties.Count, postR.Properties.Count);
            }
        }
    }
}
