// Sorophyis Project
// Copyright (C) 2026 Phantom-Con-Artist
//
// This file is part of the Sorophyis Project.
//
// The Sorophyis Project is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// The Sorophyis Project is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Sorophy.Engine.Diff;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Represents an immutable historical state or mutation of a relationship at a
/// specific point in the temporal model.
/// </summary>
public sealed class SorophyRelationshipFact
{
    /// <summary>
    /// Gets the temporal point at which this historical fact is recorded.
    /// </summary>
    public SorophyTime At { get; }

    /// <summary>
    /// Gets the identity of the relationship this fact describes.
    /// </summary>
    public Guid RelationshipId { get; }

    /// <summary>
    /// Gets the source entity of the relationship.
    /// </summary>
    public Guid SourceId { get; }

    /// <summary>
    /// Gets the target entity of the relationship.
    /// </summary>
    public Guid TargetId { get; }

    /// <summary>
    /// Gets the semantic type of the relationship at the time represented
    /// by this fact.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the authored sequence order within this relationship's history.
    /// </summary>
    public long Sequence { get; internal set; }

    /// <summary>
    /// Gets the properties associated with the relationship at the time
    /// represented by this fact.
    /// </summary>
    public IReadOnlyDictionary<string, SorophyProperty> Properties { get; }

    /// <summary>
    /// Gets the time from which the historical relationship state was valid.
    /// </summary>
    public SorophyTime? ValidFrom { get; }

    /// <summary>
    /// Gets the time until which the historical relationship state was valid.
    /// </summary>
    public SorophyTime? ValidTill { get; }

    /// <summary>
    /// Gets the identity of the event entity that originated this fact,
    /// or null when the fact is not associated with an event entity.
    /// </summary>
    public Guid? EventEntityId { get; }

    /// <summary>
    /// Gets the lifecycle transition kind represented by this fact,
    /// or null when the fact represents a legacy or mutation fact.
    /// </summary>
    public SorophyRelationshipFactKind? Kind { get; }

    /// <summary>
    /// Gets the name of the property that changed, if this fact represents a property mutation.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the previous value of the property prior to this mutation, if applicable.
    /// </summary>
    public SorophyValue? PreviousValue { get; internal set; }

    /// <summary>
    /// Gets the new value of the property established by this mutation, if applicable.
    /// </summary>
    public SorophyValue? NewValue { get; }

    /// <summary>
    /// Gets the previous semantic type prior to this mutation, if applicable.
    /// </summary>
    public string? PreviousType { get; internal set; }

    /// <summary>
    /// Gets the new semantic type established by this mutation, if applicable.
    /// </summary>
    public string? NewType { get; }

    /// <summary>
    /// Gets an optional authored description or rationale for this transition.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new historical relationship fact.
    /// </summary>
    public SorophyRelationshipFact(
        SorophyTime at,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string type,
        IReadOnlyDictionary<string, SorophyProperty>? properties = null,
        SorophyTime? validFrom = null,
        SorophyTime? validTill = null,
        Guid? eventEntityId = null,
        SorophyRelationshipFactKind? kind = null,
        string? description = null,
        long sequence = 0,
        string? propertyName = null,
        SorophyValue? previousValue = null,
        SorophyValue? newValue = null,
        string? previousType = null,
        string? newType = null)
    {
        ArgumentNullException.ThrowIfNull(
            at);

        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        if (eventEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event entity ID cannot be empty when provided.",
                nameof(eventEntityId));
        }

        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Source ID cannot be empty.",
                nameof(sourceId));
        }

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Target ID cannot be empty.",
                nameof(targetId));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(
                "Relationship type cannot be null, empty, or whitespace.",
                nameof(type));
        }

        if (kind is not null && !Enum.IsDefined(kind.Value))
        {
            throw new ArgumentException(
                $"Undefined relationship fact kind '{kind.Value}'.",
                nameof(kind));
        }

        ValidateTemporalSchemas(
            at,
            validFrom,
            validTill);

        At =
            at;

        RelationshipId =
            relationshipId;

        SourceId =
            sourceId;

        TargetId =
            targetId;

        Type =
            type;

        Properties =
            new Dictionary<string, SorophyProperty>(
                properties ??
                new Dictionary<string, SorophyProperty>(),
                StringComparer.Ordinal);

        ValidFrom =
            validFrom;

        ValidTill =
            validTill;

        EventEntityId =
            eventEntityId;

        Kind =
            kind;

        Description =
            description;

        Sequence =
            sequence;

        PropertyName =
            propertyName;

        PreviousValue =
            previousValue is not null ? SorophyValueCloner.CloneValue(previousValue) : null;

        NewValue =
            newValue is not null ? SorophyValueCloner.CloneValue(newValue) : null;

        PreviousType =
            previousType;

        NewType =
            newType;
    }

    /// <summary>
    /// Factory method to create an authored lifecycle relationship fact.
    /// </summary>
    public static SorophyRelationshipFact CreateLifecycleFact(
        SorophyTime at,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string type,
        SorophyRelationshipFactKind kind,
        IReadOnlyDictionary<string, SorophyProperty>? properties = null,
        string? description = null,
        long sequence = 0) =>
        new(
            at,
            relationshipId,
            sourceId,
            targetId,
            type,
            properties: properties,
            validFrom: null,
            validTill: null,
            eventEntityId: null,
            kind: kind,
            description: description,
            sequence: sequence);

    /// <summary>
    /// Factory method to create an authored relationship property change fact.
    /// </summary>
    public static SorophyRelationshipFact CreatePropertyChangeFact(
        SorophyTime at,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string type,
        string propertyName,
        SorophyValue? previousValue,
        SorophyValue? newValue,
        long sequence = 0,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException(
                "Property name cannot be null, empty, or whitespace.",
                nameof(propertyName));
        }

        return new SorophyRelationshipFact(
            at,
            relationshipId,
            sourceId,
            targetId,
            type,
            properties: null,
            validFrom: null,
            validTill: null,
            eventEntityId: null,
            kind: SorophyRelationshipFactKind.PropertyChanged,
            description: description,
            sequence: sequence,
            propertyName: propertyName,
            previousValue: previousValue,
            newValue: newValue);
    }

    /// <summary>
    /// Factory method to create an authored relationship type change fact.
    /// </summary>
    public static SorophyRelationshipFact CreateTypeChangeFact(
        SorophyTime at,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string previousType,
        string newType,
        IReadOnlyDictionary<string, SorophyProperty>? properties = null,
        long sequence = 0,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(previousType))
        {
            throw new ArgumentException(
                "Previous type cannot be null, empty, or whitespace.",
                nameof(previousType));
        }

        if (string.IsNullOrWhiteSpace(newType))
        {
            throw new ArgumentException(
                "New type cannot be null, empty, or whitespace.",
                nameof(newType));
        }

        return new SorophyRelationshipFact(
            at,
            relationshipId,
            sourceId,
            targetId,
            type: newType,
            properties: properties,
            validFrom: null,
            validTill: null,
            eventEntityId: null,
            kind: SorophyRelationshipFactKind.RelationshipChanged,
            description: description,
            sequence: sequence,
            propertyName: "Type",
            previousValue: new SorophyValue(SorophyValueType.String, previousType),
            newValue: new SorophyValue(SorophyValueType.String, newType),
            previousType: previousType,
            newType: newType);
    }

    /// <summary>
    /// Ensures that all supplied temporal values belong to the same
    /// temporal schema.
    /// </summary>
    private static void ValidateTemporalSchemas(
        SorophyTime at,
        SorophyTime? validFrom,
        SorophyTime? validTill)
    {
        if (validFrom is not null &&
            !Equals(
                at.Schema,
                validFrom.Schema))
        {
            throw new ArgumentException(
                "At and ValidFrom must belong to the same temporal schema.",
                nameof(validFrom));
        }

        if (validTill is not null &&
            !Equals(
                at.Schema,
                validTill.Schema))
        {
            throw new ArgumentException(
                "At and ValidTill must belong to the same temporal schema.",
                nameof(validTill));
        }

        if (validFrom is not null &&
            validTill is not null &&
            !Equals(
                validFrom.Schema,
                validTill.Schema))
        {
            throw new ArgumentException(
                "ValidFrom and ValidTill must belong to the same temporal schema.");
        }
    }
}