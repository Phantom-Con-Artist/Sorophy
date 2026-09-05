using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests.Serialization;

public class LoreSerializerTests
{
    [Fact]
    public void Serialize_ShouldIncludeFormatVersion()
    {
        var graph = new SorophyGraph();

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"formatVersion\": 2", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeEntities()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains(entity.Id.ToString(), json);
        Assert.Contains("\"name\": \"Avaria\"", json);
        Assert.Contains("\"type\": \"Kingdom\"", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeRelationships()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Avaria"
        };

        var target = new SorophyEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains(relationship.Id.ToString(), json);
        Assert.Contains("\"type\": \"capital_of\"", json);
        Assert.Contains(source.Id.ToString(), json);
        Assert.Contains(target.Id.ToString(), json);
    }

    [Fact]
    public void Serialize_ShouldIncludeEntityProperties()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"population\"", json);
        Assert.Contains("\"Integer\"", json);
        Assert.Contains("2400000", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeRelationshipProperties()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Avaria"
        };

        var target = new SorophyEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "ruled_by",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["since"] = new SorophyProperty
        {
            Name = "since",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                482L)
        };

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"since\"", json);
        Assert.Contains("\"Integer\"", json);
        Assert.Contains("482", json);
    }

    [Fact]
    public void Deserialize_ShouldRestoreEntities()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Avaria",
              "type": "Kingdom",
              "properties": {}
            }
          ],
          "relationships": []
        }
        """;

        var graph = LoreSerializer.Deserialize(json);

        Assert.Single(graph.Entities);

        var entity = graph.Entities[entityId];

        Assert.Equal("Avaria", entity.Name);
        Assert.Equal("Kingdom", entity.Type);
    }

    [Fact]
    public void Deserialize_ShouldRestoreRelationship()
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
              "name": "Avaria",
              "type": "Kingdom",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Valor",
              "type": "City",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "capital_of",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph = LoreSerializer.Deserialize(json);

        Assert.Single(graph.Relationships);

        var relationship =
            graph.Relationships[relationshipId];

        Assert.Equal("capital_of", relationship.Type);
        Assert.Equal(sourceId, relationship.SourceId);
        Assert.Equal(targetId, relationship.TargetId);
    }

    [Fact]
    public void SelfLink_ShouldSurviveSerialization()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        graph.AddEntity(entity);

        var relationship = new SorophyRelationship
        {
            Type = "references_itself",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);

        var restored = LoreSerializer.Deserialize(json);

        Assert.Single(restored.Relationships);

        var restoredRelationship =
            restored.Relationships[relationship.Id];

        Assert.Equal(
            restoredRelationship.SourceId,
            restoredRelationship.TargetId);

        Assert.Equal(
            entity.Id,
            restoredRelationship.SourceId);
    }

    [Fact]
    public void EmptyGraph_ShouldSurviveSerialization()
    {
        var graph = new SorophyGraph();

        var json = LoreSerializer.Serialize(graph);

        var restored = LoreSerializer.Deserialize(json);

        Assert.Empty(restored.Entities);
        Assert.Empty(restored.Relationships);
    }

    [Fact]
    public void CompleteGraph_ShouldSurviveRoundTrip()
    {
        var graph = new SorophyGraph();

        var avaria = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        avaria.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        var valor = new SorophyEntity
        {
            Name = "Valor",
            Type = "City"
        };

        valor.Properties["language"] = new SorophyProperty
        {
            Name = "language",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avarian")
        };

        graph.AddEntity(avaria);
        graph.AddEntity(valor);

        var relationship = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = avaria.Id,
            TargetId = valor.Id
        };

        relationship.Properties["since"] = new SorophyProperty
        {
            Name = "since",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                482L)
        };

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        Assert.Equal(
            graph.Entities.Count,
            restored.Entities.Count);

        Assert.Equal(
            graph.Relationships.Count,
            restored.Relationships.Count);

        var restoredAvaria =
            restored.Entities[avaria.Id];

        Assert.Equal(
            avaria.Name,
            restoredAvaria.Name);

        Assert.Equal(
            avaria.Type,
            restoredAvaria.Type);

        Assert.Equal(
            "Avarian",
            restored.Entities[valor.Id]
                .Properties["language"]
                .Value.Value);

        var restoredRelationship =
            restored.Relationships[relationship.Id];

        Assert.Equal(
            relationship.Type,
            restoredRelationship.Type);

        Assert.Equal(
            relationship.SourceId,
            restoredRelationship.SourceId);

        Assert.Equal(
            relationship.TargetId,
            restoredRelationship.TargetId);

        Assert.Equal(
            482L,
            restoredRelationship
                .Properties["since"]
                .Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRejectUnsupportedFormatVersion()
    {
        var json = """
        {
          "formatVersion": 999,
          "entities": [],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectRelationshipWithMissingSource()
    {
        var targetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{targetId}}",
              "name": "Valor",
              "type": "City",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{Guid.NewGuid()}}",
              "type": "capital_of",
              "sourceId": "{{Guid.NewGuid()}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveEntityListProperty()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Properties["tags"] = new SorophyProperty
        {
            Name = "tags",
            Value = new SorophyValue(
                SorophyValueType.List,
                new List<object?>
                {
                    "capital",
                    "coastal",
                    2400000L,
                    true,
                    null
                })
        };

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        var property =
            restored.Entities[entity.Id]
                .Properties["tags"];

        Assert.Equal(
            SorophyValueType.List,
            property.Value.Type);

        var list = Assert.IsType<List<object?>>(
            property.Value.Value);

        Assert.Equal(5, list.Count);
        Assert.Equal("capital", list[0]);
        Assert.Equal("coastal", list[1]);
        Assert.Equal(2400000L, list[2]);
        Assert.Equal(true, list[3]);
        Assert.Null(list[4]);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveRelationshipObjectProperty()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Avaria"
        };

        var target = new SorophyEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["metadata"] =
            new SorophyProperty
            {
                Name = "metadata",
                Value = new SorophyValue(
                    SorophyValueType.Object,
                    new Dictionary<string, object?>
                    {
                        ["since"] = 482L,
                        ["active"] = true,
                        ["title"] = "Royal Capital"
                    })
            };

        graph.AddRelationship(relationship);

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        var obj = Assert.IsType<Dictionary<string, object?>>(
            restored.Relationships[relationship.Id]
                .Properties["metadata"]
                .Value.Value);

        Assert.Equal(482L, obj["since"]);
        Assert.Equal(true, obj["active"]);
        Assert.Equal("Royal Capital", obj["title"]);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveNestedEntityProperty()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Properties["data"] = new SorophyProperty
        {
            Name = "data",
            Value = new SorophyValue(
                SorophyValueType.Object,
                new Dictionary<string, object?>
                {
                    ["tags"] = new List<object?>
                    {
                        "capital",
                        "coastal"
                    },
                    ["metadata"] = new Dictionary<string, object?>
                    {
                        ["population"] = 2400000L,
                        ["active"] = true
                    }
                })
        };

        graph.AddEntity(entity);

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        var obj = Assert.IsType<Dictionary<string, object?>>(
            restored.Entities[entity.Id]
                .Properties["data"]
                .Value.Value);

        var tags = Assert.IsType<List<object?>>(
            obj["tags"]);

        var metadata =
            Assert.IsType<Dictionary<string, object?>>(
                obj["metadata"]);

        Assert.Equal(
            new[] { "capital", "coastal" },
            tags.Cast<string>());

        Assert.Equal(2400000L, metadata["population"]);
        Assert.Equal(true, metadata["active"]);
    }


[Fact]
public void Serialize_ShouldRejectMismatchedEntityPropertyName()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Avaria"
    };

    entity.Properties["population"] = new SorophyProperty
    {
        Name = "banana",
        Value = new SorophyValue(
            SorophyValueType.Integer,
            2400000L)
    };

    graph.AddEntity(entity);

    var exception = Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));

    Assert.Contains(
        "does not match property name",
        exception.Message,
        StringComparison.Ordinal);
}

[Fact]
public void Serialize_ShouldRejectMismatchedRelationshipPropertyName()
{
    var graph = new SorophyGraph();

    var source = new SorophyEntity
    {
        Name = "Source"
    };

    var target = new SorophyEntity
    {
        Name = "Target"
    };

    graph.AddEntity(source);
    graph.AddEntity(target);

    var relationship = new SorophyRelationship
    {
        Type = "connects",
        SourceId = source.Id,
        TargetId = target.Id
    };

    relationship.Properties["strength"] = new SorophyProperty
    {
        Name = "banana",
        Value = new SorophyValue(
            SorophyValueType.Decimal,
            42.5m)
    };

    graph.AddRelationship(relationship);

    var exception = Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));

    Assert.Contains(
        "does not match property name",
        exception.Message,
        StringComparison.Ordinal);
}

    [Fact]
    public void Deserialize_ShouldRejectMalformedJson()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectMissingFormatVersion()
    {
        var json = """
        {
          "entities": [],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullEntities()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": null,
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullRelationships()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [],
          "relationships": null
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullEntity()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [
            null
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectEmptyEntityId()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "00000000-0000-0000-0000-000000000000",
              "name": "Avaria",
              "properties": {}
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectDuplicateEntityIds()
    {
        var id = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{id}}",
              "name": "Avaria",
              "properties": {}
            },
            {
              "id": "{{id}}",
              "name": "Duplicate",
              "properties": {}
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullRelationship()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [],
          "relationships": [
            null
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectEmptyRelationshipId()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Source",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Target",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "00000000-0000-0000-0000-000000000000",
              "type": "connects",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectDuplicateRelationshipIds()
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
              "name": "Source",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Target",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "connects",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            },
            {
              "id": "{{relationshipId}}",
              "type": "connects",
              "sourceId": "{{targetId}}",
              "targetId": "{{sourceId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectRelationshipWithMissingTarget()
    {
        var sourceId = Guid.NewGuid();
        var missingTargetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Source",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{Guid.NewGuid()}}",
              "type": "connects",
              "sourceId": "{{sourceId}}",
              "targetId": "{{missingTargetId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectRelationshipWithEmptyType()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Source",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Target",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{Guid.NewGuid()}}",
              "type": "",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullEntityProperties()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Avaria",
              "properties": null
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullRelationshipProperties()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Source",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "Target",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{Guid.NewGuid()}}",
              "type": "connects",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": null
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullPropertyDocument()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Avaria",
              "properties": {
                "population": null
              }
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectPropertyMissingType()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Avaria",
              "properties": {
                "population": {
                  "value": 2400000
                }
              }
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectInvalidPropertyValueShape()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Avaria",
              "properties": {
                "population": {
                  "type": "Integer",
                  "value": "2400000"
                }
              }
            }
          ],
          "relationships": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
public void CompleteGraph_ShouldPreserveAllIdentityAndTopologyAfterSecondRoundTrip()
{
    var graph = new SorophyGraph();

    var a = new SorophyEntity
    {
        Id = Guid.NewGuid(),
        Name = "Avaria",
        Type = "Kingdom"
    };

    var b = new SorophyEntity
    {
        Id = Guid.NewGuid(),
        Name = "Valor",
        Type = "City"
    };

    var c = new SorophyEntity
    {
        Id = Guid.NewGuid(),
        Name = "Eldoria",
        Type = "Province"
    };

    a.Properties["population"] = new SorophyProperty
    {
        Name = "population",
        Value = new SorophyValue(
            SorophyValueType.Integer,
            2400000L)
    };

    b.Properties["founded"] = new SorophyProperty
    {
        Name = "founded",
        Value = new SorophyValue(
            SorophyValueType.DateTime,
            new DateTime(1842, 5, 12))
    };

    c.Properties["identifier"] = new SorophyProperty
    {
        Name = "identifier",
        Value = new SorophyValue(
            SorophyValueType.Guid,
            Guid.NewGuid())
    };

    graph.AddEntity(a);
    graph.AddEntity(b);
    graph.AddEntity(c);

    var r1 = new SorophyRelationship
    {
        Id = Guid.NewGuid(),
        Type = "contains",
        SourceId = a.Id,
        TargetId = b.Id
    };

    r1.Properties["strength"] = new SorophyProperty
    {
        Name = "strength",
        Value = new SorophyValue(
            SorophyValueType.Decimal,
            87.25m)
    };

    var r2 = new SorophyRelationship
    {
        Id = Guid.NewGuid(),
        Type = "contains",
        SourceId = a.Id,
        TargetId = c.Id
    };

    var r3 = new SorophyRelationship
    {
        Id = Guid.NewGuid(),
        Type = "references",
        SourceId = b.Id,
        TargetId = c.Id
    };

    graph.AddRelationship(r1);
    graph.AddRelationship(r2);
    graph.AddRelationship(r3);

    var firstJson = LoreSerializer.Serialize(graph);
    var firstRestored = LoreSerializer.Deserialize(firstJson);

    var secondJson = LoreSerializer.Serialize(firstRestored);
    var secondRestored = LoreSerializer.Deserialize(secondJson);

    Assert.Equal(
        graph.Entities.Count,
        secondRestored.Entities.Count);

    Assert.Equal(
        graph.Relationships.Count,
        secondRestored.Relationships.Count);

    foreach (var entity in graph.Entities.Values)
    {
        var restored = secondRestored.Entities[entity.Id];

        Assert.Equal(entity.Id, restored.Id);
        Assert.Equal(entity.Name, restored.Name);
        Assert.Equal(entity.Type, restored.Type);
        Assert.Equal(
            entity.Properties.Count,
            restored.Properties.Count);
    }

    foreach (var relationship in graph.Relationships.Values)
    {
        var restored =
            secondRestored.Relationships[relationship.Id];

        Assert.Equal(
            relationship.Id,
            restored.Id);

        Assert.Equal(
            relationship.Type,
            restored.Type);

        Assert.Equal(
            relationship.SourceId,
            restored.SourceId);

        Assert.Equal(
            relationship.TargetId,
            restored.TargetId);

        Assert.Equal(
            relationship.Properties.Count,
            restored.Properties.Count);
    }

    Assert.Equal(
        87.25m,
        secondRestored.Relationships[r1.Id]
            .Properties["strength"]
            .Value.Value);
}

[Fact]
public void EmptyEntity_ShouldSurviveMultipleRoundTrips()
{
    var entity = new SorophyEntity
    {
        Id = Guid.NewGuid(),
        Name = "Empty",
        Type = null
    };

    var json1 = EntitySerializer.Serialize(entity);
    var restored1 = EntitySerializer.Deserialize(json1);

    var json2 = EntitySerializer.Serialize(restored1);
    var restored2 = EntitySerializer.Deserialize(json2);

    Assert.Equal(entity.Id, restored2.Id);
    Assert.Equal(entity.Name, restored2.Name);
    Assert.Equal(entity.Type, restored2.Type);
    Assert.Empty(restored2.Properties);
}

[Fact]
public void SelfRelationshipWithProperties_ShouldSurviveMultipleRoundTrips()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Id = Guid.NewGuid(),
        Name = "Avaria"
    };

    graph.AddEntity(entity);

    var relationship = new SorophyRelationship
    {
        Id = Guid.NewGuid(),
        Type = "self_reference",
        SourceId = entity.Id,
        TargetId = entity.Id
    };

    relationship.Properties["weight"] = new SorophyProperty
    {
        Name = "weight",
        Value = new SorophyValue(
            SorophyValueType.Decimal,
            12.5m)
    };

    graph.AddRelationship(relationship);

    var json1 = LoreSerializer.Serialize(graph);
    var restored1 = LoreSerializer.Deserialize(json1);

    var json2 = LoreSerializer.Serialize(restored1);
    var restored2 = LoreSerializer.Deserialize(json2);

    var restored =
        restored2.Relationships[relationship.Id];

    Assert.Equal(entity.Id, restored.SourceId);
    Assert.Equal(entity.Id, restored.TargetId);
    Assert.Equal("self_reference", restored.Type);

    Assert.Equal(
        12.5m,
        restored.Properties["weight"].Value.Value);
}

[Fact]
public void SerializeDeserialize_ShouldPreserveNestedDateTimeAndGuidInLore()
{
    var born = new DateTime(
        1842,
        5,
        12,
        14,
        30,
        45,
        DateTimeKind.Utc);

    var identifier = Guid.NewGuid();

    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Avaria"
    };

    entity.Properties["metadata"] = new SorophyProperty
    {
        Name = "metadata",
        Value = new SorophyValue(
            SorophyValueType.Object,
            new Dictionary<string, object?>
            {
                ["born"] = born,
                ["identifier"] = identifier,

                ["history"] = new List<object?>
                {
                    born,
                    identifier,

                    new Dictionary<string, object?>
                    {
                        ["nestedDate"] = born,
                        ["nestedGuid"] = identifier
                    }
                }
            })
    };

    graph.AddEntity(entity);

    var json = LoreSerializer.Serialize(graph);
    var restored = LoreSerializer.Deserialize(json);

    var metadata =
        Assert.IsType<Dictionary<string, object?>>(
            restored.Entities[entity.Id]
                .Properties["metadata"]
                .Value.Value);

    Assert.IsType<DateTime>(metadata["born"]);
    Assert.IsType<Guid>(metadata["identifier"]);

    Assert.Equal(born, metadata["born"]);
    Assert.Equal(identifier, metadata["identifier"]);

    var history =
        Assert.IsType<List<object?>>(
            metadata["history"]);

    Assert.IsType<DateTime>(history[0]);
    Assert.IsType<Guid>(history[1]);

    Assert.Equal(born, history[0]);
    Assert.Equal(identifier, history[1]);

    var nested =
        Assert.IsType<Dictionary<string, object?>>(
            history[2]);

    Assert.IsType<DateTime>(nested["nestedDate"]);
    Assert.IsType<Guid>(nested["nestedGuid"]);

    Assert.Equal(born, nested["nestedDate"]);
    Assert.Equal(identifier, nested["nestedGuid"]);
}

[Fact]
public void SerializeDeserialize_ShouldPreserveNestedPrimitiveClrTypesInLore()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Primitive Types"
    };

    entity.Properties["values"] = new SorophyProperty
    {
        Name = "values",
        Value = new SorophyValue(
            SorophyValueType.Object,
            new Dictionary<string, object?>
            {
                ["byte"] = (byte)7,
                ["sbyte"] = (sbyte)-8,
                ["short"] = (short)12,
                ["ushort"] = (ushort)24,
                ["int"] = 42,
                ["uint"] = (uint)84,
                ["long"] = (long)168,
                ["ulong"] = (ulong)336,
                ["float"] = 1.25f,
                ["double"] = 2.5d,
                ["decimal"] = 3.75m
            })
    };

    graph.AddEntity(entity);

    var json =
        LoreSerializer.Serialize(graph);

    var restored =
        LoreSerializer.Deserialize(json);

    var restoredEntity =
        Assert.Single(restored.Entities.Values);

    var values =
        Assert.IsType<Dictionary<string, object?>>(
            restoredEntity
                .Properties["values"]
                .Value
                .Value);

    Assert.IsType<byte>(values["byte"]);
    Assert.IsType<sbyte>(values["sbyte"]);
    Assert.IsType<short>(values["short"]);
    Assert.IsType<ushort>(values["ushort"]);
    Assert.IsType<int>(values["int"]);
    Assert.IsType<uint>(values["uint"]);
    Assert.IsType<long>(values["long"]);
    Assert.IsType<ulong>(values["ulong"]);
    Assert.IsType<float>(values["float"]);
    Assert.IsType<double>(values["double"]);
    Assert.IsType<decimal>(values["decimal"]);

    Assert.Equal((byte)7, values["byte"]);
    Assert.Equal((sbyte)-8, values["sbyte"]);
    Assert.Equal((short)12, values["short"]);
    Assert.Equal((ushort)24, values["ushort"]);
    Assert.Equal(42, values["int"]);
    Assert.Equal((uint)84, values["uint"]);
    Assert.Equal((long)168, values["long"]);
    Assert.Equal((ulong)336, values["ulong"]);
    Assert.Equal(1.25f, values["float"]);
    Assert.Equal(2.5d, values["double"]);
    Assert.Equal(3.75m, values["decimal"]);
}

[Fact]
public void Serialize_ShouldRejectNestedNaNInLore()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Invalid Numeric Entity"
    };

    entity.Properties["values"] = new SorophyProperty
    {
        Name = "values",
        Value = new SorophyValue(
            SorophyValueType.List,
            new List<object?>
            {
                1.0,
                double.NaN
            })
    };

    graph.AddEntity(entity);

    Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));
}

[Fact]
public void Serialize_ShouldRejectNestedPositiveInfinityInLore()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Invalid Numeric Entity"
    };

    entity.Properties["values"] = new SorophyProperty
    {
        Name = "values",
        Value = new SorophyValue(
            SorophyValueType.List,
            new List<object?>
            {
                double.PositiveInfinity
            })
    };

    graph.AddEntity(entity);

    Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));
}

[Fact]
public void Serialize_ShouldRejectNestedNegativeInfinityInLore()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Invalid Numeric Entity"
    };

    entity.Properties["values"] = new SorophyProperty
    {
        Name = "values",
        Value = new SorophyValue(
            SorophyValueType.Object,
            new Dictionary<string, object?>
            {
                ["value"] = double.NegativeInfinity
            })
    };

    graph.AddEntity(entity);

    Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));
}

[Fact]
public void Serialize_ShouldRejectUnsupportedNestedClrObjectInLore()
{
    var graph = new SorophyGraph();

    var entity = new SorophyEntity
    {
        Name = "Unsupported Nested Object"
    };

    entity.Properties["values"] = new SorophyProperty
    {
        Name = "values",
        Value = new SorophyValue(
            SorophyValueType.List,
            new List<object?>
            {
                new UnsupportedNestedObject()
            })
    };

    graph.AddEntity(entity);

    Assert.Throws<InvalidOperationException>(
        () => LoreSerializer.Serialize(graph));
}

private sealed class UnsupportedNestedObject
{
    public string Value { get; } = "unsupported";
}

}