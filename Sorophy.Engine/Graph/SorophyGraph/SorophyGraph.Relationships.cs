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
     * RELATIONSHIP OPERATIONS
     * =============================================================
     */

    public void AddRelationship(
        SorophyRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(
            relationship);

        /*
         * Relationship identity must always be a real stable identifier.
         */
        if (relationship.Id ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationship));
        }

        /*
         * Relationship identities are permanently retired once removed.
         *
         * This prevents a historical relationship identity from later
         * being reused for an unrelated relationship.
         */
        if (IsRelationshipIdRetired(
                relationship.Id))
        {
            throw new InvalidOperationException(
                $"Relationship ID '{relationship.Id}' has been retired " +
                "and cannot be reused.");
        }

        if (!_entities.ContainsKey(
                relationship.SourceId))
        {
            throw new InvalidOperationException(
                $"Source entity '{relationship.SourceId}' does not exist.");
        }

        if (!_entities.ContainsKey(
                relationship.TargetId))
        {
            throw new InvalidOperationException(
                $"Target entity '{relationship.TargetId}' does not exist.");
        }

        if (_relationships.ContainsKey(
                relationship.Id))
        {
            throw new InvalidOperationException(
                $"A relationship with ID '{relationship.Id}' already exists.");
        }

        /*
         * Allocate one adjacency node for each direction.
         */
        var outgoingNode =
            _adjacencyPool.Allocate(
                relationship.Id);

        var incomingNode =
            _adjacencyPool.Allocate(
                relationship.Id);

        var relationshipAdded = false;
        var indexAdded = false;
        var outgoingLinked = false;
        var incomingLinked = false;

        try
        {
            /*
             * Canonical relationship store.
             */
            _relationships.Add(
                relationship.Id,
                relationship);

            relationshipAdded =
                true;

            /*
             * Relationship metadata.
             */
            _relationshipIndex.Add(
                relationship.Id,
                new RelationshipIndex(
                    outgoingNode,
                    incomingNode,
                    _nextRelationshipSequence));

            indexAdded =
                true;

            /*
             * Build outgoing and incoming chains.
             */
            LinkOutgoing(
                relationship.SourceId,
                outgoingNode);

            outgoingLinked =
                true;

            LinkIncoming(
                relationship.TargetId,
                incomingNode);

            incomingLinked =
                true;

            /*
             * Consume the sequence only after complete success.
             */
            _nextRelationshipSequence++;
        }
        catch
        {
            /*
             * Full rollback.
             */
            if (incomingLinked)
            {
                UnlinkIncoming(
                    relationship.TargetId,
                    incomingNode);
            }

            if (outgoingLinked)
            {
                UnlinkOutgoing(
                    relationship.SourceId,
                    outgoingNode);
            }

            if (indexAdded)
            {
                _relationshipIndex.Remove(
                    relationship.Id);
            }

            if (relationshipAdded)
            {
                _relationships.Remove(
                    relationship.Id);
            }

            _adjacencyPool.Release(
                outgoingNode);

            _adjacencyPool.Release(
                incomingNode);

            throw;
        }
    }

    public bool RemoveRelationship(
        Guid relationshipId)
    {
        if (!_relationships.TryGetValue(
                relationshipId,
                out var relationship))
        {
            return false;
        }

        if (!_relationshipIndex.TryGetValue(
                relationshipId,
                out var index))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationshipId}' is missing its adjacency index.");
        }

        /*
         * Unlink while canonical relationship information still exists.
         */
        UnlinkOutgoing(
            relationship.SourceId,
            index.OutgoingNode);

        UnlinkIncoming(
            relationship.TargetId,
            index.IncomingNode);

        /*
         * Return physical nodes to the free list.
         */
        _adjacencyPool.Release(
            index.OutgoingNode);

        _adjacencyPool.Release(
            index.IncomingNode);

        /*
         * Remove canonical/index state.
         */
        _relationshipIndex.Remove(
            relationshipId);

        _relationships.Remove(
            relationshipId);

        /*
         * The relationship no longer exists in the active graph, but its
         * identity is permanently retired.
         *
         * Any historical facts associated with this identity remain
         * preserved in the history store.
         */
        RetireRelationshipId(
            relationshipId);

        return true;
    }
}