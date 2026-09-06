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
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

public sealed class SerializationByteContractHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "MainTimeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Era", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Named)),
                new SorophyTimeUnit("Year", 1, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static readonly string SnapshotDir =
        System.IO.Path.Combine(AppContext.BaseDirectory, "ByteContractSnapshots");

    private static void AssertByteContract(
        SorophyGraph graph,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var firstJson = LoreSerializer.Serialize(graph);
        var secondJson = LoreSerializer.Serialize(graph);

        // 1. Deterministic byte identity
        Assert.True(
            string.Equals(firstJson, secondJson, StringComparison.Ordinal),
            "Serialization must produce 100% byte-for-byte identical output on repeated calls.");

        // 2. Canonical round-trip byte identity
        var restored = LoreSerializer.Deserialize(firstJson);
        var restoredJson = LoreSerializer.Serialize(restored);

        Assert.True(
            string.Equals(firstJson, restoredJson, StringComparison.Ordinal),
            "Re-serialization of a restored graph must produce 100% byte-for-byte identical output.");

        // 3. Pre-optimization golden snapshot byte comparison
        System.IO.Directory.CreateDirectory(SnapshotDir);
        var snapshotPath = System.IO.Path.Combine(SnapshotDir, $"{testName}.json");
        if (!System.IO.File.Exists(snapshotPath))
        {
            System.IO.File.WriteAllText(snapshotPath, firstJson, Encoding.UTF8);
        }
        else
        {
            var goldenJson = System.IO.File.ReadAllText(snapshotPath, Encoding.UTF8);
            Assert.True(
                string.Equals(goldenJson, firstJson, StringComparison.Ordinal),
                $"Serialized output for {testName} diverged from pre-optimization golden snapshot!");
        }

        // 4. Graph validation
        Assert.Empty(restored.Validate());
    }

    [Fact]
    public void ByteContract_01_EmptyGraph()
    {
        var graph = new SorophyGraph();
        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_02_MinimalGraph()
    {
        var graph = new SorophyGraph();
        graph.AddEntity(new SorophyEntity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Node1",
            Type = "TypeA"
        });
        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_03_EntitiesWithZeroOneManyTags()
    {
        var graph = new SorophyGraph();
        var e0 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000000"), Name = "ZeroTags" };
        var e1 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000000"), Name = "OneTag" };
        e1.Tags.Add("Alpha");
        var eM = new SorophyEntity { Id = Guid.Parse("30000000-0000-0000-0000-000000000000"), Name = "ManyTags" };
        eM.Tags.Add("Zeta");
        eM.Tags.Add("Beta");
        eM.Tags.Add("Gamma");

        graph.AddEntity(e0);
        graph.AddEntity(e1);
        graph.AddEntity(eM);

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_04_EntitiesWithZeroOneManyDocuments()
    {
        var graph = new SorophyGraph();
        var e0 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "ZeroDocs" };
        var e1 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "OneDoc" };
        e1.Documents["doc1.txt"] = new SorophyEntityDocument("doc1.txt", "content1", "text/plain");
        var eM = new SorophyEntity { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Name = "ManyDocs" };
        eM.Documents["z.md"] = new SorophyEntityDocument("z.md", "# Z", "text/markdown");
        eM.Documents["a.md"] = new SorophyEntityDocument("a.md", "# A", "text/markdown");

        graph.AddEntity(e0);
        graph.AddEntity(e1);
        graph.AddEntity(eM);

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_05_EntitiesWithZeroOneManyProperties()
    {
        var graph = new SorophyGraph();
        var e0 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "ZeroProps" };
        var e1 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "OneProp" };
        e1.Properties["prop1"] = new SorophyProperty { Name = "prop1", Value = new SorophyValue(SorophyValueType.Integer, 42L) };
        var eM = new SorophyEntity { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), Name = "ManyProps" };
        eM.Properties["zeta"] = new SorophyProperty { Name = "zeta", Value = new SorophyValue(SorophyValueType.String, "z") };
        eM.Properties["alpha"] = new SorophyProperty { Name = "alpha", Value = new SorophyValue(SorophyValueType.Boolean, true) };

        graph.AddEntity(e0);
        graph.AddEntity(e1);
        graph.AddEntity(eM);

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_06_Relationships()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "E1" };
        var e2 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);
        graph.AddRelationship(new SorophyRelationship { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), SourceId = e1.Id, TargetId = e2.Id, Type = "relB" });
        graph.AddRelationship(new SorophyRelationship { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), SourceId = e1.Id, TargetId = e2.Id, Type = "relA" });

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_07_RelationshipHistories()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "E1" };
        var e2 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var relId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        var history = graph.GetOrCreateRelationshipHistory(relId);
        var time1 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);
        var time2 = new SorophyTime(schema, "200", "Year", SorophyTimePrecision.Exact);

        history.Add(new SorophyRelationshipFact(time1, relId, e1.Id, e2.Id, "allied", new Dictionary<string, SorophyProperty>()));
        history.Add(new SorophyRelationshipFact(time2, relId, e1.Id, e2.Id, "rival", new Dictionary<string, SorophyProperty>()));

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_08_TemporalValidity()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "E1" };
        var e2 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var tFrom = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var tTill = new SorophyTime(schema, "50", "Year", SorophyTimePrecision.Exact);
        graph.AddRelationship(new SorophyRelationship
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            SourceId = e1.Id,
            TargetId = e2.Id,
            Type = "treaty",
            ValidFrom = tFrom,
            ValidTill = tTill
        });

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_09_EventProvenance()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "E1" };
        var e2 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Name = "E2" };
        var ev = new SorophyEntity { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Name = "Coronation", Type = "Event" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);
        graph.AddEntity(ev);

        var relId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var history = graph.GetOrCreateRelationshipHistory(relId);
        var time1 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);
        history.Add(new SorophyRelationshipFact(time1, relId, e1.Id, e2.Id, "coronated", new Dictionary<string, SorophyProperty>(), eventEntityId: ev.Id));

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_10_RetiredRelationshipIds()
    {
        var graph = new SorophyGraph();
        graph.RetireRelationshipId(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        graph.RetireRelationshipId(Guid.Parse("eeeeeeee-1111-1111-1111-eeeeeeeeeeee"));

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_11_RichEntity()
    {
        var entity = new SorophyEntity
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name = "RichSubject",
            Type = "Subject",
            Description = "Desc"
        };
        entity.Tags.Add("T1");
        entity.Documents["doc.txt"] = new SorophyEntityDocument("doc.txt", "doc content", "text/plain");
        entity.Properties["int"] = new SorophyProperty { Name = "int", Value = new SorophyValue(SorophyValueType.Integer, 123456L) };
        entity.Properties["str"] = new SorophyProperty { Name = "str", Value = new SorophyValue(SorophyValueType.String, "hello world") };
        entity.Properties["bool"] = new SorophyProperty { Name = "bool", Value = new SorophyValue(SorophyValueType.Boolean, true) };
        entity.Properties["guid"] = new SorophyProperty { Name = "guid", Value = new SorophyValue(SorophyValueType.Guid, Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")) };
        entity.Properties["decimal"] = new SorophyProperty { Name = "decimal", Value = new SorophyValue(SorophyValueType.Decimal, 123.456m) };

        var firstJson = EntitySerializer.Serialize(entity);
        var secondJson = EntitySerializer.Serialize(entity);
        Assert.Equal(firstJson, secondJson);

        var restored = EntitySerializer.Deserialize(firstJson);
        var restoredJson = EntitySerializer.Serialize(restored);
        Assert.Equal(firstJson, restoredJson);

        System.IO.Directory.CreateDirectory(SnapshotDir);
        var snapshotPath = System.IO.Path.Combine(SnapshotDir, "ByteContract_11_RichEntity.json");
        if (!System.IO.File.Exists(snapshotPath))
        {
            System.IO.File.WriteAllText(snapshotPath, firstJson, Encoding.UTF8);
        }
        else
        {
            var goldenJson = System.IO.File.ReadAllText(snapshotPath, Encoding.UTF8);
            Assert.True(
                string.Equals(goldenJson, firstJson, StringComparison.Ordinal),
                "Serialized output for ByteContract_11_RichEntity diverged from pre-optimization golden snapshot!");
        }
    }

    [Fact]
    public void ByteContract_12_MixedGraph()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Name = "Hero", Type = "Character", Description = "The protagonist" };
        e1.Tags.Add("Protagonist");
        e1.Properties["Level"] = new SorophyProperty { Name = "Level", Value = new SorophyValue(SorophyValueType.Integer, 99L) };

        var e2 = new SorophyEntity { Id = Guid.Parse("20000000-0000-0000-0000-000000000010"), Name = "Castle", Type = "Location" };
        var ev = new SorophyEntity { Id = Guid.Parse("30000000-0000-0000-0000-000000000010"), Name = "Siege", Type = "Event" };

        graph.AddEntity(e1);
        graph.AddEntity(e2);
        graph.AddEntity(ev);

        var r1 = new SorophyRelationship { Id = Guid.Parse("40000000-0000-0000-0000-000000000010"), SourceId = e1.Id, TargetId = e2.Id, Type = "resides_at" };
        graph.AddRelationship(r1);

        graph.RetireRelationshipId(Guid.Parse("50000000-0000-0000-0000-000000000010"));

        var h = graph.GetOrCreateRelationshipHistory(r1.Id);
        var t = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        h.Add(new SorophyRelationshipFact(t, r1.Id, e1.Id, e2.Id, "resides_at", new Dictionary<string, SorophyProperty>(), eventEntityId: ev.Id));

        AssertByteContract(graph);
    }

    [Fact]
    public void ByteContract_13_LargeDeterministicGraph()
    {
        var graph = new SorophyGraph();
        for (int i = 0; i < 200; i++)
        {
            var id = Guid.Parse($"00000000-0000-0000-0000-{(i + 1):D12}");
            var e = new SorophyEntity { Id = id, Name = $"Entity_{i}", Type = "Type" };
            if (i % 2 == 0) e.Tags.Add($"Tag_{i % 5}");
            if (i % 5 == 0) e.Properties["key"] = new SorophyProperty { Name = "key", Value = new SorophyValue(SorophyValueType.Integer, (long)i) };
            graph.AddEntity(e);
        }
        for (int i = 0; i < 199; i++)
        {
            var rId = Guid.Parse($"11111111-0000-0000-0000-{(i + 1):D12}");
            var src = Guid.Parse($"00000000-0000-0000-0000-{(i + 1):D12}");
            var tgt = Guid.Parse($"00000000-0000-0000-0000-{(i + 2):D12}");
            graph.AddRelationship(new SorophyRelationship { Id = rId, SourceId = src, TargetId = tgt, Type = "link" });
        }

        AssertByteContract(graph);
    }
}
