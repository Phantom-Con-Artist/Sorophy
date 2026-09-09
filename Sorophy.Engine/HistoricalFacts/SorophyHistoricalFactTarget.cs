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

namespace Sorophy.Engine.HistoricalFacts;

/// <summary>
/// Identifies the structural target kind of an emergent historical fact.
/// </summary>
public enum SorophyHistoricalFactTarget
{
    /// <summary>
    /// The historical fact applies to an entity.
    /// </summary>
    Entity = 1,

    /// <summary>
    /// The historical fact applies to a relationship.
    /// </summary>
    Relationship = 2
}

