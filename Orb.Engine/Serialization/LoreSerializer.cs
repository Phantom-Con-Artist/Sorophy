using System.Text.Json;
using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Serialization;

public static class LoreSerializer
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(OrbGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        ValidateGraph(graph);

        var document = ToDocument(graph);

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public static OrbGraph Deserialize(string json)
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
        OrbGraph graph)
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
                    Properties =
                        ConvertProperties(
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
            ValidateProperty(
                property.Key,
                property.Value);

            result[property.Key] =
                new EntityPropertyDocument
                {
                    Type =
                        property.Value.Value.Type.ToString(),

                    Value =
                        OrbValueCodec.Serialize(
                            property.Value.Value)
                };
        }

        return result;
    }

    private static void RestoreProperties(
        Dictionary<string, OrbProperty> target,
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
                new OrbProperty
                {
                    Name = property.Key,

                    Value =
                        OrbValueCodec.Deserialize(
                            type,
                            property.Value.Value)
                };
        }
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
            case OrbValueType.Null:
                if (element.ValueKind !=
                    JsonValueKind.Null)
                {
                    throw new InvalidOperationException(
                        "Null OrbValue must be represented by JSON null.");
                }

                break;

            case OrbValueType.String:
                if (element.ValueKind !=
                    JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        "String OrbValue must be represented by a JSON string.");
                }

                break;

            case OrbValueType.Boolean:
                if (element.ValueKind is not
                    (JsonValueKind.True or
                     JsonValueKind.False))
                {
                    throw new InvalidOperationException(
                        "Boolean OrbValue must be represented by a JSON boolean.");
                }

                break;

            case OrbValueType.Integer:
                if (element.ValueKind !=
                        JsonValueKind.Number ||
                    !element.TryGetInt64(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Integer OrbValue must be represented by a valid Int64 JSON number.");
                }

                break;

            case OrbValueType.Decimal:
                if (element.ValueKind !=
                        JsonValueKind.Number ||
                    !element.TryGetDecimal(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Decimal OrbValue must be represented by a valid Decimal JSON number.");
                }

                break;

            case OrbValueType.DateTime:
                if (element.ValueKind !=
                        JsonValueKind.String ||
                    !element.TryGetDateTime(
                        out _))
                {
                    throw new InvalidOperationException(
                        "DateTime OrbValue must be represented by a valid DateTime JSON string.");
                }

                break;

            case OrbValueType.Guid:
                if (element.ValueKind !=
                        JsonValueKind.String ||
                    !element.TryGetGuid(
                        out _))
                {
                    throw new InvalidOperationException(
                        "Guid OrbValue must be represented by a valid GUID JSON string.");
                }

                break;

            case OrbValueType.List:
                if (element.ValueKind !=
                    JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        "List OrbValue must be represented by a JSON array.");
                }

                break;

            case OrbValueType.Object:
                if (element.ValueKind !=
                    JsonValueKind.Object)
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

    private static void ValidateGraph(
        OrbGraph graph)
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
        OrbEntity entity)
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
        OrbRelationship relationship,
        OrbGraph graph)
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
    }

    private static void ValidateProperty(
        string key,
        OrbProperty property)
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
                $"Property '{key}' cannot contain a null OrbValue.");
        }
    }
}