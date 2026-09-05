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
/// Represents the termination of an existing relationship at a specific
/// temporal point.
/// </summary>
/// <remarks>
/// <para>
/// This operation only describes the requested transition. It does not
/// mutate the graph and does not create historical facts by itself.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipTermination
    : SorophyRelationshipEvolution
{
    /// <summary>
    /// Initializes a new relationship termination operation.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity of the relationship being terminated.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which the relationship terminates.
    /// </param>
    public SorophyRelationshipTermination(
        Guid relationshipId,
        SorophyTime effectiveTime)
        : base(
            relationshipId,
            effectiveTime)
    {
    }
}