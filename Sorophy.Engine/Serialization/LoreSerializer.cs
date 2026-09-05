/*
Sorophy Engine — a structured knowledge and graph engine
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
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Serialization;

public static class LoreSerializer
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(SorophyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        ValidateGraph(graph);

        var document = ToDocument(graph);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static SorophyGraph Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        LoreDocument document;

        try
        {
            document =
                JsonSerializer.Deserialize<LoreDocument>(
                    json,
                    JsonOptions)
                ?? throw new InvalidOperationException(
                    "The .lore document could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "The .lore document contains invalid JSON.",
                ex);
        }

        ValidateDocument(document);

        return FromDocument(document);
    }

    private static LoreDocument ToDocument(
        SorophyGraph graph)
    {
        var document = new LoreDocument
        {
            FormatVersion = CurrentFormatVersion
        };

        foreach (var entity in graph.Entities.Values)
        {
            ValidateEntity(entity);

            document.Entities.Add(
                new LoreEntityDocument
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Type = entity.Type,
                    Properties =
                        ConvertProperties(
                            entity.Properties)
                });
        }

        foreach (var relationship in graph.Relationships.Values)
        {
            ValidateRelationship(
                relationship,
                graph);

            document.Relationships.Add(
                new LoreRelationshipDocument
                {
                    Id = relationship.Id,
                    Type = relationship.Type,
                    SourceId = relationship.SourceId,
                    TargetId = relationship.TargetId,
                    ValidFrom =
                        SerializeTime(
                            relationship.ValidFrom),
                    ValidTill =
                        SerializeTime(
                            relationship.ValidTill),
                    Properties =
                        ConvertProperties(
                            relationship.Properties)
                });
        }

        return document;
    }

    private static SorophyGraph FromDocument(
        LoreDocument document)
    {
        var graph = new SorophyGraph();

        foreach (var entityDocument in document.Entities)
        {
            var entity = new SorophyEntity
            {
                Id = entityDocument.Id,
                Name = entityDocument.Name!,
                Type = entityDocument.Type
            };

            RestoreProperties(
                entity.Properties,
                entityDocument.Properties);

            graph.AddEntity(entity);
        }

        foreach (var relationshipDocument in document.Relationships)
        {
            if (!graph.Entities.ContainsKey(
                    relationshipDocument.SourceId))
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationshipDocument.Id}' references missing source entity '{relationshipDocument.SourceId}'.");
            }

            if (!graph.Entities.ContainsKey(
                    relationshipDocument.TargetId))
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationshipDocument.Id}' references missing target entity '{relationshipDocument.TargetId}'.");
            }

            SorophyRelationship relationship;

            try
            {
                relationship = new SorophyRelationship
                {
                    Id = relationshipDocument.Id,
                    Type = relationshipDocument.Type,
                    SourceId = relationshipDocument.SourceId,
                    TargetId = relationshipDocument.TargetId,
                    ValidFrom =
                        DeserializeTime(
                            relationshipDocument.ValidFrom,
                            $"Relationship '{relationshipDocument.Id}' ValidFrom"),
                    ValidTill =
                        DeserializeTime(
                            relationshipDocument.ValidTill,
                            $"Relationship '{relationshipDocument.Id}' ValidTill")
                };
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationshipDocument.Id}' contains invalid temporal validity data.",
                    ex);
            }

            RestoreProperties(
                relationship.Properties,
                relationshipDocument.Properties);

            graph.AddRelationship(relationship);
        }

        return graph;
    }

    private static LoreTimeDocument? SerializeTime(
        SorophyTime? time)
    {
        if (time is null)
        {
            return null;
        }

        return new LoreTimeDocument
        {
            Schema = new LoreTimeSchemaDocument
            {
                Timeline = time.Schema.Timeline,
                Units = SerializeTimeUnits(time.Schema)
            },
            Position = time.Position,
            Unit = time.Unit.Name,
            Precision = time.Precision.ToString()
        };
    }

    private static List<LoreTimeUnitDocument> SerializeTimeUnits(
        SorophyTimeSchema schema)
    {
        var units = new List<LoreTimeUnitDocument>();

        foreach (var unit in schema.Units)
        {
            units.Add(
                new LoreTimeUnitDocument
                {
                    Name = unit.Name,
                    Order = unit.Order,
                    PositionDefinition =
                        new LoreTimePositionDefinitionDocument
                        {
                            Kind = unit.PositionDefinition.Kind.ToString(),
                            Pattern = unit.PositionDefinition.Pattern
                        }
                });
        }

        return units;
    }

    private static SorophyTime? DeserializeTime(
        LoreTimeDocument? document,
        string context)
    {
        if (document is null)
        {
            return null;
        }

        if (document.Schema is null)
        {
            throw new InvalidOperationException(
                $"{context} is missing its temporal schema.");
        }

        if (document.Schema.Units is null)
        {
            throw new InvalidOperationException(
                $"{context} temporal schema units cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(document.Schema.Timeline))
        {
            throw new InvalidOperationException(
                $"{context} temporal schema timeline cannot be empty.");
        }

        var units = new List<SorophyTimeUnit>();

        foreach (var unitDocument in document.Schema.Units)
        {
            if (unitDocument is null)
            {
                throw new InvalidOperationException(
                    $"{context} temporal schema cannot contain a null unit.");
            }

            if (string.IsNullOrWhiteSpace(unitDocument.Name))
            {
                throw new InvalidOperationException(
                    $"{context} contains a temporal unit with an empty name.");
            }

            if (unitDocument.PositionDefinition is null)
            {
                throw new InvalidOperationException(
                    $"{context} temporal unit '{unitDocument.Name}' is missing its position definition.");
            }

            if (string.IsNullOrWhiteSpace(
                    unitDocument.PositionDefinition.Kind))
            {
                throw new InvalidOperationException(
                    $"{context} temporal unit '{unitDocument.Name}' is missing its position definition kind.");
            }

            if (!Enum.TryParse<SorophyTimePositionKind>(
                    unitDocument.PositionDefinition.Kind,
                    ignoreCase: true,
                    out var kind))
            {
                throw new InvalidOperationException(
                    $"{context} contains unknown temporal position kind '{unitDocument.PositionDefinition.Kind}'.");
            }

            SorophyTimePositionDefinition positionDefinition;

            try
            {
                positionDefinition =
                    new SorophyTimePositionDefinition(
                        kind,
                        unitDocument.PositionDefinition.Pattern);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"{context} contains an invalid position definition for temporal unit '{unitDocument.Name}'.",
                    ex);
            }

            try
            {
                units.Add(
                    new SorophyTimeUnit(
                        unitDocument.Name,
                        unitDocument.Order,
                        positionDefinition));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"{context} contains an invalid temporal unit '{unitDocument.Name}'.",
                    ex);
            }
        }

        SorophyTimeSchema schema;

        try
        {
            schema = new SorophyTimeSchema(
                document.Schema.Timeline,
                units);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"{context} contains an invalid temporal schema.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(document.Position))
        {
            throw new InvalidOperationException(
                $"{context} position cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(document.Unit))
        {
            throw new InvalidOperationException(
                $"{context} unit cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(document.Precision))
        {
            throw new InvalidOperationException(
                $"{context} precision cannot be empty.");
        }

        if (!Enum.TryParse<SorophyTimePrecision>(
                document.Precision,
                ignoreCase: true,
                out var precision))
        {
            throw new InvalidOperationException(
                $"{context} contains unknown temporal precision '{document.Precision}'.");
        }

        if (!schema.ContainsUnit(document.Unit))
        {
            throw new InvalidOperationException(
                $"{context} references temporal unit '{document.Unit}', which does not exist in its schema.");
        }

        try
        {
            return new SorophyTime(
                schema,
                document.Position,
                document.Unit,
                precision);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"{context} contains an invalid temporal point.",
                ex);
        }
    }

    private static Dictionary<string, EntityPropertyDocument>
        ConvertProperties(
            Dictionary<string, SorophyProperty> properties)
    {
        var result =
            new Dictionary<string, EntityPropertyDocument>();

        foreach (var property in properties)
        {
            ValidateProperty(
                property.Key,
                property.Value);

            result[property.Key] =
                new EntityPropertyDocument
                {
                    Type =
                        property.Value.Value.Type.ToString(),

                    Value =
                        SorophyValueCodec.Serialize(
                            property.Value.Value)
                };
        }

        return result;
    }

    private static void RestoreProperties(
        Dictionary<string, SorophyProperty> target,
        Dictionary<string, EntityPropertyDocument> source)
    {
        if (source is null)
        {
            throw new InvalidOperationException(
                "Properties cannot be null.");
        }

        foreach (var property in source)
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
                    "Property name cannot be empty.");
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

            ValidateSerializedValue(
                property.Value.Type,
                property.Value.Value);

            target[property.Key] =
                new SorophyProperty
                {
                    Name = property.Key,

                    Value =
                        SorophyValueCodec.Deserialize(
                            type,
                            property.Value.Value)
                };
        }
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
        LoreDocument document)
    {
        if (document.FormatVersion !=
            CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported lore format version: " +
                $"{document.FormatVersion}.");
        }

        if (document.Entities is null)
        {
            throw new InvalidOperationException(
                "Lore document entities cannot be null.");
        }

        if (document.Relationships is null)
        {
            throw new InvalidOperationException(
                "Lore document relationships cannot be null.");
        }

        var entityIds =
            new HashSet<Guid>();

        foreach (var entity in document.Entities)
        {
            if (entity is null)
            {
                throw new InvalidOperationException(
                    "Lore document cannot contain a null entity.");
            }

            if (entity.Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Lore entity must have a non-empty id.");
            }

            if (!entityIds.Add(entity.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate entity id '{entity.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    entity.Name))
            {
                throw new InvalidOperationException(
                    $"Lore entity '{entity.Id}' must have a non-empty name.");
            }

            if (entity.Properties is null)
            {
                throw new InvalidOperationException(
                    $"Lore entity '{entity.Id}' properties cannot be null.");
            }

            ValidateProperties(
                entity.Properties);
        }

        var relationshipIds =
            new HashSet<Guid>();

        foreach (var relationship in
                 document.Relationships)
        {
            if (relationship is null)
            {
                throw new InvalidOperationException(
                    "Lore document cannot contain a null relationship.");
            }

            if (relationship.Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Lore relationship must have a non-empty id.");
            }

            if (!relationshipIds.Add(
                    relationship.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate relationship id '{relationship.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    relationship.Type))
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.Id}' must have a type.");
            }

            if (!entityIds.Contains(
                    relationship.SourceId))
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.Id}' references missing source entity '{relationship.SourceId}'.");
            }

            if (!entityIds.Contains(
                    relationship.TargetId))
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.Id}' references missing target entity '{relationship.TargetId}'.");
            }

            if (relationship.Properties is null)
            {
                throw new InvalidOperationException(
                    $"Relationship '{relationship.Id}' properties cannot be null.");
            }

            ValidateProperties(
                relationship.Properties);

            ValidateSerializedTime(
                relationship.ValidFrom,
                $"Relationship '{relationship.Id}' ValidFrom");

            ValidateSerializedTime(
                relationship.ValidTill,
                $"Relationship '{relationship.Id}' ValidTill");
        }
    }

    private static void ValidateProperties(
        Dictionary<string, EntityPropertyDocument>
            properties)
    {
        foreach (var property in properties)
        {
            if (string.IsNullOrWhiteSpace(
                    property.Key))
            {
                throw new InvalidOperationException(
                    "Property name cannot be empty.");
            }

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

            ParseValueType(
                property.Value.Type);

            ValidateSerializedValue(
                property.Value.Type,
                property.Value.Value);
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

    private static void ValidateGraph(
        SorophyGraph graph)
    {
        if (graph.Entities is null)
        {
            throw new InvalidOperationException(
                "Graph entities cannot be null.");
        }

        if (graph.Relationships is null)
        {
            throw new InvalidOperationException(
                "Graph relationships cannot be null.");
        }

        var entityIds =
            new HashSet<Guid>();

        foreach (var entity in graph.Entities.Values)
        {
            ValidateEntity(entity);

            if (!entityIds.Add(entity.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate entity id '{entity.Id}'.");
            }
        }

        foreach (var relationship in
                 graph.Relationships.Values)
        {
            ValidateRelationship(
                relationship,
                graph);
        }
    }

    private static void ValidateEntity(
        SorophyEntity entity)
    {
        if (entity is null)
        {
            throw new InvalidOperationException(
                "Graph cannot contain a null entity.");
        }

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Entity must have a non-empty id.");
        }

        if (string.IsNullOrWhiteSpace(
                entity.Name))
        {
            throw new InvalidOperationException(
                "Entity must have a non-empty name.");
        }

        if (entity.Properties is null)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id}' properties cannot be null.");
        }
    }

    private static void ValidateRelationship(
        SorophyRelationship relationship,
        SorophyGraph graph)
    {
        if (relationship is null)
        {
            throw new InvalidOperationException(
                "Graph cannot contain a null relationship.");
        }

        if (relationship.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Relationship must have a non-empty id.");
        }

        if (string.IsNullOrWhiteSpace(
                relationship.Type))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Id}' must have a type.");
        }

        if (!graph.Entities.ContainsKey(
                relationship.SourceId))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Id}' references missing source entity '{relationship.SourceId}'.");
        }

        if (!graph.Entities.ContainsKey(
                relationship.TargetId))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Id}' references missing target entity '{relationship.TargetId}'.");
        }

        if (relationship.Properties is null)
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Id}' properties cannot be null.");
        }

        ValidateRelationshipTemporalState(relationship);
    }

    private static void ValidateSerializedTime(
        LoreTimeDocument? document,
        string context)
    {
        if (document is null)
        {
            return;
        }

        _ = DeserializeTime(document, context);
    }

    private static void ValidateRelationshipTemporalState(
        SorophyRelationship relationship)
    {
        if (relationship.ValidFrom is null ||
            relationship.ValidTill is null)
        {
            return;
        }

        if (!Equals(
                relationship.ValidFrom.Schema,
                relationship.ValidTill.Schema))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationship.Id}' ValidFrom and ValidTill must belong to the same temporal schema.");
        }
    }

    private static void ValidateProperty(
        string key,
        SorophyProperty property)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Property name cannot be empty.");
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
}