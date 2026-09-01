using System.Text.Json;
using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Serialization;

public static class EntitySerializer
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(OrbEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateEntity(entity);

        var document = ToDocument(entity);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static OrbEntity Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        EntityDocument document;

        try
        {
            document =
                JsonSerializer.Deserialize<EntityDocument>(
                    json,
                    JsonOptions)
                ?? throw new InvalidOperationException(
                    "The .entity document could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "The .entity document contains invalid JSON.",
                ex);
        }

        ValidateDocument(document);

        return FromDocument(document);
    }

    private static EntityDocument ToDocument(
        OrbEntity entity)
    {
        var document = new EntityDocument
        {
            FormatVersion = CurrentFormatVersion,
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type
        };

        foreach (var property in entity.Properties)
        {
            ValidateProperty(
                property.Key,
                property.Value);

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
            Name = document.Name!,
            Type = document.Type
        };

        foreach (var property in document.Properties)
        {
            if (property.Value is null)
            {
                throw new InvalidOperationException(
                    $"Property '{property.Key}' cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(property.Value.Type))
            {
                throw new InvalidOperationException(
                    $"Property '{property.Key}' is missing its type.");
            }

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

        if (value.Value is null)
        {
            throw new InvalidOperationException(
                $"OrbValue of type '{value.Type}' cannot contain a null value.");
        }

        return JsonSerializer.SerializeToElement(
            value.Value,
            value.Value.GetType());
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
        try
        {
            return type switch
            {
                OrbValueType.Null =>
                    DeserializeNull(element),

                OrbValueType.String =>
                    DeserializeString(element),

                OrbValueType.Boolean =>
                    DeserializeBoolean(element),

                OrbValueType.Integer =>
                    DeserializeInteger(element),

                OrbValueType.Decimal =>
                    DeserializeDecimal(element),

                OrbValueType.DateTime =>
                    DeserializeDateTime(element),

                OrbValueType.Guid =>
                    DeserializeGuid(element),

                OrbValueType.List =>
                    DeserializeList(element),

                OrbValueType.Object =>
                    DeserializeObject(element),

                _ => throw new InvalidOperationException(
                    $"Unsupported OrbValueType '{type}'.")
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is FormatException ||
                  ex is OverflowException)
        {
            throw new InvalidOperationException(
                $"Invalid value for OrbValueType '{type}'.",
                ex);
        }
    }

    private static object? DeserializeNull(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidOperationException(
                "Null OrbValue must be represented by JSON null.");
        }

        return null;
    }

    private static string DeserializeString(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "String OrbValue must be represented by a JSON string.");
        }

        return element.GetString()
            ?? throw new InvalidOperationException(
                "String OrbValue cannot contain a null value.");
    }

    private static bool DeserializeBoolean(
        JsonElement element)
    {
        if (element.ValueKind is not
            (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidOperationException(
                "Boolean OrbValue must be represented by a JSON boolean.");
        }

        return element.GetBoolean();
    }

    private static long DeserializeInteger(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                "Integer OrbValue must be represented by a JSON number.");
        }

        return element.GetInt64();
    }

    private static decimal DeserializeDecimal(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                "Decimal OrbValue must be represented by a JSON number.");
        }

        return element.GetDecimal();
    }

    private static DateTime DeserializeDateTime(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "DateTime OrbValue must be represented by a JSON string.");
        }

        return element.GetDateTime();
    }

    private static Guid DeserializeGuid(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "Guid OrbValue must be represented by a JSON string.");
        }

        return element.GetGuid();
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

    private static void ValidateDocument(
        EntityDocument document)
    {
        if (document.FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported entity format version: " +
                $"{document.FormatVersion}.");
        }

        if (document.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Entity document must contain a valid non-empty id.");
        }

        if (document.Properties is null)
        {
            throw new InvalidOperationException(
                "Entity document properties cannot be null.");
        }

        foreach (var property in document.Properties)
        {
            if (property.Value is null)
            {
                throw new InvalidOperationException(
                    $"Property '{property.Key}' cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(property.Key))
            {
                throw new InvalidOperationException(
                    "Entity document contains a property with an empty name.");
            }

            if (string.IsNullOrWhiteSpace(property.Value.Type))
            {
                throw new InvalidOperationException(
                    $"Property '{property.Key}' is missing its type.");
            }

            _ = ParseValueType(
                property.Value.Type);

            ValidateSerializedValue(
                property.Value.Type,
                property.Value.Value);
        }
    }

    private static void ValidateEntity(
        OrbEntity entity)
    {
        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Entity must have a non-empty id.");
        }

        if (entity.Properties is null)
        {
            throw new InvalidOperationException(
                "Entity properties cannot be null.");
        }
    }

    private static void ValidateProperty(
        string key,
        OrbProperty property)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Entity contains a property with an empty name.");
        }

        if (property is null)
        {
            throw new InvalidOperationException(
                $"Property '{key}' cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(property.Name))
        {
            throw new InvalidOperationException(
                $"Property '{key}' has an empty name.");
        }

        if (!string.Equals(
                key,
                property.Name,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Property key '{key}' does not match property name '{property.Name}'.");
        }

        if (property.Value is null)
        {
            throw new InvalidOperationException(
                $"Property '{key}' cannot contain a null OrbValue.");
        }
    }

    private static void ValidateSerializedValue(
        string typeName,
        JsonElement element)
    {
        var type = ParseValueType(typeName);

        switch (type)
        {
            case OrbValueType.Null:
                if (element.ValueKind != JsonValueKind.Null)
                {
                    throw new InvalidOperationException(
                        "Null OrbValue must be represented by JSON null.");
                }
                break;

            case OrbValueType.String:
                if (element.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "String OrbValue must be represented by a JSON string.");
                }
                break;

            case OrbValueType.Boolean:
                if (element.ValueKind is not
                    (JsonValueKind.True or JsonValueKind.False))
                {
                    throw new InvalidOperationException(
                        "Boolean OrbValue must be represented by a JSON boolean.");
                }
                break;

            case OrbValueType.Integer:
                if (element.ValueKind != JsonValueKind.Number ||
                    !element.TryGetInt64(out _))
                {
                    throw new InvalidOperationException(
                        "Integer OrbValue must be represented by a valid Int64 JSON number.");
                }
                break;

            case OrbValueType.Decimal:
                if (element.ValueKind != JsonValueKind.Number ||
                    !element.TryGetDecimal(out _))
                {
                    throw new InvalidOperationException(
                        "Decimal OrbValue must be represented by a valid Decimal JSON number.");
                }
                break;

            case OrbValueType.DateTime:
                if (element.ValueKind != JsonValueKind.String ||
                    !element.TryGetDateTime(out _))
                {
                    throw new InvalidOperationException(
                        "DateTime OrbValue must be represented by a valid DateTime JSON string.");
                }
                break;

            case OrbValueType.Guid:
                if (element.ValueKind != JsonValueKind.String ||
                    !element.TryGetGuid(out _))
                {
                    throw new InvalidOperationException(
                        "Guid OrbValue must be represented by a valid GUID JSON string.");
                }
                break;

            case OrbValueType.List:
                if (element.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        "List OrbValue must be represented by a JSON array.");
                }
                break;

            case OrbValueType.Object:
                if (element.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        "Object OrbValue must be represented by a JSON object.");
                }
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported OrbValueType '{type}'.");
        }
    }
}