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

namespace Sorophy.Engine.Graph.Canon;

/// <summary>
/// Categorizes the specific violation identified by a Canon Check operation.
/// </summary>
public enum SorophyCanonViolationKind
{
    /// <summary>
    /// No violation; the proposed operation is canonically valid.
    /// </summary>
    None = 0,

    /// <summary>
    /// The target entity does not exist in the canonical graph.
    /// </summary>
    EntityNotFound = 1,

    /// <summary>
    /// The entity already exists in the canonical graph.
    /// </summary>
    EntityAlreadyExists = 2,

    /// <summary>
    /// The temporal coordinate uses a timeline or schema incompatible with the entity's established timeline.
    /// </summary>
    IncompatibleTimeline = 3,

    /// <summary>
    /// The proposed retirement coordinate strictly precedes the entity's established creation coordinate.
    /// </summary>
    PrecedesCreation = 4,

    /// <summary>
    /// The entity already has an established retirement fact.
    /// </summary>
    AlreadyRetired = 5,

    /// <summary>
    /// No retirement fact exists at the specified coordinate to revert.
    /// </summary>
    RetirementNotFound = 6,

    /// <summary>
    /// The proposed retirement contradicts an established incident relationship requiring the entity's existence.
    /// </summary>
    ContradictsIncidentRelationships = 7,

    /// <summary>
    /// The proposed operation contradicts an established future historical fact.
    /// </summary>
    ContradictsFutureHistory = 8,

    /// <summary>
    /// The target relationship does not exist in the canonical graph.
    /// </summary>
    RelationshipNotFound = 9,

    /// <summary>
    /// The relationship already exists in the canonical graph.
    /// </summary>
    RelationshipAlreadyExists = 10,

    /// <summary>
    /// One or both endpoints of the relationship are not active at the specified temporal coordinate.
    /// </summary>
    EndpointNotActiveAtCoordinate = 11,

    /// <summary>
    /// The proposed operation contradicts an established later active world state.
    /// </summary>
    ContradictsEstablishedLaterState = 12,

    /// <summary>
    /// The target entity is not active at the specified temporal coordinate (e.g., uncreated or already retired).
    /// </summary>
    EntityNotActiveAtCoordinate = 13,

    /// <summary>
    /// The target relationship is not active at the specified temporal coordinate (e.g., uncreated or already retired).
    /// </summary>
    RelationshipNotActiveAtCoordinate = 14
}

