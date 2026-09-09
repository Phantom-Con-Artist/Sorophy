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
/// Authoritative implementation of the read-only Canon Check contradiction-validation domain.
/// </summary>
public sealed class TemporalCanonCheck : ITemporalCanonCheck
{
    private readonly SorophyGraph _graph;

    /// <summary>
    /// Initializes a new Canon Check domain bound to the specified canonical graph.
    /// </summary>
    /// <param name="graph">The canonical graph whose temporal state and history are evaluated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
    public TemporalCanonCheck(SorophyGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /* =============================================================
     * ENTITY CANON CHECK
     * =============================================================
     */

    /// <inheritdoc />
    public SorophyCanonValidationResult CanCreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            _graph,
            entity.Id,
            creationTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanCreateEntity(
        Guid entityId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            _graph,
            entityId,
            creationTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRetireEntity(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateEntityRetirement(
            _graph,
            entityId,
            retirementTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRevertEntityRetirement(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRetirementReversal(
            _graph,
            entityId,
            retirementTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanSetEntityProperty(
        Guid entityId,
        string propertyName,
        SorophyValue value,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(time);

        return SorophyCanonValidator.ValidateEntityPropertyMutation(
            _graph,
            entityId,
            propertyName,
            time);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRemoveEntityProperty(
        Guid entityId,
        string propertyName,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        return SorophyCanonValidator.ValidateEntityPropertyMutation(
            _graph,
            entityId,
            propertyName,
            time);
    }

    /* =============================================================
     * RELATIONSHIP CANON CHECK
     * =============================================================
     */

    /// <inheritdoc />
    public SorophyCanonValidationResult CanCreateRelationship(
        SorophyRelationship relationship,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateRelationshipCreation(
            _graph,
            relationship,
            creationTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanCreateRelationship(
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateRelationshipCreation(
            _graph,
            relationshipId,
            sourceId,
            targetId,
            creationTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRetireRelationship(
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRelationshipRetirement(
            _graph,
            relationshipId,
            retirementTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRevertRelationshipRetirement(
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRelationshipRetirementReversal(
            _graph,
            relationshipId,
            retirementTime);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanChangeRelationshipType(
        Guid relationshipId,
        string newType,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        return SorophyCanonValidator.ValidateRelationshipTypeChange(
            _graph,
            relationshipId,
            newType,
            time);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanSetRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyValue value,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(time);

        return SorophyCanonValidator.ValidateRelationshipPropertyMutation(
            _graph,
            relationshipId,
            propertyName,
            time);
    }

    /// <inheritdoc />
    public SorophyCanonValidationResult CanRemoveRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        return SorophyCanonValidator.ValidateRelationshipPropertyMutation(
            _graph,
            relationshipId,
            propertyName,
            time);
    }
}

