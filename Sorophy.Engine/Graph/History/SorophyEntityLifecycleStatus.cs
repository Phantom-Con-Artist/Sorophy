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

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Represents the observable temporal lifecycle status of an entity at a given temporal coordinate.
/// </summary>
public enum SorophyEntityLifecycleStatus
{
    /// <summary>
    /// The entity has not yet been created at the evaluated temporal coordinate.
    /// </summary>
    Uncreated = 0,

    /// <summary>
    /// The entity exists and is actively present in the graph at the evaluated temporal coordinate.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The entity has been retired and is no longer actively present in the temporal graph projection at the evaluated coordinate.
    /// </summary>
    Retired = 2
}

