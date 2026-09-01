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
                2400000L)
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
                2400000L)
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

    [Fact]
    public void SerializeDeserialize_ShouldPreserveListProperty()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["tags"] = new OrbProperty
        {
            Name = "tags",
            Value = new OrbValue(
                OrbValueType.List,
                new List<object?>
                {
                    "capital",
                    "coastal",
                    2400000L,
                    true,
                    null
                })
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var property = restored.Properties["tags"];

        Assert.Equal(
            OrbValueType.List,
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
    public void SerializeDeserialize_ShouldPreserveObjectProperty()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["metadata"] = new OrbProperty
        {
            Name = "metadata",
            Value = new OrbValue(
                OrbValueType.Object,
                new Dictionary<string, object?>
                {
                    ["population"] = 2400000L,
                    ["active"] = true,
                    ["language"] = "Avarian",
                    ["unknown"] = null
                })
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var property = restored.Properties["metadata"];

        Assert.Equal(
            OrbValueType.Object,
            property.Value.Type);

        var obj = Assert.IsType<Dictionary<string, object?>>(
            property.Value.Value);

        Assert.Equal(4, obj.Count);
        Assert.Equal(2400000L, obj["population"]);
        Assert.Equal(true, obj["active"]);
        Assert.Equal("Avarian", obj["language"]);
        Assert.Null(obj["unknown"]);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveNestedListAndObject()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        entity.Properties["data"] = new OrbProperty
        {
            Name = "data",
            Value = new OrbValue(
                OrbValueType.Object,
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

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var obj = Assert.IsType<Dictionary<string, object?>>(
            restored.Properties["data"].Value.Value);

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
public void Serialize_ShouldRejectMismatchedPropertyName()
{
    var entity = new OrbEntity
    {
        Name = "Avaria"
    };

    entity.Properties["population"] = new OrbProperty
    {
        Name = "banana",
        Value = new OrbValue(
            OrbValueType.Integer,
            2400000L)
    };

    var exception = Assert.Throws<InvalidOperationException>(
        () => EntitySerializer.Serialize(entity));

    Assert.Contains(
        "does not match property name",
        exception.Message,
        StringComparison.Ordinal);
}

[Fact]
public void Serialize_ShouldRejectEmptyPropertyName()
{
    var entity = new OrbEntity
    {
        Name = "Avaria"
    };

    entity.Properties["population"] = new OrbProperty
    {
        Name = " ",
        Value = new OrbValue(
            OrbValueType.Integer,
            2400000L)
    };

    var exception = Assert.Throws<InvalidOperationException>(
        () => EntitySerializer.Serialize(entity));

    Assert.Contains(
        "has an empty name",
        exception.Message,
        StringComparison.Ordinal);
  }

      [Fact]
    public void Deserialize_ShouldRejectMalformedJson()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectMissingFormatVersion()
    {
        var json = """
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectEmptyEntityId()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000000",
          "name": "Avaria",
          "properties": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectInvalidEntityId()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "not-a-guid",
          "name": "Avaria",
          "properties": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullProperties()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": null
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullPropertyDocument()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "population": null
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectPropertyMissingType()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "population": {
              "value": 2400000
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectUnknownPropertyType()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
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
    public void Deserialize_ShouldRejectIntegerString()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "population": {
              "type": "Integer",
              "value": "2400000"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectIntegerOverflow()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "population": {
              "type": "Integer",
              "value": 9223372036854775808
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectBooleanNumber()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "active": {
              "type": "Boolean",
              "value": 1
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectDecimalString()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "rate": {
              "type": "Decimal",
              "value": "12.5"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectInvalidDateTime()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "founded": {
              "type": "DateTime",
              "value": "not-a-date"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectInvalidGuidProperty()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "reference": {
              "type": "Guid",
              "value": "not-a-guid"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullTypeWithNonNullValue()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "unknown": {
              "type": "Null",
              "value": "something"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectListWithWrongJsonShape()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "tags": {
              "type": "List",
              "value": "not-a-list"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectObjectWithWrongJsonShape()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "properties": {
            "metadata": {
              "type": "Object",
              "value": "not-an-object"
            }
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

}
