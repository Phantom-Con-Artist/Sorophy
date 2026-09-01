using System.Text.Json;
using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Serialization;

public static class EntitySerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(OrbEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var document = ToDocument(entity);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static OrbEntity Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document = JsonSerializer.Deserialize<EntityDocument>(
            json,
            JsonOptions);

        if (document is null)
        {
            throw new InvalidOperationException(
                "The .entity document could not be deserialized.");
        }

        if (document.FormatVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported entity format version: " +
                $"{document.FormatVersion}.");
        }

        return FromDocument(document);
    }

    private static EntityDocument ToDocument(
        OrbEntity entity)
    {
        var document = new EntityDocument
        {
            FormatVersion = 1,
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type
        };

        foreach (var property in entity.Properties)
        {
            document.Properties[property.Key] =
                new EntityPropertyDocument
                {
                    Type = property.Value.Value.Type.ToString(),
                    Value = SerializeValue(
                        property.Value.Value)
                };
        }

        return document;
    }

    private static OrbEntity FromDocument(
        EntityDocument document)
    {
        var entity = new OrbEntity
        {
            Id = document.Id,
            Name = document.Name,
            Type = document.Type
        };

        foreach (var property in document.Properties)
        {
            var type = ParseValueType(
                property.Value.Type);

            var value = DeserializeValue(
                type,
                property.Value.Value);

            entity.Properties[property.Key] =
                new OrbProperty
                {
                    Name = property.Key,
                    Value = new OrbValue(
                        type,
                        value)
                };
        }

        return entity;
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