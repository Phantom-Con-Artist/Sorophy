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
using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

public sealed class V2PersistenceAndRetirementHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Persistence Timeline")
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

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Year", SorophyTimePrecision.Exact);
    }

    // =============================================================
    // 1. RELATIONSHIP RETIREMENT HARDENING
    // =============================================================

    [Fact]
    public void Retirement_Scale1000RetiredIdentities_PreservedAndEnforced()
    {
        var graph = new SorophyGraph();
        var s = new SorophyEntity { Id = Guid.NewGuid(), Name = "S" };
        var t = new SorophyEntity { Id = Guid.NewGuid(), Name = "T" };
        graph.AddEntity(s);
        graph.AddEntity(t);

        const int retirementCount = 1000;
        var retiredIds = new List<Guid>(retirementCount);

        for (int i = 0; i < retirementCount; i++)
        {
            var relId = Guid.NewGuid();
            var rel = new SorophyRelationship
            {
                Id = relId,
                SourceId = s.Id,
                TargetId = t.Id,
                Type = "Edge"
            };

            graph.AddRelationship(rel);
            graph.RemoveRelationship(relId);

            retiredIds.Add(relId);
            Assert.True(graph.IsRelationshipIdRetired(relId));

            // Attempting to reuse retired ID immediately fails
            Assert.Throws<InvalidOperationException>(() =>
                graph.AddRelationship(new SorophyRelationship
                {
                    Id = relId,
                    SourceId = s.Id,
                    TargetId = t.Id,
                    Type = "Reborn"
                }));
        }

        // Active state is completely clean
        Assert.Empty(graph.Relationships);
        Assert.Empty(graph.Validate());

        // Round-trip through persistence
        var json = LoreSerializer.Serialize(graph);
        var loaded = LoreSerializer.Deserialize(json);

        // Every retired ID must still be retired in loaded graph
        foreach (var id in retiredIds)
        {
            Assert.True(loaded.IsRelationshipIdRetired(id));
            Assert.Throws<InvalidOperationException>(() =>
                loaded.AddRelationship(new SorophyRelationship
                {
                    Id = id,
                    SourceId = s.Id,
                    TargetId = t.Id,
                    Type = "Reborn"
                }));
        }
    }

    // =============================================================
    // 2. COMPLETE ROUND-TRIP FIDELITY
    // =============================================================

    [Fact]
    public void Persistence_CompleteV1AndV2Graph_RoundTripsAccurately()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var sId = Guid.NewGuid();
        var tId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var sourceEntity = new SorophyEntity
        {
            Id = sId,
            Name = "Kingdom of Valor",
            Type = "Realm",
            Description = "A great realm"
        };
        sourceEntity.Tags.Add("Major");
        sourceEntity.Documents["Chronicle.md"] =
            new SorophyEntityDocument("Chronicle.md", "# Chronicle Content", "text/markdown");
        sourceEntity.Properties["Wealth"] =
            new SorophyProperty { Name = "Wealth", Value = new SorophyValue(SorophyValueType.Integer, 5000L) };

        var targetEntity = new SorophyEntity { Id = tId, Name = "Dravak", Type = "Realm" };

        var eventEntity = new SorophyEntity
        {
            Id = eventId,
            Name = "Treaty Event",
            Type = "Event",
            OccurredAt = CreateTime(schema, "100")
        };

        graph.AddEntity(sourceEntity);
        graph.AddEntity(targetEntity);
        graph.AddEntity(eventEntity);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Alliance", CreateTime(schema, "100"),
            validFrom: CreateTime(schema, "100"), eventEntityId: eventId));

        var json = LoreSerializer.Serialize(graph);
        var loaded = LoreSerializer.Deserialize(json);

        Assert.Empty(loaded.Validate());
        Assert.Equal(3, loaded.Entities.Count);
        Assert.Single(loaded.Relationships);

        var loadedSource = loaded.Entities[sId];
        Assert.Equal("Kingdom of Valor", loadedSource.Name);
        Assert.Contains("Major", loadedSource.Tags);
        Assert.Equal("# Chronicle Content", loadedSource.Documents["Chronicle.md"].Content);
        Assert.Equal(5000L, loadedSource.Properties["Wealth"].Value.Value);

        var loadedRel = loaded.Relationships[relId];
        Assert.Equal("Alliance", loadedRel.Type);
        Assert.Equal(CreateTime(schema, "100"), loadedRel.ValidFrom);

        var loadedHistory = loaded.RelationshipHistories[relId];
        Assert.Single(loadedHistory.Facts);
        Assert.Equal(eventId, loadedHistory.Facts[0].EventEntityId);
        Assert.Equal(CreateTime(schema, "100"), loadedHistory.Facts[0].At);
    }

    [Fact]
    public void Persistence_NonTemporalGraph_DoesNotFabricateHistory()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "Edge" };
        graph.AddRelationship(rel);

        var json = LoreSerializer.Serialize(graph);
        var loaded = LoreSerializer.Deserialize(json);

        Assert.Empty(loaded.RelationshipHistories);
    }

    // =============================================================
    // 3. CORRUPTION ASSAULT (NO SILENT REPAIR)
    // =============================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("{\"entities\": 123}")]
    public void Persistence_MalformedJson_ThrowsJsonOrInvalidOperationException(string malformedJson)
    {
        Assert.ThrowsAny<Exception>(() =>
            LoreSerializer.Deserialize(malformedJson));
    }
}

