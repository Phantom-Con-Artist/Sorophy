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

namespace Sorophy.Engine.Graph.Evolution;

/// <summary>
/// Represents an immutable operation that evolves the state of a
/// relationship within a Sorophy graph.
/// </summary>
/// <remarks>
/// <para>
/// An evolution describes an operation to be applied to relationship
/// state. It does not itself mutate the graph and does not represent
/// a replacement relationship state.
/// </para>
///
/// <para>
/// Concrete evolution types define the operation-specific data required
/// to perform a particular relationship transition.
/// </para>
///
/// <para>
/// The actual execution of an evolution, including historical fact
/// creation and canonical relationship mutation, belongs to the graph
/// evolution executor and not to this value object.
/// </para>
/// </remarks>
public abstract class SorophyRelationshipEvolution
{
    /// <summary>
    /// Gets the identity of the relationship targeted by this evolution.
    /// </summary>
    public Guid RelationshipId { get; }

    /// <summary>
    /// Gets the temporal point at which this evolution takes effect.
    /// </summary>
    public SorophyTime EffectiveTime { get; }

    /// <summary>
    /// Gets the identity of the event entity that originated this evolution,
    /// or null when the evolution is not associated with an event entity.
    /// </summary>
    public Guid? EventEntityId { get; }

    /// <summary>
    /// Initializes a new relationship evolution.
    /// </summary>
    /// <param name="relationshipId">
    /// The relationship identity targeted by the evolution.
    /// For relationship creation, this is the identity being created.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which the evolution takes effect.
    /// </param>
    /// <param name="eventEntityId">
    /// Optional identity of the event entity that originated this evolution.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationshipId"/> is empty or when
    /// <paramref name="eventEntityId"/> is explicitly supplied as empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="effectiveTime"/> is null.
    /// </exception>
    protected SorophyRelationshipEvolution(
        Guid relationshipId,
        SorophyTime effectiveTime,
        Guid? eventEntityId = null)
    {
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

        ArgumentNullException.ThrowIfNull(
            effectiveTime);

        RelationshipId =
            relationshipId;

        EffectiveTime =
            effectiveTime;

        EventEntityId =
            eventEntityId;
    }
}