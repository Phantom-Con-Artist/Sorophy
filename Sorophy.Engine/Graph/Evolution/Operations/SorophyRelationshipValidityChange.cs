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
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.Evolution.Operations;

/// <summary>
/// Represents a change to the temporal validity interval of a relationship.
/// </summary>
/// <remarks>
/// <para>
/// Either validity endpoint may be null. A null <see cref="NewValidFrom"/>
/// represents an unbounded validity start, while a null
/// <see cref="NewValidTill"/> represents an unbounded validity end.
/// </para>
///
/// <para>
/// This operation does not compare temporal positions itself. It only
/// requires that both supplied endpoints use the same temporal schema.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipValidityChange
    : SorophyRelationshipEvolution
{
    /// <summary>
    /// Gets the new validity start.
    /// </summary>
    public SorophyTime? NewValidFrom { get; }

    /// <summary>
    /// Gets the new validity end.
    /// </summary>
    public SorophyTime? NewValidTill { get; }

    /// <summary>
    /// Initializes a new relationship-validity change operation.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity of the relationship whose validity changes.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which the validity change takes effect.
    /// </param>
    /// <param name="newValidFrom">
    /// The new validity start, or null for an unbounded start.
    /// </param>
    /// <param name="newValidTill">
    /// The new validity end, or null for an unbounded end.
    /// </param>
    /// <param name="eventEntityId">
    /// Optional identity of the event entity that originated this validity change.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied endpoints belong to different temporal
    /// schemas.
    /// </exception>
    public SorophyRelationshipValidityChange(
        Guid relationshipId,
        SorophyTime effectiveTime,
        SorophyTime? newValidFrom = null,
        SorophyTime? newValidTill = null,
        Guid? eventEntityId = null)
        : base(
            relationshipId,
            effectiveTime,
            eventEntityId)
    {
        ValidateSchema(
            newValidFrom,
            newValidTill);

        NewValidFrom =
            newValidFrom;

        NewValidTill =
            newValidTill;
    }

    private static void ValidateSchema(
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
                "NewValidFrom and NewValidTill must belong to the same temporal schema.");
        }
    }
}