using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests.Campaigns;

[TestCampaign(
    "Mutation Chaos",
    "mutation",
    order: 100)]
public static class MutationChaosTests
{
    private static readonly string[] RelationshipTypes =
    {
        "related_to",
        "owns",
        "knows",
        "depends_on",
        "contains",
        "follows",
        "references"
    };

    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            Stopwatch.StartNew();

        var random =
            new DeterministicRandom(
                context.Seed);

        var graph =
            new SorophyGraph();

        var reference =
            new ReferenceGraph();

        try
        {
            for (var operation = 1;
                 operation <= context.Operations;
                 operation++)
            {
                ExecuteRandomOperation(
                    graph,
                    reference,
                    random);

                if (operation % context.AuditInterval == 0 ||
                    operation == context.Operations)
                {
                    Audit(
                        graph,
                        reference,
                        operation);
                }

                if (operation % 10_000 == 0)
                {
                    var rate =
                        operation /
                        Math.Max(
                            started.Elapsed.TotalSeconds,
                            0.001);

                    Console.WriteLine(
                        $"    [{operation,10:N0}] " +
                        $"Entities={graph.Entities.Count,8:N0} " +
                        $"Relationships={graph.Relationships.Count,8:N0} " +
                        $"Rate={rate,10:N0} ops/s");
                }
            }

            started.Stop();

            return TestCampaignResult.Pass(
                "Mutation Chaos",
                "mutation",
                passedChecks: 4,
                totalChecks: 4,
                started.Elapsed,
                "Entity lifecycle, relationship integrity, mutation invariants, and graph queries passed.");
        }
        catch (Exception exception)
        {
            started.Stop();

            return TestCampaignResult.Fail(
                "Mutation Chaos",
                "mutation",
                started.Elapsed,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 4);
        }
    }

    private static void ExecuteRandomOperation(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        var choice =
            random.Next(100);

        if (choice < 25)
        {
            AddEntity(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 40)
        {
            RemoveEntity(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 58)
        {
            AddRelationship(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 68)
        {
            RemoveRelationship(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 76)
        {
            SelfReference(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 83)
        {
            QueryRelationships(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 89)
        {
            Reachability(
                graph,
                reference,
                random);

            return;
        }

        if (choice < 94)
        {
            Traversal(
                graph,
                reference,
                random);

            return;
        }

        DuplicateOrInvalidOperation(
            graph,
            reference,
            random);
    }

    private static void AddEntity(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        var id =
            NextUniqueGuid(
                random,
                reference);

        var entity =
            new SorophyEntity
            {
                Id = id,
                Name = $"Entity-{random.Next(1_000_000)}",
                Type = $"StressType-{random.Next(16)}"
            };

        entity.Properties["value"] =
            new SorophyProperty
            {
                Name = "value",
                Value =
                    new SorophyValue(
                        SorophyValueType.Integer,
                        random.NextLong(
                            -1_000_000,
                            1_000_001))
            };

        entity.Properties["active"] =
            new SorophyProperty
            {
                Name = "active",
                Value =
                    new SorophyValue(
                        SorophyValueType.Boolean,
                        random.Next(2) == 0)
            };

        graph.AddEntity(entity);

        if (!reference.AddEntity(id))
        {
            throw new InvalidOperationException(
                "Reference model rejected a valid entity.");
        }
    }

    private static void RemoveEntity(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        var id =
            reference.EntityCount == 0
                ? random.NextGuid()
                : reference.SelectEntity(random);

        var expected =
            reference.RemoveEntity(id);

        var actual =
            graph.RemoveEntity(id);

        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Entity removal mismatch for '{id:D}'. " +
                $"Expected={expected}, Actual={actual}.");
        }
    }

    private static void AddRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount < 2)
        {
            AddEntity(
                graph,
                reference,
                random);

            return;
        }

        var source =
            reference.SelectEntity(random);

        var target =
            reference.SelectEntity(random);

        var id =
            NextUniqueRelationshipGuid(
                random,
                reference);

        var type =
            RelationshipTypes[
                random.Next(
                    RelationshipTypes.Length)];

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = id,
                Type = type,
                SourceId = source,
                TargetId = target
            });

        if (!reference.AddRelationship(
                id,
                source,
                target,
                type))
        {
            throw new InvalidOperationException(
                "Reference model rejected a valid relationship.");
        }
    }

    private static void RemoveRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.RelationshipCount == 0)
        {
            return;
        }

        var id =
            reference.SelectRelationship(random);

        var expected =
            reference.RemoveRelationship(id);

        var actual =
            graph.RemoveRelationship(id);

        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Relationship removal mismatch for '{id:D}'. " +
                $"Expected={expected}, Actual={actual}.");
        }
    }

    private static void SelfReference(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount == 0)
        {
            AddEntity(
                graph,
                reference,
                random);

            return;
        }

        var entityId =
            reference.SelectEntity(random);

        var relationshipId =
            NextUniqueRelationshipGuid(
                random,
                reference);

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                Type = "self_reference",
                SourceId = entityId,
                TargetId = entityId
            });

        if (!reference.AddRelationship(
                relationshipId,
                entityId,
                entityId,
                "self_reference"))
        {
            throw new InvalidOperationException(
                "Reference model rejected a valid self-reference.");
        }
    }

    private static void QueryRelationships(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount == 0)
        {
            return;
        }

        var entityId =
            reference.SelectEntity(random);

        var actualOutgoing =
            graph.GetOutgoingRelationships(entityId)
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToArray();

        var expectedOutgoing =
            reference.GetOutgoing(entityId)
                .OrderBy(x => x)
                .ToArray();

        if (!actualOutgoing.SequenceEqual(
                expectedOutgoing))
        {
            throw new InvalidOperationException(
                "Outgoing relationship query mismatch.");
        }

        var actualIncoming =
            graph.GetIncomingRelationships(entityId)
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToArray();

        var expectedIncoming =
            reference.GetIncoming(entityId)
                .OrderBy(x => x)
                .ToArray();

        if (!actualIncoming.SequenceEqual(
                expectedIncoming))
        {
            throw new InvalidOperationException(
                "Incoming relationship query mismatch.");
        }
    }

    private static void Reachability(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount == 0)
        {
            return;
        }

        var source =
            reference.SelectEntity(random);

        var target =
            reference.SelectEntity(random);

        var expected =
            reference.IsReachable(
                source,
                target);

        var actual =
            graph.IsReachable(
                source,
                target);

        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Reachability mismatch. " +
                $"Source={source:D}; Target={target:D}; " +
                $"Expected={expected}; Actual={actual}.");
        }
    }

    private static void Traversal(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount == 0)
        {
            return;
        }

        var source =
            reference.SelectEntity(random);

        var expected =
            reference.Traverse(source)
                .OrderBy(x => x)
                .ToArray();

        var actual =
            graph.Traverse(source)
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToArray();

        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                $"Traversal mismatch for '{source:D}'.");
        }
    }

    private static void DuplicateOrInvalidOperation(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom random)
    {
        if (reference.EntityCount == 0)
        {
            return;
        }

        if (random.Next(2) == 0)
        {
            var id =
                reference.SelectEntity(random);

            var rejected =
                false;

            try
            {
                graph.AddEntity(
                    new SorophyEntity
                    {
                        Id = id,
                        Name = "DUPLICATE"
                    });
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            if (!rejected)
            {
                throw new InvalidOperationException(
                    $"SorophyGraph accepted duplicate entity '{id:D}'.");
            }

            return;
        }

        var source =
            reference.SelectEntity(random);

        var invalidTarget =
            NextUniqueGuid(
                random,
                reference);

        var relationshipId =
            NextUniqueRelationshipGuid(
                random,
                reference);

        var invalid =
            new SorophyRelationship
            {
                Id = relationshipId,
                Type = "invalid",
                SourceId = source,
                TargetId = invalidTarget
            };

        var threw =
            false;

        try
        {
            graph.AddRelationship(invalid);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        if (!threw)
        {
            throw new InvalidOperationException(
                "SorophyGraph accepted a relationship with a missing endpoint.");
        }
    }

    private static void Audit(
        SorophyGraph graph,
        ReferenceGraph reference,
        int operation)
    {
        var errors =
            graph.Validate();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Graph validation failed at operation {operation}. " +
                string.Join(
                    " | ",
                    errors.Take(20)));
        }

        if (graph.Entities.Count !=
            reference.EntityCount)
        {
            throw new InvalidOperationException(
                $"Entity count mismatch at operation {operation}. " +
                $"Graph={graph.Entities.Count}; " +
                $"Reference={reference.EntityCount}.");
        }

        if (graph.Relationships.Count !=
            reference.RelationshipCount)
        {
            throw new InvalidOperationException(
                $"Relationship count mismatch at operation {operation}. " +
                $"Graph={graph.Relationships.Count}; " +
                $"Reference={reference.RelationshipCount}.");
        }

        foreach (var id in reference.EntityIds)
        {
            if (!graph.ContainsEntity(id))
            {
                throw new InvalidOperationException(
                    $"Graph is missing entity '{id:D}'.");
            }
        }

        foreach (var id in reference.RelationshipIds)
        {
            if (!graph.ContainsRelationship(id))
            {
                throw new InvalidOperationException(
                    $"Graph is missing relationship '{id:D}'.");
            }
        }
    }

    private static Guid NextUniqueGuid(
        DeterministicRandom random,
        ReferenceGraph reference)
    {
        Guid id;

        do
        {
            id = random.NextGuid();
        }
        while (reference.ContainsEntity(id));

        return id;
    }

    private static Guid NextUniqueRelationshipGuid(
        DeterministicRandom random,
        ReferenceGraph reference)
    {
        Guid id;

        do
        {
            id = random.NextGuid();
        }
        while (reference.ContainsRelationship(id));

        return id;
    }

    private sealed class ReferenceGraph
    {
        private readonly HashSet<Guid> _entities = new();
        private readonly List<Guid> _entityOrder = new();
        private readonly Dictionary<Guid, int> _entityIndex = new();

        private readonly Dictionary<Guid, ReferenceRelationship> _relationships = new();
        private readonly List<Guid> _relationshipOrder = new();
        private readonly Dictionary<Guid, int> _relationshipIndex = new();

        public int EntityCount => _entities.Count;

        public int RelationshipCount => _relationships.Count;

        public IEnumerable<Guid> EntityIds => _entities;

        public IEnumerable<Guid> RelationshipIds => _relationships.Keys;

        public bool ContainsEntity(Guid id) =>
            _entities.Contains(id);

        public bool ContainsRelationship(Guid id) =>
            _relationships.ContainsKey(id);

        public bool AddEntity(Guid id)
        {
            if (!_entities.Add(id))
            {
                return false;
            }

            _entityIndex[id] =
                _entityOrder.Count;

            _entityOrder.Add(id);

            return true;
        }

        public bool RemoveEntity(Guid id)
        {
            if (!_entities.Remove(id))
            {
                return false;
            }

            RemoveEntityFromOrder(id);

            var toRemove =
                _relationships.Values
                    .Where(
                        relationship =>
                            relationship.SourceId == id ||
                            relationship.TargetId == id)
                    .Select(
                        relationship => relationship.Id)
                    .ToArray();

            foreach (var relationshipId in toRemove)
            {
                RemoveRelationship(relationshipId);
            }

            return true;
        }

        public bool AddRelationship(
            Guid id,
            Guid sourceId,
            Guid targetId,
            string type)
        {
            if (!ContainsEntity(sourceId) ||
                !ContainsEntity(targetId) ||
                ContainsRelationship(id))
            {
                return false;
            }

            _relationships[id] =
                new ReferenceRelationship(
                    id,
                    sourceId,
                    targetId,
                    type);

            _relationshipIndex[id] =
                _relationshipOrder.Count;

            _relationshipOrder.Add(id);

            return true;
        }

        public bool RemoveRelationship(Guid id)
        {
            if (!_relationships.Remove(id))
            {
                return false;
            }

            RemoveRelationshipFromOrder(id);

            return true;
        }

        public Guid SelectEntity(
            DeterministicRandom random)
        {
            return _entityOrder[
                random.Next(
                    _entityOrder.Count)];
        }

        public Guid SelectRelationship(
            DeterministicRandom random)
        {
            return _relationshipOrder[
                random.Next(
                    _relationshipOrder.Count)];
        }

        public IEnumerable<Guid> GetOutgoing(
            Guid entityId)
        {
            return _relationships.Values
                .Where(
                    x => x.SourceId == entityId)
                .Select(
                    x => x.Id);
        }

        public IEnumerable<Guid> GetIncoming(
            Guid entityId)
        {
            return _relationships.Values
                .Where(
                    x => x.TargetId == entityId)
                .Select(
                    x => x.Id);
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

            queue.Enqueue(sourceId);
            visited.Add(sourceId);

            while (queue.Count > 0)
            {
                var current =
                    queue.Dequeue();

                foreach (var relationship in _relationships.Values)
                {
                    if (relationship.SourceId != current)
                    {
                        continue;
                    }

                    var next =
                        relationship.TargetId;

                    if (next == targetId)
                    {
                        return true;
                    }

                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }

        public IEnumerable<Guid> Traverse(
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

            queue.Enqueue(sourceId);

            while (queue.Count > 0)
            {
                var current =
                    queue.Dequeue();

                foreach (var relationship in _relationships.Values)
                {
                    if (relationship.SourceId != current)
                    {
                        continue;
                    }

                    var target =
                        relationship.TargetId;

                    if (!visited.Add(target))
                    {
                        continue;
                    }

                    yield return target;
                    queue.Enqueue(target);
                }
            }
        }

        private void RemoveEntityFromOrder(
            Guid id)
        {
            var index =
                _entityIndex[id];

            var lastIndex =
                _entityOrder.Count - 1;

            var lastId =
                _entityOrder[lastIndex];

            if (index != lastIndex)
            {
                _entityOrder[index] =
                    lastId;

                _entityIndex[lastId] =
                    index;
            }

            _entityOrder.RemoveAt(lastIndex);
            _entityIndex.Remove(id);
        }

        private void RemoveRelationshipFromOrder(
            Guid id)
        {
            var index =
                _relationshipIndex[id];

            var lastIndex =
                _relationshipOrder.Count - 1;

            var lastId =
                _relationshipOrder[lastIndex];

            if (index != lastIndex)
            {
                _relationshipOrder[index] =
                    lastId;

                _relationshipIndex[lastId] =
                    index;
            }

            _relationshipOrder.RemoveAt(lastIndex);
            _relationshipIndex.Remove(id);
        }
    }

    private sealed record ReferenceRelationship(
        Guid Id,
        Guid SourceId,
        Guid TargetId,
        string Type);

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(
            int seed)
        {
            _state =
                unchecked(
                    (ulong)(uint)seed +
                    0x9E3779B97F4A7C15UL);

            if (_state == 0)
            {
                _state =
                    0x9E3779B97F4A7C15UL;
            }
        }

        private ulong NextUInt64()
        {
            _state +=
                0x9E3779B97F4A7C15UL;

            var z =
                _state;

            z =
                (z ^ (z >> 30)) *
                0xBF58476D1CE4E5B9UL;

            z =
                (z ^ (z >> 27)) *
                0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }

        public int Next(
            int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax));
            }

            return (int)(
                NextUInt64() %
                (ulong)exclusiveMax);
        }

        public long NextLong(
            long minimumInclusive,
            long maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExclusive));
            }

            var range =
                unchecked(
                    (ulong)(
                        maximumExclusive -
                        minimumInclusive));

            return unchecked(
                (long)(
                    NextUInt64() %
                    range)) +
                minimumInclusive;
        }

        public Guid NextGuid()
        {
            Span<byte> bytes =
                stackalloc byte[16];

            for (var i = 0;
                 i < bytes.Length;
                 i += 8)
            {
                var value =
                    NextUInt64();

                for (var j = 0;
                     j < 8;
                     j++)
                {
                    bytes[i + j] =
                        (byte)(
                            value >>
                            (j * 8));
                }
            }

            return new Guid(bytes);
        }
    }
}
