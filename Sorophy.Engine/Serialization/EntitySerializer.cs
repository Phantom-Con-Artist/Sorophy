/*
Sorophy™ — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Serialization;

public static class EntitySerializer
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateEntity(entity);

        var document = ToDocument(entity);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static SorophyEntity Deserialize(string json)
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
        SorophyEntity entity)
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
                    Type =
                        property.Value.Value.Type.ToString(),

                    Value =
                        SorophyValueCodec.Serialize(
                            property.Value.Value)
                };
        }

        return document;
    }

    private static SorophyEntity FromDocument(
        EntityDocument document)
    {
        var entity = new SorophyEntity
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

            if (string.IsNullOrWhiteSpace(
                    property.Value.Type))
            {
                throw new InvalidOperationException(
                    $"Property '{property.Key}' is missing its type.");
            }

            var type =
                ParseValueType(
                    property.Value.Type);

            entity.Properties[property.Key] =
                new SorophyProperty
                {
                    Name = property.Key,

                    Value =
                        SorophyValueCodec.Deserialize(
                            type,
                            property.Value.Value)
                };
        }

        return entity;
    }

    private static SorophyValueType ParseValueType(
        string type)
    {
        if (!Enum.TryParse<SorophyValueType>(
                type,
                ignoreCase: true,
                out var result))
        {
            throw new InvalidOperationException(
                $"Unknown SorophyValueType '{type}'.");
        }

        return result;
    }

    private static void ValidateDocument(
        EntityDocument document)
    {
        if (document.FormatVersion !=
            CurrentFormatVersion)
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

            if (string.IsNullOrWhiteSpace(
                    property.Key))
            {
                throw new InvalidOperationException(
                    "Entity document contains a property with an empty name.");
            }

            if (string.IsNullOrWhiteSpace(
                    property.Value.Type))
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
        SorophyEntity entity)
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
        SorophyProperty property)
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

        if (string.IsNullOrWhiteSpace(
                property.Name))
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
                $"Property '{key}' cannot contain a null SorophyValue.");
        }
    }

    private static void ValidateSerializedValue(
        string typeName,
        JsonElement element)
    {
        var type =
            ParseValueType(typeName);

        switch (type)
        {
            case SorophyValueType.Null:
                if (element.ValueKind !=
                    JsonValueKind.Null)
                {
                    throw new InvalidOperationException(
                        "Null SorophyValue must be represented by JSON null.");
                }

                break;

            case SorophyValueType.String:
                if (element.ValueKind !=
                    JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "String SorophyValue must be represented by a JSON string.");
                }

                break;

            case SorophyValueType.Boolean:
                if (element.ValueKind is not
                    (JsonValueKind.True or
                     JsonValueKind.False))
                {
                    throw new InvalidOperationException(
                        "Boolean SorophyValue must be represented by a JSON boolean.");
                }

                break;

            case SorophyValueType.Integer:
                if (element.ValueKind !=
                        JsonValueKind.Number ||
                    !element.TryGetInt64(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Integer SorophyValue must be represented by a valid Int64 JSON number.");
                }

                break;

            case SorophyValueType.Decimal:
                if (element.ValueKind !=
                        JsonValueKind.Number ||
                    !element.TryGetDecimal(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Decimal SorophyValue must be represented by a valid Decimal JSON number.");
                }

                break;

            case SorophyValueType.DateTime:
                if (element.ValueKind !=
                        JsonValueKind.String ||
                    !element.TryGetDateTime(
                        out _))
                {
                    throw new InvalidOperationException(
                        "DateTime SorophyValue must be represented by a valid DateTime JSON string.");
                }

                break;

            case SorophyValueType.Guid:
                if (element.ValueKind !=
                        JsonValueKind.String ||
                    !element.TryGetGuid(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Guid SorophyValue must be represented by a valid GUID JSON string.");
                }

                break;

            case SorophyValueType.List:
                if (element.ValueKind !=
                    JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        "List SorophyValue must be represented by a JSON array.");
                }

                break;

            case SorophyValueType.Object:
                if (element.ValueKind !=
                    JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        "Object SorophyValue must be represented by a JSON object.");
                }

                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported SorophyValueType '{type}'.");
        }
    }
}