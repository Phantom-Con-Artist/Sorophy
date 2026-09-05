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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Serialization;

public static class LoreSerializer
{
    private const int CurrentFormatVersion = 2;
    private const int MinimumSupportedFormatVersion = 1;

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
            FormatVersion = CurrentFormatVersion,
            RelationshipHistories = new List<LoreRelationshipHistoryDocument>(),
            RetiredRelationshipIds = new List<Guid>()
        };

        foreach (var entity in graph.Entities.Values
                     .OrderBy(e => e.Id))
        {
            ValidateEntity(entity);

            document.Entities.Add(
                new LoreEntityDocument
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Type = entity.Type,
                    Description = entity.Description,
                    Tags = entity.Tags
                        .OrderBy(
                            tag => tag,
                            StringComparer.Ordinal)
                        .ToList(),
                    Documents =
                        ConvertDocuments(
                            entity.Documents),
                    Properties =
                        ConvertProperties(
                            entity.Properties)
                });
        }

        foreach (var relationship in graph.Relationships.Values
                     .OrderBy(r => r.Id))
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

        foreach (var history in graph.RelationshipHistories.Values
                     .OrderBy(h => h.RelationshipId))
        {
            ValidateRelationshipHistory(history);

            var historyDocument =
                new LoreRelationshipHistoryDocument
                {
                    RelationshipId = history.RelationshipId,
                    Facts = new List<LoreRelationshipFactDocument>()
                };

            foreach (var fact in history.Facts)
            {
                ValidateRelationshipFact(
                    fact,
                    history.RelationshipId);

                historyDocument.Facts.Add(
                    new LoreRelationshipFactDocument
                    {
                        At =
                            SerializeTime(
                                fact.At),
                        RelationshipId = fact.RelationshipId,
                        SourceId = fact.SourceId,
                        TargetId = fact.TargetId,
                        Type = fact.Type,
                        Properties =
                            ConvertProperties(
                                fact.Properties),
                        ValidFrom =
                            SerializeTime(
                                fact.ValidFrom),
                        ValidTill =
                            SerializeTime(
                                fact.ValidTill)
                    });
            }

            document.RelationshipHistories.Add(historyDocument);
        }

        foreach (var retiredId in graph.RetiredRelationshipIds
                     .OrderBy(id => id))
        {
            if (retiredId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Retired relationship ID cannot be empty.");
            }

            document.RetiredRelationshipIds.Add(retiredId);
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
                Type = entityDocument.Type,
                Description = entityDocument.Description
            };

            if (entityDocument.Tags is not null)
            {
                foreach (var tag in entityDocument.Tags)
                {
                    ValidateTag(tag);
                    entity.Tags.Add(tag);
                }
            }

            if (entityDocument.Documents is not null)
            {
                foreach (var documentEntry in entityDocument.Documents)
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
            }

            RestoreProperties(
                entity.Properties,
                entityDocument.Properties);

            graph.AddEntity(entity);
        }

        if (document.RetiredRelationshipIds is not null)
        {
            foreach (var retiredId in document.RetiredRelationshipIds)
            {
                graph.RetireRelationshipId(retiredId);
            }
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

        if (document.RelationshipHistories is not null)
        {
            foreach (var historyDocument in document.RelationshipHistories)
            {
                var history =
                    graph.GetOrCreateRelationshipHistory(
                        historyDocument.RelationshipId);

                foreach (var factDocument in historyDocument.Facts)
                {
                    var at =
                        DeserializeTime(
                            factDocument.At,
                            $"Relationship '{factDocument.RelationshipId}' fact At")
                        ?? throw new InvalidOperationException(
                            $"Relationship '{factDocument.RelationshipId}' fact is missing At temporal point.");

                    var validFrom =
                        DeserializeTime(
                            factDocument.ValidFrom,
                            $"Relationship '{factDocument.RelationshipId}' fact ValidFrom");

                    var validTill =
                        DeserializeTime(
                            factDocument.ValidTill,
                            $"Relationship '{factDocument.RelationshipId}' fact ValidTill");

                    var factProperties =
                        new Dictionary<string, SorophyProperty>(
                            StringComparer.Ordinal);

                    RestoreProperties(
                        factProperties,
                        factDocument.Properties);

                    SorophyRelationshipFact fact;

                    try
                    {
                        fact = new SorophyRelationshipFact(
                            at,
                            factDocument.RelationshipId,
                            factDocument.SourceId,
                            factDocument.TargetId,
                            factDocument.Type,
                            factProperties,
                            validFrom,
                            validTill);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidOperationException(
                            $"Relationship '{factDocument.RelationshipId}' fact contains invalid temporal or identity data.",
                            ex);
                    }

                    history.Add(fact);
                }
            }
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
            IReadOnlyDictionary<string, SorophyProperty> properties)
    {
        var result =
            new Dictionary<string, EntityPropertyDocument>(
                StringComparer.Ordinal);

        foreach (var property in properties
                     .OrderBy(
                         p => p.Key,
                         StringComparer.Ordinal))
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
        if (document.FormatVersion is null)
        {
            throw new InvalidOperationException(
                "Lore document is missing its format version.");
        }

        if (document.FormatVersion <
            MinimumSupportedFormatVersion ||
            document.FormatVersion >
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

            if (document.FormatVersion == 2)
            {
                if (entity.Tags is null)
                {
                    throw new InvalidOperationException(
                        $"Lore entity '{entity.Id}' tags cannot be null in format version 2.");
                }

                if (entity.Documents is null)
                {
                    throw new InvalidOperationException(
                        $"Lore entity '{entity.Id}' documents cannot be null in format version 2.");
                }
            }

            if (entity.Tags is not null)
            {
                var seenTags =
                    new HashSet<string>(StringComparer.Ordinal);

                foreach (var tag in entity.Tags)
                {
                    ValidateTag(tag);

                    if (!seenTags.Add(tag))
                    {
                        throw new InvalidOperationException(
                            $"Lore entity '{entity.Id}' contains duplicate tag '{tag}'.");
                    }
                }
            }

            if (entity.Documents is not null)
            {
                foreach (var documentEntry in entity.Documents)
                {
                    ValidateDocumentEntry(
                        documentEntry.Key,
                        documentEntry.Value);
                }
            }
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

            if (relationship.ValidFrom is not null &&
                relationship.ValidTill is not null)
            {
                var validFrom =
                    DeserializeTime(
                        relationship.ValidFrom,
                        $"Relationship '{relationship.Id}' ValidFrom");

                var validTill =
                    DeserializeTime(
                        relationship.ValidTill,
                        $"Relationship '{relationship.Id}' ValidTill");

                if (!Equals(
                        validFrom!.Schema,
                        validTill!.Schema))
                {
                    throw new InvalidOperationException(
                        $"Relationship '{relationship.Id}' ValidFrom and ValidTill must belong to the same temporal schema.");
                }
            }
        }

        if (document.FormatVersion == 2)
        {
            if (document.RelationshipHistories is null)
            {
                throw new InvalidOperationException(
                    "Lore document relationship histories cannot be null in format version 2.");
            }

            if (document.RetiredRelationshipIds is null)
            {
                throw new InvalidOperationException(
                    "Lore document retired relationship IDs cannot be null in format version 2.");
            }
        }

        var retiredIds =
            new HashSet<Guid>();

        if (document.RetiredRelationshipIds is not null)
        {
            foreach (var retiredId in document.RetiredRelationshipIds)
            {
                if (retiredId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "Retired relationship ID cannot be empty.");
                }

                if (!retiredIds.Add(retiredId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate retired relationship ID '{retiredId}'.");
                }

                if (relationshipIds.Contains(retiredId))
                {
                    throw new InvalidOperationException(
                        $"Relationship '{retiredId}' cannot be both active and retired.");
                }
            }
        }

        if (document.RelationshipHistories is not null)
        {
            var historyIds =
                new HashSet<Guid>();

            foreach (var history in document.RelationshipHistories)
            {
                if (history is null)
                {
                    throw new InvalidOperationException(
                        "Lore document cannot contain a null relationship history.");
                }

                if (history.RelationshipId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "Relationship history must have a non-empty relationship id.");
                }

                if (!historyIds.Add(history.RelationshipId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate relationship history for relationship id '{history.RelationshipId}'.");
                }

                if (history.Facts is null)
                {
                    throw new InvalidOperationException(
                        $"Relationship history '{history.RelationshipId}' facts cannot be null.");
                }

                foreach (var fact in history.Facts)
                {
                    if (fact is null)
                    {
                        throw new InvalidOperationException(
                            $"Relationship history '{history.RelationshipId}' cannot contain a null fact.");
                    }

                    if (fact.RelationshipId == Guid.Empty)
                    {
                        throw new InvalidOperationException(
                            $"Fact in history '{history.RelationshipId}' must have a non-empty relationship id.");
                    }

                    if (fact.RelationshipId != history.RelationshipId)
                    {
                        throw new InvalidOperationException(
                            $"Historical fact belongs to relationship '{fact.RelationshipId}', but this history belongs to relationship '{history.RelationshipId}'.");
                    }

                    if (fact.SourceId == Guid.Empty)
                    {
                        throw new InvalidOperationException(
                            $"Fact for relationship '{history.RelationshipId}' must have a non-empty source id.");
                    }

                    if (fact.TargetId == Guid.Empty)
                    {
                        throw new InvalidOperationException(
                            $"Fact for relationship '{history.RelationshipId}' must have a non-empty target id.");
                    }

                    if (string.IsNullOrWhiteSpace(fact.Type))
                    {
                        throw new InvalidOperationException(
                            $"Fact for relationship '{history.RelationshipId}' must have a type.");
                    }

                    if (fact.Properties is null)
                    {
                        throw new InvalidOperationException(
                            $"Fact for relationship '{history.RelationshipId}' properties cannot be null.");
                    }

                    ValidateProperties(fact.Properties);

                    if (fact.At is null)
                    {
                        throw new InvalidOperationException(
                            $"Fact for relationship '{history.RelationshipId}' must have an At temporal point.");
                    }

                    ValidateSerializedTime(
                        fact.At,
                        $"Relationship '{history.RelationshipId}' fact At");

                    ValidateSerializedTime(
                        fact.ValidFrom,
                        $"Relationship '{history.RelationshipId}' fact ValidFrom");

                    ValidateSerializedTime(
                        fact.ValidTill,
                        $"Relationship '{history.RelationshipId}' fact ValidTill");

                    ValidateFactTemporalSchemas(
                        fact,
                        history.RelationshipId);
                }
            }
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

        if (graph.RelationshipHistories is null)
        {
            throw new InvalidOperationException(
                "Graph relationship histories cannot be null.");
        }

        if (graph.RetiredRelationshipIds is null)
        {
            throw new InvalidOperationException(
                "Graph retired relationship IDs cannot be null.");
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

        var relationshipIds =
            new HashSet<Guid>();

        foreach (var relationship in
                 graph.Relationships.Values)
        {
            ValidateRelationship(
                relationship,
                graph);

            if (!relationshipIds.Add(relationship.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate relationship id '{relationship.Id}'.");
            }
        }

        var retiredIds =
            new HashSet<Guid>();

        foreach (var retiredId in graph.RetiredRelationshipIds)
        {
            if (retiredId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Retired relationship ID cannot be empty.");
            }

            if (!retiredIds.Add(retiredId))
            {
                throw new InvalidOperationException(
                    $"Duplicate retired relationship ID '{retiredId}'.");
            }

            if (relationshipIds.Contains(retiredId))
            {
                throw new InvalidOperationException(
                    $"Relationship '{retiredId}' cannot be both active and retired.");
            }
        }

        var historyIds =
            new HashSet<Guid>();

        foreach (var pair in graph.RelationshipHistories)
        {
            var relationshipId = pair.Key;
            var history = pair.Value;

            if (relationshipId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Relationship history store contains an empty relationship ID.");
            }

            if (history is null)
            {
                throw new InvalidOperationException(
                    $"Relationship history store contains a null history for relationship '{relationshipId}'.");
            }

            if (history.RelationshipId != relationshipId)
            {
                throw new InvalidOperationException(
                    $"Relationship history dictionary key '{relationshipId}' does not match history relationship ID '{history.RelationshipId}'.");
            }

            if (!historyIds.Add(history.RelationshipId))
            {
                throw new InvalidOperationException(
                    $"Duplicate relationship history for relationship id '{history.RelationshipId}'.");
            }

            ValidateRelationshipHistory(history);
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

        if (entity.Tags is null)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id}' tags cannot be null.");
        }

        if (entity.Documents is null)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id}' documents cannot be null.");
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

    private static void ValidateRelationshipHistory(
        SorophyRelationshipHistory history)
    {
        if (history is null)
        {
            throw new InvalidOperationException(
                "Relationship history cannot be null.");
        }

        if (history.RelationshipId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Relationship history must have a non-empty relationship ID.");
        }

        if (history.Facts is null)
        {
            throw new InvalidOperationException(
                $"Relationship history '{history.RelationshipId}' facts cannot be null.");
        }

        foreach (var fact in history.Facts)
        {
            ValidateRelationshipFact(
                fact,
                history.RelationshipId);
        }
    }

    private static void ValidateRelationshipFact(
        SorophyRelationshipFact fact,
        Guid expectedRelationshipId)
    {
        if (fact is null)
        {
            throw new InvalidOperationException(
                $"Relationship history '{expectedRelationshipId}' cannot contain a null fact.");
        }

        if (fact.RelationshipId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Fact in history '{expectedRelationshipId}' must have a non-empty relationship ID.");
        }

        if (fact.RelationshipId != expectedRelationshipId)
        {
            throw new InvalidOperationException(
                $"Historical fact belongs to relationship '{fact.RelationshipId}', but this history belongs to relationship '{expectedRelationshipId}'.");
        }

        if (fact.At is null)
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' must have an At temporal point.");
        }

        if (fact.SourceId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' must have a non-empty source ID.");
        }

        if (fact.TargetId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' must have a non-empty target ID.");
        }

        if (string.IsNullOrWhiteSpace(fact.Type))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' must have a type.");
        }

        if (fact.Properties is null)
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' properties cannot be null.");
        }

        foreach (var property in fact.Properties)
        {
            ValidateProperty(
                property.Key,
                property.Value);
        }

        if (fact.ValidFrom is not null &&
            !Equals(
                fact.At.Schema,
                fact.ValidFrom.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' At and ValidFrom must belong to the same temporal schema.");
        }

        if (fact.ValidTill is not null &&
            !Equals(
                fact.At.Schema,
                fact.ValidTill.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' At and ValidTill must belong to the same temporal schema.");
        }

        if (fact.ValidFrom is not null &&
            fact.ValidTill is not null &&
            !Equals(
                fact.ValidFrom.Schema,
                fact.ValidTill.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{expectedRelationshipId}' ValidFrom and ValidTill must belong to the same temporal schema.");
        }
    }

    private static void ValidateFactTemporalSchemas(
        LoreRelationshipFactDocument fact,
        Guid relationshipId)
    {
        var at =
            DeserializeTime(
                fact.At,
                $"Relationship '{relationshipId}' fact At")!;

        var validFrom =
            DeserializeTime(
                fact.ValidFrom,
                $"Relationship '{relationshipId}' fact ValidFrom");

        var validTill =
            DeserializeTime(
                fact.ValidTill,
                $"Relationship '{relationshipId}' fact ValidTill");

        if (validFrom is not null &&
            !Equals(
                at.Schema,
                validFrom.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{relationshipId}' At and ValidFrom must belong to the same temporal schema.");
        }

        if (validTill is not null &&
            !Equals(
                at.Schema,
                validTill.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{relationshipId}' At and ValidTill must belong to the same temporal schema.");
        }

        if (validFrom is not null &&
            validTill is not null &&
            !Equals(
                validFrom.Schema,
                validTill.Schema))
        {
            throw new InvalidOperationException(
                $"Fact for relationship '{relationshipId}' ValidFrom and ValidTill must belong to the same temporal schema.");
        }
    }

    private static Dictionary<string, EntityEmbeddedDocument>
        ConvertDocuments(
            Dictionary<string, SorophyEntityDocument> documents)
    {
        var result =
            new Dictionary<string, EntityEmbeddedDocument>(
                StringComparer.Ordinal);

        foreach (var entry in documents
                     .OrderBy(
                         d => d.Key,
                         StringComparer.Ordinal))
        {
            ValidateDocumentEntry(
                entry.Key,
                entry.Value);

            result[entry.Key] =
                new EntityEmbeddedDocument
                {
                    ContentType =
                        entry.Value.ContentType,
                    Content =
                        entry.Value.Content
                };
        }

        return result;
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
}
