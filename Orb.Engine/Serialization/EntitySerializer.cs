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
                    Value = JsonSerializer.SerializeToElement(
                        property.Value.Value.Value,
                        property.Value.Value.Value?.GetType()
                    )
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
            var type = ParseValueType(property.Value.Type);

            var value = DeserializeValue(
                type,
                property.Value.Value);

            entity.Properties[property.Key] =
                new OrbProperty
                {
                    Name = property.Key,
                    Value = new OrbValue(type, value)
                };
        }

        return entity;
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
                element,

            OrbValueType.Object =>
                element,

            _ => throw new InvalidOperationException(
                $"Unsupported OrbValueType '{type}'.")
        };
    }
}