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

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * ENTITY OPERATIONS
     * =============================================================
     */

    public void AddEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        if (!_entities.TryAdd(
                entity.Id,
                entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }

        try
        {
            /*
             * The Entity becomes visible to the tag index only after the
             * canonical store accepts it. If tag indexing fails, roll the
             * canonical insertion back so AddEntity remains atomic.
             */
            _tagIndex.IndexEntity(
                entity);
        }
        catch
        {
            _entities.Remove(
                entity.Id);

            _tagIndex.RemoveEntity(
                entity.Id);

            throw;
        }
    }

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

        return true;
    }
}