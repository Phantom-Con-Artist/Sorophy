/*
Orb Engine — a structured knowledge and graph engine
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
using System.Linq;

namespace Orb.Engine.Graph;

public sealed class OrbGraph
{
    /*
     * Canonical graph state.
     *
     * These remain the authoritative stores for entities and
     * relationships and are what the serializers consume.
     */
    private readonly Dictionary<Guid, OrbEntity> _entities = new();
    private readonly Dictionary<Guid, OrbRelationship> _relationships = new();

    /*
     * Secondary adjacency indexes.
     *
     * Outgoing:
     *     EntityId -> relationship IDs originating from that entity
     *
     * Incoming:
     *     EntityId -> relationship IDs terminating at that entity
     *
     * RelationshipBucket preserves insertion order while also
     * allowing O(1) removal of a relationship from the bucket.
     */
    private readonly Dictionary<Guid, RelationshipBucket> _outgoing = new();
    private readonly Dictionary<Guid, RelationshipBucket> _incoming = new();

    /*
     * Relationship insertion order.
     *
     * The old implementation enumerated _relationships.Values when
     * answering combined relationship/neighbor queries. Modern .NET
     * Dictionary enumeration preserves insertion order, so we retain
     * an explicit sequence number here instead of scanning the whole
     * relationship dictionary merely to recover that ordering.
     */
    private readonly Dictionary<Guid, long> _relationshipSequence = new();

    private long _nextRelationshipSequence;

    private readonly ReadOnlyDictionary<Guid, OrbEntity> _readOnlyEntities;
    private readonly ReadOnlyDictionary<Guid, OrbRelationship> _readOnlyRelationships;

    public OrbGraph()
    {
        _readOnlyEntities =
            new ReadOnlyDictionary<Guid, OrbEntity>(_entities);

        _readOnlyRelationships =
            new ReadOnlyDictionary<Guid, OrbRelationship>(_relationships);
    }

    public IReadOnlyDictionary<Guid, OrbEntity> Entities =>
        _readOnlyEntities;

    public IReadOnlyDictionary<Guid, OrbRelationship> Relationships =>
        _readOnlyRelationships;

    public void AddEntity(OrbEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }
    }

    public void AddRelationship(OrbRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (!_entities.ContainsKey(relationship.SourceId))
        {
            throw new InvalidOperationException(
                $"Source entity '{relationship.SourceId}' does not exist.");
        }

        if (!_entities.ContainsKey(relationship.TargetId))
        {
            throw new InvalidOperationException(
                $"Target entity '{relationship.TargetId}' does not exist.");
        }

        if (_relationships.ContainsKey(relationship.Id))
        {
            throw new InvalidOperationException(
                $"A relationship with ID '{relationship.Id}' already exists.");
        }

        var outgoingBucket =
            GetOrCreateBucket(
                _outgoing,
                relationship.SourceId);

        var incomingBucket =
            GetOrCreateBucket(
                _incoming,
                relationship.TargetId);

        /*
         * A healthy graph should never already contain this relationship
         * ID in either adjacency bucket because the canonical relationship
         * dictionary rejected the ID above.
         *
         * Treat a stale index entry as an invariant failure rather than
         * silently corrupting the index further.
         */
        if (outgoingBucket.Contains(relationship.Id) ||
            incomingBucket.Contains(relationship.Id) ||
            _relationshipSequence.ContainsKey(relationship.Id))
        {
            RemoveEmptyBucket(
                _outgoing,
                relationship.SourceId,
                outgoingBucket);

            RemoveEmptyBucket(
                _incoming,
                relationship.TargetId,
                incomingBucket);

            throw new InvalidOperationException(
                $"Relationship adjacency indexes already contain ID '{relationship.Id}'.");
        }

        var outgoingAdded = false;
        var incomingAdded = false;
        var relationshipAdded = false;
        var sequenceAdded = false;

        try
        {
            outgoingAdded =
                outgoingBucket.Add(
                    relationship.Id);

            if (!outgoingAdded)
            {
                throw new InvalidOperationException(
                    $"Outgoing adjacency index already contains relationship ID '{relationship.Id}'.");
            }

            incomingAdded =
                incomingBucket.Add(
                    relationship.Id);

            if (!incomingAdded)
            {
                throw new InvalidOperationException(
                    $"Incoming adjacency index already contains relationship ID '{relationship.Id}'.");
            }

            _relationships.Add(
                relationship.Id,
                relationship);

            relationshipAdded = true;

            _relationshipSequence.Add(
                relationship.Id,
                _nextRelationshipSequence);

            sequenceAdded = true;

            _nextRelationshipSequence++;
        }
        catch
        {
            if (sequenceAdded)
            {
                _relationshipSequence.Remove(
                    relationship.Id);
            }

            if (relationshipAdded)
            {
                _relationships.Remove(
                    relationship.Id);
            }

            if (incomingAdded)
            {
                incomingBucket.Remove(
                    relationship.Id);
            }

            if (outgoingAdded)
            {
                outgoingBucket.Remove(
                    relationship.Id);
            }

            RemoveEmptyBucket(
                _outgoing,
                relationship.SourceId,
                outgoingBucket);

            RemoveEmptyBucket(
                _incoming,
                relationship.TargetId,
                incomingBucket);

            throw;
        }
    }

    public bool RemoveEntity(Guid entityId)
    {
        if (!_entities.ContainsKey(entityId))
        {
            return false;
        }

        /*
         * A self-link appears in both indexes, so use a HashSet to
         * guarantee each relationship is removed exactly once.
         */
        var relationshipIds =
            new HashSet<Guid>();

        if (_outgoing.TryGetValue(
                entityId,
                out var outgoingBucket))
        {
            foreach (var relationshipId in outgoingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        if (_incoming.TryGetValue(
                entityId,
                out var incomingBucket))
        {
            foreach (var relationshipId in incomingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        foreach (var relationshipId in relationshipIds)
        {
            RemoveRelationship(
                relationshipId);
        }

        _entities.Remove(
            entityId);

        /*
         * The relationship removals above should already have emptied
         * these buckets. Remove them explicitly so isolated index
         * containers cannot survive an entity deletion.
         */
        _outgoing.Remove(
            entityId);

        _incoming.Remove(
            entityId);

        return true;
    }

    public bool RemoveRelationship(Guid relationshipId)
    {
        if (!_relationships.TryGetValue(
                relationshipId,
                out var relationship))
        {
            return false;
        }

        /*
         * Remove from the canonical store first. The adjacency indexes
         * are then updated from the relationship's recorded endpoints.
         */
        _relationships.Remove(
            relationshipId);

        _relationshipSequence.Remove(
            relationshipId);

        if (_outgoing.TryGetValue(
                relationship.SourceId,
                out var outgoingBucket))
        {
            outgoingBucket.Remove(
                relationshipId);

            RemoveEmptyBucket(
                _outgoing,
                relationship.SourceId,
                outgoingBucket);
        }

        if (_incoming.TryGetValue(
                relationship.TargetId,
                out var incomingBucket))
        {
            incomingBucket.Remove(
                relationshipId);

            RemoveEmptyBucket(
                _incoming,
                relationship.TargetId,
                incomingBucket);
        }

        return true;
    }

    public bool ContainsEntity(Guid entityId)
    {
        return _entities.ContainsKey(
            entityId);
    }

    public bool ContainsRelationship(Guid relationshipId)
    {
        return _relationships.ContainsKey(
            relationshipId);
    }

    public bool TryGetEntity(
        Guid entityId,
        out OrbEntity? entity)
    {
        return _entities.TryGetValue(
            entityId,
            out entity);
    }

    public bool TryGetRelationship(
        Guid relationshipId,
        out OrbRelationship? relationship)
    {
        return _relationships.TryGetValue(
            relationshipId,
            out relationship);
    }

    public IEnumerable<OrbRelationship> GetOutgoingRelationships(
        Guid entityId)
    {
        if (!_outgoing.TryGetValue(
                entityId,
                out var bucket))
        {
            yield break;
        }

        foreach (var relationshipId in bucket)
        {
            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }
        }
    }

    public IEnumerable<OrbRelationship> GetIncomingRelationships(
        Guid entityId)
    {
        if (!_incoming.TryGetValue(
                entityId,
                out var bucket))
        {
            yield break;
        }

        foreach (var relationshipId in bucket)
        {
            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }
        }
    }

    public IEnumerable<OrbRelationship> GetRelationships(
        Guid entityId)
    {
        /*
         * The old implementation scanned the entire relationship store
         * and therefore naturally returned relationships in global
         * insertion order.
         *
         * We collect only this entity's incoming/outgoing relationship IDs
         * and then restore that same global order using the sequence map.
         */
        var relationshipIds =
            new HashSet<Guid>();

        if (_outgoing.TryGetValue(
                entityId,
                out var outgoingBucket))
        {
            foreach (var relationshipId in outgoingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        if (_incoming.TryGetValue(
                entityId,
                out var incomingBucket))
        {
            foreach (var relationshipId in incomingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        foreach (var relationshipId in
                 relationshipIds.OrderBy(
                     id => _relationshipSequence[id]))
        {
            if (_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                yield return relationship;
            }
        }
    }

    public IEnumerable<OrbEntity> GetNeighbors(
        Guid entityId)
    {
        /*
         * Preserve the old behavior:
         *
         * - incoming and outgoing are both considered
         * - self-links do not produce a neighbor
         * - a neighbor is returned only once
         * - relationship insertion order determines discovery order
         */
        var relationshipIds =
            new HashSet<Guid>();

        if (_outgoing.TryGetValue(
                entityId,
                out var outgoingBucket))
        {
            foreach (var relationshipId in outgoingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        if (_incoming.TryGetValue(
                entityId,
                out var incomingBucket))
        {
            foreach (var relationshipId in incomingBucket)
            {
                relationshipIds.Add(
                    relationshipId);
            }
        }

        var neighborIds =
            new HashSet<Guid>();

        foreach (var relationshipId in
                 relationshipIds.OrderBy(
                     id => _relationshipSequence[id]))
        {
            if (!_relationships.TryGetValue(
                    relationshipId,
                    out var relationship))
            {
                continue;
            }

            if (relationship.SourceId == entityId &&
                relationship.TargetId != entityId)
            {
                neighborIds.Add(
                    relationship.TargetId);
            }

            if (relationship.TargetId == entityId &&
                relationship.SourceId != entityId)
            {
                neighborIds.Add(
                    relationship.SourceId);
            }
        }

        foreach (var neighborId in neighborIds)
        {
            if (_entities.TryGetValue(
                    neighborId,
                    out var entity))
            {
                yield return entity;
            }
        }
    }

    public bool IsReachable(
        Guid sourceId,
        Guid targetId)
    {
        if (!ContainsEntity(sourceId) ||
            !ContainsEntity(targetId))
        {
            return false;
        }

        if (sourceId == targetId)
        {
            return true;
        }

        var visited =
            new HashSet<Guid>();

        var queue =
            new Queue<Guid>();

        queue.Enqueue(
            sourceId);

        visited.Add(
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

                if (nextId == targetId)
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

    public IEnumerable<OrbEntity> Traverse(
        Guid sourceId)
    {
        if (!ContainsEntity(sourceId))
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

    public IReadOnlyList<string> Validate()
    {
        var errors =
            new List<string>();

        /*
         * ---------------------------------------------------------
         * ENTITY STORE
         * ---------------------------------------------------------
         */
        foreach (var pair in _entities)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Entity dictionary contains a null entity for ID '{pair.Key}'.");

                continue;
            }

            if (pair.Key != pair.Value.Id)
            {
                errors.Add(
                    $"Entity dictionary key '{pair.Key}' does not match entity ID '{pair.Value.Id}'.");
            }

            ValidateProperties(
                pair.Value.Properties,
                $"Entity '{pair.Value.Id}'",
                errors);
        }

        /*
         * ---------------------------------------------------------
         * RELATIONSHIP STORE
         * ---------------------------------------------------------
         */
        foreach (var pair in _relationships)
        {
            if (pair.Value is null)
            {
                errors.Add(
                    $"Relationship dictionary contains a null relationship for ID '{pair.Key}'.");

                continue;
            }

            var relationship =
                pair.Value;

            if (pair.Key != relationship.Id)
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

            if (!_relationshipSequence.ContainsKey(
                    relationship.Id))
            {
                errors.Add(
                    $"Relationship '{relationship.Id}' is missing a global insertion sequence.");
            }

            ValidateProperties(
                relationship.Properties,
                $"Relationship '{relationship.Id}'",
                errors);
        }

        /*
         * ---------------------------------------------------------
         * GLOBAL SEQUENCE INDEX
         * ---------------------------------------------------------
         */
        foreach (var sequence in _relationshipSequence)
        {
            if (!_relationships.ContainsKey(
                    sequence.Key))
            {
                errors.Add(
                    $"Relationship sequence index contains stale relationship ID '{sequence.Key}'.");
            }
        }

        /*
         * ---------------------------------------------------------
         * OUTGOING ADJACENCY
         * ---------------------------------------------------------
         */
        var indexedOutgoing =
            new HashSet<Guid>();

        foreach (var pair in _outgoing)
        {
            var entityId =
                pair.Key;

            var bucket =
                pair.Value;

            if (!_entities.ContainsKey(
                    entityId))
            {
                errors.Add(
                    $"Outgoing adjacency index contains unknown entity '{entityId}'.");
            }

            if (bucket is null)
            {
                errors.Add(
                    $"Outgoing adjacency index contains a null bucket for entity '{entityId}'.");

                continue;
            }

            if (!bucket.IsInternallyConsistent())
            {
                errors.Add(
                    $"Outgoing adjacency bucket for entity '{entityId}' is internally inconsistent.");
            }

            foreach (var relationshipId in bucket)
            {
                if (!indexedOutgoing.Add(
                        relationshipId))
                {
                    errors.Add(
                        $"Outgoing adjacency index contains duplicate relationship ID '{relationshipId}'.");
                }

                if (!_relationships.TryGetValue(
                        relationshipId,
                        out var relationship))
                {
                    errors.Add(
                        $"Outgoing adjacency index for entity '{entityId}' references missing relationship '{relationshipId}'.");

                    continue;
                }

                if (relationship.SourceId != entityId)
                {
                    errors.Add(
                        $"Outgoing adjacency index for entity '{entityId}' contains relationship '{relationshipId}' whose source is '{relationship.SourceId}'.");
                }
            }
        }

        /*
         * ---------------------------------------------------------
         * INCOMING ADJACENCY
         * ---------------------------------------------------------
         */
        var indexedIncoming =
            new HashSet<Guid>();

        foreach (var pair in _incoming)
        {
            var entityId =
                pair.Key;

            var bucket =
                pair.Value;

            if (!_entities.ContainsKey(
                    entityId))
            {
                errors.Add(
                    $"Incoming adjacency index contains unknown entity '{entityId}'.");
            }

            if (bucket is null)
            {
                errors.Add(
                    $"Incoming adjacency index contains a null bucket for entity '{entityId}'.");

                continue;
            }

            if (!bucket.IsInternallyConsistent())
            {
                errors.Add(
                    $"Incoming adjacency bucket for entity '{entityId}' is internally inconsistent.");
            }

            foreach (var relationshipId in bucket)
            {
                if (!indexedIncoming.Add(
                        relationshipId))
                {
                    errors.Add(
                        $"Incoming adjacency index contains duplicate relationship ID '{relationshipId}'.");
                }

                if (!_relationships.TryGetValue(
                        relationshipId,
                        out var relationship))
                {
                    errors.Add(
                        $"Incoming adjacency index for entity '{entityId}' references missing relationship '{relationshipId}'.");

                    continue;
                }

                if (relationship.TargetId != entityId)
                {
                    errors.Add(
                        $"Incoming adjacency index for entity '{entityId}' contains relationship '{relationshipId}' whose target is '{relationship.TargetId}'.");
                }
            }
        }

        /*
         * ---------------------------------------------------------
         * BIDIRECTIONAL INDEX COMPLETENESS
         * ---------------------------------------------------------
         *
         * Every canonical relationship must exist:
         *
         *   exactly once in outgoing
         *   exactly once in incoming
         *
         * Self-links intentionally appear once in each separate index.
         */
        foreach (var pair in _relationships)
        {
            var relationshipId =
                pair.Key;

            var relationship =
                pair.Value;

            if (!_outgoing.TryGetValue(
                    relationship.SourceId,
                    out var outgoingBucket) ||
                outgoingBucket is null ||
                !outgoingBucket.Contains(
                    relationshipId))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' is missing from outgoing adjacency index for source '{relationship.SourceId}'.");
            }

            if (!_incoming.TryGetValue(
                    relationship.TargetId,
                    out var incomingBucket) ||
                incomingBucket is null ||
                !incomingBucket.Contains(
                    relationshipId))
            {
                errors.Add(
                    $"Relationship '{relationshipId}' is missing from incoming adjacency index for target '{relationship.TargetId}'.");
            }
        }

        if (indexedOutgoing.Count !=
            _relationships.Count)
        {
            errors.Add(
                $"Outgoing adjacency index contains {indexedOutgoing.Count} unique relationships, but canonical storage contains {_relationships.Count}.");
        }

        if (indexedIncoming.Count !=
            _relationships.Count)
        {
            errors.Add(
                $"Incoming adjacency index contains {indexedIncoming.Count} unique relationships, but canonical storage contains {_relationships.Count}.");
        }

        return errors;
    }

    private static RelationshipBucket GetOrCreateBucket(
        Dictionary<Guid, RelationshipBucket> index,
        Guid entityId)
    {
        if (index.TryGetValue(
                entityId,
                out var bucket))
        {
            return bucket;
        }

        bucket =
            new RelationshipBucket();

        index.Add(
            entityId,
            bucket);

        return bucket;
    }

    private static void RemoveEmptyBucket(
        Dictionary<Guid, RelationshipBucket> index,
        Guid entityId,
        RelationshipBucket bucket)
    {
        if (bucket.Count != 0)
        {
            return;
        }

        index.Remove(
            entityId);
    }

    private static void ValidateProperties(
        IReadOnlyDictionary<string, OrbProperty> properties,
        string ownerDescription,
        List<string> errors)
    {
        foreach (var pair in properties)
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
                    $"{ownerDescription} property dictionary key '{propertyKey}' does not match property name '{property.Name}'.");
            }
        }
    }

    /*
     * -------------------------------------------------------------
     * ORDERED ADJACENCY BUCKET
     * -------------------------------------------------------------
     *
     * LinkedList preserves insertion order.
     *
     * Dictionary<Guid, LinkedListNode<Guid>> allows relationship
     * removal without scanning the entire adjacency list.
     *
     * That gives us:
     *
     *   Add       O(1)
     *   Contains  O(1)
     *   Remove   O(1)
     *   Iterate   O(degree)
     */
    private sealed class RelationshipBucket
    {
        private readonly LinkedList<Guid> _order = new();

        private readonly Dictionary<
            Guid,
            LinkedListNode<Guid>> _nodes = new();

        public int Count =>
            _order.Count;

        public bool Add(Guid relationshipId)
        {
            if (_nodes.ContainsKey(
                    relationshipId))
            {
                return false;
            }

            var node =
                _order.AddLast(
                    relationshipId);

            _nodes.Add(
                relationshipId,
                node);

            return true;
        }

        public bool Remove(
            Guid relationshipId)
        {
            if (!_nodes.TryGetValue(
                    relationshipId,
                    out var node))
            {
                return false;
            }

            _nodes.Remove(
                relationshipId);

            _order.Remove(
                node);

            return true;
        }

        public bool Contains(
            Guid relationshipId)
        {
            return _nodes.ContainsKey(
                relationshipId);
        }

        public bool IsInternallyConsistent()
        {
            if (_order.Count !=
                _nodes.Count)
            {
                return false;
            }

            foreach (var relationshipId in _order)
            {
                if (!_nodes.TryGetValue(
                        relationshipId,
                        out var node))
                {
                    return false;
                }

                if (node.List != _order ||
                    node.Value != relationshipId)
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerator<Guid> GetEnumerator()
        {
            return _order.GetEnumerator();
        }
    }
}