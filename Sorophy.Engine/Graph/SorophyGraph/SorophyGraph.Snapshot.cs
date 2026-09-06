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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /*
     * =============================================================
     * GRAPH SNAPSHOT & TEMPORAL MATERIALIZATION
     * =============================================================
     */

    /// <summary>
    /// Materializes an immutable point-in-time snapshot of the graph at the
    /// specified authoritative temporal coordinate.
    /// </summary>
    /// <param name="targetTime">
    /// The temporal coordinate at which to materialize the graph state.
    /// </param>
    /// <returns>
    /// An immutable <see cref="ISorophySnapshot"/> representing the structural state
    /// of the graph at <paramref name="targetTime"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="targetTime"/> is null.
    /// </exception>
    public ISorophySnapshot CreateSnapshot(
        SorophyTime targetTime)
    {
        ArgumentNullException.ThrowIfNull(
            targetTime);

        return SorophySnapshotMaterializer.Materialize(
            this,
            targetTime);
    }

    /// <summary>
    /// Materializes an immutable point-in-time snapshot of the graph at the
    /// temporal coordinate of the specified Event entity.
    /// </summary>
    /// <param name="eventEntity">
    /// An entity classified as an Event with an authoritative <see cref="SorophyEntity.OccurredAt"/> coordinate.
    /// </param>
    /// <returns>
    /// An immutable <see cref="ISorophySnapshot"/> representing the structural state
    /// of the graph at the Event's temporal coordinate.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventEntity"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventEntity"/> is not classified as an Event or
    /// does not contain an authoritative <see cref="SorophyEntity.OccurredAt"/> coordinate.
    /// </exception>
    public ISorophySnapshot CreateSnapshot(
        SorophyEntity eventEntity)
    {
        ArgumentNullException.ThrowIfNull(
            eventEntity);

        if (!eventEntity.IsEvent)
        {
            throw new ArgumentException(
                $"Entity '{eventEntity.Id}' is not classified as an Event (Type is '{eventEntity.Type}').",
                nameof(eventEntity));
        }

        if (eventEntity.OccurredAt is null)
        {
            throw new ArgumentException(
                $"Event entity '{eventEntity.Id}' does not have a valid OccurredAt coordinate.",
                nameof(eventEntity));
        }

        return CreateSnapshot(
            eventEntity.OccurredAt);
    }
}

