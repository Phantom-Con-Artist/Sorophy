using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests.Campaigns;

[TestCampaign(
    "Performance Benchmark",
    "performance",
    order: 300)]
public static class PerformanceBenchmarkTests
{
    private const int MinimumGraphSize = 1_000;
    private const int MediumGraphSize = 10_000;
    private const int MaximumGraphSize = 100_000;

    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var overall =
            Stopwatch.StartNew();

        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                 SOROPHY PERFORMANCE                     ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine(
            $"║ Seed:       {context.Seed,-45}║");
        Console.WriteLine(
            $"║ Operations: {context.Operations,-45}║");
        Console.WriteLine(
            $"║ Profile:    {context.Profile,-45}║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var graphSizes =
                BuildGraphSizes(
                    context.Operations);

            var allMeasurements =
                new List<PerformanceMeasurement>();

            var peakResource =
                ResourceMetrics.Capture();

            foreach (var graphSize in graphSizes)
            {
                Console.WriteLine(
                    $"  ┌─ Graph Size: {graphSize:N0} entities");

                Console.WriteLine();

                ResourceMetrics.Stabilize();

                var result =
                    BenchmarkGraphSize(
                        context.Seed,
                        graphSize);

                allMeasurements.AddRange(
                    result.Measurements);

                peakResource =
                    MergePeakResources(
                        peakResource,
                        result.Resources);

                Console.WriteLine();
                Console.WriteLine(
                    $"  └─ Completed {graphSize:N0}-entity benchmark");
                Console.WriteLine();
            }

            Console.WriteLine(
                "  ┌─ Traversal / Reachability Micro-Benchmark");

            var traversalResult =
                BenchmarkTraversal();

            allMeasurements.AddRange(
                traversalResult.Measurements);

            peakResource =
                MergePeakResources(
                    peakResource,
                    traversalResult.Resources);

            Console.WriteLine(
                "  └─ Traversal micro-benchmark completed");

            overall.Stop();

            PrintSummary(
                allMeasurements,
                peakResource,
                overall.Elapsed);

            return TestCampaignResult.Pass(
                "Performance Benchmark",
                "performance",
                passedChecks: 10,
                totalChecks: 10,
                overall.Elapsed,
                BuildSummaryMessage(
                    allMeasurements,
                    peakResource));
        }
        catch (Exception exception)
        {
            overall.Stop();

            Console.WriteLine();
            Console.WriteLine(
                "  💥 PERFORMANCE BENCHMARK FAILED");
            Console.WriteLine();
            Console.WriteLine(
                $"  {exception.Message}");
            Console.WriteLine();

            return TestCampaignResult.Fail(
                "Performance Benchmark",
                "performance",
                overall.Elapsed,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 10);
        }
    }

    private static BenchmarkResult BenchmarkGraphSize(
        int seed,
        int graphSize)
    {
        var graph =
            new SorophyGraph();

        var entityIds =
            CreateEntityIds(
                seed,
                graphSize);

        /*
         * A linear chain with N entities has N-1 relationships.
         */
        var relationshipCount =
            graphSize - 1;

        var relationshipIds =
            CreateRelationshipIds(
                seed,
                relationshipCount);

        var measurements =
            new List<PerformanceMeasurement>();

        /*
         * ---------------------------------------------------------
         * ENTITY INSERTION
         * ---------------------------------------------------------
         */

        var entityInsertMeasurement =
            PerformanceMetrics.Measure(
                $"Entities / Add ({graphSize:N0})",
                graphSize,
                () =>
                {
                    var index =
                        graph.Entities.Count;

                    graph.AddEntity(
                        new SorophyEntity
                        {
                            Id =
                                entityIds[index],

                            Name =
                                $"PerfEntity-{index}",

                            Type =
                                "PerformanceBenchmark"
                        });

                    return 1;
                });

        measurements.Add(
            entityInsertMeasurement);

        PrintMeasurement(
            entityInsertMeasurement);

        /*
         * ---------------------------------------------------------
         * ENTITY LOOKUP
         * ---------------------------------------------------------
         */

        var lookupIterations =
            Math.Min(
                graphSize,
                10_000);

        var entityLookupMeasurement =
            PerformanceMetrics.Measure(
                $"Entities / Contains ({graphSize:N0})",
                lookupIterations,
                () =>
                {
                    var index =
                        RandomizedIndex(
                            seed,
                            graphSize);

                    return graph.ContainsEntity(
                        entityIds[index])
                        ? 1
                        : 0;
                });

        measurements.Add(
            entityLookupMeasurement);

        PrintMeasurement(
            entityLookupMeasurement);

        /*
         * ---------------------------------------------------------
         * RELATIONSHIP INSERTION
         * ---------------------------------------------------------
         */

        var relationshipInsertMeasurement =
            PerformanceMetrics.Measure(
                $"Relationships / Add ({graphSize:N0})",
                relationshipCount,
                () =>
                {
                    var index =
                        graph.Relationships.Count;

                    graph.AddRelationship(
                        new SorophyRelationship
                        {
                            Id =
                                relationshipIds[index],

                            Type =
                                "chain",

                            SourceId =
                                entityIds[index],

                            TargetId =
                                entityIds[index + 1]
                        });

                    return 1;
                });

        measurements.Add(
            relationshipInsertMeasurement);

        PrintMeasurement(
            relationshipInsertMeasurement);

        /*
         * ---------------------------------------------------------
         * RELATIONSHIP LOOKUP
         * ---------------------------------------------------------
         */

        var relationshipLookupMeasurement =
            PerformanceMetrics.Measure(
                $"Relationships / Contains ({graphSize:N0})",
                Math.Min(
                    relationshipCount,
                    10_000),
                () =>
                {
                    var index =
                        RandomizedIndex(
                            seed + 1,
                            relationshipCount);

                    return graph.ContainsRelationship(
                        relationshipIds[index])
                        ? 1
                        : 0;
                });

        measurements.Add(
            relationshipLookupMeasurement);

        PrintMeasurement(
            relationshipLookupMeasurement);

        /*
         * ---------------------------------------------------------
         * OUTGOING QUERY
         * ---------------------------------------------------------
         */

        var outgoingMeasurement =
            PerformanceMetrics.Measure(
                $"Relationships / Outgoing ({graphSize:N0})",
                Math.Min(
                    graphSize,
                    1_000),
                () =>
                {
                    var entityId =
                        entityIds[
                            RandomizedIndex(
                                seed + 2,
                                graphSize)];

                    return graph
                        .GetOutgoingRelationships(
                            entityId)
                        .Count();
                });

        measurements.Add(
            outgoingMeasurement);

        PrintMeasurement(
            outgoingMeasurement);

        /*
         * ---------------------------------------------------------
         * INCOMING QUERY
         * ---------------------------------------------------------
         */

        var incomingMeasurement =
            PerformanceMetrics.Measure(
                $"Relationships / Incoming ({graphSize:N0})",
                Math.Min(
                    graphSize,
                    1_000),
                () =>
                {
                    var entityId =
                        entityIds[
                            RandomizedIndex(
                                seed + 3,
                                graphSize)];

                    return graph
                        .GetIncomingRelationships(
                            entityId)
                        .Count();
                });

        measurements.Add(
            incomingMeasurement);

        PrintMeasurement(
            incomingMeasurement);

        /*
         * ---------------------------------------------------------
         * GRAPH VALIDATION
         * ---------------------------------------------------------
         */

        ResourceMetrics.Stabilize();

        var validationBefore =
            ResourceMetrics.Capture();

        var validationWatch =
            Stopwatch.StartNew();

        var validationErrors =
            graph.Validate();

        validationWatch.Stop();

        var validationAfter =
            ResourceMetrics.Capture();

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Graph validation failed for {graphSize:N0}-entity benchmark. " +
                string.Join(
                    " | ",
                    validationErrors.Take(20)));
        }

        var validationMeasurement =
            new PerformanceMeasurement(
                $"Graph / Validate ({graphSize:N0})",
                1,
                validationWatch.Elapsed,
                validationAfter.TotalAllocatedBytes -
                validationBefore.TotalAllocatedBytes,
                validationBefore,
                validationAfter);

        measurements.Add(
            validationMeasurement);

        PrintMeasurement(
            validationMeasurement);

        /*
         * ---------------------------------------------------------
         * RELATIONSHIP REMOVAL
         * ---------------------------------------------------------
         */

        var removalMeasurement =
            PerformanceMetrics.Measure(
                $"Relationships / Remove ({graphSize:N0})",
                relationshipCount,
                () =>
                {
                    var currentCount =
                        graph.Relationships.Count;

                    if (currentCount == 0)
                    {
                        return 0;
                    }

                    var index =
                        currentCount - 1;

                    return graph.RemoveRelationship(
                        relationshipIds[index])
                        ? 1
                        : 0;
                });

        measurements.Add(
            removalMeasurement);

        PrintMeasurement(
            removalMeasurement);

        /*
         * Rebuild the chain so the benchmark leaves the graph
         * in a complete state.
         */
        for (var index = 0;
             index < relationshipCount;
             index++)
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id =
                        relationshipIds[index],

                    Type =
                        "chain",

                    SourceId =
                        entityIds[index],

                    TargetId =
                        entityIds[index + 1]
                });
        }

        var resources =
            ResourceMetrics.Capture();

        return new BenchmarkResult(
            measurements,
            resources);
    }

    private static BenchmarkResult BenchmarkTraversal()
    {
        const int size = 2_000;
        const int traversalIterations = 5;
        const int reachabilityIterations = 10;

        var graph =
            new SorophyGraph();

        var ids =
            new Guid[size];

        for (var index = 0;
             index < size;
             index++)
        {
            ids[index] =
                DeterministicGuid(
                    0x7711,
                    index);

            graph.AddEntity(
                new SorophyEntity
                {
                    Id =
                        ids[index],

                    Name =
                        $"TraversalEntity-{index}",

                    Type =
                        "TraversalBenchmark"
                });
        }

        for (var index = 0;
             index < size - 1;
             index++)
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id =
                        DeterministicGuid(
                            0x8811,
                            index),

                    Type =
                        "chain",

                    SourceId =
                        ids[index],

                    TargetId =
                        ids[index + 1]
                });
        }

        var measurements =
            new List<PerformanceMeasurement>();

        var traversal =
            PerformanceMetrics.Measure(
                "Traversal / 2K Chain",
                traversalIterations,
                () =>
                    graph
                        .Traverse(ids[0])
                        .LongCount());

        measurements.Add(
            traversal);

        PrintMeasurement(
            traversal);

        var reachable =
            PerformanceMetrics.Measure(
                "Reachability / 2K Chain",
                reachabilityIterations,
                () =>
                    graph.IsReachable(
                        ids[0],
                        ids[size - 1])
                        ? 1
                        : 0);

        measurements.Add(
            reachable);

        PrintMeasurement(
            reachable);

        return new BenchmarkResult(
            measurements,
            ResourceMetrics.Capture());
    }

    private static int[] BuildGraphSizes(
        long requestedOperations)
    {
        var sizes =
            new List<int>();

        AddUnique(
            sizes,
            MinimumGraphSize);

        if (requestedOperations >=
            MediumGraphSize)
        {
            AddUnique(
                sizes,
                MediumGraphSize);
        }
        else
        {
            AddUnique(
                sizes,
                checked(
                    (int)requestedOperations));
        }

        if (requestedOperations >=
            MaximumGraphSize)
        {
            AddUnique(
                sizes,
                MaximumGraphSize);
        }

        return sizes
            .Where(
                x => x > 0)
            .ToArray();
    }

    private static void AddUnique(
        List<int> list,
        int value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }

    private static Guid[] CreateEntityIds(
        int seed,
        int count)
    {
        var result =
            new Guid[count];

        for (var index = 0;
             index < count;
             index++)
        {
            result[index] =
                DeterministicGuid(
                    seed,
                    index);
        }

        return result;
    }

    private static Guid[] CreateRelationshipIds(
        int seed,
        int count)
    {
        if (count <= 0)
        {
            return Array.Empty<Guid>();
        }

        var result =
            new Guid[count];

        for (var index = 0;
             index < result.Length;
             index++)
        {
            result[index] =
                DeterministicGuid(
                    seed ^
                    0x5A5A5A5A,
                    index);
        }

        return result;
    }

    private static int RandomizedIndex(
        int seed,
        int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        var value =
            unchecked(
                (uint)(
                    seed *
                    1_664_525 +
                    1_013_904_223));

        return (int)(
            value %
            (uint)count);
    }

    private static Guid DeterministicGuid(
        int seed,
        int index)
    {
        Span<byte> bytes =
            stackalloc byte[16];

        var state =
            unchecked(
                (ulong)(uint)seed ^
                ((ulong)(uint)index *
                 0x9E3779B97F4A7C15UL));

        for (var block = 0;
             block < 2;
             block++)
        {
            state +=
                0x9E3779B97F4A7C15UL;

            var z =
                state;

            z =
                (z ^ (z >> 30)) *
                0xBF58476D1CE4E5B9UL;

            z =
                (z ^ (z >> 27)) *
                0x94D049BB133111EBUL;

            z ^=
                z >> 31;

            for (var offset = 0;
                 offset < 8;
                 offset++)
            {
                bytes[
                    block * 8 +
                    offset] =
                    (byte)(
                        z >>
                        (offset * 8));
            }
        }

        return new Guid(bytes);
    }

    private static ResourceSnapshot MergePeakResources(
        ResourceSnapshot first,
        ResourceSnapshot second)
    {
        return new ResourceSnapshot(
            WorkingSetBytes:
                Math.Max(
                    first.WorkingSetBytes,
                    second.WorkingSetBytes),

            PrivateMemoryBytes:
                Math.Max(
                    first.PrivateMemoryBytes,
                    second.PrivateMemoryBytes),

            ManagedHeapBytes:
                Math.Max(
                    first.ManagedHeapBytes,
                    second.ManagedHeapBytes),

            TotalAllocatedBytes:
                Math.Max(
                    first.TotalAllocatedBytes,
                    second.TotalAllocatedBytes),

            Gen0Collections:
                Math.Max(
                    first.Gen0Collections,
                    second.Gen0Collections),

            Gen1Collections:
                Math.Max(
                    first.Gen1Collections,
                    second.Gen1Collections),

            Gen2Collections:
                Math.Max(
                    first.Gen2Collections,
                    second.Gen2Collections),

            PeakWorkingSetBytes:
                Math.Max(
                    first.PeakWorkingSetBytes,
                    second.PeakWorkingSetBytes));
    }

    private static void PrintMeasurement(
        PerformanceMeasurement measurement)
    {
        Console.WriteLine(
            $"    {measurement.Name,-43} " +
            $"{measurement.OperationsPerSecond,12:N0} ops/s  " +
            $"{measurement.AverageMicroseconds,10:N2} µs/op");
    }

    private static void PrintSummary(
        IReadOnlyList<PerformanceMeasurement> measurements,
        ResourceSnapshot resources,
        TimeSpan elapsed)
    {
        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                 PERFORMANCE SUMMARY");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        foreach (var measurement in measurements)
        {
            Console.WriteLine(
                $"{measurement.Name,-45} " +
                $"{measurement.OperationsPerSecond,12:N0} ops/s");
        }

        Console.WriteLine();

        Console.WriteLine(
            $"Working set:          {resources.WorkingSetMegabytes:F1} MB");

        Console.WriteLine(
            $"Private memory:       {resources.PrivateMemoryMegabytes:F1} MB");

        Console.WriteLine(
            $"Managed heap:         {resources.ManagedHeapMegabytes:F1} MB");

        Console.WriteLine(
            $"Peak working set:     {resources.PeakWorkingSetMegabytes:F1} MB");

        Console.WriteLine(
            $"Gen 0 collections:    {resources.Gen0Collections:N0}");

        Console.WriteLine(
            $"Gen 1 collections:    {resources.Gen1Collections:N0}");

        Console.WriteLine(
            $"Gen 2 collections:    {resources.Gen2Collections:N0}");

        Console.WriteLine();

        Console.WriteLine(
            $"Total benchmark time: {elapsed}");
    }

    private static string BuildSummaryMessage(
        IReadOnlyList<PerformanceMeasurement> measurements,
        ResourceSnapshot resources)
    {
        var fastest =
            measurements
                .OrderByDescending(
                    x => x.OperationsPerSecond)
                .First();

        var slowest =
            measurements
                .OrderBy(
                    x => x.OperationsPerSecond)
                .First();

        return
            $"Fastest: {fastest.Name} at " +
            $"{fastest.OperationsPerSecond:N0} ops/s. " +
            $"Slowest: {slowest.Name} at " +
            $"{slowest.OperationsPerSecond:N0} ops/s. " +
            $"Peak working set: " +
            $"{resources.PeakWorkingSetMegabytes:F1} MB.";
    }

    private sealed record BenchmarkResult(
        IReadOnlyList<PerformanceMeasurement> Measurements,
        ResourceSnapshot Resources);
}