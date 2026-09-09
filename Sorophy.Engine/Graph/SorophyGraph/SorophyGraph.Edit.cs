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
using Sorophy.Engine.TemporalEdit;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /*
     * =============================================================
     * TEMPORAL EDITING SCOPE
     * =============================================================
     */

    /// <summary>
    /// Creates a scoped temporal editor for modifying canonical graph state
    /// while positioned at the specified temporal coordinate.
    /// </summary>
    /// <param name="time">The authoritative temporal coordinate at which edits are authored.</param>
    /// <returns>An <see cref="ISorophyTemporalEditor"/> bound to this graph and coordinate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    public ISorophyTemporalEditor EditAt(SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);
        return new SorophyTemporalEditor(this, time);
    }
}

