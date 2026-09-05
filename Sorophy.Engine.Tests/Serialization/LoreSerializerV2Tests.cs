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
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

public sealed class LoreSerializerV2Tests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimePositionDefinition NamedPosition =>
        new(SorophyTimePositionKind.Named);

    private static SorophyTimeSchema CreateSchema(
        string timeline = "Era of Black Pig")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Era",
                    0,
                    NamedPosition),

                new SorophyTimeUnit(
                    "Year",
                    1,
                    NumericPosition),

                new SorophyTimeUnit(
                    "Day",
                    2,
                    NumericPosition)
            });
    }

    private static SorophyTime CreateTime(
        SorophyTimeSchema schema,
        string position,
        string unitName = "Year",
        SorophyTimePrecision precision = SorophyTimePrecision.Exact)
    {
        return new SorophyTime(
            schema,
            position,
            unitName,
            precision);
    }

    private static SorophyProperty CreateStringProperty(
        string name,
        string value)
    {
        return new SorophyProperty
        {
            Name = name,
            Value = new SorophyValue(
                SorophyValueType.String,
                value)
        };
    }

    private static SorophyProperty CreateIntegerProperty(
        string name,
        long value)
    {
        return new SorophyProperty
        {
            Name = name,
            Value = new SorophyValue(
                SorophyValueType.Integer,
                value)
        };
    }

    // =========================================================================
    // 1. BASIC V2 SERIALIZATION
    // =========================================================================

    [Fact]
    public void Serialize_EmptyGraph_ShouldEmitFormatVersion2AndEmptyCollections()
    {
        var graph = new SorophyGraph();

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"formatVersion\": 2", json);
        Assert.Contains("\"entities\": []", json);
        Assert.Contains("\"relationships\": []", json);
        Assert.Contains("\"relationshipHistories\": []", json);
        Assert.Contains("\"retiredRelationshipIds\": []", json);

        var deserialized = LoreSerializer.Deserialize(json);
        Assert.Empty(deserialized.Entities);
        Assert.Empty(deserialized.Relationships);
        Assert.Empty(deserialized.RelationshipHistories);
        Assert.Empty(deserialized.RetiredRelationshipIds);
    }

    [Fact]
    public void Serialize_PopulatedGraph_ShouldEmitFormatVersion2()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "Avaria", Type = "Kingdom" };
        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"formatVersion\": 2", json);
        Assert.Contains(entity.Id.ToString(), json);
    }

    // =========================================================================
    // 2. ENTITY 2.0 ROUNDTRIP & DETERMINISTIC ORDERING
    // =========================================================================

    [Fact]
    public void Roundtrip_Entity20_ShouldPreserveDescriptionTagsDocumentsProperties()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom",
            Description = "A sprawling empire situated on the coast."
        };

        entity.Tags.Add("Capital");
        entity.Tags.Add("Coastal");
        entity.Tags.Add("Imperial");

        entity.Documents["Lore.md"] = new SorophyEntityDocument(
            "Lore.md",
            "# Historical Lore\nDetailed backstory.",
            "text/markdown");
        entity.Documents["Summary.txt"] = new SorophyEntityDocument(
            "Summary.txt",
            "Short summary.",
            "text/plain");

        entity.Properties["Population"] = CreateIntegerProperty("Population", 1000000);
        entity.Properties["Motto"] = CreateStringProperty("Motto", "Strength in Unity");

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        Assert.True(deserialized.TryGetEntity(entity.Id, out var restored));
        Assert.NotNull(restored);
        Assert.Equal(entity.Name, restored!.Name);
        Assert.Equal(entity.Type, restored.Type);
        Assert.Equal(entity.Description, restored.Description);

        Assert.Equal(3, restored.Tags.Count);
        Assert.Contains("Capital", restored.Tags);
        Assert.Contains("Coastal", restored.Tags);
        Assert.Contains("Imperial", restored.Tags);

        Assert.Equal(2, restored.Documents.Count);
        Assert.Equal("# Historical Lore\nDetailed backstory.", restored.Documents["Lore.md"].Content);
        Assert.Equal("text/markdown", restored.Documents["Lore.md"].ContentType);
        Assert.Equal("Short summary.", restored.Documents["Summary.txt"].Content);
        Assert.Equal("text/plain", restored.Documents["Summary.txt"].ContentType);

        Assert.Equal(1000000L, restored.Properties["Population"].Value.Value);
        Assert.Equal("Strength in Unity", restored.Properties["Motto"].Value.Value);
    }

    [Fact]
    public void Serialize_Entity20_ShouldOrderTagsAndDocumentsDeterministically()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "SortedEntity" };

        entity.Tags.Add("Zeta");
        entity.Tags.Add("Alpha");
        entity.Tags.Add("Gamma");
        entity.Tags.Add("Beta");

        entity.Documents["zeta.txt"] = new SorophyEntityDocument("zeta.txt", "content", "text/plain");
        entity.Documents["alpha.txt"] = new SorophyEntityDocument("alpha.txt", "content", "text/plain");

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);

        var alphaTagIndex = json.IndexOf("\"Alpha\"", StringComparison.Ordinal);
        var betaTagIndex = json.IndexOf("\"Beta\"", StringComparison.Ordinal);
        var gammaTagIndex = json.IndexOf("\"Gamma\"", StringComparison.Ordinal);
        var zetaTagIndex = json.IndexOf("\"Zeta\"", StringComparison.Ordinal);

        Assert.True(alphaTagIndex < betaTagIndex);
        Assert.True(betaTagIndex < gammaTagIndex);
        Assert.True(gammaTagIndex < zetaTagIndex);

        var alphaDocIndex = json.IndexOf("\"alpha.txt\"", StringComparison.Ordinal);
        var zetaDocIndex = json.IndexOf("\"zeta.txt\"", StringComparison.Ordinal);
        Assert.True(alphaDocIndex < zetaDocIndex);
    }

    // =========================================================================
    // 3. ACTIVE RELATIONSHIPS
    // =========================================================================

    [Fact]
    public void Roundtrip_ActiveRelationships_ShouldPreservePropertiesAndTemporalBounds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Aran" };
        var target = new SorophyEntity { Name = "Lyra" };
        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            SourceId = source.Id,
            TargetId = target.Id,
            ValidFrom = CreateTime(schema, "100"),
            ValidTill = CreateTime(schema, "150")
        };
        relationship.Properties["Ceremony"] = CreateStringProperty("Ceremony", "Cathedral");

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        Assert.True(deserialized.TryGetRelationship(relationship.Id, out var restored));
        Assert.NotNull(restored);
        Assert.Equal("married_to", restored!.Type);
        Assert.Equal(source.Id, restored.SourceId);
        Assert.Equal(target.Id, restored.TargetId);
        Assert.NotNull(restored.ValidFrom);
        Assert.Equal("100", restored.ValidFrom!.Position);
        Assert.NotNull(restored.ValidTill);
        Assert.Equal("150", restored.ValidTill!.Position);
        Assert.Equal("Cathedral", restored.Properties["Ceremony"].Value.Value);
    }

    // =========================================================================
    // 4. RELATIONSHIP HISTORY
    // =========================================================================

    [Fact]
    public void Roundtrip_EmptyRelationshipHistory_ShouldPreserveHistory()
    {
        var graph = new SorophyGraph();
        var relId = Guid.NewGuid();
        _ = graph.GetOrCreateRelationshipHistory(relId);

        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        Assert.True(deserialized.TryGetRelationshipHistory(relId, out var history));
        Assert.NotNull(history);
        Assert.Empty(history!);
    }

    [Fact]
    public void Roundtrip_RelationshipHistory_ShouldPreserveFactsInInsertionOrder()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var history = graph.GetOrCreateRelationshipHistory(relationshipId);

        // Intentionally insert facts in non-chronological order
        var fact1 = new SorophyRelationshipFact(
            CreateTime(schema, "2000"),
            relationshipId,
            sourceId,
            targetId,
            "friend");

        var fact2 = new SorophyRelationshipFact(
            CreateTime(schema, "1990"),
            relationshipId,
            sourceId,
            targetId,
            "rival");

        var fact3 = new SorophyRelationshipFact(
            CreateTime(schema, "2010"),
            relationshipId,
            sourceId,
            targetId,
            "ally");

        history.Add(fact1);
        history.Add(fact2);
        history.Add(fact3);

        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        Assert.True(deserialized.TryGetRelationshipHistory(relationshipId, out var restoredHistory));
        Assert.NotNull(restoredHistory);
        Assert.Equal(3, restoredHistory!.Count);

        // Must preserve exact insertion order, NOT sorted by At!
        Assert.Equal("friend", restoredHistory.Facts[0].Type);
        Assert.Equal("2000", restoredHistory.Facts[0].At.Position);

        Assert.Equal("rival", restoredHistory.Facts[1].Type);
        Assert.Equal("1990", restoredHistory.Facts[1].At.Position);

        Assert.Equal("ally", restoredHistory.Facts[2].Type);
        Assert.Equal("2010", restoredHistory.Facts[2].At.Position);
    }

    [Fact]
    public void Roundtrip_RelationshipHistory_ShouldPreserveFactPropertiesAndTemporalBounds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var relId = Guid.NewGuid();
        var srcId = Guid.NewGuid();
        var tgtId = Guid.NewGuid();

        var properties = new Dictionary<string, SorophyProperty>
        {
            ["Tier"] = CreateIntegerProperty("Tier", 3)
        };

        var fact = new SorophyRelationshipFact(
            CreateTime(schema, "100"),
            relId,
            srcId,
            tgtId,
            "contract",
            properties,
            validFrom: CreateTime(schema, "80"),
            validTill: CreateTime(schema, "120"));

        graph.RecordRelationshipFact(fact);

        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        Assert.True(deserialized.TryGetRelationshipHistory(relId, out var restoredHistory));
        Assert.NotNull(restoredHistory);
        var restoredFact = restoredHistory!.Facts.Single();

        Assert.Equal("100", restoredFact.At.Position);
        Assert.Equal(relId, restoredFact.RelationshipId);
        Assert.Equal(srcId, restoredFact.SourceId);
        Assert.Equal(tgtId, restoredFact.TargetId);
        Assert.Equal("contract", restoredFact.Type);
        Assert.Equal("80", restoredFact.ValidFrom?.Position);
        Assert.Equal("120", restoredFact.ValidTill?.Position);
        Assert.Equal(3L, restoredFact.Properties["Tier"].Value.Value);
    }

    // =========================================================================
    // 5. LIFECYCLE & TERMINATION
    // =========================================================================

    [Fact]
    public void Roundtrip_TerminatedRelationship_ShouldBeAbsentFromActive_RetainHistory_RetainRetiredId()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Alpha" };
        var target = new SorophyEntity { Name = "Beta" };
        graph.AddEntity(source);
        graph.AddEntity(target);

        var relId = Guid.NewGuid();
        var rel = new SorophyRelationship
        {
            Id = relId,
            Type = "Alliance",
            SourceId = source.Id,
            TargetId = target.Id,
            ValidFrom = CreateTime(schema, "10")
        };
        graph.AddRelationship(rel);

        var terminateTime = CreateTime(schema, "50");
        var termination = new SorophyRelationshipTermination(relId, terminateTime);
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, termination);

        // Prior to serialization
        Assert.False(graph.TryGetRelationship(relId, out _));
        Assert.True(graph.IsRelationshipIdRetired(relId));
        Assert.True(graph.TryGetRelationshipHistory(relId, out var originalHistory));
        Assert.Single(originalHistory!.Facts);

        // Serialize and deserialize
        var json = LoreSerializer.Serialize(graph);
        var deserialized = LoreSerializer.Deserialize(json);

        // Active relationship is absent
        Assert.False(deserialized.TryGetRelationship(relId, out _));

        // Retired ID is retained
        Assert.True(deserialized.IsRelationshipIdRetired(relId));

        // Historical facts are retained
        Assert.True(deserialized.TryGetRelationshipHistory(relId, out var restoredHistory));
        Assert.NotNull(restoredHistory);
        Assert.Single(restoredHistory!.Facts);
        Assert.Equal("Alliance", restoredHistory.Facts[0].Type);

        // Reusing retired relationship ID must be rejected by the graph
        var reuseAttempt = new SorophyRelationship
        {
            Id = relId,
            Type = "NewType",
            SourceId = source.Id,
            TargetId = target.Id
        };
        Assert.Throws<InvalidOperationException>(() => deserialized.AddRelationship(reuseAttempt));
    }

    // =========================================================================
    // 6. CRITICAL NEGATIVE SEMANTIC TESTS
    // =========================================================================

    [Fact]
    public void OrdinaryEntityModification_DoesNotCreateHistoryOrRetirement()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "Entity1" };
        graph.AddEntity(entity);

        entity.Description = "Updated description";
        entity.Properties["Key"] = CreateStringProperty("Key", "Value");

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"relationshipHistories\": []", json);
        Assert.Contains("\"retiredRelationshipIds\": []", json);
        Assert.Empty(graph.RelationshipHistories);
        Assert.Empty(graph.RetiredRelationshipIds);
    }

    [Fact]
    public void OrdinaryRelationshipAddition_DoesNotCreateHistoryOrRetirement()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship
        {
            Type = "link",
            SourceId = e1.Id,
            TargetId = e2.Id
        };
        graph.AddRelationship(rel);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"relationshipHistories\": []", json);
        Assert.Contains("\"retiredRelationshipIds\": []", json);
        Assert.Empty(graph.RelationshipHistories);
        Assert.Empty(graph.RetiredRelationshipIds);
    }

    [Fact]
    public void OrdinaryRelationshipRemoval_DoesNotCreateHistory()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship
        {
            Type = "link",
            SourceId = e1.Id,
            TargetId = e2.Id
        };
        graph.AddRelationship(rel);

        // Ordinary removal directly via graph API
        graph.RemoveRelationship(rel.Id);

        // Ordinary removal does NOT synthesize history facts
        Assert.Empty(graph.RelationshipHistories);

        var json = LoreSerializer.Serialize(graph);
        Assert.Contains("\"relationshipHistories\": []", json);
    }

    [Fact]
    public void EditingRelationshipProperties_DoesNotCreateHistory()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship
        {
            Type = "link",
            SourceId = e1.Id,
            TargetId = e2.Id
        };
        rel.Properties["Strength"] = CreateIntegerProperty("Strength", 1);
        graph.AddRelationship(rel);

        // Direct property modification without evolution executor
        rel.Properties["Strength"] = CreateIntegerProperty("Strength", 2);

        Assert.Empty(graph.RelationshipHistories);

        var json = LoreSerializer.Serialize(graph);
        Assert.Contains("\"relationshipHistories\": []", json);
    }

    // =========================================================================
    // 7. INTEGRITY & ADVERSARIAL VALIDATION
    // =========================================================================

    [Fact]
    public void Deserialize_ShouldRejectDuplicateHistoryId()
    {
        var relId = Guid.NewGuid();
        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [
            { "relationshipId": "{{relId}}", "facts": [] },
            { "relationshipId": "{{relId}}", "facts": [] }
          ],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectFactWithMismatchedRelationshipId()
    {
        var schema = CreateSchema();
        var historyId = Guid.NewGuid();
        var differentFactId = Guid.NewGuid();
        var srcId = Guid.NewGuid();
        var tgtId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [
            {
              "relationshipId": "{{historyId}}",
              "facts": [
                {
                  "at": {
                    "schema": {
                      "timeline": "Era of Black Pig",
                      "units": [
                        { "name": "Year", "order": 0, "positionDefinition": { "kind": "Numeric" } }
                      ]
                    },
                    "position": "100",
                    "unit": "Year",
                    "precision": "Exact"
                  },
                  "relationshipId": "{{differentFactId}}",
                  "sourceId": "{{srcId}}",
                  "targetId": "{{tgtId}}",
                  "type": "ally",
                  "properties": {}
                }
              ]
            }
          ],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectFactMissingAt()
    {
        var relId = Guid.NewGuid();
        var srcId = Guid.NewGuid();
        var tgtId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [
            {
              "relationshipId": "{{relId}}",
              "facts": [
                {
                  "at": null,
                  "relationshipId": "{{relId}}",
                  "sourceId": "{{srcId}}",
                  "targetId": "{{tgtId}}",
                  "type": "ally",
                  "properties": {}
                }
              ]
            }
          ],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectFactWithEmptyEndpoints()
    {
        var relId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [
            {
              "relationshipId": "{{relId}}",
              "facts": [
                {
                  "at": {
                    "schema": {
                      "timeline": "Era of Black Pig",
                      "units": [
                        { "name": "Year", "order": 0, "positionDefinition": { "kind": "Numeric" } }
                      ]
                    },
                    "position": "100",
                    "unit": "Year",
                    "precision": "Exact"
                  },
                  "relationshipId": "{{relId}}",
                  "sourceId": "{{Guid.Empty}}",
                  "targetId": "{{Guid.NewGuid()}}",
                  "type": "ally",
                  "properties": {}
                }
              ]
            }
          ],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectFactWithInconsistentTemporalSchemas()
    {
        var relId = Guid.NewGuid();
        var srcId = Guid.NewGuid();
        var tgtId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [
            {
              "relationshipId": "{{relId}}",
              "facts": [
                {
                  "at": {
                    "schema": {
                      "timeline": "Timeline A",
                      "units": [
                        { "name": "Year", "order": 0, "positionDefinition": { "kind": "Numeric" } }
                      ]
                    },
                    "position": "100",
                    "unit": "Year",
                    "precision": "Exact"
                  },
                  "validFrom": {
                    "schema": {
                      "timeline": "Timeline B",
                      "units": [
                        { "name": "Year", "order": 0, "positionDefinition": { "kind": "Numeric" } }
                      ]
                    },
                    "position": "50",
                    "unit": "Year",
                    "precision": "Exact"
                  },
                  "relationshipId": "{{relId}}",
                  "sourceId": "{{srcId}}",
                  "targetId": "{{tgtId}}",
                  "type": "ally",
                  "properties": {}
                }
              ]
            }
          ],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectRelationshipActiveAndRetiredSimultaneously()
    {
        var entity1Id = Guid.NewGuid();
        var entity2Id = Guid.NewGuid();
        var relId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [
            { "id": "{{entity1Id}}", "name": "E1", "type": "T", "tags": [], "documents": {}, "properties": {} },
            { "id": "{{entity2Id}}", "name": "E2", "type": "T", "tags": [], "documents": {}, "properties": {} }
          ],
          "relationships": [
            {
              "id": "{{relId}}",
              "type": "link",
              "sourceId": "{{entity1Id}}",
              "targetId": "{{entity2Id}}",
              "properties": {}
            }
          ],
          "relationshipHistories": [],
          "retiredRelationshipIds": [
            "{{relId}}"
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectDuplicateRetiredRelationshipId()
    {
        var relId = Guid.NewGuid();
        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": [],
          "retiredRelationshipIds": [
            "{{relId}}",
            "{{relId}}"
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectV2MissingRelationshipHistories()
    {
        var json = """
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectV2MissingRetiredRelationshipIds()
    {
        var json = """
        {
          "formatVersion": 2,
          "entities": [],
          "relationships": [],
          "relationshipHistories": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectV2EntityMissingTags()
    {
        var entityId = Guid.NewGuid();
        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "E1",
              "documents": {},
              "properties": {}
            }
          ],
          "relationships": [],
          "relationshipHistories": [],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectV2EntityMissingDocuments()
    {
        var entityId = Guid.NewGuid();
        var json = $$"""
        {
          "formatVersion": 2,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "E1",
              "tags": [],
              "properties": {}
            }
          ],
          "relationships": [],
          "relationshipHistories": [],
          "retiredRelationshipIds": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    // =========================================================================
    // 8. FORMAT V1 BACKWARD COMPATIBILITY
    // =========================================================================

    [Fact]
    public void Deserialize_V1Json_ShouldSucceedAndDefaultV2Fields()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relId = Guid.NewGuid();

        var v1Json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Kingdom A",
              "type": "Realm",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Kingdom B",
              "type": "Realm",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relId}}",
              "type": "trade_pact",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph = LoreSerializer.Deserialize(v1Json);

        Assert.Equal(2, graph.Entities.Count);
        Assert.Single(graph.Relationships);
        Assert.Empty(graph.RelationshipHistories);
        Assert.Empty(graph.RetiredRelationshipIds);

        var entityA = graph.Entities[sourceId];
        Assert.Null(entityA.Description);
        Assert.Empty(entityA.Tags);
        Assert.Empty(entityA.Documents);
    }

    [Fact]
    public void Reserialize_V1LoadedGraph_ShouldProduceValidV2Format()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relId = Guid.NewGuid();

        var v1Json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Kingdom A",
              "type": "Realm",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Kingdom B",
              "type": "Realm",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relId}}",
              "type": "trade_pact",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph = LoreSerializer.Deserialize(v1Json);
        var v2Json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"formatVersion\": 2", v2Json);
        Assert.Contains("\"relationshipHistories\": []", v2Json);
        Assert.Contains("\"retiredRelationshipIds\": []", v2Json);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public void Deserialize_UnsupportedFormatVersions_ShouldBeRejected(int unsupportedVersion)
    {
        var json = $$"""
        {
          "formatVersion": {{unsupportedVersion}},
          "entities": [],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(json));
    }

    // =========================================================================
    // 9. DETERMINISTIC SERIALIZATION
    // =========================================================================

    [Fact]
    public void Serialize_Determinism_IndependentRunsWithReorderedInserts_ShouldProduceIdenticalJson()
    {
        var schema = CreateSchema();

        var e1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var e2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var r1Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var r2Id = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var retired1 = Guid.Parse("00000000-0000-0000-0000-000000000100");
        var retired2 = Guid.Parse("00000000-0000-0000-0000-000000000200");

        // Graph 1: add in ascending order
        var g1 = new SorophyGraph();
        var g1E1 = new SorophyEntity { Id = e1Id, Name = "E1" };
        g1E1.Tags.Add("Alpha");
        g1E1.Tags.Add("Beta");
        g1E1.Documents["Doc1.md"] = new SorophyEntityDocument("Doc1.md", "c1", "text/plain");
        g1E1.Documents["Doc2.md"] = new SorophyEntityDocument("Doc2.md", "c2", "text/plain");
        g1E1.Properties["P1"] = CreateStringProperty("P1", "v1");
        g1E1.Properties["P2"] = CreateStringProperty("P2", "v2");

        var g1E2 = new SorophyEntity { Id = e2Id, Name = "E2" };
        g1.AddEntity(g1E1);
        g1.AddEntity(g1E2);

        var g1R1 = new SorophyRelationship { Id = r1Id, Type = "rel1", SourceId = e1Id, TargetId = e2Id };
        var g1R2 = new SorophyRelationship { Id = r2Id, Type = "rel2", SourceId = e1Id, TargetId = e2Id };
        g1.AddRelationship(g1R1);
        g1.AddRelationship(g1R2);

        g1.RetireRelationshipId(retired1);
        g1.RetireRelationshipId(retired2);

        // Graph 2: add in reverse order
        var g2 = new SorophyGraph();
        var g2E2 = new SorophyEntity { Id = e2Id, Name = "E2" };
        var g2E1 = new SorophyEntity { Id = e1Id, Name = "E1" };
        // Reverse tag insert
        g2E1.Tags.Add("Beta");
        g2E1.Tags.Add("Alpha");
        // Reverse document insert
        g2E1.Documents["Doc2.md"] = new SorophyEntityDocument("Doc2.md", "c2", "text/plain");
        g2E1.Documents["Doc1.md"] = new SorophyEntityDocument("Doc1.md", "c1", "text/plain");
        // Reverse property insert
        g2E1.Properties["P2"] = CreateStringProperty("P2", "v2");
        g2E1.Properties["P1"] = CreateStringProperty("P1", "v1");

        g2.AddEntity(g2E2);
        g2.AddEntity(g2E1);

        var g2R2 = new SorophyRelationship { Id = r2Id, Type = "rel2", SourceId = e1Id, TargetId = e2Id };
        var g2R1 = new SorophyRelationship { Id = r1Id, Type = "rel1", SourceId = e1Id, TargetId = e2Id };
        g2.AddRelationship(g2R2);
        g2.AddRelationship(g2R1);

        g2.RetireRelationshipId(retired2);
        g2.RetireRelationshipId(retired1);

        var json1 = LoreSerializer.Serialize(g1);
        var json2 = LoreSerializer.Serialize(g2);

        Assert.Equal(json1, json2);
    }
}

