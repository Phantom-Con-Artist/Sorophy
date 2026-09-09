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
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Graph.Canon;

/// <summary>
/// Defines the read-only Canon Check contradiction-validation contract in The Saga Architecture.
/// </summary>
/// <remarks>
/// <para>
/// Canon Check evaluates proposed temporal mutations against the authoritative temporal history
/// of the world to prevent edits that contradict already-established history.
/// </para>
/// <para>
/// Canon Check is strictly read-only: it does not mutate canonical graph state, does not modify
/// histories, does not create secondary stores, and does not generate synthetic events.
/// </para>
/// </remarks>
public interface ITemporalCanonCheck
{
    /* =============================================================
     * ENTITY CANON CHECK
     * =============================================================
     */

    /// <summary>
    /// Validates whether an entity can be canonically created at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanCreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime);

    /// <summary>
    /// Validates whether an entity with the specified ID can be canonically created at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanCreateEntity(
        Guid entityId,
        SorophyTime creationTime);

    /// <summary>
    /// Validates whether an entity can be canonically retired at the specified temporal coordinate
    /// without violating established history or referential requirements.
    /// </summary>
    SorophyCanonValidationResult CanRetireEntity(
        Guid entityId,
        SorophyTime retirementTime);

    /// <summary>
    /// Validates whether an established retirement fact for an entity can be canonically reverted.
    /// </summary>
    SorophyCanonValidationResult CanRevertEntityRetirement(
        Guid entityId,
        SorophyTime retirementTime);

    /// <summary>
    /// Validates whether a property can be mutated on an entity at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanSetEntityProperty(
        Guid entityId,
        string propertyName,
        SorophyValue value,
        SorophyTime time);

    /// <summary>
    /// Validates whether a property can be removed from an entity at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanRemoveEntityProperty(
        Guid entityId,
        string propertyName,
        SorophyTime time);

    /* =============================================================
     * RELATIONSHIP CANON CHECK
     * =============================================================
     */

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanCreateRelationship(
        SorophyRelationship relationship,
        SorophyTime creationTime);

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanCreateRelationship(
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        SorophyTime creationTime);

    /// <summary>
    /// Validates whether a relationship can be canonically retired at the specified temporal coordinate
    /// without violating established history or later active world states.
    /// </summary>
    SorophyCanonValidationResult CanRetireRelationship(
        Guid relationshipId,
        SorophyTime retirementTime);

    /// <summary>
    /// Validates whether an established retirement fact for a relationship can be canonically reverted.
    /// </summary>
    SorophyCanonValidationResult CanRevertRelationshipRetirement(
        Guid relationshipId,
        SorophyTime retirementTime);

    /// <summary>
    /// Validates whether the semantic type of a relationship can be changed at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanChangeRelationshipType(
        Guid relationshipId,
        string newType,
        SorophyTime time);

    /// <summary>
    /// Validates whether a property can be mutated on a relationship at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanSetRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyValue value,
        SorophyTime time);

    /// <summary>
    /// Validates whether a property can be removed from a relationship at the specified temporal coordinate.
    /// </summary>
    SorophyCanonValidationResult CanRemoveRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyTime time);
}

