using Orb.Engine.Graph;
using Orb.Engine.Serialization;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Serialization;

public class LoreSerializerTests
{
    [Fact]
    public void Serialize_ShouldIncludeFormatVersion()
    {
        var graph = new OrbGraph();

        var json = LoreSerializer.Serialize(graph);

        Assert.Contains("\"formatVersion\": 1", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeEntities()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
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
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "Avaria"
        };

        var target = new OrbEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
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
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000)
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
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "Avaria"
        };

        var target = new OrbEntity
        {
            Name = "Valor"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "ruled_by",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["since"] = new OrbProperty
        {
            Name = "since",
            Value = new OrbValue(
                OrbValueType.Integer,
                482)
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
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        graph.AddEntity(entity);

        var relationship = new OrbRelationship
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
        var graph = new OrbGraph();

        var json = LoreSerializer.Serialize(graph);

        var restored = LoreSerializer.Deserialize(json);

        Assert.Empty(restored.Entities);
        Assert.Empty(restored.Relationships);
    }

    [Fact]
    public void CompleteGraph_ShouldSurviveRoundTrip()
    {
        var graph = new OrbGraph();

        var avaria = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        avaria.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000)
        };

        var valor = new OrbEntity
        {
            Name = "Valor",
            Type = "City"
        };

        valor.Properties["language"] = new OrbProperty
        {
            Name = "language",
            Value = new OrbValue(
                OrbValueType.String,
                "Avarian")
        };

        graph.AddEntity(avaria);
        graph.AddEntity(valor);

        var relationship = new OrbRelationship
        {
            Type = "capital_of",
            SourceId = avaria.Id,
            TargetId = valor.Id
        };

        relationship.Properties["since"] = new OrbProperty
        {
            Name = "since",
            Value = new OrbValue(
                OrbValueType.Integer,
                482)
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
}