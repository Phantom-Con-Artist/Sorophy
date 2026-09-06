/*
 * Sorophy Engine — a structured knowledge and graph engine
 * Copyright (C) 2026  Subhradeep Sarkar
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Serialization;

public static class EntitySerializer
{
    private const int CurrentFormatVersion = 2;
    private const int MinimumSupportedFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> EmptyTagList =
        new(0);

    private static readonly Dictionary<string, EntityPropertyDocument> EmptyPropertyDict =
        new(0, StringComparer.Ordinal);

    private static readonly Dictionary<string, EntityEmbeddedDocument> EmptyDocumentDict =
        new(0, StringComparer.Ordinal);

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

    private static EntityDocument ToDocument(SorophyEntity entity)
    {
        var document = new EntityDocument
        {
            FormatVersion = CurrentFormatVersion,
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            Tags = entity.Tags.Count switch
            {
                0 => EmptyTagList,
                1 => new List<string>(1) { entity.Tags.First() },
                _ => entity.Tags
                    .OrderBy(
                        tag => tag,
                        StringComparer.Ordinal)
                    .ToList()
            },
            Properties = entity.Properties.Count == 0
                ? EmptyPropertyDict
                : new Dictionary<string, EntityPropertyDocument>(
                    entity.Properties.Count,
                    StringComparer.Ordinal),
            Documents = entity.Documents.Count == 0
                ? EmptyDocumentDict
                : new Dictionary<string, EntityEmbeddedDocument>(
                    entity.Documents.Count,
                    StringComparer.Ordinal)
        };

        if (entity.Properties.Count > 0)
        {
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
        }

        if (entity.Documents.Count == 1)
        {
            using var enumerator = entity.Documents.GetEnumerator();
            enumerator.MoveNext();
            var documentEntry = enumerator.Current;

            ValidateDocumentEntry(
                documentEntry.Key,
                documentEntry.Value);

            document.Documents[documentEntry.Key] =
                new EntityEmbeddedDocument
                {
                    ContentType =
                        documentEntry.Value.ContentType,
                    Content =
                        documentEntry.Value.Content
                };
        }
        else if (entity.Documents.Count > 1)
        {
            foreach (var documentEntry in entity.Documents
                         .OrderBy(
                             entry => entry.Key,
                             StringComparer.Ordinal))
            {
                ValidateDocumentEntry(
                    documentEntry.Key,
                    documentEntry.Value);

                document.Documents[documentEntry.Key] =
                    new EntityEmbeddedDocument
                    {
                        ContentType =
                            documentEntry.Value.ContentType,
                        Content =
                            documentEntry.Value.Content
                    };
            }
        }

        return document;
    }

    private static SorophyEntity FromDocument(EntityDocument document)
    {
        var entity = new SorophyEntity
        {
            Id = document.Id,
            Name = document.Name,
            Type = document.Type,
            Description = document.Description
        };

        foreach (var tag in document.Tags)
        {
            ValidateTag(tag);
            entity.Tags.Add(tag);
        }

        foreach (var documentEntry in document.Documents)
        {
            ValidateDocumentEntry(
                documentEntry.Key,
                documentEntry.Value);

            entity.Documents[documentEntry.Key] =
                new SorophyEntityDocument(
                    documentEntry.Key,
                    documentEntry.Value.Content!,
                    documentEntry.Value.ContentType!);
        }

        if (document.Properties.Count > 0)
        {
            entity.Properties.EnsureCapacity(document.Properties.Count);
        }

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

    private static SorophyValueType ParseValueType(string type)
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
        if (document.FormatVersion is null)
        {
            throw new InvalidOperationException(
                "Entity document is missing its format version.");
        }

        if (document.FormatVersion <
            MinimumSupportedFormatVersion ||
            document.FormatVersion >
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

        if (document.Tags is null)
        {
            throw new InvalidOperationException(
                "Entity document tags cannot be null.");
        }

        if (document.Documents is null)
        {
            throw new InvalidOperationException(
                "Entity document documents cannot be null.");
        }

        var seenTags =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (var tag in document.Tags)
        {
            ValidateTag(tag);

            if (!seenTags.Add(tag))
            {
                throw new InvalidOperationException(
                    $"Entity document contains duplicate tag '{tag}'.");
            }
        }

        foreach (var documentEntry in document.Documents)
        {
            ValidateDocumentEntry(
                documentEntry.Key,
                documentEntry.Value);
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

    private static void ValidateEntity(SorophyEntity entity)
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

        if (entity.Tags is null)
        {
            throw new InvalidOperationException(
                "Entity tags cannot be null.");
        }

        if (entity.Documents is null)
        {
            throw new InvalidOperationException(
                "Entity documents cannot be null.");
        }

        foreach (var tag in entity.Tags)
        {
            ValidateTag(tag);
        }

        foreach (var documentEntry in entity.Documents)
        {
            ValidateDocumentEntry(
                documentEntry.Key,
                documentEntry.Value);
        }
    }

    private static void ValidateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException(
                "Entity contains an empty tag.");
        }
    }

    private static void ValidateDocumentEntry(
        string name,
        SorophyEntityDocument? document)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Entity contains an embedded document with an empty name.");
        }

        if (document is null)
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' has an empty document name.");
        }

        if (!string.Equals(
                name,
                document.Name,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Embedded document key '{name}' does not match document name '{document.Name}'.");
        }

        if (string.IsNullOrWhiteSpace(document.ContentType))
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' must have a content type.");
        }

        if (document.Content is null)
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' cannot contain null content.");
        }
    }

    private static void ValidateDocumentEntry(
        string name,
        EntityEmbeddedDocument? document)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Entity document contains an embedded document with an empty name.");
        }

        if (document is null)
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(document.ContentType))
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' must have a content type.");
        }

        if (document.Content is null)
        {
            throw new InvalidOperationException(
                $"Embedded document '{name}' cannot contain null content.");
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
                if (element.ValueKind != JsonValueKind.Null)
                {
                    throw new InvalidOperationException(
                        "Null SorophyValue must be represented by JSON null.");
                }

                break;

            case SorophyValueType.String:
                if (element.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "String SorophyValue must be represented by a JSON string.");
                }

                break;

            case SorophyValueType.Boolean:
                if (element.ValueKind is not
                    (JsonValueKind.True or JsonValueKind.False))
                {
                    throw new InvalidOperationException(
                        "Boolean SorophyValue must be represented by a JSON boolean.");
                }

                break;

            case SorophyValueType.Integer:
                if (element.ValueKind != JsonValueKind.Number ||
                    !element.TryGetInt64(out _))
                {
                    throw new InvalidOperationException(
                        "Integer SorophyValue must be represented by a valid Int64 JSON number.");
                }

                break;

            case SorophyValueType.Decimal:
                if (element.ValueKind != JsonValueKind.Number ||
                    !element.TryGetDecimal(out _))
                {
                    throw new InvalidOperationException(
                        "Decimal SorophyValue must be represented by a valid Decimal JSON number.");
                }

                break;

            case SorophyValueType.DateTime:
                if (element.ValueKind != JsonValueKind.String ||
                    !element.TryGetDateTime(out _))
                {
                    throw new InvalidOperationException(
                        "DateTime SorophyValue must be represented by a valid DateTime JSON string.");
                }

                break;

            case SorophyValueType.Guid:
                if (element.ValueKind != JsonValueKind.String ||
                    !element.TryGetGuid(out _))
                {
                    throw new InvalidOperationException(
                        "Guid SorophyValue must be represented by a valid GUID JSON string.");
                }

                break;

            case SorophyValueType.List:
                if (element.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        "List SorophyValue must be represented by a JSON array.");
                }

                break;

            case SorophyValueType.Object:
                if (element.ValueKind != JsonValueKind.Object)
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
