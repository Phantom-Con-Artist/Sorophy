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
     * ADJACENCY LINKING
     * =============================================================
     *
     * AdjacencyNode is a struct.
     *
     * Therefore every actual mutation MUST use a ref local.
     */

    private void LinkOutgoing(
        Guid entityId,
        int nodeIndex)
    {
        var adjacency =
            _entityAdjacency.TryGetValue(
                entityId,
                out var existing)
                ? existing
                : EntityAdjacency.Empty;

        ref var node =
            ref _adjacencyPool.GetNode(
                nodeIndex);

        node.Previous =
            adjacency.OutgoingTail;

        node.Next =
            AdjacencySlabPool.None;

        if (adjacency.OutgoingTail ==
            AdjacencySlabPool.None)
        {
            adjacency.OutgoingHead =
                nodeIndex;
        }
        else
        {
            ref var tail =
                ref _adjacencyPool.GetNode(
                    adjacency.OutgoingTail);

            tail.Next =
                nodeIndex;
        }

        adjacency.OutgoingTail =
            nodeIndex;

        _entityAdjacency[entityId] =
            adjacency;
    }

    private void LinkIncoming(
        Guid entityId,
        int nodeIndex)
    {
        var adjacency =
            _entityAdjacency.TryGetValue(
                entityId,
                out var existing)
                ? existing
                : EntityAdjacency.Empty;

        ref var node =
            ref _adjacencyPool.GetNode(
                nodeIndex);

        node.Previous =
            adjacency.IncomingTail;

        node.Next =
            AdjacencySlabPool.None;

        if (adjacency.IncomingTail ==
            AdjacencySlabPool.None)
        {
            adjacency.IncomingHead =
                nodeIndex;
        }
        else
        {
            ref var tail =
                ref _adjacencyPool.GetNode(
                    adjacency.IncomingTail);

            tail.Next =
                nodeIndex;
        }

        adjacency.IncomingTail =
            nodeIndex;

        _entityAdjacency[entityId] =
            adjacency;
    }

    /*
     * =============================================================
     * ADJACENCY UNLINKING
     * =============================================================
     */

    private void UnlinkOutgoing(
        Guid entityId,
        int nodeIndex)
    {
        if (!_entityAdjacency.TryGetValue(
                entityId,
                out var adjacency))
        {
            throw new InvalidOperationException(
                $"Outgoing adjacency index for entity '{entityId}' does not exist.");
        }

        var node =
            _adjacencyPool.GetNode(
                nodeIndex);

        var previous =
            node.Previous;

        var next =
            node.Next;

        if (previous ==
            AdjacencySlabPool.None)
        {
            adjacency.OutgoingHead =
                next;
        }
        else
        {
            ref var previousNode =
                ref _adjacencyPool.GetNode(
                    previous);

            previousNode.Next =
                next;
        }

        if (next ==
            AdjacencySlabPool.None)
        {
            adjacency.OutgoingTail =
                previous;
        }
        else
        {
            ref var nextNode =
                ref _adjacencyPool.GetNode(
                    next);

            nextNode.Previous =
                previous;
        }

        StoreOrRemoveAdjacency(
            entityId,
            adjacency);
    }

    private void UnlinkIncoming(
        Guid entityId,
        int nodeIndex)
    {
        if (!_entityAdjacency.TryGetValue(
                entityId,
                out var adjacency))
        {
            throw new InvalidOperationException(
                $"Incoming adjacency index for entity '{entityId}' does not exist.");
        }

        var node =
            _adjacencyPool.GetNode(
                nodeIndex);

        var previous =
            node.Previous;

        var next =
            node.Next;

        if (previous ==
            AdjacencySlabPool.None)
        {
            adjacency.IncomingHead =
                next;
        }
        else
        {
            ref var previousNode =
                ref _adjacencyPool.GetNode(
                    previous);

            previousNode.Next =
                next;
        }

        if (next ==
            AdjacencySlabPool.None)
        {
            adjacency.IncomingTail =
                previous;
        }
        else
        {
            ref var nextNode =
                ref _adjacencyPool.GetNode(
                    next);

            nextNode.Previous =
                previous;
        }

        StoreOrRemoveAdjacency(
            entityId,
            adjacency);
    }

    private void StoreOrRemoveAdjacency(
        Guid entityId,
        EntityAdjacency adjacency)
    {
        if (adjacency.IsEmpty)
        {
            _entityAdjacency.Remove(
                entityId);
        }
        else
        {
            _entityAdjacency[entityId] =
                adjacency;
        }
    }
}
