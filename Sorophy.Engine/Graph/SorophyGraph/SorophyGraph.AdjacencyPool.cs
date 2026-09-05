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
     * ENTITY ADJACENCY RECORD
     * =============================================================
     */

    private struct EntityAdjacency
    {
        public int OutgoingHead;
        public int OutgoingTail;

        public int IncomingHead;
        public int IncomingTail;

        public bool IsEmpty =>
            OutgoingHead ==
                AdjacencySlabPool.None &&
            OutgoingTail ==
                AdjacencySlabPool.None &&
            IncomingHead ==
                AdjacencySlabPool.None &&
            IncomingTail ==
                AdjacencySlabPool.None;

        public static EntityAdjacency Empty =>
            new()
            {
                OutgoingHead =
                    AdjacencySlabPool.None,

                OutgoingTail =
                    AdjacencySlabPool.None,

                IncomingHead =
                    AdjacencySlabPool.None,

                IncomingTail =
                    AdjacencySlabPool.None
            };
    }

    /*
     * =============================================================
     * RELATIONSHIP INDEX RECORD
     * =============================================================
     */

    private readonly struct RelationshipIndex
    {
        public RelationshipIndex(
            int outgoingNode,
            int incomingNode,
            long sequence)
        {
            OutgoingNode =
                outgoingNode;

            IncomingNode =
                incomingNode;

            Sequence =
                sequence;
        }

        public int OutgoingNode { get; }

        public int IncomingNode { get; }

        public long Sequence { get; }
    }

    /*
     * =============================================================
     * ADJACENCY NODE
     * =============================================================
     */

    private struct AdjacencyNode
    {
        public Guid RelationshipId;

        public int Previous;

        public int Next;

        public bool IsFree =>
            Previous ==
            AdjacencySlabPool.FreeMarker;
    }

    /*
     * =============================================================
     * SLAB POOL
     * =============================================================
     *
     * All adjacency nodes are stored inside fixed-size arrays.
     *
     * Deleted nodes return to a free list and are reused.
     */

    private sealed class AdjacencySlabPool
    {
        public const int None =
            -1;

        public const int FreeMarker =
            int.MinValue;

        private const int SlabSize =
            4096;

        private readonly List<AdjacencyNode[]> _slabs =
            new();

        private int _allocatedCount;

        private int _freeHead =
            None;

        public int AllocatedCount =>
            _allocatedCount;

        public int FreeHead =>
            _freeHead;

        public int Allocate(
            Guid relationshipId)
        {
            int index;

            if (_freeHead !=
                None)
            {
                index =
                    _freeHead;

                var freeNode =
                    GetNode(
                        index);

                _freeHead =
                    freeNode.Next;
            }
            else
            {
                index =
                    _allocatedCount;

                _allocatedCount++;

                EnsureCapacity(
                    index);
            }

            /*
             * CRITICAL:
             *
             * GetNode() returns the actual node by reference.
             * Therefore these writes modify the slab itself.
             */
            ref var node =
                ref GetNode(
                    index);

            node.RelationshipId =
                relationshipId;

            node.Previous =
                None;

            node.Next =
                None;

            return index;
        }

        public void Release(
            int index)
        {
            if (!IsValidIndex(
                    index))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Adjacency node index '{index}' is invalid.");
            }

            /*
             * CRITICAL:
             *
             * This is a ref to the actual slab node.
             */
            ref var node =
                ref GetNode(
                    index);

            if (node.IsFree)
            {
                throw new InvalidOperationException(
                    $"Adjacency node '{index}' has already been released.");
            }

            /*
             * Clear the relationship payload first.
             */
            node.RelationshipId =
                Guid.Empty;

            /*
             * Previous identifies this node as free.
             * Next points to the next free node.
             */
            node.Previous =
                FreeMarker;

            node.Next =
                _freeHead;

            _freeHead =
                index;
        }

        public bool IsValidIndex(
            int index)
        {
            return index >= 0 &&
                   index < _allocatedCount;
        }

        public ref AdjacencyNode GetNode(
            int index)
        {
            if (!IsValidIndex(
                    index))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Adjacency node index '{index}' is invalid.");
            }

            var slabIndex =
                index /
                SlabSize;

            var offset =
                index %
                SlabSize;

            return ref _slabs[
                slabIndex][
                offset];
        }

        private void EnsureCapacity(
            int index)
        {
            var requiredSlab =
                index /
                SlabSize;

            while (_slabs.Count <=
                   requiredSlab)
            {
                _slabs.Add(
                    new AdjacencyNode[
                        SlabSize]);
            }
        }
    }
}
