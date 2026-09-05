using System;
using System.Linq;
using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

public sealed class LoreRelationshipTemporalSerializationTests
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

    private static SorophyGraph CreateGraph(
        SorophyRelationship relationship)
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Id = relationship.SourceId,
            Name = "Aran",
            Type = "Character"
        };

        var target = new SorophyEntity
        {
            Id = relationship.TargetId,
            Name = "Lyra",
            Type = "Character"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);
        graph.AddRelationship(relationship);

        return graph;
    }

    // -------------------------------------------------------------------------
    // Serialization
    // -------------------------------------------------------------------------

    [Fact]
    public void Serialize_ShouldIncludeValidFromAndValidTill()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "100"),
            ValidTill = CreateTime(
                schema,
                "110")
        };

        var graph = CreateGraph(relationship);

        var json = LoreSerializer.Serialize(graph);

        using var document =
            JsonDocument.Parse(json);

        var relationshipDocument =
            document.RootElement
                .GetProperty("relationships")
                .EnumerateArray()
                .Single();

        Assert.True(
            relationshipDocument.TryGetProperty(
                "validFrom",
                out var validFrom));

        Assert.True(
            relationshipDocument.TryGetProperty(
                "validTill",
                out var validTill));

        Assert.Equal(
            "100",
            validFrom.GetProperty("position").GetString());

        Assert.Equal(
            "110",
            validTill.GetProperty("position").GetString());

        Assert.Equal(
            "Year",
            validFrom.GetProperty("unit").GetString());

        Assert.Equal(
            "Year",
            validTill.GetProperty("unit").GetString());

        Assert.Equal(
            "Exact",
            validFrom.GetProperty("precision").GetString());

        Assert.Equal(
            "Exact",
            validTill.GetProperty("precision").GetString());
    }

    [Fact]
    public void Serialize_ShouldRepresentMissingTemporalBoundsAsNull()
    {
        var relationship = new SorophyRelationship
        {
            Type = "knows",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        var graph = CreateGraph(relationship);

        var json = LoreSerializer.Serialize(graph);

        using var document =
            JsonDocument.Parse(json);

        var relationshipDocument =
            document.RootElement
                .GetProperty("relationships")
                .EnumerateArray()
                .Single();

        Assert.Equal(
            JsonValueKind.Null,
            relationshipDocument
                .GetProperty("validFrom")
                .ValueKind);

        Assert.Equal(
            JsonValueKind.Null,
            relationshipDocument
                .GetProperty("validTill")
                .ValueKind);
    }

    [Fact]
    public void Serialize_ShouldPreserveTemporalSchema()
    {
        var schema = CreateSchema(
            "Asterra Calendar");

        var relationship = new SorophyRelationship
        {
            Type = "ruled_by",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "42")
        };

        var graph = CreateGraph(relationship);

        var json = LoreSerializer.Serialize(graph);

        using var document =
            JsonDocument.Parse(json);

        var validFrom =
            document.RootElement
                .GetProperty("relationships")
                .EnumerateArray()
                .Single()
                .GetProperty("validFrom");

        var serializedSchema =
            validFrom.GetProperty("schema");

        Assert.Equal(
            "Asterra Calendar",
            serializedSchema
                .GetProperty("timeline")
                .GetString());

        var units =
            serializedSchema
                .GetProperty("units")
                .EnumerateArray()
                .ToArray();

        Assert.Equal(3, units.Length);

        Assert.Equal(
            "Era",
            units[0].GetProperty("name").GetString());

        Assert.Equal(
            0,
            units[0].GetProperty("order").GetInt32());

        Assert.Equal(
            "Year",
            units[1].GetProperty("name").GetString());

        Assert.Equal(
            1,
            units[1].GetProperty("order").GetInt32());

        Assert.Equal(
            "Day",
            units[2].GetProperty("name").GetString());

        Assert.Equal(
            2,
            units[2].GetProperty("order").GetInt32());
    }

    // -------------------------------------------------------------------------
    // Round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public void SerializeDeserialize_ShouldPreserveBothTemporalBounds()
    {
        var schema = CreateSchema(
            "Asterra Calendar");

        var validFrom = CreateTime(
            schema,
            "100");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            Type = "married_to",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = validFrom,
            ValidTill = validTill
        };

        var graph = CreateGraph(relationship);

        var json =
            LoreSerializer.Serialize(graph);

        var restored =
            LoreSerializer.Deserialize(json);

        var restoredRelationship =
            restored.Relationships[relationship.Id];

        Assert.NotNull(
            restoredRelationship.ValidFrom);

        Assert.NotNull(
            restoredRelationship.ValidTill);

        Assert.Equal(
            "100",
            restoredRelationship.ValidFrom!.Position);

        Assert.Equal(
            "110",
            restoredRelationship.ValidTill!.Position);

        Assert.Equal(
            "Year",
            restoredRelationship.ValidFrom.Unit.Name);

        Assert.Equal(
            "Year",
            restoredRelationship.ValidTill.Unit.Name);

        Assert.Equal(
            SorophyTimePrecision.Exact,
            restoredRelationship.ValidFrom.Precision);

        Assert.Equal(
            SorophyTimePrecision.Exact,
            restoredRelationship.ValidTill.Precision);

        Assert.Equal(
            "Asterra Calendar",
            restoredRelationship.ValidFrom
                .Schema
                .Timeline);

        Assert.Equal(
            "Asterra Calendar",
            restoredRelationship.ValidTill
                .Schema
                .Timeline);

        Assert.Equal(
            restoredRelationship.ValidFrom.Schema,
            restoredRelationship.ValidTill.Schema);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveValidFromOnly()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "divorced_from",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "110")
        };

        var graph = CreateGraph(relationship);

        var restored =
            LoreSerializer.Deserialize(
                LoreSerializer.Serialize(graph));

        var restoredRelationship =
            restored.Relationships[relationship.Id];

        Assert.NotNull(
            restoredRelationship.ValidFrom);

        Assert.Equal(
            "110",
            restoredRelationship.ValidFrom!.Position);

        Assert.Null(
            restoredRelationship.ValidTill);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveValidTillOnly()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "existed_before",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidTill = CreateTime(
                schema,
                "110")
        };

        var graph = CreateGraph(relationship);

        var restored =
            LoreSerializer.Deserialize(
                LoreSerializer.Serialize(graph));

        var restoredRelationship =
            restored.Relationships[relationship.Id];

        Assert.Null(
            restoredRelationship.ValidFrom);

        Assert.NotNull(
            restoredRelationship.ValidTill);

        Assert.Equal(
            "110",
            restoredRelationship.ValidTill!.Position);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveApproximatePrecision()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "ruled_by",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "Third Season",
                "Era",
                SorophyTimePrecision.Approximate)
        };

        var graph = CreateGraph(relationship);

        var restored =
            LoreSerializer.Deserialize(
                LoreSerializer.Serialize(graph));

        var restoredTime =
            restored.Relationships[relationship.Id]
                .ValidFrom;

        Assert.NotNull(restoredTime);

        Assert.Equal(
            "Third Season",
            restoredTime!.Position);

        Assert.Equal(
            "Era",
            restoredTime.Unit.Name);

        Assert.Equal(
            SorophyTimePrecision.Approximate,
            restoredTime.Precision);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreservePatternPosition()
    {
        var schema =
            new SorophyTimeSchema(
                "Gregorian-Compatible",
                new[]
                {
                    new SorophyTimeUnit(
                        "Date",
                        0,
                        new SorophyTimePositionDefinition(
                            SorophyTimePositionKind.Pattern,
                            @"\d{2}-\d{2}-\d{4}"))
                });

        var relationship = new SorophyRelationship
        {
            Type = "founded_on",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = new SorophyTime(
                schema,
                "03-07-2001",
                "Date",
                SorophyTimePrecision.Exact)
        };

        var graph = CreateGraph(relationship);

        var restored =
            LoreSerializer.Deserialize(
                LoreSerializer.Serialize(graph));

        var restoredTime =
            restored.Relationships[relationship.Id]
                .ValidFrom;

        Assert.NotNull(restoredTime);

        Assert.Equal(
            "03-07-2001",
            restoredTime!.Position);

        Assert.Equal(
            SorophyTimePositionKind.Pattern,
            restoredTime.Unit
                .PositionDefinition
                .Kind);

        Assert.Equal(
            @"\d{2}-\d{2}-\d{4}",
            restoredTime.Unit
                .PositionDefinition
                .Pattern);
    }

    // -------------------------------------------------------------------------
    // Compatibility
    // -------------------------------------------------------------------------

    [Fact]
    public void Deserialize_ShouldLoadLegacyRelationshipWithoutTemporalFields()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph =
            LoreSerializer.Deserialize(json);

        var relationship =
            graph.Relationships[relationshipId];

        Assert.Equal(
            "married_to",
            relationship.Type);

        Assert.Equal(
            sourceId,
            relationship.SourceId);

        Assert.Equal(
            targetId,
            relationship.TargetId);

        Assert.Null(
            relationship.ValidFrom);

        Assert.Null(
            relationship.ValidTill);
    }

    // -------------------------------------------------------------------------
    // Invalid temporal data
    // -------------------------------------------------------------------------

    [Fact]
    public void Deserialize_ShouldRejectUnknownTemporalPrecision()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "validFrom": {
                "schema": {
                  "timeline": "Asterra",
                  "units": [
                    {
                      "name": "Year",
                      "order": 0,
                      "positionDefinition": {
                        "kind": "Numeric",
                        "pattern": null
                      }
                    }
                  ]
                },
                "position": "100",
                "unit": "Year",
                "precision": "AbsolutelyCertain"
              },
              "validTill": null,
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectInvalidTemporalPosition()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "validFrom": {
                "schema": {
                  "timeline": "Asterra",
                  "units": [
                    {
                      "name": "Year",
                      "order": 0,
                      "positionDefinition": {
                        "kind": "Numeric",
                        "pattern": null
                      }
                    }
                  ]
                },
                "position": "banana",
                "unit": "Year",
                "precision": "Exact"
              },
              "validTill": null,
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectTemporalUnitNotDefinedBySchema()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "validFrom": {
                "schema": {
                  "timeline": "Asterra",
                  "units": [
                    {
                      "name": "Year",
                      "order": 0,
                      "positionDefinition": {
                        "kind": "Numeric",
                        "pattern": null
                      }
                    }
                  ]
                },
                "position": "100",
                "unit": "Month",
                "precision": "Exact"
              },
              "validTill": null,
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectDifferentSchemasForRelationshipBounds()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "validFrom": {
                "schema": {
                  "timeline": "Asterra",
                  "units": [
                    {
                      "name": "Year",
                      "order": 0,
                      "positionDefinition": {
                        "kind": "Numeric",
                        "pattern": null
                      }
                    }
                  ]
                },
                "position": "100",
                "unit": "Year",
                "precision": "Exact"
              },
              "validTill": {
                "schema": {
                  "timeline": "Earth",
                  "units": [
                    {
                      "name": "Year",
                      "order": 0,
                      "positionDefinition": {
                        "kind": "Numeric",
                        "pattern": null
                      }
                    }
                  ]
                },
                "position": "2001",
                "unit": "Year",
                "precision": "Exact"
              },
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectMissingTemporalSchema()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Aran",
              "type": "Character",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Lyra",
              "type": "Character",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "married_to",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "validFrom": {
                "position": "100",
                "unit": "Year",
                "precision": "Exact"
              },
              "validTill": null,
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }
}