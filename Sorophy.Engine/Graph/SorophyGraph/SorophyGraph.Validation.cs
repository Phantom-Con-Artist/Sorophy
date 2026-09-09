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

        ValidateRelationshipHistoryStore(
            errors);

        ValidateEntityHistoryStore(
            errors);

        ValidateTagIndex(
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

    private void ValidateTagIndex(
        List<string> errors)
    {
        /*
         * First validate the index's own forward/reverse invariants.
         */
        foreach (var error in
                 _tagIndex.Validate())
        {
            errors.Add(
                $"Tag index: {error}");
        }

        /*
         * Every canonical entity must have a corresponding reverse-index
         * entry, even if it currently carries no tags.
         */
        if (_tagIndex.EntityCount !=
            _entities.Count)
        {
            errors.Add(
                $"Tag index contains {_tagIndex.EntityCount} entities, " +
                $"but canonical entity storage contains {_entities.Count}.");
        }

        foreach (var pair in
                 _entities)
        {
            var entityId =
                pair.Key;

            var entity =
                pair.Value;

            if (entity is null)
            {
                continue;
            }

            if (!_tagIndex.ContainsEntity(
                    entityId))
            {
                errors.Add(
                    $"Entity '{entityId}' is missing from the tag index.");

                continue;
            }

            foreach (var tag in
                     entity.Tags)
            {
                if (string.IsNullOrWhiteSpace(
                        tag))
                {
                    errors.Add(
                        $"Entity '{entityId}' contains an empty tag.");

                    continue;
                }

                if (!_tagIndex.HasTag(
                        entityId,
                        tag))
                {
                    errors.Add(
                        $"Tag index is missing membership for entity " +
                        $"'{entityId}' under tag '{tag}'.");
                }
            }
        }

        /*
         * Walk every tag bucket and verify that each indexed Entity exists
         * canonically and still carries that tag. This also detects stale
         * memberships caused by direct mutation of Entity.Tags.
         */
        foreach (var tag in
                 _tagIndex.GetTags())
        {
            foreach (var entityId in
                     _tagIndex.GetEntityIds(
                         tag))
            {
                if (!_entities.TryGetValue(
                        entityId,
                        out var entity))
                {
                    errors.Add(
                        $"Tag index contains stale entity '{entityId}' " +
                        $"under tag '{tag}'.");

                    continue;
                }

                if (!entity.Tags.Contains(
                        tag))
                {
                    errors.Add(
                        $"Tag index contains entity '{entityId}' under tag " +
                        $"'{tag}', but the entity does not carry that tag.");
                }
            }
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

}
