using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests.Serialization;

public class SerializationAdversarialTests
{
    // ------------------------------------------------------------
    // ENTITY: INVALID INPUT
    // ------------------------------------------------------------

    [Fact]
    public void EntityDeserialize_ShouldRejectMalformedJson()
    {
        Assert.ThrowsAny<Exception>(() =>
            EntitySerializer.Deserialize("{ this is not valid json"));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectUnsupportedFormatVersion()
    {
        var json = """
        {
          "formatVersion": 999,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectInvalidGuid()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "not-a-guid",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {}
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectUnknownSorophyValueType()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "population": {
              "type": "Banana",
              "value": 2400000
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectIntegerContainingString()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "population": {
              "type": "Integer",
              "value": "not-an-integer"
            }
          }
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectBooleanContainingString()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "active": {
              "type": "Boolean",
              "value": "not-a-boolean"
            }
          }
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldRejectGuidContainingString()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "reference": {
              "type": "Guid",
              "value": "not-a-guid"
            }
          }
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void EntityDeserialize_ShouldAcceptNullValue()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "unknown": {
              "type": "Null",
              "value": null
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(
            SorophyValueType.Null,
            entity.Properties["unknown"].Value.Type);

        Assert.Null(
            entity.Properties["unknown"].Value.Value);
    }

    // ------------------------------------------------------------
    // LORE: INVALID INPUT
    // ------------------------------------------------------------

    [Fact]
    public void LoreDeserialize_ShouldRejectMalformedJson()
    {
        Assert.ThrowsAny<Exception>(() =>
            LoreSerializer.Deserialize("{ completely broken"));
    }

    [Fact]
    public void LoreDeserialize_ShouldRejectUnsupportedFormatVersion()
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
    public void LoreDeserialize_ShouldRejectInvalidEntityGuid()
    {
        var json = """
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "not-a-guid",
              "name": "Avaria",
              "type": "Kingdom",
              "properties": {}
            }
          ],
          "relationships": []
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void LoreDeserialize_ShouldRejectDuplicateEntityIds()
    {
        var id = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{id}}",
              "name": "Avaria",
              "type": "Kingdom",
              "properties": {}
            },
            {
              "id": "{{id}}",
              "name": "Duplicate",
              "type": "City",
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
    public void LoreDeserialize_ShouldRejectDuplicateRelationshipIds()
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
            },
            {
              "id": "{{relationshipId}}",
              "type": "different",
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
    public void LoreDeserialize_ShouldRejectMissingTarget()
    {
        var sourceId = Guid.NewGuid();
        var missingTargetId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "Avaria",
              "type": "Kingdom",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{Guid.NewGuid()}}",
              "type": "capital_of",
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
    public void LoreDeserialize_ShouldRejectInvalidRelationshipGuid()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

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
              "id": "not-a-guid",
              "type": "capital_of",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        Assert.ThrowsAny<Exception>(() =>
            LoreSerializer.Deserialize(json));
    }

    [Fact]
    public void LoreDeserialize_ShouldRejectUnknownRelationshipPropertyType()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

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
              "id": "{{Guid.NewGuid()}}",
              "type": "capital_of",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {
                "since": {
                  "type": "DefinitelyNotAType",
                  "value": 482
                }
              }
            }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            LoreSerializer.Deserialize(json));
    }

    // ------------------------------------------------------------
    // LORE: STRUCTURAL EDGE CASES
    // ------------------------------------------------------------

    [Fact]
    public void LoreDeserialize_ShouldAllowIsolatedEntities()
    {
        var entityId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Isolated",
              "type": "Unknown",
              "properties": {}
            }
          ],
          "relationships": []
        }
        """;

        var graph = LoreSerializer.Deserialize(json);

        Assert.Single(graph.Entities);
        Assert.Empty(graph.Relationships);
    }

    [Fact]
    public void LoreDeserialize_ShouldAllowSelfRelationship()
    {
        var entityId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

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
          "relationships": [
            {
              "id": "{{relationshipId}}",
              "type": "references",
              "sourceId": "{{entityId}}",
              "targetId": "{{entityId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph = LoreSerializer.Deserialize(json);

        Assert.Single(graph.Relationships);

        var relationship =
            graph.Relationships[relationshipId];

        Assert.Equal(
            relationship.SourceId,
            relationship.TargetId);
    }

    [Fact]
    public void LoreDeserialize_ShouldAllowMultipleRelationshipsBetweenSameEntities()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var firstRelationshipId = Guid.NewGuid();
        var secondRelationshipId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "entities": [
            {
              "id": "{{sourceId}}",
              "name": "A",
              "type": "Entity",
              "properties": {}
            },
            {
              "id": "{{targetId}}",
              "name": "B",
              "type": "Entity",
              "properties": {}
            }
          ],
          "relationships": [
            {
              "id": "{{firstRelationshipId}}",
              "type": "knows",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            },
            {
              "id": "{{secondRelationshipId}}",
              "type": "knows",
              "sourceId": "{{sourceId}}",
              "targetId": "{{targetId}}",
              "properties": {}
            }
          ]
        }
        """;

        var graph = LoreSerializer.Deserialize(json);

        Assert.Equal(2, graph.Relationships.Count);
    }
}