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
/// Represents a change to the semantic type of an existing relationship.
/// </summary>
public sealed class SorophyRelationshipTypeChange
    : SorophyRelationshipEvolution
{
    /// <summary>
    /// Gets the new semantic type that the relationship will acquire.
    /// </summary>
    public string NewType { get; }

    /// <summary>
    /// Initializes a new relationship type-change operation.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity of the relationship whose type will change.
    /// </param>
    /// <param name="newType">
    /// The new semantic relationship type.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which the new type takes effect.
    /// </param>
    /// <param name="eventEntityId">
    /// Optional identity of the event entity that originated this type change.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newType"/> is null, empty, or whitespace.
    /// </exception>
    public SorophyRelationshipTypeChange(
        Guid relationshipId,
        string newType,
        SorophyTime effectiveTime,
        Guid? eventEntityId = null)
        : base(
            relationshipId,
            effectiveTime,
            eventEntityId)
    {
        if (string.IsNullOrWhiteSpace(
                newType))
        {
            throw new ArgumentException(
                "New relationship type cannot be null, empty, or whitespace.",
                nameof(newType));
        }

        NewType =
            newType;
    }
}