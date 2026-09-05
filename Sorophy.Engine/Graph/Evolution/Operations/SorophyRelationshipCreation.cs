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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.Evolution.Operations;

/// <summary>
/// Represents the creation of a new relationship.
/// </summary>
/// <remarks>
/// <para>
/// For this operation, <see cref="SorophyRelationshipEvolution.RelationshipId"/>
/// represents the identity being created rather than an already-existing
/// relationship.
/// </para>
///
/// <para>
/// The operation contains all relationship state required to construct
/// the new canonical relationship.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipCreation
    : SorophyRelationshipEvolution
{
    /// <summary>
    /// Gets the source entity of the relationship being created.
    /// </summary>
    public Guid SourceId { get; }

    /// <summary>
    /// Gets the target entity of the relationship being created.
    /// </summary>
    public Guid TargetId { get; }

    /// <summary>
    /// Gets the semantic type of the relationship being created.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the properties assigned to the relationship at creation.
    /// </summary>
    public IReadOnlyDictionary<string, SorophyProperty> Properties { get; }

    /// <summary>
    /// Gets the temporal point from which the created relationship is valid.
    /// </summary>
    public SorophyTime? ValidFrom { get; }

    /// <summary>
    /// Gets the temporal point until which the created relationship is valid.
    /// </summary>
    public SorophyTime? ValidTill { get; }

    /// <summary>
    /// Initializes a new relationship creation operation.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity to assign to the new relationship.
    /// </param>
    /// <param name="sourceId">
    /// The source entity identity.
    /// </param>
    /// <param name="targetId">
    /// The target entity identity.
    /// </param>
    /// <param name="type">
    /// The semantic relationship type.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which creation takes effect.
    /// </param>
    /// <param name="properties">
    /// Optional initial relationship properties.
    /// </param>
    /// <param name="validFrom">
    /// Optional temporal validity start.
    /// </param>
    /// <param name="validTill">
    /// Optional temporal validity end.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when an entity identity is empty or when the relationship
    /// type is blank.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when validity endpoints belong to different temporal schemas.
    /// </exception>
    public SorophyRelationshipCreation(
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        string type,
        SorophyTime effectiveTime,
        IReadOnlyDictionary<string, SorophyProperty>? properties = null,
        SorophyTime? validFrom = null,
        SorophyTime? validTill = null)
        : base(
            relationshipId,
            effectiveTime)
    {
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

        if (string.IsNullOrWhiteSpace(
                type))
        {
            throw new ArgumentException(
                "Relationship type cannot be null, empty, or whitespace.",
                nameof(type));
        }

        ValidateValiditySchema(
            validFrom,
            validTill);

        SourceId =
            sourceId;

        TargetId =
            targetId;

        Type =
            type;

        Properties =
            CopyProperties(
                properties);

        ValidFrom =
            validFrom;

        ValidTill =
            validTill;
    }

    private static IReadOnlyDictionary<string, SorophyProperty>
        CopyProperties(
            IReadOnlyDictionary<string, SorophyProperty>? properties)
    {
        var result =
            new Dictionary<string, SorophyProperty>(
                StringComparer.Ordinal);

        if (properties is null)
        {
            return result;
        }

        foreach (var pair in
                 properties)
        {
            if (string.IsNullOrWhiteSpace(
                    pair.Key))
            {
                throw new ArgumentException(
                    "Relationship property names cannot be null, empty, or whitespace.",
                    nameof(properties));
            }

            ArgumentNullException.ThrowIfNull(
                pair.Value);

            result.Add(
                pair.Key,
                pair.Value);
        }

        return result;
    }

    private static void ValidateValiditySchema(
        SorophyTime? validFrom,
        SorophyTime? validTill)
    {
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