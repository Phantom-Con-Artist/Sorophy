using Orb.Engine.Graph;
using Orb.Engine.Serialization;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Serialization;

public class EntitySerializerTests
{
    [Fact]
    public void Serialize_ShouldIncludeEntityFields()
    {
        var id = Guid.NewGuid();

        var entity = new OrbEntity
        {
            Id = id,
            Name = "Avaria",
            Type = "Kingdom"
        };

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains("\"formatVersion\": 1", json);
        Assert.Contains(id.ToString(), json);
        Assert.Contains("\"name\": \"Avaria\"", json);
        Assert.Contains("\"type\": \"Kingdom\"", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeProperties()
    {
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

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains("\"population\"", json);
        Assert.Contains("\"Integer\"", json);
        Assert.Contains("2400000", json);
    }

    [Fact]
    public void Deserialize_ShouldRestoreEntityFields()
    {
        var id = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "id": "{{id}}",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {}
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(id, entity.Id);
        Assert.Equal("Avaria", entity.Name);
        Assert.Equal("Kingdom", entity.Type);
    }

    [Fact]
    public void Deserialize_ShouldRestoreStringProperty()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "language": {
              "type": "String",
              "value": "Avarian"
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        var property = entity.Properties["language"];

        Assert.Equal(
            OrbValueType.String,
            property.Value.Type);

        Assert.Equal(
            "Avarian",
            property.Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRestoreIntegerProperty()
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
              "value": 2400000
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        var property = entity.Properties["population"];

        Assert.Equal(
            OrbValueType.Integer,
            property.Value.Type);

        Assert.Equal(
            2400000L,
            property.Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRestoreBooleanProperty()
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
              "value": true
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        var property = entity.Properties["active"];

        Assert.Equal(
            OrbValueType.Boolean,
            property.Value.Type);

        Assert.Equal(
            true,
            property.Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRestoreDecimalProperty()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "tax_rate": {
              "type": "Decimal",
              "value": 12.5
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        var property = entity.Properties["tax_rate"];

        Assert.Equal(
            OrbValueType.Decimal,
            property.Value.Type);

        Assert.Equal(
            12.5m,
            property.Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRestoreGuidProperty()
    {
        var propertyId = Guid.NewGuid();

        var json = $$"""
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {
            "reference": {
              "type": "Guid",
              "value": "{{propertyId}}"
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        var property = entity.Properties["reference"];

        Assert.Equal(
            OrbValueType.Guid,
            property.Value.Type);

        Assert.Equal(
            propertyId,
            property.Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRestoreNullProperty()
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

        var property = entity.Properties["unknown"];

        Assert.Equal(
            OrbValueType.Null,
            property.Value.Type);

        Assert.Null(property.Value.Value);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveEntity()
    {
        var id = Guid.NewGuid();

        var entity = new OrbEntity
        {
            Id = id,
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000)
        };

        entity.Properties["language"] = new OrbProperty
        {
            Name = "language",
            Value = new OrbValue(
                OrbValueType.String,
                "Avarian")
        };

        entity.Properties["active"] = new OrbProperty
        {
            Name = "active",
            Value = new OrbValue(
                OrbValueType.Boolean,
                true)
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        Assert.Equal(entity.Id, restored.Id);
        Assert.Equal(entity.Name, restored.Name);
        Assert.Equal(entity.Type, restored.Type);

        Assert.Equal(
            entity.Properties.Count,
            restored.Properties.Count);

        Assert.Equal(
            "Avarian",
            restored.Properties["language"].Value.Value);

        Assert.Equal(
            2400000L,
            restored.Properties["population"].Value.Value);

        Assert.Equal(
            true,
            restored.Properties["active"].Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRejectUnsupportedFormatVersion()
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
}