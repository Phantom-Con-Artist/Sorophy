using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sorophy.Engine.Graph;

namespace Sorophy.Engine.StressTests;

public static class MutationPerformanceTests
{
    private const int MinimumOperations = 1_000;

    public static int Run(
        int seed = 12345,
        int operations = 10_000)
    {
        if (operations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "Operation count must be greater than zero.");
        }

        var count =
            Math.Max(
                operations,
                MinimumOperations);

        PrintHeader(
            seed,
            count);

        /*
         * =============================================================
         * PRE-GENERATE WORKLOADS
         * =============================================================
         *
         * No GUID generation or workload-object creation occurs inside
         * a timed mutation section.
         */
        var generator =
            new WorkloadGenerator(seed);

        var entityWorkload =
            generator.CreateEntities(count);

        var relationshipWorkload =
            generator.CreateRelationships(
                entityWorkload);

        var connectedWorkload =
            generator.CreateConnectedWorkload(count);

        var churnWorkload =
            generator.CreateChurnWorkload(count);

        var mixedEntities =
            generator.CreateEntities(count);

        var mixedRelationships =
            generator.CreateMixedRelationships(
                mixedEntities,
                generator.AnchorId);

        Console.WriteLine(
            "Workloads pre-generated before timing.");

        Console.WriteLine(
            "Graph construction is excluded from prepared mutation timings.");

        Console.WriteLine(
            "Reference-model execution and full validation are excluded.");

        Console.WriteLine();

        /*
         * =============================================================
         * JIT WARM-UP
         * =============================================================
         */
        WarmUp(
            generator,
            entityWorkload);

        var results =
            new List<BenchmarkResult>();

        /*
         * =============================================================
         * 1. ENTITY ADD
         * =============================================================
         */

        Console.WriteLine(
            "  ┌─ Entity Mutation");

        var entityAdd =
            Measure(
                "Entities / Add",
                count,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    return new PreparedBenchmark(
                        graph,
                        () =>
                        {
                            for (var index = 0;
                                 index < entityWorkload.Length;
                                 index++)
                            {
                                graph.AddEntity(
                                    entityWorkload[index]);
                            }
                        },
                        entityWorkload.Length,
                        0);
                });

        results.Add(
            entityAdd);

        PrintMeasurement(
            entityAdd);

        /*
         * =============================================================
         * 2. ENTITY REMOVE
         * =============================================================
         */

        var entityRemove =
            MeasurePrepared(
                "Entities / Remove",
                count,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    AddEntities(
                        graph,
                        entityWorkload);

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < entityWorkload.Length;
                         index++)
                    {
                        if (!graph.RemoveEntity(
                                entityWorkload[index].Id))
                        {
                            throw new InvalidOperationException(
                                "Entity removal failed.");
                        }
                    }
                },
                0,
                0);

        results.Add(
            entityRemove);

        PrintMeasurement(
            entityRemove);

        Console.WriteLine(
            "  └─ Entity mutation completed");

        Console.WriteLine();

        /*
         * =============================================================
         * 3. RELATIONSHIP ADD
         * =============================================================
         */

        Console.WriteLine(
            "  ┌─ Relationship Mutation");

        var relationshipAdd =
            MeasurePrepared(
                "Relationships / Add",
                count,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    AddEntities(
                        graph,
                        entityWorkload);

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < relationshipWorkload.Length;
                         index++)
                    {
                        graph.AddRelationship(
                            relationshipWorkload[index]);
                    }
                },
                entityWorkload.Length,
                relationshipWorkload.Length);

        results.Add(
            relationshipAdd);

        PrintMeasurement(
            relationshipAdd);

        /*
         * =============================================================
         * 4. RELATIONSHIP REMOVE
         * =============================================================
         *
         * IMPORTANT:
         *
         * The graph is completely constructed before the timer starts.
         * Therefore the measured region is only RemoveRelationship().
         */

        var relationshipRemove =
            MeasurePrepared(
                "Relationships / Remove",
                count,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    AddEntities(
                        graph,
                        entityWorkload);

                    AddRelationships(
                        graph,
                        relationshipWorkload);

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < relationshipWorkload.Length;
                         index++)
                    {
                        if (!graph.RemoveRelationship(
                                relationshipWorkload[index].Id))
                        {
                            throw new InvalidOperationException(
                                "Relationship removal failed.");
                        }
                    }
                },
                entityWorkload.Length,
                0);

        results.Add(
            relationshipRemove);

        PrintMeasurement(
            relationshipRemove);

        /*
         * =============================================================
         * 5. ENTITY REMOVE + CONNECTED EDGE
         * =============================================================
         *
         * Each source entity owns exactly one outgoing relationship.
         *
         * RemoveEntity() therefore exercises:
         *
         *     entity lookup
         *     adjacency lookup
         *     relationship removal
         *     adjacency unlinking
         *     slab release
         *     entity removal
         */

        var connectedRemove =
            MeasurePrepared(
                "Entities / Remove + Edge",
                connectedWorkload.Count,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    for (var index = 0;
                         index < connectedWorkload.Count;
                         index++)
                    {
                        var workload =
                            connectedWorkload[index];

                        graph.AddEntity(
                            workload.Source);

                        graph.AddEntity(
                            workload.Target);

                        graph.AddRelationship(
                            workload.Relationship);
                    }

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < connectedWorkload.Count;
                         index++)
                    {
                        if (!graph.RemoveEntity(
                                connectedWorkload[index].Source.Id))
                        {
                            throw new InvalidOperationException(
                                "Connected entity removal failed.");
                        }
                    }
                },
                connectedWorkload.Count,
                0);

        results.Add(
            connectedRemove);

        PrintMeasurement(
            connectedRemove);

        Console.WriteLine(
            "  └─ Relationship mutation completed");

        Console.WriteLine();

        /*
         * =============================================================
         * 6. RELATIONSHIP CHURN / SLAB REUSE
         * =============================================================
         *
         * The graph repeatedly performs:
         *
         *     AddRelationship()
         *     RemoveRelationship()
         *
         * Existing slab capacity should be reused.
         */

        Console.WriteLine(
            "  ┌─ Relationship Churn / Slab Reuse");

        var churnOperations =
            checked(
                churnWorkload.Length * 2);

        var churnResult =
            MeasurePrepared(
                "Relationship / Add+Remove Churn",
                churnOperations,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    var source =
                        new SorophyEntity
                        {
                            Id =
                                generator.AnchorId,

                            Name =
                                "ChurnSource",

                            Type =
                                "Benchmark"
                        };

                    var target =
                        new SorophyEntity
                        {
                            Id =
                                generator.TargetId,

                            Name =
                                "ChurnTarget",

                            Type =
                                "Benchmark"
                        };

                    graph.AddEntity(
                        source);

                    graph.AddEntity(
                        target);

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < churnWorkload.Length;
                         index++)
                    {
                        var relationship =
                            churnWorkload[index];

                        graph.AddRelationship(
                            relationship);

                        if (!graph.RemoveRelationship(
                                relationship.Id))
                        {
                            throw new InvalidOperationException(
                                "Relationship churn removal failed.");
                        }
                    }
                },
                2,
                0);

        results.Add(
            churnResult);

        PrintMeasurement(
            churnResult);

        Console.WriteLine(
            "  └─ Churn benchmark completed");

        Console.WriteLine();

        /*
         * =============================================================
         * 7. MIXED MUTATION
         * =============================================================
         *
         * Each cycle performs:
         *
         *     AddEntity
         *     AddRelationship
         *     RemoveRelationship
         *     RemoveEntity
         */

        Console.WriteLine(
            "  ┌─ Mixed Mutation");

        var mixedOperations =
            checked(
                mixedEntities.Length * 4);

        var mixedResult =
            MeasurePrepared(
                "Mixed / Entity+Relationship Churn",
                mixedOperations,
                () =>
                {
                    var graph =
                        new SorophyGraph();

                    var anchor =
                        new SorophyEntity
                        {
                            Id =
                                generator.AnchorId,

                            Name =
                                "MixedAnchor",

                            Type =
                                "Benchmark"
                        };

                    graph.AddEntity(
                        anchor);

                    return graph;
                },
                graph =>
                {
                    for (var index = 0;
                         index < mixedEntities.Length;
                         index++)
                    {
                        var entity =
                            mixedEntities[index];

                        var relationship =
                            mixedRelationships[index];

                        graph.AddEntity(
                            entity);

                        graph.AddRelationship(
                            relationship);

                        if (!graph.RemoveRelationship(
                                relationship.Id))
                        {
                            throw new InvalidOperationException(
                                "Mixed relationship removal failed.");
                        }

                        if (!graph.RemoveEntity(
                                entity.Id))
                        {
                            throw new InvalidOperationException(
                                "Mixed entity removal failed.");
                        }
                    }
                },
                1,
                0);

        results.Add(
            mixedResult);

        PrintMeasurement(
            mixedResult);

        Console.WriteLine(
            "  └─ Mixed mutation completed");

        /*
         * =============================================================
         * SUMMARY
         * =============================================================
         */

        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine(
            "                 MUTATION PERFORMANCE SUMMARY");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine();

        foreach (var result in results)
        {
            Console.WriteLine(
                $"{result.Name,-42}" +
                $"{result.OperationsPerSecond,14:N0} ops/s" +
                $"  {result.MicrosecondsPerOperation,9:F2} µs/op");
        }

        Console.WriteLine();

        Console.WriteLine(
            "ALLOCATION SUMMARY");

        foreach (var result in results)
        {
            Console.WriteLine(
                $"{result.Name,-42}" +
                $"{result.AllocatedBytesPerOperation,10:N1} B/op");
        }

        Console.WriteLine();

        Console.WriteLine(
            $"Total benchmark time: {CalculateTotalDuration(results)}");

        Console.WriteLine();

        Console.WriteLine(
            "Interpretation:");

        Console.WriteLine(
            "  • Remove benchmarks prepare the graph before timing.");

        Console.WriteLine(
            "  • Relationship add benchmarks prepare all entities first.");

        Console.WriteLine(
            "  • Workloads and GUIDs are generated before timing.");

        Console.WriteLine(
            "  • Reference-model work and graph validation are excluded.");

        Console.WriteLine(
            "  • Allocation figures measure the timed mutation region.");

        Console.WriteLine(
            "  • Churn specifically exercises adjacency slab reuse.");

        Console.WriteLine();

        return 0;
    }

    /*
     * =============================================================
     * PREPARED BENCHMARK
     * =============================================================
     */

    private static BenchmarkResult MeasurePrepared(
        string name,
        int operationCount,
        Func<SorophyGraph> prepare,
        Action<SorophyGraph> mutation,
        int expectedEntities,
        int expectedRelationships)
    {
        /*
         * Warm-up.
         *
         * Completely excluded from the reported timing.
         */
        var warmGraph =
            prepare();

        mutation(
            warmGraph);

        ValidateFinalState(
            warmGraph,
            expectedEntities,
            expectedRelationships);

        /*
         * Real measured graph.
         *
         * Preparation happens entirely outside the timer.
         */
        var graph =
            prepare();

        ForceCollection();

        var allocatedBefore =
            GC.GetAllocatedBytesForCurrentThread();

        var started =
            Stopwatch.GetTimestamp();

        mutation(
            graph);

        var elapsedTicks =
            Stopwatch.GetTimestamp() -
            started;

        var allocatedAfter =
            GC.GetAllocatedBytesForCurrentThread();

        ValidateFinalState(
            graph,
            expectedEntities,
            expectedRelationships);

        return CreateResult(
            name,
            operationCount,
            elapsedTicks,
            allocatedAfter -
                allocatedBefore);
    }

    /*
     * =============================================================
     * EMPTY-GRAPH / DIRECT BENCHMARK
     * =============================================================
     */

    private static BenchmarkResult Measure(
        string name,
        int operationCount,
        Func<PreparedBenchmark> prepare)
    {
        /*
         * Warm-up.
         */
        var warmup =
            prepare();

        warmup.Mutation();

        ValidateFinalState(
            warmup.Graph,
            warmup.ExpectedEntities,
            warmup.ExpectedRelationships);

        /*
         * Real measurement.
         */
        var scenario =
            prepare();

        ForceCollection();

        var allocatedBefore =
            GC.GetAllocatedBytesForCurrentThread();

        var started =
            Stopwatch.GetTimestamp();

        scenario.Mutation();

        var elapsedTicks =
            Stopwatch.GetTimestamp() -
            started;

        var allocatedAfter =
            GC.GetAllocatedBytesForCurrentThread();

        ValidateFinalState(
            scenario.Graph,
            scenario.ExpectedEntities,
            scenario.ExpectedRelationships);

        return CreateResult(
            name,
            operationCount,
            elapsedTicks,
            allocatedAfter -
                allocatedBefore);
    }

    /*
     * =============================================================
     * FINAL STATE VALIDATION
     * =============================================================
     */

    private static void ValidateFinalState(
        SorophyGraph graph,
        int expectedEntities,
        int expectedRelationships)
    {
        if (graph.Entities.Count !=
            expectedEntities)
        {
            throw new InvalidOperationException(
                $"Expected {expectedEntities} entities, " +
                $"but found {graph.Entities.Count}.");
        }

        if (graph.Relationships.Count !=
            expectedRelationships)
        {
            throw new InvalidOperationException(
                $"Expected {expectedRelationships} relationships, " +
                $"but found {graph.Relationships.Count}.");
        }
    }

    /*
     * =============================================================
     * RESULT CREATION
     * =============================================================
     */

    private static BenchmarkResult CreateResult(
        string name,
        int operationCount,
        long elapsedTicks,
        long allocatedBytes)
    {
        var seconds =
            Math.Max(
                (double)elapsedTicks /
                Stopwatch.Frequency,
                double.Epsilon);

        var operationsPerSecond =
            operationCount /
            seconds;

        var microsecondsPerOperation =
            seconds *
            1_000_000.0 /
            operationCount;

        var allocated =
            Math.Max(
                0L,
                allocatedBytes);

        var allocatedPerOperation =
            (double)allocated /
            operationCount;

        return new BenchmarkResult(
            name,
            operationCount,
            TimeSpan.FromSeconds(seconds),
            operationsPerSecond,
            microsecondsPerOperation,
            allocated,
            allocatedPerOperation);
    }

    /*
     * =============================================================
     * JIT WARM-UP
     * =============================================================
     */

    private static void WarmUp(
        WorkloadGenerator generator,
        IReadOnlyList<SorophyEntity> entities)
    {
        var graph =
            new SorophyGraph();

        var first =
            entities.Count > 0
                ? entities[0]
                : new SorophyEntity
                {
                    Id =
                        generator.CreateGuid(),

                    Name =
                        "WarmupSource",

                    Type =
                        "Benchmark"
                };

        var second =
            entities.Count > 1
                ? entities[1]
                : new SorophyEntity
                {
                    Id =
                        generator.CreateGuid(),

                    Name =
                        "WarmupTarget",

                    Type =
                        "Benchmark"
                };

        graph.AddEntity(
            first);

        graph.AddEntity(
            second);

        var relationship =
            new SorophyRelationship
            {
                Id =
                    generator.CreateGuid(),

                Type =
                    "warmup",

                SourceId =
                    first.Id,

                TargetId =
                    second.Id
            };

        graph.AddRelationship(
            relationship);

        _ =
            graph.ContainsEntity(
                first.Id);

        _ =
            graph.ContainsRelationship(
                relationship.Id);

        _ =
            graph.GetOutgoingRelationships(
                first.Id)
            .GetEnumerator()
            .MoveNext();

        _ =
            graph.RemoveRelationship(
                relationship.Id);

        _ =
            graph.RemoveEntity(
                first.Id);

        GC.KeepAlive(
            graph);
    }

    /*
     * =============================================================
     * GRAPH SETUP
     * =============================================================
     */

    private static void AddEntities(
        SorophyGraph graph,
        IReadOnlyList<SorophyEntity> entities)
    {
        for (var index = 0;
             index < entities.Count;
             index++)
        {
            graph.AddEntity(
                entities[index]);
        }
    }

    private static void AddRelationships(
        SorophyGraph graph,
        IReadOnlyList<SorophyRelationship> relationships)
    {
        for (var index = 0;
             index < relationships.Count;
             index++)
        {
            graph.AddRelationship(
                relationships[index]);
        }
    }

    /*
     * =============================================================
     * GC CONTROL
     * =============================================================
     */

    private static void ForceCollection()
    {
        GC.Collect(
            GC.MaxGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: true);

        GC.WaitForPendingFinalizers();

        GC.Collect(
            GC.MaxGeneration,
            GCCollectionMode.Forced,
            blocking: true,
            compacting: true);
    }

    /*
     * =============================================================
     * OUTPUT
     * =============================================================
     */

    private static void PrintHeader(
        int seed,
        int operations)
    {
        Console.WriteLine();

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                SOROPHY ENGINE MUTATION PERFORMANCE             ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");

        Console.WriteLine(
            $"║ Seed:           {seed,-43}║");

        Console.WriteLine(
            $"║ Operations:     {operations,-43}║");

        Console.WriteLine(
            "║ Mode:           ISOLATED MUTATION BENCHMARK               ║");

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");

        Console.WriteLine();
    }

    private static void PrintMeasurement(
        BenchmarkResult result)
    {
        Console.WriteLine();

        Console.WriteLine(
            $"    {result.Name,-42}" +
            $"{result.OperationsPerSecond,14:N0} ops/s" +
            $"  {result.MicrosecondsPerOperation,9:F2} µs/op");

        Console.WriteLine(
            $"    {"Allocation",-42}" +
            $"{result.AllocatedBytesPerOperation,10:N1} B/op");
    }

    private static TimeSpan CalculateTotalDuration(
        IReadOnlyList<BenchmarkResult> results)
    {
        var ticks =
            0L;

        for (var index = 0;
             index < results.Count;
             index++)
        {
            ticks +=
                results[index]
                    .Elapsed
                    .Ticks;
        }

        return TimeSpan.FromTicks(
            ticks);
    }

    /*
     * =============================================================
     * RESULT TYPES
     * =============================================================
     */

    private sealed record BenchmarkResult(
        string Name,
        int OperationCount,
        TimeSpan Elapsed,
        double OperationsPerSecond,
        double MicrosecondsPerOperation,
        long AllocatedBytes,
        double AllocatedBytesPerOperation);

    private sealed record PreparedBenchmark(
        SorophyGraph Graph,
        Action Mutation,
        int ExpectedEntities,
        int ExpectedRelationships);

    private sealed record ConnectedEntityWorkload(
        SorophyEntity Source,
        SorophyEntity Target,
        SorophyRelationship Relationship);

    /*
     * =============================================================
     * WORKLOAD GENERATOR
     * =============================================================
     */

    private sealed class WorkloadGenerator
    {
        private readonly Random _random;

        public WorkloadGenerator(
            int seed)
        {
            _random =
                new Random(seed);
        }

        public Guid AnchorId { get; } =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000001");

        public Guid TargetId { get; } =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000002");

        public Guid CreateGuid()
        {
            Span<byte> bytes =
                stackalloc byte[16];

            _random.NextBytes(
                bytes);

            return new Guid(
                bytes);
        }

        public SorophyEntity[] CreateEntities(
            int count)
        {
            var entities =
                new SorophyEntity[count];

            for (var index = 0;
                 index < count;
                 index++)
            {
                entities[index] =
                    new SorophyEntity
                    {
                        Id =
                            CreateGuid(),

                        Name =
                            $"MutationEntity-{index:N0}",

                        Type =
                            "MutationBenchmark"
                    };
            }

            return entities;
        }

        public SorophyRelationship[] CreateRelationships(
            IReadOnlyList<SorophyEntity> entities)
        {
            var relationships =
                new SorophyRelationship[
                    entities.Count];

            if (entities.Count == 0)
            {
                return relationships;
            }

            for (var index = 0;
                 index < entities.Count;
                 index++)
            {
                var source =
                    entities[index];

                var target =
                    entities[
                        (index + 1) %
                        entities.Count];

                relationships[index] =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "mutation",

                        SourceId =
                            source.Id,

                        TargetId =
                            target.Id
                    };
            }

            return relationships;
        }

        public List<ConnectedEntityWorkload>
            CreateConnectedWorkload(
                int count)
        {
            var workload =
                new List<ConnectedEntityWorkload>(
                    count);

            for (var index = 0;
                 index < count;
                 index++)
            {
                var source =
                    new SorophyEntity
                    {
                        Id =
                            CreateGuid(),

                        Name =
                            $"ConnectedSource-{index:N0}",

                        Type =
                            "MutationBenchmark"
                    };

                var target =
                    new SorophyEntity
                    {
                        Id =
                            CreateGuid(),

                        Name =
                            $"ConnectedTarget-{index:N0}",

                        Type =
                            "MutationBenchmark"
                    };

                var relationship =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "connected",

                        SourceId =
                            source.Id,

                        TargetId =
                            target.Id
                    };

                workload.Add(
                    new ConnectedEntityWorkload(
                        source,
                        target,
                        relationship));
            }

            return workload;
        }

        public SorophyRelationship[] CreateChurnWorkload(
            int count)
        {
            var relationships =
                new SorophyRelationship[count];

            for (var index = 0;
                 index < count;
                 index++)
            {
                relationships[index] =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "churn",

                        SourceId =
                            AnchorId,

                        TargetId =
                            TargetId
                    };
            }

            return relationships;
        }

        public SorophyRelationship[] CreateMixedRelationships(
            IReadOnlyList<SorophyEntity> entities,
            Guid anchorId)
        {
            var relationships =
                new SorophyRelationship[
                    entities.Count];

            for (var index = 0;
                 index < entities.Count;
                 index++)
            {
                relationships[index] =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "mixed",

                        SourceId =
                            anchorId,

                        TargetId =
                            entities[index].Id
                    };
            }

            return relationships;
        }
    }
}