[1mdiff --git a/Orb.Engine/Graph/OrbGraph.cs b/Orb.Engine/Graph/OrbGraph.cs[m
[1mindex f027731..39b059f 100644[m
[1m--- a/Orb.Engine/Graph/OrbGraph.cs[m
[1m+++ b/Orb.Engine/Graph/OrbGraph.cs[m
[36m@@ -19,59 +19,83 @@[m [malong with this program.  If not, see <https://www.gnu.org/licenses/>.[m
 using System;[m
 using System.Collections.Generic;[m
 using System.Collections.ObjectModel;[m
[31m-using System.Linq;[m
 [m
 namespace Orb.Engine.Graph;[m
 [m
 public sealed class OrbGraph[m
 {[m
     /*[m
[31m-     * Canonical graph state.[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * CANONICAL GRAPH STATE[m
[32m+[m[32m     * =============================================================[m
      *[m
      * These remain the authoritative stores for entities and[m
[31m-     * relationships and are what the serializers consume.[m
[32m+[m[32m     * relationships. Serialization continues to operate against[m
[32m+[m[32m     * these public graph collections.[m
      */[m
     private readonly Dictionary<Guid, OrbEntity> _entities = new();[m
[32m+[m
     private readonly Dictionary<Guid, OrbRelationship> _relationships = new();[m
 [m
     /*[m
[31m-     * Secondary adjacency indexes.[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * COMPACT ADJACENCY INDEX[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     *[m
[32m+[m[32m     * One compact record per entity replaces the previous pair of[m
[32m+[m[32m     * heap-heavy RelationshipBucket objects.[m
[32m+[m[32m     *[m
[32m+[m[32m     * The record contains four integer indexes:[m
[32m+[m[32m     *[m
[32m+[m[32m     *     outgoing head / tail[m
[32m+[m[32m     *     incoming head / tail[m
      *[m
[31m-     * Outgoing:[m
[31m-     *     EntityId -> relationship IDs originating from that entity[m
[32m+[m[32m     * The actual adjacency records live in _adjacencyPool.[m
[32m+[m[32m     */[m
[32m+[m[32m    private readonly Dictionary<Guid, EntityAdjacency> _entityAdjacency =[m
[32m+[m[32m        new();[m
[32m+[m
[32m+[m[32m    /*[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * RELATIONSHIP INDEX[m
[32m+[m[32m     * =============================================================[m
      *[m
[31m-     * Incoming:[m
[31m-     *     EntityId -> relationship IDs terminating at that entity[m
[32m+[m[32m     * Maps a relationship ID to its two adjacency-node positions[m
[32m+[m[32m     * plus its global insertion sequence.[m
      *[m
[31m-     * RelationshipBucket preserves insertion order while also[m
[31m-     * allowing O(1) removal of a relationship from the bucket.[m
[32m+[m[32m     * This replaces the old dedicated _relationshipSequence dictionary.[m
      */[m
[31m-    private readonly Dictionary<Guid, RelationshipBucket> _outgoing = new();[m
[31m-    private readonly Dictionary<Guid, RelationshipBucket> _incoming = new();[m
[32m+[m[32m    private readonly Dictionary<Guid, RelationshipIndex> _relationshipIndex =[m
[32m+[m[32m        new();[m
 [m
     /*[m
[31m-     * Relationship insertion order.[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * ADJACENCY POOL[m
[32m+[m[32m     * =============================================================[m
      *[m
[31m-     * The old implementation enumerated _relationships.Values when[m
[31m-     * answering combined relationship/neighbor queries. Modern .NET[m
[31m-     * Dictionary enumeration preserves insertion order, so we retain[m
[31m-     * an explicit sequence number here instead of scanning the whole[m
[31m-     * relationship dictionary merely to recover that ordering.[m
[32m+[m[32m     * Adjacency nodes are stored in slabs rather than as individual[m
[32m+[m[32m     * LinkedListNode / Dictionary objects.[m
      */[m
[31m-    private readonly Dictionary<Guid, long> _relationshipSequence = new();[m
[32m+[m[32m    private readonly AdjacencySlabPool _adjacencyPool =[m
[32m+[m[32m        new();[m
 [m
     private long _nextRelationshipSequence;[m
 [m
[31m-    private readonly ReadOnlyDictionary<Guid, OrbEntity> _readOnlyEntities;[m
[31m-    private readonly ReadOnlyDictionary<Guid, OrbRelationship> _readOnlyRelationships;[m
[32m+[m[32m    private readonly ReadOnlyDictionary<Guid, OrbEntity>[m
[32m+[m[32m        _readOnlyEntities;[m
[32m+[m
[32m+[m[32m    private readonly ReadOnlyDictionary<Guid, OrbRelationship>[m
[32m+[m[32m        _readOnlyRelationships;[m
 [m
     public OrbGraph()[m
     {[m
         _readOnlyEntities =[m
[31m-            new ReadOnlyDictionary<Guid, OrbEntity>(_entities);[m
[32m+[m[32m            new ReadOnlyDictionary<Guid, OrbEntity>([m
[32m+[m[32m                _entities);[m
 [m
         _readOnlyRelationships =[m
[31m-            new ReadOnlyDictionary<Guid, OrbRelationship>(_relationships);[m
[32m+[m[32m            new ReadOnlyDictionary<Guid, OrbRelationship>([m
[32m+[m[32m                _relationships);[m
     }[m
 [m
     public IReadOnlyDictionary<Guid, OrbEntity> Entities =>[m
[36m@@ -80,270 +104,297 @@[m [mpublic sealed class OrbGraph[m
     public IReadOnlyDictionary<Guid, OrbRelationship> Relationships =>[m
         _readOnlyRelationships;[m
 [m
[32m+[m[32m    /*[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * ENTITY MUTATION[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     */[m
[32m+[m
     public void AddEntity(OrbEntity entity)[m
     {[m
         ArgumentNullException.ThrowIfNull(entity);[m
 [m
[31m-        if (!_entities.TryAdd(entity.Id, entity))[m
[32m+[m[32m        if (!_entities.TryAdd([m
[32m+[m[32m                entity.Id,[m
[32m+[m[32m                entity))[m
         {[m
             throw new InvalidOperationException([m
                 $"An entity with ID '{entity.Id}' already exists.");[m
         }[m
     }[m
 [m
[31m-    public void AddRelationship(OrbRelationship relationship)[m
[32m+[m[32m    public bool RemoveEntity(Guid entityId)[m
[32m+[m[32m    {[m
[32m+[m[32m        if (!_entities.ContainsKey(entityId))[m
[32m+[m[32m        {[m
[32m+[m[32m            return false;[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m[32m        /*[m
[32m+[m[32m         * Remove outgoing relationships first.[m
[32m+[m[32m         *[m
[32m+[m[32m         * Self-links are naturally handled because removing the[m
[32m+[m[32m         * relationship removes both its outgoing and incoming nodes.[m
[32m+[m[32m         */[m
[32m+[m[32m        EntityAdjacency adjacency;[m
[32m+[m
[32m+[m[32m        while (_entityAdjacency.TryGetValue([m
[32m+[m[32m                   entityId,[m
[32m+[m[32m                   out adjacency) &&[m
[32m+[m[32m               adjacency.OutgoingHead !=[m
[32m+[m[32m                   AdjacencySlabPool.None)[m
[32m+[m[32m        {[m
[32m+[m[32m            var nodeIndex =[m
[32m+[m[32m                adjacency.OutgoingHead;[m
[32m+[m
[32m+[m[32m            var relationshipId =[m
[32m+[m[32m                _adjacencyPool[m
[32m+[m[32m                    .GetNode(nodeIndex)[m
[32m+[m[32m                    .RelationshipId;[m
[32m+[m
[32m+[m[32m            if (!RemoveRelationship([m
[32m+[m[32m                    relationshipId))[m
[32m+[m[32m            {[m
[32m+[m[32m                throw new InvalidOperationException([m
[32m+[m[32m                    $"Adjacency index references relationship '{relationshipId}', " +[m
[32m+[m[32m                    "but the relationship could not be removed.");[m
[32m+[m[32m            }[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m[32m        /*[m
[32m+[m[32m         * There may still be incoming relationships after all outgoing[m
[32m+[m[32m         * relationships have been removed.[m
[32m+[m[32m         */[m
[32m+[m[32m        while (_entityAdjacency.TryGetValue([m
[32m+[m[32m                   entityId,[m
[32m+[m[32m                   out adjacency) &&[m
[32m+[m[32m               adjacency.IncomingHead !=[m
[32m+[m[32m                   AdjacencySlabPool.None)[m
[32m+[m[32m        {[m
[32m+[m[32m            var nodeIndex =[m
[32m+[m[32m                adjacency.IncomingHead;[m
[32m+[m
[32m+[m[32m            var relationshipId =[m
[32m+[m[32m                _adjacencyPool[m
[32m+[m[32m                    .GetNode(nodeIndex)[m
[32m+[m[32m                    .RelationshipId;[m
[32m+[m
[32m+[m[32m            if (!RemoveRelationship([m
[32m+[m[32m                    relationshipId))[m
[32m+[m[32m            {[m
[32m+[m[32m                throw new InvalidOperationException([m
[32m+[m[32m                    $"Adjacency index references relationship '{relationshipId}', " +[m
[32m+[m[32m                    "but the relationship could not be removed.");[m
[32m+[m[32m            }[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m[32m        _entityAdjacency.Remove([m
[32m+[m[32m            entityId);[m
[32m+[m
[32m+[m[32m        _entities.Remove([m
[32m+[m[32m            entityId);[m
[32m+[m
[32m+[m[32m        return true;[m
[32m+[m[32m    }[m
[32m+[m
[32m+[m[32m    /*[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * RELATIONSHIP MUTATION[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     */[m
[32m+[m
[32m+[m[32m    public void AddRelationship([m
[32m+[m[32m        OrbRelationship relationship)[m
     {[m
         ArgumentNullException.ThrowIfNull(relationship);[m
 [m
[31m-        if (!_entities.ContainsKey(relationship.SourceId))[m
[32m+[m[32m        if (!_entities.ContainsKey([m
[32m+[m[32m                relationship.SourceId))[m
         {[m
             throw new InvalidOperationException([m
                 $"Source entity '{relationship.SourceId}' does not exist.");[m
         }[m
 [m
[31m-        if (!_entities.ContainsKey(relationship.TargetId))[m
[32m+[m[32m        if (!_entities.ContainsKey([m
[32m+[m[32m                relationship.TargetId))[m
         {[m
             throw new InvalidOperationException([m
                 $"Target entity '{relationship.TargetId}' does not exist.");[m
         }[m
 [m
[31m-        if (_relationships.ContainsKey(relationship.Id))[m
[32m+[m[32m        if (_relationships.ContainsKey([m
[32m+[m[32m                relationship.Id))[m
         {[m
             throw new InvalidOperationException([m
                 $"A relationship with ID '{relationship.Id}' already exists.");[m
         }[m
 [m
[31m-        var outgoingBucket =[m
[31m-            GetOrCreateBucket([m
[31m-                _outgoing,[m
[31m-                relationship.SourceId);[m
[31m-[m
[31m-        var incomingBucket =[m
[31m-            GetOrCreateBucket([m
[31m-                _incoming,[m
[31m-                relationship.TargetId);[m
[31m-[m
         /*[m
[31m-         * A healthy graph should never already contain this relationship[m
[31m-         * ID in either adjacency bucket because the canonical relationship[m
[31m-         * dictionary rejected the ID above.[m
[31m-         *[m
[31m-         * Treat a stale index entry as an invariant failure rather than[m
[31m-         * silently corrupting the index further.[m
[32m+[m[32m         * Allocate the two compact adjacency records.[m
          */[m
[31m-        if (outgoingBucket.Contains(relationship.Id) ||[m
[31m-            incomingBucket.Contains(relationship.Id) ||[m
[31m-            _relationshipSequence.ContainsKey(relationship.Id))[m
[31m-        {[m
[31m-            RemoveEmptyBucket([m
[31m-                _outgoing,[m
[31m-                relationship.SourceId,[m
[31m-                outgoingBucket);[m
[31m-[m
[31m-            RemoveEmptyBucket([m
[31m-                _incoming,[m
[31m-                relationship.TargetId,[m
[31m-                incomingBucket);[m
[32m+[m[32m        var outgoingNode =[m
[32m+[m[32m            _adjacencyPool.Allocate([m
[32m+[m[32m                relationship.Id);[m
 [m
[31m-            throw new InvalidOperationException([m
[31m-                $"Relationship adjacency indexes already contain ID '{relationship.Id}'.");[m
[31m-        }[m
[32m+[m[32m        var incomingNode =[m
[32m+[m[32m            _adjacencyPool.Allocate([m
[32m+[m[32m                relationship.Id);[m
 [m
[31m-        var outgoingAdded = false;[m
[31m-        var incomingAdded = false;[m
         var relationshipAdded = false;[m
[31m-        var sequenceAdded = false;[m
[32m+[m[32m        var relationshipIndexAdded = false;[m
[32m+[m[32m        var outgoingLinked = false;[m
[32m+[m[32m        var incomingLinked = false;[m
 [m
         try[m
         {[m
[31m-            outgoingAdded =[m
[31m-                outgoingBucket.Add([m
[31m-                    relationship.Id);[m
[31m-[m
[31m-            if (!outgoingAdded)[m
[31m-            {[m
[31m-                throw new InvalidOperationException([m
[31m-                    $"Outgoing adjacency index already contains relationship ID '{relationship.Id}'.");[m
[31m-            }[m
[31m-[m
[31m-            incomingAdded =[m
[31m-                incomingBucket.Add([m
[31m-                    relationship.Id);[m
[31m-[m
[31m-            if (!incomingAdded)[m
[31m-            {[m
[31m-                throw new InvalidOperationException([m
[31m-                    $"Incoming adjacency index already contains relationship ID '{relationship.Id}'.");[m
[31m-            }[m
[31m-[m
[32m+[m[32m            /*[m
[32m+[m[32m             * Canonical relationship store.[m
[32m+[m[32m             */[m
             _relationships.Add([m
                 relationship.Id,[m
                 relationship);[m
 [m
             relationshipAdded = true;[m
 [m
[31m-            _relationshipSequence.Add([m
[32m+[m[32m            /*[m
[32m+[m[32m             * Compact relationship metadata.[m
[32m+[m[32m             */[m
[32m+[m[32m            _relationshipIndex.Add([m
                 relationship.Id,[m
[31m-                _nextRelationshipSequence);[m
[32m+[m[32m                new RelationshipIndex([m
[32m+[m[32m                    outgoingNode,[m
[32m+[m[32m                    incomingNode,[m
[32m+[m[32m                    _nextRelationshipSequence));[m
[32m+[m
[32m+[m[32m            relationshipIndexAdded = true;[m
[32m+[m
[32m+[m[32m            /*[m
[32m+[m[32m             * Link both directional indexes.[m
[32m+[m[32m             */[m
[32m+[m[32m            LinkOutgoing([m
[32m+[m[32m                relationship.SourceId,[m
[32m+[m[32m                outgoingNode);[m
[32m+[m
[32m+[m[32m            outgoingLinked = true;[m
[32m+[m
[32m+[m[32m            LinkIncoming([m
[32m+[m[32m                relationship.TargetId,[m
[32m+[m[32m                incomingNode);[m
 [m
[31m-            sequenceAdded = true;[m
[32m+[m[32m            incomingLinked = true;[m
 [m
[32m+[m[32m            /*[m
[32m+[m[32m             * Only consume the sequence number once the entire mutation[m
[32m+[m[32m             * has succeeded.[m
[32m+[m[32m             */[m
             _nextRelationshipSequence++;[m
         }[m
         catch[m
         {[m
[31m-            if (sequenceAdded)[m
[32m+[m[32m            /*[m
[32m+[m[32m             * Complete transactional rollback.[m
[32m+[m[32m             */[m
[32m+[m[32m            if (incomingLinked)[m
             {[m
[31m-                _relationshipSequence.Remove([m
[31m-                    relationship.Id);[m
[32m+[m[32m                UnlinkIncoming([m
[32m+[m[32m                    relationship.TargetId,[m
[32m+[m[32m                    incomingNode);[m
             }[m
 [m
[31m-            if (relationshipAdded)[m
[32m+[m[32m            if (outgoingLinked)[m
             {[m
[31m-                _relationships.Remove([m
[31m-                    relationship.Id);[m
[32m+[m[32m                UnlinkOutgoing([m
[32m+[m[32m                    relationship.SourceId,[m
[32m+[m[32m                    outgoingNode);[m
             }[m
 [m
[31m-            if (incomingAdded)[m
[32m+[m[32m            if (relationshipIndexAdded)[m
             {[m
[31m-                incomingBucket.Remove([m
[32m+[m[32m                _relationshipIndex.Remove([m
                     relationship.Id);[m
             }[m
 [m
[31m-            if (outgoingAdded)[m
[32m+[m[32m            if (relationshipAdded)[m
             {[m
[31m-                outgoingBucket.Remove([m
[32m+[m[32m                _relationships.Remove([m
                     relationship.Id);[m
             }[m
 [m
[31m-            RemoveEmptyBucket([m
[31m-                _outgoing,[m
[31m-                relationship.SourceId,[m
[31m-                outgoingBucket);[m
[32m+[m[32m            _adjacencyPool.Release([m
[32m+[m[32m                outgoingNode);[m
 [m
[31m-            RemoveEmptyBucket([m
[31m-                _incoming,[m
[31m-                relationship.TargetId,[m
[31m-                incomingBucket);[m
[32m+[m[32m            _adjacencyPool.Release([m
[32m+[m[32m                incomingNode);[m
 [m
             throw;[m
         }[m
     }[m
 [m
[31m-    public bool RemoveEntity(Guid entityId)[m
[32m+[m[32m    public bool RemoveRelationship([m
[32m+[m[32m        Guid relationshipId)[m
     {[m
[31m-        if (!_entities.ContainsKey(entityId))[m
[32m+[m[32m        if (!_relationships.TryGetValue([m
[32m+[m[32m                relationshipId,[m
[32m+[m[32m                out var relationship))[m
         {[m
             return false;[m
         }[m
 [m
[31m-        /*[m
[31m-         * A self-link appears in both indexes, so use a HashSet to[m
[31m-         * guarantee each relationship is removed exactly once.[m
[31m-         */[m
[31m-        var relationshipIds =[m
[31m-            new HashSet<Guid>();[m
[31m-[m
[31m-        if (_outgoing.TryGetValue([m
[31m-                entityId,[m
[31m-                out var outgoingBucket))[m
[31m-        {[m
[31m-            foreach (var relationshipId in outgoingBucket)[m
[31m-            {[m
[31m-                relationshipIds.Add([m
[31m-                    relationshipId);[m
[31m-            }[m
[31m-        }[m
[31m-[m
[31m-        if (_incoming.TryGetValue([m
[31m-                entityId,[m
[31m-                out var incomingBucket))[m
[31m-        {[m
[31m-            foreach (var relationshipId in incomingBucket)[m
[31m-            {[m
[31m-                relationshipIds.Add([m
[31m-                    relationshipId);[m
[31m-            }[m
[31m-        }[m
[31m-[m
[31m-        foreach (var relationshipId in relationshipIds)[m
[31m-        {[m
[31m-            RemoveRelationship([m
[31m-                relationshipId);[m
[31m-        }[m
[31m-[m
[31m-        _entities.Remove([m
[31m-            entityId);[m
[31m-[m
[31m-        /*[m
[31m-         * The relationship removals above should already have emptied[m
[31m-         * these buckets. Remove them explicitly so isolated index[m
[31m-         * containers cannot survive an entity deletion.[m
[31m-         */[m
[31m-        _outgoing.Remove([m
[31m-            entityId);[m
[31m-[m
[31m-        _incoming.Remove([m
[31m-            entityId);[m
[31m-[m
[31m-        return true;[m
[31m-    }[m
[31m-[m
[31m-    public bool RemoveRelationship(Guid relationshipId)[m
[31m-    {[m
[31m-        if (!_relationships.TryGetValue([m
[32m+[m[32m        if (!_relationshipIndex.TryGetValue([m
                 relationshipId,[m
[31m-                out var relationship))[m
[32m+[m[32m                out var relationshipIndex))[m
         {[m
[31m-            return false;[m
[32m+[m[32m            throw new InvalidOperationException([m
[32m+[m[32m                $"Relationship '{relationshipId}' is missing its relationship index.");[m
         }[m
 [m
         /*[m
[31m-         * Remove from the canonical store first. The adjacency indexes[m
[31m-         * are then updated from the relationship's recorded endpoints.[m
[32m+[m[32m         * Remove canonical state first.[m
          */[m
         _relationships.Remove([m
             relationshipId);[m
 [m
[31m-        _relationshipSequence.Remove([m
[32m+[m[32m        _relationshipIndex.Remove([m
             relationshipId);[m
 [m
[31m-        if (_outgoing.TryGetValue([m
[31m-                relationship.SourceId,[m
[31m-                out var outgoingBucket))[m
[31m-        {[m
[31m-            outgoingBucket.Remove([m
[31m-                relationshipId);[m
[32m+[m[32m        /*[m
[32m+[m[32m         * Remove both adjacency nodes.[m
[32m+[m[32m         */[m
[32m+[m[32m        UnlinkOutgoing([m
[32m+[m[32m            relationship.SourceId,[m
[32m+[m[32m            relationshipIndex.OutgoingNode);[m
 [m
[31m-            RemoveEmptyBucket([m
[31m-                _outgoing,[m
[31m-                relationship.SourceId,[m
[31m-                outgoingBucket);[m
[31m-        }[m
[32m+[m[32m        UnlinkIncoming([m
[32m+[m[32m            relationship.TargetId,[m
[32m+[m[32m            relationshipIndex.IncomingNode);[m
 [m
[31m-        if (_incoming.TryGetValue([m
[31m-                relationship.TargetId,[m
[31m-                out var incomingBucket))[m
[31m-        {[m
[31m-            incomingBucket.Remove([m
[31m-                relationshipId);[m
[32m+[m[32m        /*[m
[32m+[m[32m         * Return the physical nodes to the free list.[m
[32m+[m[32m         */[m
[32m+[m[32m        _adjacencyPool.Release([m
[32m+[m[32m            relationshipIndex.OutgoingNode);[m
 [m
[31m-            RemoveEmptyBucket([m
[31m-                _incoming,[m
[31m-                relationship.TargetId,[m
[31m-                incomingBucket);[m
[31m-        }[m
[32m+[m[32m        _adjacencyPool.Release([m
[32m+[m[32m            relationshipIndex.IncomingNode);[m
 [m
         return true;[m
     }[m
 [m
[31m-    public bool ContainsEntity(Guid entityId)[m
[32m+[m[32m    /*[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * DIRECT LOOKUPS[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     */[m
[32m+[m
[32m+[m[32m    public bool ContainsEntity([m
[32m+[m[32m        Guid entityId)[m
     {[m
         return _entities.ContainsKey([m
             entityId);[m
     }[m
 [m
[31m-    public bool ContainsRelationship(Guid relationshipId)[m
[32m+[m[32m    public bool ContainsRelationship([m
[32m+[m[32m        Guid relationshipId)[m
     {[m
         return _relationships.ContainsKey([m
             relationshipId);[m
[36m@@ -367,87 +418,123 @@[m [mpublic sealed class OrbGraph[m
             out relationship);[m
     }[m
 [m
[31m-    public IEnumerable<OrbRelationship> GetOutgoingRelationships([m
[31m-        Guid entityId)[m
[32m+[m[32m    /*[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     * OUTGOING RELATIONSHIPS[m
[32m+[m[32m     * =============================================================[m
[32m+[m[32m     *[m
[32m+[m[32m     * IMPORTANT:[m
[32m+[m[32m     *[m
[32m+[m[32m     * Do not keep ref/ref-readonly locals alive across yield return.[m
[32m+[m[32m     * C# 