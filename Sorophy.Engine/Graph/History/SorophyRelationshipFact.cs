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
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Represents an immutable historical state of a relationship at a
/// specific point in the temporal model.
/// </summary>
/// <remarks>
/// <para>
/// A relationship fact records what was true about a relationship
/// at <see cref="At"/>, including its semantic type, properties,
/// endpoints, and validity interval.
/// </para>
///
/// <para>
/// Historical facts are append-only semantic records. They are not
/// backups or mutable snapshots of <see cref="SorophyRelationship"/>.
/// </para>
///
/// <para>
/// <see cref="At"/> identifies when this historical fact belongs in the
/// relationship's history. <see cref="ValidFrom"/> and
/// <see cref="ValidTill"/> describe the validity interval of the
/// relationship state represented by the fact. These are separate
/// temporal concepts and must not be conflated.
/// </para>
/// </remarks>
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
    /// Initializes a new historical relationship fact.
    /// </summary>
    /// <param name="at">
    /// The temporal point at which this fact is recorded.
    /// </param>
    /// <param name="relationshipId">
    /// The identity of the relationship represented by this fact.
    /// </param>
    /// <param name="sourceId">
    /// The source entity of the relationship.
    /// </param>
    /// <param name="targetId">
    /// The target entity of the relationship.
    /// </param>
    /// <param name="type">
    /// The semantic relationship type represented by the fact.
    /// </param>
    /// <param name="properties">
    /// Optional relationship properties represented by the fact.
    /// </param>
    /// <param name="validFrom">
    /// Optional validity start of the represented relationship state.
    /// </param>
    /// <param name="validTill">
    /// Optional validity end of the represented relationship state.
    /// </param>
    /// <param name="eventEntityId">
    /// Optional identity of the event entity that originated this fact.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="at"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an identity is empty, when the relationship type is
    /// blank, or when temporal schemas are inconsistent.
    /// </exception>
    public SorophyRelationshipFact(
        SorophyTime at,
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string type,
        IReadOnlyDictionary<string, SorophyProperty>? properties = null,
        SorophyTime? validFrom = null,
        SorophyTime? validTill = null,
        Guid? eventEntityId = null)
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