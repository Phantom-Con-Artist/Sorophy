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
/// Machine-level classification of an emergent historical fact in The Saga Architecture.
/// Presentation labels (e.g. "Ended", "Changed", "Died", "Destroyed") are defined downstream by applications.
/// </summary>
public enum SorophyHistoricalFactKind
{
    /// <summary>
    /// Represents the temporal creation of an entity or relationship.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Represents the temporal retirement of an entity or relationship.
    /// </summary>
    Retired = 2,

    /// <summary>
    /// Represents the temporal mutation of a property on an entity or relationship.
    /// </summary>
    PropertyChanged = 3,

    /// <summary>
    /// Represents the temporal mutation of the semantic type of a relationship.
    /// </summary>
    RelationshipChanged = 4
}

