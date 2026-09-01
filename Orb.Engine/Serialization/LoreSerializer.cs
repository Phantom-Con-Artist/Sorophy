using System.Text.Json;
using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Serialization;

public static class LoreSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(OrbGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var document = ToDocument(graph);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static OrbGraph Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document = JsonSerializer.Deserialize<LoreDocument>(
            json,
            JsonOptions);

        if (document is null)
        {
            throw new InvalidOperationException(
                "The .lore document could not be deserialized.");
        }

        if (document.FormatVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported lore format version: " +
                $"{document.FormatVersion}.");
        }

        return FromDocument(document);
    }

    private static LoreDocument ToDocument(
        OrbGraph graph)
    {
        var document = new LoreDocument
        {
            FormatVersion = 1
        };

        foreach (var entity in graph.Entities.Values)
        {
            document.Entities.Add(
                new LoreEntityDocument
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Type = entity.Type,
                    Properties = ConvertProperties(
                        entity.Properties)
                });
        }

        foreach (var relationship in graph.Relationships.Values)
        {
            document.Relationships.Add(
                new LoreRelationshipDocument
                {
                    Id = relationship.Id,
                    Type = relationship.Type,
                    SourceId = relationship.SourceId,
                    TargetId = relationship.TargetId,
                    Properties = ConvertProperties(
                        relationship.Properties)
                });
        }

        return document;
    }

    private static OrbGraph FromDocument(
        LoreDocument document)
    {
        var graph = new OrbGraph();

        foreach (var entityDocument in document.Entities)
        {
            var entity = new OrbEntity
            {
                Id = entityDocument.Id,
                Name = entityDocument.Name,
                Type = entityDocument.Type
            };

            RestoreProperties(
                entity.Properties,
                entityDocument.Properties);

            graph.AddEntity(entity);
        }

        foreach (var relationshipDocument in document.Relationships)
        {
            var relationship = new OrbRelationship
            {
                Id = relationshipDocument.Id,
                Type = relationshipDocument.Type,
                SourceId = relationshipDocument.SourceId,
                TargetId = relationshipDocument.TargetId
            };

            RestoreProperties(
                relationship.Properties,
                relationshipDocument.Properties);

            graph.AddRelationship(relationship);
        }

        return graph;
    }

    private static Dictionary<string, EntityPropertyDocument>
        ConvertProperties(
            Dictionary<string, OrbProperty> properties)
    {
        var result =
            new Dictionary<string, EntityPropertyDocument>();

        foreach (var property in properties)
        {
            result[property.Key] =
                new EntityPropertyDocument
                {
                    Type = property.Value.Value.Type.ToString(),
                    Value = SerializeValue(
                        property.Value.Value)
                };
        }

        return result;
    }

    private static void RestoreProperties(
        Dictionary<string, OrbProperty> target,
        Dictionary<string, EntityPropertyDocument> source)
    {
        foreach (var property in source)
        {
            var type = ParseValueType(
                property.Value.Type);

            var value = DeserializeValue(
                type,
                property.Value.Value);

            target[property.Key] =
                new OrbProperty
                {
                    Name = property.Key,
                    Value = new OrbValue(
                        type,
                        value)
                };
        }
    }

    private static JsonElement SerializeValue(
        OrbValue value)
    {
        if (value.Type == OrbValueType.Null)
        {
            return JsonSerializer.SerializeToElement<object?>(
                null);
        }

        return JsonSerializer.SerializeToElement(
            value.Value,
            value.Value!.GetType());
    }

    private static OrbValueType ParseValueType(
        string type)
    {
        if (!Enum.TryParse<OrbValueType>(
                type,
                ignoreCase: true,
                out var result))
        {
            throw new InvalidOperationException(
                $"Unknown OrbValueType '{type}'.");
        }

        return result;
    }

    private static object? DeserializeValue(
        OrbValueType type,
        JsonElement element)
    {
        return type switch
        {
            OrbValueType.Null =>
                null,

            OrbValueType.String =>
                element.GetString(),

            OrbValueType.Boolean =>
                element.GetBoolean(),

            OrbValueType.Integer =>
                element.GetInt64(),

            OrbValueType.Decimal =>
                element.GetDecimal(),

            OrbValueType.DateTime =>
                element.GetDateTime(),

            OrbValueType.Guid =>
                element.GetGuid(),

            OrbValueType.List =>
                DeserializeList(element),

            OrbValueType.Object =>
                DeserializeObject(element),

            _ => throw new InvalidOperationException(
                $"Unsupported OrbValueType '{type}'.")
        };
    }

    private static List<object?> DeserializeList(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "List OrbValue must be represented by a JSON array.");
        }

        var result = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            result.Add(
                DeserializeJsonElement(item));
        }

        return result;
    }

    private static Dictionary<string, object?> DeserializeObject(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Object OrbValue must be represented by a JSON object.");
        }

        var result =
            new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] =
                DeserializeJsonElement(property.Value);
        }

        return result;
    }

    private static object? DeserializeJsonElement(
        JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null =>
                null,

            JsonValueKind.String =>
                element.GetString(),

            JsonValueKind.True =>
                true,

            JsonValueKind.False =>
                false,

            JsonValueKind.Number =>
                DeserializeNumber(element),

            JsonValueKind.Array =>
                DeserializeList(element),

            JsonValueKind.Object =>
                DeserializeObject(element),

            _ => throw new InvalidOperationException(
                $"Unsupported JSON value kind '{element.ValueKind}'.")
        };
    }

    private static object DeserializeNumber(
        JsonElement element)
    {
        if (element.TryGetInt64(out var integer))
        {
            return integer;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }
}