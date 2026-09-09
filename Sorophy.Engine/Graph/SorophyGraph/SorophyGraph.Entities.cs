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
using System.Collections.Generic;
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * CANON CHECK INSPECTION
     * =============================================================
     */

    /// <summary>
    /// Validates whether an entity can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            this,
            entity.Id,
            creationTime);
    }

    /// <summary>
    /// Validates whether an entity with the specified ID can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateEntity(
        Guid entityId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            this,
            entityId,
            creationTime);
    }

    /// <summary>
    /// Validates whether an entity can be canonically retired at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanRetireEntity(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateEntityRetirement(
            this,
            entityId,
            retirementTime);
    }

    /// <summary>
    /// Validates whether an established retirement fact for an entity can be canonically reverted.
    /// </summary>
    public SorophyCanonResult CanRevertRetirement(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRetirementReversal(
            this,
            entityId,
            retirementTime);
    }

    /* =============================================================
     * ENTITY LIFECYCLE OPERATIONS
     * =============================================================
     */

    /// <summary>
    /// Restores an entity directly into canonical storage and the tag index without authoring a lifecycle fact.
    /// Used by deserialization and test fixtures to restore entities (including legacy timeless entities).
    /// </summary>
    internal void RestoreEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }

        try
        {
            /*
             * The Entity becomes visible to the tag index only after the
             * canonical store accepts it. If tag indexing fails, roll the
             * canonical insertion back so RestoreEntity remains atomic.
             */
            _tagIndex.IndexEntity(entity);
        }
        catch
        {
            _entities.Remove(entity.Id);
            _tagIndex.RemoveEntity(entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Creates an entity in the graph at the specified temporal coordinate, executing Canon Check
    /// validation and recording an authored <see cref="SorophyEntityFactKind.Created"/> fact.
    /// </summary>
    public void CreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(creationTime);

        var canonResult = CanCreateEntity(entity, creationTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot create entity '{entity.Id}': {canonResult.ErrorMessage}");
        }

        RestoreEntity(entity);

        try
        {
            RecordEntityFact(
                SorophyEntityFact.Create(
                    creationTime,
                    entity.Id,
                    SorophyEntityFactKind.Created,
                    $"Entity '{entity.Name}' created at {creationTime}"));
        }
        catch
        {
            _entities.Remove(entity.Id);
            _tagIndex.RemoveEntity(entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Temporally retires an entity at the specified temporal coordinate without mutating canonical entity or relationship storage.
    /// </summary>
    public bool RetireEntity(
        Guid entityId,
        SorophyTime retirementTime,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRetireEntity(entityId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot retire entity '{entityId}': {canonResult.ErrorMessage}");
        }

        RecordEntityFact(
            SorophyEntityFact.Create(
                retirementTime,
                entityId,
                SorophyEntityFactKind.Retired,
                description ?? $"Entity '{entityId}' retired at {retirementTime}"));

        return true;
    }

    /// <summary>
    /// Reverts an established temporal retirement for an entity, removing the retirement fact
    /// from the authoritative temporal history.
    /// </summary>
    public bool RevertRetirement(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRevertRetirement(entityId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot revert retirement for entity '{entityId}': {canonResult.ErrorMessage}");
        }

        return RevertEntityRetirementFact(entityId, retirementTime);
    }

    /* =============================================================
     * TEMPORAL EXISTENCE & LIFECYCLE QUERIES
     * =============================================================
     */

    /// <summary>
    /// Determines whether an entity exists at the specified temporal coordinate.
    /// </summary>
    public bool EntityExistsAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (entityId == Guid.Empty || !_entities.ContainsKey(entityId))
        {
            return false;
        }

        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.ExistsAt(time);
        }

        // Legacy / unversioned baseline entity: exists across all coordinates
        return true;
    }

    /// <summary>
    /// Gets the lifecycle status of an entity at the specified temporal coordinate.
    /// </summary>
    public SorophyEntityLifecycleStatus GetEntityLifecycleStatus(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (entityId == Guid.Empty || !_entities.ContainsKey(entityId))
        {
            return SorophyEntityLifecycleStatus.Uncreated;
        }

        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.GetStatusAt(time);
        }

        // Legacy / unversioned baseline entity: Active across all coordinates
        return SorophyEntityLifecycleStatus.Active;
    }

    /// <summary>
    /// Determines whether an entity is currently retired in its authoritative temporal history.
    /// </summary>
    public bool IsEntityRetired(
        Guid entityId)
    {
        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.IsRetired;
        }

        return false;
    }

    /* =============================================================
     * PERMANENT ENTITY DELETION
     * =============================================================
     */

    /// <summary>
    /// Permanently deletes an entity from the graph, removing all incident relationships,
    /// tag index entries, and authoritative temporal history.
    /// </summary>
    public bool RemoveEntity(
        Guid entityId)
    {
        if (!_entities.Remove(
                entityId))
        {
            return false;
        }

        /*
         * Remove relationships connected to this entity if any adjacency
         * records exist.
         */
        if (_entityAdjacency.ContainsKey(
                entityId))
        {
            while (_entityAdjacency.TryGetValue(
                       entityId,
                       out var outgoingAdjacency) &&
                   outgoingAdjacency.OutgoingHead !=
                       AdjacencySlabPool.None)
            {
                var nodeIndex =
                    outgoingAdjacency.OutgoingHead;

                var node =
                    _adjacencyPool.GetNode(
                        nodeIndex);

                var relationshipId =
                    node.RelationshipId;

                if (!RemoveRelationship(
                        relationshipId))
                {
                    throw new InvalidOperationException(
                        $"Adjacency index references relationship '{relationshipId}', " +
                        "but the relationship could not be removed.");
                }
            }

            while (_entityAdjacency.TryGetValue(
                       entityId,
                       out var incomingAdjacency) &&
                   incomingAdjacency.IncomingHead !=
                       AdjacencySlabPool.None)
            {
                var nodeIndex =
                    incomingAdjacency.IncomingHead;

                var node =
                    _adjacencyPool.GetNode(
                        nodeIndex);

                var relationshipId =
                    node.RelationshipId;

                if (!RemoveRelationship(
                        relationshipId))
                {
                    throw new InvalidOperationException(
                        $"Adjacency index references relationship '{relationshipId}', " +
                        "but the relationship could not be removed.");
                }
            }

            _entityAdjacency.Remove(
                entityId);
        }

        /*
         * Remove the entity from every tag bucket.
         */
        _tagIndex.RemoveEntity(
            entityId);

        /*
         * Permanent deletion purges the entity's temporal history.
         */
        _entityHistories.Remove(
            entityId);

        return true;
    }

    /// <summary>
    /// Permanently deletes an entity from the graph, removing all incident relationships,
    /// tag index entries, and authoritative temporal history.
    /// </summary>
    public bool DeleteEntity(
        Guid entityId)
    {
        return RemoveEntity(entityId);
    }
}