using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests.Serialization;

public class EntitySerializerTests
{
    [Fact]
    public void Serialize_ShouldIncludeEntityFields()
    {
        var id = Guid.NewGuid();

        var entity = new SorophyEntity
        {
            Id = id,
            Name = "Avaria",
            Type = "Kingdom"
        };

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains("\"formatVersion\": 2", json);
        Assert.Contains(id.ToString(), json);
        Assert.Contains("\"name\": \"Avaria\"", json);
        Assert.Contains("\"type\": \"Kingdom\"", json);
    }

    [Fact]
    public void Serialize_ShouldIncludeDescription()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Description = "A coastal kingdom known for its merchant fleets."
        };

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains(
            "\"description\": \"A coastal kingdom known for its merchant fleets.\"",
            json);
    }

    [Fact]
    public void Serialize_ShouldIncludeTags()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");
        entity.Tags.Add("Root Document");

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains("\"tags\"", json);
        Assert.Contains("\"Important\"", json);
        Assert.Contains("\"Kingdom\"", json);
        Assert.Contains("\"Root Document\"", json);
    }

    [Fact]
    public void Serialize_ShouldSortTagsDeterministically()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Tags.Add("Root Document");
        entity.Tags.Add("Antagonist");
        entity.Tags.Add("Important");
        entity.Tags.Add("Character");
        entity.Tags.Add("Protagonist");

        var json = EntitySerializer.Serialize(entity);

        var antagonistIndex = json.IndexOf(
            "\"Antagonist\"",
            StringComparison.Ordinal);

        var characterIndex = json.IndexOf(
            "\"Character\"",
            StringComparison.Ordinal);

        var importantIndex = json.IndexOf(
            "\"Important\"",
            StringComparison.Ordinal);

        var protagonistIndex = json.IndexOf(
            "\"Protagonist\"",
            StringComparison.Ordinal);

        var rootDocumentIndex = json.IndexOf(
            "\"Root Document\"",
            StringComparison.Ordinal);

        Assert.True(antagonistIndex >= 0);
        Assert.True(characterIndex > antagonistIndex);
        Assert.True(importantIndex > characterIndex);
        Assert.True(protagonistIndex > importantIndex);
        Assert.True(rootDocumentIndex > protagonistIndex);
    }

    [Fact]
    public void Serialize_ShouldIncludeEmbeddedDocuments()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Documents["History.md"] =
            new SorophyEntityDocument(
                "History.md",
                "# History\n\nAvaria was founded centuries ago.");

        entity.Documents["Culture.md"] =
            new SorophyEntityDocument(
                "Culture.md",
                "# Culture\n\nAvarian culture values scholarship and trade.");

        var json = EntitySerializer.Serialize(entity);

        Assert.Contains("\"documents\"", json);
        Assert.Contains("\"History.md\"", json);
        Assert.Contains("\"Culture.md\"", json);
        Assert.Contains("\"contentType\": \"text/markdown\"", json);
        Assert.Contains(
            "Avaria was founded centuries ago.",
            json);
        Assert.Contains(
            "Avarian culture values scholarship and trade.",
            json);
    }

    [Fact]
    public void Serialize_ShouldIncludeProperties()
    {
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
    public void Deserialize_ShouldRestoreDescription()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "description": "A coastal kingdom known for its merchant fleets.",
          "properties": {},
          "tags": [],
          "documents": {}
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(
            "A coastal kingdom known for its merchant fleets.",
            entity.Description);
    }

    [Fact]
    public void Deserialize_ShouldRestoreTags()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [
            "Character",
            "Important",
            "Protagonist"
          ],
          "documents": {}
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(3, entity.Tags.Count);
        Assert.Contains("Character", entity.Tags);
        Assert.Contains("Important", entity.Tags);
        Assert.Contains("Protagonist", entity.Tags);
    }

    [Fact]
    public void Deserialize_ShouldRestoreEmbeddedDocuments()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [],
          "documents": {
            "History.md": {
              "contentType": "text/markdown",
              "content": "# History\n\nAvaria was founded centuries ago."
            },
            "Culture.md": {
              "contentType": "text/markdown",
              "content": "# Culture\n\nAvarian culture values scholarship."
            }
          }
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(2, entity.Documents.Count);

        Assert.Equal(
            "History.md",
            entity.Documents["History.md"].Name);

        Assert.Equal(
            "text/markdown",
            entity.Documents["History.md"].ContentType);

        Assert.Equal(
            "# History\n\nAvaria was founded centuries ago.",
            entity.Documents["History.md"].Content);

        Assert.Equal(
            "Culture.md",
            entity.Documents["Culture.md"].Name);

        Assert.Equal(
            "text/markdown",
            entity.Documents["Culture.md"].ContentType);

        Assert.Equal(
            "# Culture\n\nAvarian culture values scholarship.",
            entity.Documents["Culture.md"].Content);
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
            SorophyValueType.String,
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
            SorophyValueType.Integer,
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
            SorophyValueType.Boolean,
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
            SorophyValueType.Decimal,
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
            SorophyValueType.Guid,
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
            SorophyValueType.Null,
            property.Value.Type);

        Assert.Null(property.Value.Value);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveEntity()
    {
        var id = Guid.NewGuid();

        var entity = new SorophyEntity
        {
            Id = id,
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["language"] = new SorophyProperty
        {
            Name = "language",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avarian")
        };

        entity.Properties["active"] = new SorophyProperty
        {
            Name = "active",
            Value = new SorophyValue(
                SorophyValueType.Boolean,
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
    public void SerializeDeserialize_ShouldPreserveCompleteEntityV2()
    {
        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Avaria",
            Type = "Kingdom",
            Description =
                "A prosperous coastal kingdom known for trade and scholarship."
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["capital"] = new SorophyProperty
        {
            Name = "capital",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avaris")
        };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");
        entity.Tags.Add("Root Document");

        entity.Documents["History.md"] =
            new SorophyEntityDocument(
                "History.md",
                "# History\n\nAvaria was founded centuries ago.");

        entity.Documents["Culture.md"] =
            new SorophyEntityDocument(
                "Culture.md",
                "# Culture\n\nScholarship is highly valued.");

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        Assert.Equal(entity.Id, restored.Id);
        Assert.Equal(entity.Name, restored.Name);
        Assert.Equal(entity.Type, restored.Type);
        Assert.Equal(entity.Description, restored.Description);

        Assert.Equal(entity.Tags, restored.Tags);

        Assert.Equal(
            entity.Documents.Count,
            restored.Documents.Count);

        Assert.Equal(
            entity.Documents["History.md"].Content,
            restored.Documents["History.md"].Content);

        Assert.Equal(
            entity.Documents["History.md"].ContentType,
            restored.Documents["History.md"].ContentType);

        Assert.Equal(
            entity.Documents["Culture.md"].Content,
            restored.Documents["Culture.md"].Content);

        Assert.Equal(
            entity.Documents["Culture.md"].ContentType,
            restored.Documents["Culture.md"].ContentType);

        Assert.Equal(
            entity.Properties.Count,
            restored.Properties.Count);

        Assert.Equal(
            2400000L,
            restored.Properties["population"].Value.Value);

        Assert.Equal(
            "Avaris",
            restored.Properties["capital"].Value.Value);
    }

    [Fact]
    public void Deserialize_ShouldRemainCompatibleWithV1()
    {
        var json = """
        {
          "formatVersion": 1,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {}
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            entity.Id);

        Assert.Equal("Avaria", entity.Name);
        Assert.Equal("Kingdom", entity.Type);

        Assert.Null(entity.Description);
        Assert.Empty(entity.Tags);
        Assert.Empty(entity.Documents);
        Assert.Empty(entity.Properties);
    }

    [Fact]
    public void Deserialize_ShouldRejectNullTags()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": null,
          "documents": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullDocuments()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [],
          "documents": null
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Serialize_ShouldRejectEmptyTag()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Tags.Add(" ");

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Serialize(entity));
    }

    [Fact]
    public void Serialize_ShouldRejectMismatchedDocumentName()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Documents["History.md"] =
            new SorophyEntityDocument(
                "DifferentName.md",
                "# History");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    EntitySerializer.Serialize(entity));

        Assert.Contains(
            "does not match document name",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ShouldRejectNullDocument()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Documents["History.md"] = null!;

        Assert.Throws<InvalidOperationException>(
            () =>
                EntitySerializer.Serialize(entity));
    }

    [Fact]
    public void Deserialize_ShouldRejectEmptyTag()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [
            ""
          ],
          "documents": {}
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectEmptyDocumentName()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [],
          "documents": {
            "": "# Invalid"
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRejectNullDocumentContent()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "properties": {},
          "tags": [],
          "documents": {
            "History.md": null
          }
        }
        """;

        Assert.Throws<InvalidOperationException>(() =>
            EntitySerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ShouldRestoreMissingV2CollectionsAsEmpty()
    {
        var json = """
        {
          "formatVersion": 2,
          "id": "00000000-0000-0000-0000-000000000001",
          "name": "Avaria",
          "type": "Kingdom",
          "description": "Avaria"
        }
        """;

        var entity = EntitySerializer.Deserialize(json);

        Assert.Equal("Avaria", entity.Description);
        Assert.Empty(entity.Tags);
        Assert.Empty(entity.Documents);
        Assert.Empty(entity.Properties);
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
        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
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

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var property = restored.Properties["tags"];

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
    public void SerializeDeserialize_ShouldPreserveObjectProperty()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["metadata"] = new SorophyProperty
        {
            Name = "metadata",
            Value = new SorophyValue(
                SorophyValueType.Object,
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
            SorophyValueType.Object,
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
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = " ",
            Value = new SorophyValue(
                SorophyValueType.Integer,
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

    [Fact]
    public void SerializeDeserialize_ShouldPreserveAllScalarTypes()
    {
        var id = Guid.NewGuid();

        var date = new DateTime(
            2026,
            8,
            31,
            14,
            30,
            45,
            DateTimeKind.Utc);

        var guid = Guid.NewGuid();

        var entity = new SorophyEntity
        {
            Id = id,
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["string"] = new SorophyProperty
        {
            Name = "string",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avarian")
        };

        entity.Properties["integer"] = new SorophyProperty
        {
            Name = "integer",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                long.MaxValue)
        };

        entity.Properties["decimal"] = new SorophyProperty
        {
            Name = "decimal",
            Value = new SorophyValue(
                SorophyValueType.Decimal,
                123456789.123456789m)
        };

        entity.Properties["boolean"] = new SorophyProperty
        {
            Name = "boolean",
            Value = new SorophyValue(
                SorophyValueType.Boolean,
                true)
        };

        entity.Properties["datetime"] = new SorophyProperty
        {
            Name = "datetime",
            Value = new SorophyValue(
                SorophyValueType.DateTime,
                date)
        };

        entity.Properties["guid"] = new SorophyProperty
        {
            Name = "guid",
            Value = new SorophyValue(
                SorophyValueType.Guid,
                guid)
        };

        entity.Properties["null"] = new SorophyProperty
        {
            Name = "null",
            Value = new SorophyValue(
                SorophyValueType.Null,
                null)
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        Assert.Equal(entity.Id, restored.Id);
        Assert.Equal(entity.Name, restored.Name);
        Assert.Equal(entity.Type, restored.Type);

        Assert.Equal(
            "Avarian",
            restored.Properties["string"].Value.Value);

        Assert.Equal(
            long.MaxValue,
            restored.Properties["integer"].Value.Value);

        Assert.Equal(
            123456789.123456789m,
            restored.Properties["decimal"].Value.Value);

        Assert.Equal(
            true,
            restored.Properties["boolean"].Value.Value);

        Assert.Equal(
            date,
            restored.Properties["datetime"].Value.Value);

        Assert.Equal(
            guid,
            restored.Properties["guid"].Value.Value);

        Assert.Equal(
            SorophyValueType.Null,
            restored.Properties["null"].Value.Type);

        Assert.Null(
            restored.Properties["null"].Value.Value);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveEmptyCollections()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        entity.Properties["emptyList"] = new SorophyProperty
        {
            Name = "emptyList",
            Value = new SorophyValue(
                SorophyValueType.List,
                new List<object?>())
        };

        entity.Properties["emptyObject"] = new SorophyProperty
        {
            Name = "emptyObject",
            Value = new SorophyValue(
                SorophyValueType.Object,
                new Dictionary<string, object?>())
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var list = Assert.IsType<List<object?>>(
            restored.Properties["emptyList"].Value.Value);

        var obj = Assert.IsType<Dictionary<string, object?>>(
            restored.Properties["emptyObject"].Value.Value);

        Assert.Empty(list);
        Assert.Empty(obj);
        Assert.Empty(restored.Tags);
        Assert.Empty(restored.Documents);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveUnicodeStrings()
    {
        var entity = new SorophyEntity
        {
            Name = "অ্যাভারিয়া",
            Type = "王国"
        };

        entity.Properties["description"] = new SorophyProperty
        {
            Name = "description",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avaria — बंगाल — アヴァリア — 🜂")
        };

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        Assert.Equal(entity.Name, restored.Name);
        Assert.Equal(entity.Type, restored.Type);

        Assert.Equal(
            "Avaria — बंगाल — アヴァリア — 🜂",
            restored.Properties["description"].Value.Value);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveSecondRoundTrip()
    {
        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Avaria",
            Type = "Kingdom",
            Description = "Avaria is a prosperous coastal kingdom."
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["ratio"] = new SorophyProperty
        {
            Name = "ratio",
            Value = new SorophyValue(
                SorophyValueType.Decimal,
                42.75m)
        };

        entity.Properties["tags"] = new SorophyProperty
        {
            Name = "tags",
            Value = new SorophyValue(
                SorophyValueType.List,
                new List<object?>
                {
                    "capital",
                    2400000L,
                    true,
                    null
                })
        };

        entity.Tags.Add("Important");
        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Root Document");

        entity.Documents["History.md"] =
            new SorophyEntityDocument(
                "History.md",
                "# History\n\nAvaria was founded centuries ago.");

        var firstJson = EntitySerializer.Serialize(entity);
        var firstRestored = EntitySerializer.Deserialize(firstJson);

        var secondJson = EntitySerializer.Serialize(firstRestored);
        var secondRestored = EntitySerializer.Deserialize(secondJson);

        Assert.Equal(entity.Id, secondRestored.Id);
        Assert.Equal(entity.Name, secondRestored.Name);
        Assert.Equal(entity.Type, secondRestored.Type);
        Assert.Equal(entity.Description, secondRestored.Description);

        Assert.Equal(
            2400000L,
            secondRestored.Properties["population"].Value.Value);

        Assert.Equal(
            42.75m,
            secondRestored.Properties["ratio"].Value.Value);

        var tags = Assert.IsType<List<object?>>(
            secondRestored.Properties["tags"].Value.Value);

        Assert.Equal(
            new object?[]
            {
                "capital",
                2400000L,
                true,
                null
            },
            tags);

        Assert.Equal(
            entity.Tags,
            secondRestored.Tags);

        Assert.Equal(
            entity.Documents["History.md"].Content,
            secondRestored.Documents["History.md"].Content);

        Assert.Equal(
            entity.Documents["History.md"].ContentType,
            secondRestored.Documents["History.md"].ContentType);

        Assert.Equal(
            firstJson,
            secondJson);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveNestedDateTimeAndGuid()
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

        var json = EntitySerializer.Serialize(entity);
        var restored = EntitySerializer.Deserialize(json);

        var metadata =
            Assert.IsType<Dictionary<string, object?>>(
                restored.Properties["metadata"].Value.Value);

        Assert.IsType<DateTime>(
            metadata["born"]);

        Assert.IsType<Guid>(
            metadata["identifier"]);

        Assert.Equal(
            born,
            metadata["born"]);

        Assert.Equal(
            identifier,
            metadata["identifier"]);

        var history =
            Assert.IsType<List<object?>>(
                metadata["history"]);

        Assert.IsType<DateTime>(
            history[0]);

        Assert.IsType<Guid>(
            history[1]);

        Assert.Equal(
            born,
            history[0]);

        Assert.Equal(
            identifier,
            history[1]);

        var nested =
            Assert.IsType<Dictionary<string, object?>>(
                history[2]);

        Assert.IsType<DateTime>(
            nested["nestedDate"]);

        Assert.IsType<Guid>(
            nested["nestedGuid"]);

        Assert.Equal(
            born,
            nested["nestedDate"]);

        Assert.Equal(
            identifier,
            nested["nestedGuid"]);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveNestedPrimitiveClrTypes()
    {
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
                    ["short"] = (short)12,
                    ["int"] = 42,
                    ["uint"] = (uint)84,
                    ["long"] = (long)168,
                    ["float"] = 1.25f,
                    ["double"] = 2.5d,
                    ["decimal"] = 3.75m
                })
        };

        var json =
            EntitySerializer.Serialize(entity);

        var restored =
            EntitySerializer.Deserialize(json);

        var values =
            Assert.IsType<Dictionary<string, object?>>(
                restored.Properties["values"]
                    .Value
                    .Value);

        Assert.IsType<byte>(
            values["byte"]);

        Assert.IsType<short>(
            values["short"]);

        Assert.IsType<int>(
            values["int"]);

        Assert.IsType<uint>(
            values["uint"]);

        Assert.IsType<long>(
            values["long"]);

        Assert.IsType<float>(
            values["float"]);

        Assert.IsType<double>(
            values["double"]);

        Assert.IsType<decimal>(
            values["decimal"]);

        Assert.Equal(
            (byte)7,
            values["byte"]);

        Assert.Equal(
            (short)12,
            values["short"]);

        Assert.Equal(
            42,
            values["int"]);

        Assert.Equal(
            (uint)84,
            values["uint"]);

        Assert.Equal(
            (long)168,
            values["long"]);

        Assert.Equal(
            1.25f,
            values["float"]);

        Assert.Equal(
            2.5d,
            values["double"]);

        Assert.Equal(
            3.75m,
            values["decimal"]);
    }

    [Fact]
    public void Serialize_ShouldRejectNestedNaN()
    {
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

        Assert.Throws<InvalidOperationException>(
            () => EntitySerializer.Serialize(entity));
    }

    [Fact]
    public void Serialize_ShouldRejectNestedPositiveInfinity()
    {
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
                    double.PositiveInfinity
                })
        };

        Assert.Throws<InvalidOperationException>(
            () => EntitySerializer.Serialize(entity));
    }

    [Fact]
    public void Serialize_ShouldRejectNestedNegativeInfinity()
    {
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

        Assert.Throws<InvalidOperationException>(
            () => EntitySerializer.Serialize(entity));
    }

    [Fact]
    public void Serialize_ShouldRejectUnsupportedNestedClrObject()
    {
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

        Assert.Throws<InvalidOperationException>(
            () => EntitySerializer.Serialize(entity));
    }

    private sealed class UnsupportedNestedObject
    {
        public string Value { get; } = "unsupported";
    }
}