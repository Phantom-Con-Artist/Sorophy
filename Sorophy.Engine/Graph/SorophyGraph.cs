/*
Sorophy™ — a structured knowledge and graph engine
Copyright (C) 2026  Subhradeep Sarkar

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sorophy.Engine.Graph;

public sealed class SorophyGraph
{
    /*
     * =============================================================
     * CANONICAL GRAPH STATE
     * =============================================================
     *
     * These are the authoritative graph stores.
     *
     * Serialization uses these public collections, so the adjacency
     * index remains an internal acceleration structure only.
     */
    private readonly Dictionary<Guid, SorophyEntity> _entities =
        new();

    private readonly Dictionary<Guid, SorophyRelationship> _relationships =
        new();

    /*
     * =============================================================
     * COMPACT ADJACENCY INDEX
     * =============================================================
     *
     * One compact value per entity.
     *
     * The physical adjacency records live in the slab pool below.
     */
    private readonly Dictionary<Guid, EntityAdjacency> _entityAdjacency =
        new();

    /*
     * =============================================================
     * RELATIONSHIP INDEX
     * =============================================================
     *
     * Relationship ID ->
     *
     *   outgoing adjacency node
     *   incoming adjacency node
     *   global insertion sequence
     */
    private readonly Dictionary<Guid, RelationshipIndex> _relationshipIndex =
        new();

    /*
     * =============================================================
     * ADJACENCY POOL
     * =============================================================
     */
    private readonly AdjacencySlabPool _adjacencyPool =
        new();

    private long _nextRelationshipSequence;

    private readonly ReadOnlyDictionary<Guid, SorophyEntity>
        _readOnlyEntities;

    private readonly ReadOnlyDictionary<Guid, SorophyRelationship>
        _readOnlyRelationships;

    public SorophyGraph()
    {
        _readOnlyEntities =
            new ReadOnlyDictionary<Guid, SorophyEntity>(
                _entities);

        _readOnlyRelationships =
            new ReadOnlyDictionary<Guid, SorophyRelationship>(
                _relationships);
    }

    public IReadOnlyDictionary<Guid, SorophyEntity> Entities =>
        _readOnlyEntities;

    public IReadOnlyDictionary<Guid, SorophyRelationship> Relationships =>
        _readOnlyRelationships;

    /*
     * =============================================================
     * ENTITY OPERATIONS
     * =============================================================
     */

    public void AddEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(
                entity.Id,
                entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }
    }

    public bool RemoveEntity(
        Guid entityId)
    {
        if (!_entities.ContainsKey(
                entityId))
        {
            return false;
        }

        /*
         * Remove all outgoing relationships first.
         *
         * Self-links are handled naturally because RemoveRelationship()
         * removes both its outgoing and incoming adjacency nodes.
         */
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

        /*
         * Remove relationships terminating at this entity.
         *
         * IMPORTANT:
         * This intentionally uses a separate local
         * `incomingAdjacency`. There is no cross-scope reuse of
         * the outgoing variable.
         */
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

        _entities.Remove(
            entityId);

        return true;
    }

    /*
     * =============================================================
     * RELATIONSHIP OPERATIONS
     * =============================================================
     */

    public void AddRelationship(
        SorophyRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(
            relationship);

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

        return true;
    }

    /*
     * =============================================================
     * DIRECT LOOKUPS
     * =============================================================
     */

    public bool ContainsEntity(
        Guid entityId)
    {
        return _entities.ContainsKey(
            entityId);
    }

    public bool ContainsRelationship(
        Guid relationshipId)
    {
        return _relationships.ContainsKey(
            relationshipId);
    }

    public bool TryGetEntity(
        Guid entityId,
        out SorophyEntity? entity)
    {
        return _entities.TryGetValue(
            entityId,
            out entity);
    }

    public bool TryGetRelationship(
        Guid relationshipId,
        out SorophyRelationship? relationship)
    {
        return _relationships.TryGetValue(
            relationshipId,
            out relationship);
    }

    /*
     * =============================================================
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

    /*
     * =============================================================
     * NEIGHBORS
     * =============================================================
     */

    public IEnumerable<SorophyEntity>
        GetNeighbors(
            Guid entityId)
    {
        var neighborIds =
            new HashSet<Guid>();

        foreach (var relationshipId in
                 EnumerateIncidentRelationshipIds(
                     entityId))
        {
            if (!_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                continue;
            }

            /*
             * A self-link is not considered a neighbor.
             */
            if (relationship.SourceId ==
                    entityId &&
                relationship.TargetId ==
                    entityId)
            {
                continue;
            }

            Guid neighborId;

            if (relationship.SourceId ==
                entityId)
            {
                neighborId =
                    relationship.TargetId;
            }
            else if (relationship.TargetId ==
                     entityId)
            {
                neighborId =
                    relationship.SourceId;
            }
            else
            {
                /*
                 * Stale/corrupt adjacency state.
                 * Validate() reports this condition.
                 */
                continue;
            }

            if (!neighborIds.Add(
                    neighborId))
            {
                continue;
            }

            if (_entities.TryGetValue(
                    neighborId,
                    out var entity))
            {
                yield return entity;
            }
        }
    }

    /*
     * =============================================================
     * REACHABILITY
     * =============================================================
     */

    public bool IsReachable(
        Guid sourceId,
        Guid targetId)
    {
        if (!ContainsEntity(
                sourceId) ||
            !ContainsEntity(
                targetId))
        {
            return false;
        }

        if (sourceId ==
            targetId)
        {
            return true;
        }

        var visited =
            new HashSet<Guid>();

        var queue =
            new Queue<Guid>();

        visited.Add(
            sourceId);

        queue.Enqueue(
            sourceId);

        while (queue.Count > 0)
        {
            var currentId =
                queue.Dequeue();

            foreach (var relationship in
                     GetOutgoingRelationships(
                         currentId))
            {
                var nextId =
                    relationship.TargetId;

                if (nextId ==
                    targetId)
                {
                    return true;
                }

                if (visited.Add(
                        nextId))
                {
                    queue.Enqueue(
                        nextId);
                }
            }
        }

        return false;
    }

    /*
     * =============================================================
     * BREADTH-FIRST TRAVERSAL
     * =============================================================
     */

    public IEnumerable<SorophyEntity>
        Traverse(
            Guid sourceId)
    {
        if (!ContainsEntity(
                sourceId))
        {
            yield break;
        }

        var visited =
            new HashSet<Guid>
            {
                sourceId
            };

        var queue =
            new Queue<Guid>();

        queue.Enqueue(
            sourceId);

        while (queue.Count > 0)
        {
            var currentId =
                queue.Dequeue();

            foreach (var relationship in
                     GetOutgoingRelationships(
                         currentId))
            {
                var targetId =
                    relationship.TargetId;

                if (!visited.Add(
                        targetId))
                {
                    continue;
                }

                if (_entities.TryGetValue(
                        targetId,
                        out var entity))
                {
                    yield return entity;
                }

                queue.Enqueue(
                    targetId);
            }
        }
    }

    /*
     * =============================================================
     * GRAPH VALIDATION
     * =============================================================
     *
     * This is an integrity audit over the canonical graph, the
     * relationship index, the directional adjacency chains, and
     * the slab free list.
     *
     * The adjacency audit performs one physical walk of each active
     * chain. Relationship-index placement is checked while the node
     * is already in hand, and relationship membership is checked from
     * the sets collected during those walks.
     *
     * This avoids the previous repeated chain searches that could
     * become quadratic for high-degree entities, without introducing
     * another O(E) node-ownership dictionary or other persistent
     * graph state.
     */

    public IReadOnlyList<string>
        Validate()
    {
        var errors =
            new List<string>();

        ValidateEntities(
            errors);

        ValidateRelationships(
            errors);

        ValidateRelationshipIndex(
            errors);

        ValidateAdjacency(
            errors);

        ValidateFreeList(
            errors);

        return errors;
    }

    private void ValidateEntities(
        List<string> errors)
    {
        foreach (var pair in
                 _entities)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Entity dictionary contains a null entity for ID '{pair.Key}'.");

                continue;
            }

            if (pair.Key !=
                pair.Value.Id)
            {
                errors.Add(
                    $"Entity dictionary key '{pair.Key}' does not match entity ID '{pair.Value.Id}'.");
            }

            ValidateProperties(
                pair.Value.Properties,
                $"Entity '{pair.Value.Id}'",
                errors);
        }
    }

    private void ValidateRelationships(
        List<string> errors)
    {
        foreach (var pair in
                 _relationships)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Relationship dictionary contains a null relationship for ID '{pair.Key}'.");

                continue;
            }

            var relationship =
                pair.Value;

            if (pair.Key !=
                relationship.Id)
            {
                errors.Add(
                    $"Relationship dictionary key '{pair.Key}' does not match relationship ID '{relationship.Id}'.");
            }

            if (!_entities.ContainsKey(
                    relationship.SourceId))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' references missing source entity '{relationship.SourceId}'.");
            }

            if (!_entities.ContainsKey(
                    relationship.TargetId))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' references missing target entity '{relationship.TargetId}'.");
            }

            if (!_relationshipIndex.ContainsKey(
                    relationship.Id))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' is missing its relationship index.");
            }

            ValidateProperties(
                relationship.Properties,
                $"Relationship '{relationship.Id}'",
                errors);
        }
    }

    private void ValidateRelationshipIndex(
        List<string> errors)
    {
        var indexedRelationshipIds =
            new HashSet<Guid>();

        var sequenceValues =
            new HashSet<long>();

        foreach (var pair in
                 _relationshipIndex)
        {
            var relationshipId =
                pair.Key;

            var index =
                pair.Value;

            if (!_relationships.ContainsKey(
                    relationshipId))
            {
                errors.Add(
                    $"Relationship index contains stale relationship ID '{relationshipId}'.");
            }
            else
            {
                indexedRelationshipIds.Add(
                    relationshipId);
            }

            if (!sequenceValues.Add(
                    index.Sequence))
            {
                errors.Add(
                    $"Relationship index contains duplicate sequence '{index.Sequence}'.");
            }

            if (!_adjacencyPool.IsValidIndex(
                    index.OutgoingNode))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' has an invalid outgoing adjacency node.");
            }
            else
            {
                var outgoingNode =
                    _adjacencyPool.GetNode(
                        index.OutgoingNode);

                if (outgoingNode.IsFree)
                {
                    errors.Add(
                        $"Relationship '{relationshipId}' points to a freed outgoing adjacency node.");
                }
                else if (outgoingNode.RelationshipId !=
                         relationshipId)
                {
                    errors.Add(
                        $"Relationship '{relationshipId}' outgoing adjacency node " +
                        $"points to relationship '{outgoingNode.RelationshipId}'.");
                }
            }

            if (!_adjacencyPool.IsValidIndex(
                    index.IncomingNode))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' has an invalid incoming adjacency node.");
            }
            else
            {
                var incomingNode =
                    _adjacencyPool.GetNode(
                        index.IncomingNode);

                if (incomingNode.IsFree)
                {
                    errors.Add(
                        $"Relationship '{relationshipId}' points to a freed incoming adjacency node.");
                }
                else if (incomingNode.RelationshipId !=
                         relationshipId)
                {
                    errors.Add(
                        $"Relationship '{relationshipId}' incoming adjacency node " +
                        $"points to relationship '{incomingNode.RelationshipId}'.");
                }
            }
        }

        if (indexedRelationshipIds.Count !=
            _relationships.Count)
        {
            errors.Add(
                $"Relationship index contains " +
                $"{indexedRelationshipIds.Count} relationships, " +
                $"but canonical storage contains {_relationships.Count}.");
        }
    }

    private void ValidateAdjacency(
        List<string> errors)
    {
        /*
         * These sets already existed in the validator and remain the
         * only O(E) temporary membership structures used by this pass.
         * No additional node->owner map is introduced.
         */
        var activeNodes =
            new HashSet<int>();

        var outgoingRelationshipIds =
            new HashSet<Guid>();

        var incomingRelationshipIds =
            new HashSet<Guid>();

        foreach (var pair in
                 _entityAdjacency)
        {
            var entityId =
                pair.Key;

            var adjacency =
                pair.Value;

            if (!_entities.ContainsKey(
                    entityId))
            {
                errors.Add(
                    $"Adjacency index contains unknown entity '{entityId}'.");
            }

            ValidateChain(
                entityId,
                adjacency.OutgoingHead,
                adjacency.OutgoingTail,
                outgoing: true,
                activeNodes,
                outgoingRelationshipIds,
                errors);

            ValidateChain(
                entityId,
                adjacency.IncomingHead,
                adjacency.IncomingTail,
                outgoing: false,
                activeNodes,
                incomingRelationshipIds,
                errors);
        }

        if (outgoingRelationshipIds.Count !=
            _relationships.Count)
        {
            errors.Add(
                $"Outgoing adjacency index contains " +
                $"{outgoingRelationshipIds.Count} relationships, " +
                $"but canonical storage contains {_relationships.Count}.");
        }

        if (incomingRelationshipIds.Count !=
            _relationships.Count)
        {
            errors.Add(
                $"Incoming adjacency index contains " +
                $"{incomingRelationshipIds.Count} relationships, " +
                $"but canonical storage contains {_relationships.Count}.");
        }

        var expectedActiveNodes =
            _relationships.Count * 2;

        if (activeNodes.Count !=
            expectedActiveNodes)
        {
            errors.Add(
                $"Adjacency pool contains {activeNodes.Count} active nodes, " +
                $"but {_relationships.Count} relationships require " +
                $"{expectedActiveNodes}.");
        }

        /*
         * The chain walks above already established relationship
         * membership. Use O(1) HashSet membership checks here to retain
         * the precise missing-from-direction diagnostics without
         * traversing any chain again.
         */
        foreach (var pair in
                 _relationships)
        {
            var relationshipId =
                pair.Key;

            var relationship =
                pair.Value;

            if (!_relationshipIndex.TryGetValue(
                    relationshipId,
                    out var index))
            {
                continue;
            }

            if (!_adjacencyPool.IsValidIndex(
                    index.OutgoingNode) ||
                !_adjacencyPool.IsValidIndex(
                    index.IncomingNode))
            {
                continue;
            }

            if (!outgoingRelationshipIds.Contains(
                    relationshipId))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' is missing from outgoing adjacency " +
                    $"for source '{relationship.SourceId}'.");
            }

            if (!incomingRelationshipIds.Contains(
                    relationshipId))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' is missing from incoming adjacency " +
                    $"for target '{relationship.TargetId}'.");
            }
        }
    }

    private void ValidateChain(
        Guid entityId,
        int head,
        int tail,
        bool outgoing,
        HashSet<int> activeNodes,
        HashSet<Guid> relationshipIds,
        List<string> errors)
    {
        var empty =
            head ==
            AdjacencySlabPool.None;

        if (empty !=
            (tail ==
             AdjacencySlabPool.None))
        {
            errors.Add(
                $"{(outgoing ? "Outgoing" : "Incoming")} adjacency for " +
                $"entity '{entityId}' has mismatched head/tail emptiness.");

            return;
        }

        if (empty)
        {
            return;
        }

        var current =
            head;

        var expectedPrevious =
            AdjacencySlabPool.None;

        var visited =
            0;

        var safetyLimit =
            _adjacencyPool.AllocatedCount + 1;

        while (current !=
               AdjacencySlabPool.None)
        {
            if (++visited >
                safetyLimit)
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency chain for " +
                    $"entity '{entityId}' contains a cycle.");

                return;
            }

            if (!_adjacencyPool.IsValidIndex(
                    current))
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency chain for " +
                    $"entity '{entityId}' references invalid node '{current}'.");

                return;
            }

            if (!activeNodes.Add(
                    current))
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency node " +
                    $"'{current}' is referenced more than once.");

                return;
            }

            var node =
                _adjacencyPool.GetNode(
                    current);

            if (node.IsFree)
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency chain for " +
                    $"entity '{entityId}' references freed node '{current}'.");

                return;
            }

            if (node.Previous !=
                expectedPrevious)
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency node " +
                    $"'{current}' has incorrect previous pointer.");
            }

            if (!_relationships.TryGetValue(
                    node.RelationshipId,
                    out var relationship))
            {
                errors.Add(
                    $"{(outgoing ? "Outgoing" : "Incoming")} adjacency node " +
                    $"'{current}' references missing relationship '{node.RelationshipId}'.");
            }
            else
            {
                relationshipIds.Add(
                    relationship.Id);

                if (outgoing &&
                    relationship.SourceId !=
                        entityId)
                {
                    errors.Add(
                        $"Outgoing adjacency for entity '{entityId}' contains " +
                        $"relationship '{relationship.Id}' whose source is " +
                        $"'{relationship.SourceId}'.");
                }

                if (!outgoing &&
                    relationship.TargetId !=
                        entityId)
                {
                    errors.Add(
                        $"Incoming adjacency for entity '{entityId}' contains " +
                        $"relationship '{relationship.Id}' whose target is " +
                        $"'{relationship.TargetId}'.");
                }

                /*
                 * The relationship index stores the exact physical
                 * node expected for each direction. The current node is
                 * already loaded, so compare directly and avoid a second
                 * walk of the entity's adjacency chain.
                 */
                if (_relationshipIndex.TryGetValue(
                        relationship.Id,
                        out var index))
                {
                    if (outgoing &&
                        index.OutgoingNode !=
                            current)
                    {
                        errors.Add(
                            $"Outgoing adjacency node '{current}' for relationship " +
                            $"'{relationship.Id}' does not match relationship index node " +
                            $"'{index.OutgoingNode}'.");
                    }

                    if (!outgoing &&
                        index.IncomingNode !=
                            current)
                    {
                        errors.Add(
                            $"Incoming adjacency node '{current}' for relationship " +
                            $"'{relationship.Id}' does not match relationship index node " +
                            $"'{index.IncomingNode}'.");
                    }
                }
            }

            expectedPrevious =
                current;

            current =
                node.Next;
        }

        if (expectedPrevious !=
            tail)
        {
            errors.Add(
                $"{(outgoing ? "Outgoing" : "Incoming")} adjacency chain for " +
                $"entity '{entityId}' has tail '{expectedPrevious}' but " +
                $"expected '{tail}'.");
        }
    }

    private void ValidateFreeList(
        List<string> errors)
    {
        var freeNodes =
            new HashSet<int>();

        var current =
            _adjacencyPool.FreeHead;

        var visited =
            0;

        var safetyLimit =
            _adjacencyPool.AllocatedCount + 1;

        while (current !=
               AdjacencySlabPool.None)
        {
            if (++visited >
                safetyLimit)
            {
                errors.Add(
                    "Adjacency pool free-list contains a cycle.");

                return;
            }

            if (!_adjacencyPool.IsValidIndex(
                    current))
            {
                errors.Add(
                    $"Adjacency pool free-list contains invalid node '{current}'.");

                return;
            }

            if (!freeNodes.Add(
                    current))
            {
                errors.Add(
                    $"Adjacency pool free-list references node '{current}' more than once.");

                return;
            }

            var node =
                _adjacencyPool.GetNode(
                    current);

            if (!node.IsFree)
            {
                errors.Add(
                    $"Adjacency pool free-list contains active node '{current}'.");
            }

            current =
                node.Next;
        }

        var activeNodeCount =
            _relationships.Count * 2;

        if (activeNodeCount +
            freeNodes.Count !=
            _adjacencyPool.AllocatedCount)
        {
            errors.Add(
                $"Adjacency pool accounting mismatch. " +
                $"Active={activeNodeCount}, " +
                $"Free={freeNodes.Count}, " +
                $"Allocated={_adjacencyPool.AllocatedCount}.");
        }
    }

    /*
     * =============================================================
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

    /*
     * =============================================================
     * PROPERTY VALIDATION
     * =============================================================
     */

    private static void ValidateProperties(
        IReadOnlyDictionary<string, SorophyProperty> properties,
        string ownerDescription,
        List<string> errors)
    {
        foreach (var pair in
                 properties)
        {
            var propertyKey =
                pair.Key;

            var property =
                pair.Value;

            if (string.IsNullOrWhiteSpace(
                    propertyKey))
            {
                errors.Add(
                    $"{ownerDescription} contains a property with an empty dictionary key.");
            }

            if (property is null)
            {
                errors.Add(
                    $"{ownerDescription} property '{propertyKey}' is null.");

                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    property.Name))
            {
                errors.Add(
                    $"{ownerDescription} property '{propertyKey}' has an empty name.");

                continue;
            }

            if (!string.Equals(
                    propertyKey,
                    property.Name,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"{ownerDescription} property dictionary key '{propertyKey}' " +
                    $"does not match property name '{property.Name}'.");
            }
        }
    }

    /*
     * =============================================================
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