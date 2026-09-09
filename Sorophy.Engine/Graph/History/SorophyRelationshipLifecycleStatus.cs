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
/// Identifies the temporal lifecycle status of a relationship at a specific coordinate.
/// </summary>
public enum SorophyRelationshipLifecycleStatus
{
    /// <summary>
    /// The relationship has not yet been created at the evaluated coordinate.
    /// </summary>
    Uncreated = 0,

    /// <summary>
    /// The relationship is active at the evaluated coordinate and both endpoints exist.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The relationship has been retired at or before the evaluated coordinate.
    /// </summary>
    Retired = 2,

    /// <summary>
    /// The relationship has been created and is not retired, but at least one endpoint entity does not exist at the evaluated coordinate.
    /// </summary>
    EndpointInactive = 3
}

