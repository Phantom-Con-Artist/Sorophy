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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
     /* =============================================================
     * OUTGOING RELATIONSHIPS
     * =============================================================
     */

    public IEnumerable<SorophyRelationship>
        GetOutgoingRelationships(
            Guid entityId)
    {
        if (!_entityAdjacency.TryGetValue(
                entityId,
                out var adjacency))
        {
            yield break;
        }

        var current =
            adjacency.OutgoingHead;

        while (current !=
               AdjacencySlabPool.None)
        {
            /*
             * Value copy is intentional here.
             *
             * This is an iterator and no ref local may survive
             * across a yield boundary.
             */
            var node =
                _adjacencyPool.GetNode(
                    current);

            var relationshipId =
                node.RelationshipId;

            var next =
                node.Next;

            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }

            current =
                next;
        }
    }

    /*
     * =============================================================
     * INCOMING RELATIONSHIPS
     * =============================================================
     */

    public IEnumerable<SorophyRelationship>
        GetIncomingRelationships(
            Guid entityId)
    {
        if (!_entityAdjacency.TryGetValue(
                entityId,
                out var adjacency))
        {
            yield break;
        }

        var current =
            adjacency.IncomingHead;

        while (current !=
               AdjacencySlabPool.None)
        {
            var node =
                _adjacencyPool.GetNode(
                    current);

            var relationshipId =
                node.RelationshipId;

            var next =
                node.Next;

            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }

            current =
                next;
        }
    }

    /*
     * =============================================================
     * ALL INCIDENT RELATIONSHIPS
     * =============================================================
     *
     * Outgoing and incoming adjacency chains each preserve global
     * relationship insertion order.
     *
     * We therefore merge the two chains rather than allocating a
     * temporary collection and sorting it.
     */

    public IEnumerable<SorophyRelationship>
        GetRelationships(
            Guid entityId)
    {
        foreach (var relationshipId in
                 EnumerateIncidentRelationshipIds(
                     entityId))
        {
            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }
        }
    }

    private IEnumerable<Guid>
        EnumerateIncidentRelationshipIds(
            Guid entityId)
    {
        var outgoingNode =
            AdjacencySlabPool.None;

        var incomingNode =
            AdjacencySlabPool.None;

        if (_entityAdjacency.TryGetValue(
                entityId,
                out var adjacency))
        {
            outgoingNode =
                adjacency.OutgoingHead;

            incomingNode =
                adjacency.IncomingHead;
        }

        while (outgoingNode !=
                   AdjacencySlabPool.None ||
               incomingNode !=
                   AdjacencySlabPool.None)
        {
            /*
             * Only incoming remains.
             */
            if (outgoingNode ==
                AdjacencySlabPool.None)
            {
                var incomingData =
                    _adjacencyPool.GetNode(
                        incomingNode);

                var incomingRelationshipId =
                    incomingData.RelationshipId;

                var nextIncoming =
                    incomingData.Next;

                yield return incomingRelationshipId;

                incomingNode =
                    nextIncoming;

                continue;
            }

            /*
             * Only outgoing remains.
             */
            if (incomingNode ==
                AdjacencySlabPool.None)
            {
                var outgoingData =
                    _adjacencyPool.GetNode(
                        outgoingNode);

                var outgoingRelationshipId =
                    outgoingData.RelationshipId;

                var nextOutgoing =
                    outgoingData.Next;

                yield return outgoingRelationshipId;

                outgoingNode =
                    nextOutgoing;

                continue;
            }

            /*
             * Both chains still contain relationships.
             */
            var outgoingData2 =
                _adjacencyPool.GetNode(
                    outgoingNode);

            var incomingData2 =
                _adjacencyPool.GetNode(
                    incomingNode);

            var outgoingRelationshipId2 =
                outgoingData2.RelationshipId;

            var incomingRelationshipId2 =
                incomingData2.RelationshipId;

            /*
             * Self-link exists once in each directional chain.
             * Emit it only once.
             */
            if (outgoingRelationshipId2 ==
                incomingRelationshipId2)
            {
                var nextOutgoing =
                    outgoingData2.Next;

                var nextIncoming =
                    incomingData2.Next;

                yield return outgoingRelationshipId2;

                outgoingNode =
                    nextOutgoing;

                incomingNode =
                    nextIncoming;

                continue;
            }

            var outgoingSequence =
                GetRelationshipSequence(
                    outgoingRelationshipId2);

            var incomingSequence =
                GetRelationshipSequence(
                    incomingRelationshipId2);

            if (outgoingSequence <=
                incomingSequence)
            {
                var nextOutgoing =
                    outgoingData2.Next;

                yield return outgoingRelationshipId2;

                outgoingNode =
                    nextOutgoing;
            }
            else
            {
                var nextIncoming =
                    incomingData2.Next;

                yield return incomingRelationshipId2;

                incomingNode =
                    nextIncoming;
            }
        }
    }

    private long GetRelationshipSequence(
        Guid relationshipId)
    {
        if (!_relationshipIndex.TryGetValue(
                relationshipId,
                out var index))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationshipId}' is missing its relationship index.");
        }

        return index.Sequence;
    }
}
