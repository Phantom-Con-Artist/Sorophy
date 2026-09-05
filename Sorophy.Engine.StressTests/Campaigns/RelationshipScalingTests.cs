using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests.Campaigns;

[TestCampaign(
    "Relationship Scaling",
    "relationship-scaling",
    order: 400)]
public static class RelationshipScalingTests
{
    private const int QuickLargeSize = 10_000;
    private const int FullLargeSize = 100_000;
    private const int ExtremeLargeSize = 500_000;

    private const int QuerySamples = 1_000;

    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var overall =
            Stopwatch.StartNew();

        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                 RELATIONSHIP SCALING                       ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine(
            $"║ Seed:       {context.Seed,-45}║");
        Console.WriteLine(
            $"║ Profile:    {context.Profile,-45}║");
        Console.WriteLine(
            $"║ Operations: {context.Operations,-45}║");
        Console.WriteLine(
            "║ Topology:   Sparse linear chain                            ║");
        Console.WriteLine(
            "║ Purpose:    Measure relationship-query scaling             ║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var sizes =
                GetGraphSizes(
                    context);

            var allMeasurements =
                new List<PerformanceMeasurement>();

            ResourceSnapshot peakResources =
                ResourceMetrics.Capture();

            var scaleResults =
                new List<ScalePoint>();

            foreach (var size in sizes)
            {
                Console.WriteLine(
                    $"  ┌─ Graph Size: {size:N0} entities / " +
                    $"{Math.Max(size - 1, 0):N0} relationships");

                Console.WriteLine();

                ResourceMetrics.Stabilize();

                var result =
                    BenchmarkGraph(
                        context.Seed,
                        size);

                allMeasurements.AddRange(
                    result.Measurements);

                peakResources =
                    MergePeakResources(
                        peakResources,
                        result.Resources);

                scaleResults.Add(
                    result.Scale);

                Console.WriteLine();
                Console.WriteLine(
                    $"  └─ Completed {size:N0}-node scaling point");

                Console.WriteLine();
            }

            PrintScalingAnalysis(
                scaleResults);

            PrintResourceSummary(
                peakResources);

            overall.Stop();

            Console.WriteLine(
                $"Total relationship-scaling time: {overall.Elapsed}");

            Console.WriteLine();

            return TestCampaignResult.Pass(
                "Relationship Scaling",
                "relationship-scaling",
                passedChecks: 8,
                totalChecks: 8,
                overall.Elapsed,
                BuildSummaryMessage(
                    scaleResults,
                    peakResources));
        }
        catch (Exception exception)
        {
            overall.Stop();

            Console.WriteLine();
            Console.WriteLine(
                "  💥 RELATIONSHIP SCALING FAILED");
            Console.WriteLine();
            Console.WriteLine(
                $"  {exception.Message}");
            Console.WriteLine();

            return TestCampaignResult.Fail(
                "Relationship Scaling",
                "relationship-scaling",
                overall.Elapsed,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 8);
        }
    }

    private static BenchmarkResult BenchmarkGraph(
        int seed,
        int size)
    {
        if (size < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Graph size must be at least 2.");
        }

        var graph =
            new SorophyGraph();

        var entityIds =
            CreateEntityIds(
                seed,
                size);

        var relationshipIds =
            CreateRelationshipIds(
                seed,
                size - 1);

        /*
         * ---------------------------------------------------------
         * BUILD THE GRAPH
         * ---------------------------------------------------------
         *
         * Sparse chain:
         *
         * E0 → E1 → E2 → E3 → ... → En
         *
         * This intentionally keeps node degree tiny.
         */
        Console.WriteLine(
            $"    Building sparse chain...");

        var buildWatch =
            Stopwatch.StartNew();

        for (var index = 0;
             index < size;
             index++)
        {
            graph.AddEntity(
                new SorophyEntity
                {
                    Id =
                        entityIds[index],

                    Name =
                        $"ScalingEntity-{index}",

                    Type =
                        "RelationshipScaling"
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
                        relationshipIds[index],

                    Type =
                        "chain",

                    SourceId =
                        entityIds[index],

                    TargetId =
                        entityIds[index + 1]
                });
        }

        buildWatch.Stop();

        Console.WriteLine(
            $"    Built in {buildWatch.Elapsed.TotalMilliseconds:F2} ms");

        /*
         * ---------------------------------------------------------
         * BASIC INTEGRITY
         * ---------------------------------------------------------
         */
        if (graph.Entities.Count != size)
        {
            throw new InvalidOperationException(
                $"Entity count mismatch. " +
                $"Expected={size}, Actual={graph.Entities.Count}.");
        }

        if (graph.Relationships.Count != size - 1)
        {
            throw new InvalidOperationException(
                $"Relationship count mismatch. " +
                $"Expected={size - 1}, " +
                $"Actual={graph.Relationships.Count}.");
        }

        /*
         * ---------------------------------------------------------
         * ENTITY LOOKUP
         * ---------------------------------------------------------
         *
         * Baseline control.
         *
         * If ContainsEntity stays flat while relationship queries
         * deteriorate, the relationship query implementation is
         * the more obvious suspect.
         */
        var entityLookup =
            PerformanceMetrics.Measure(
                $"Entity / Contains ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 11,
                            size);

                    return graph.ContainsEntity(
                        entityIds[index])
                        ? 1
                        : 0;
                });

        PrintMeasurement(
            entityLookup);

        /*
         * ---------------------------------------------------------
         * RELATIONSHIP LOOKUP
         * ---------------------------------------------------------
         */
        var relationshipLookup =
            PerformanceMetrics.Measure(
                $"Relationship / Contains ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 17,
                            size - 1);

                    return graph.ContainsRelationship(
                        relationshipIds[index])
                        ? 1
                        : 0;
                });

        PrintMeasurement(
            relationshipLookup);

        /*
         * ---------------------------------------------------------
         * OUTGOING QUERY
         * ---------------------------------------------------------
         *
         * Each sample queries a deterministic entity.
         *
         * In the chain each entity has degree ≤ 1, so an indexed
         * implementation should remain relatively stable as total
         * E grows.
         */
        var outgoing =
            PerformanceMetrics.Measure(
                $"Outgoing / Query ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 23,
                            size - 1);

                    return graph
                        .GetOutgoingRelationships(
                            entityIds[index])
                        .Count();
                });

        PrintMeasurement(
            outgoing);

        /*
         * ---------------------------------------------------------
         * INCOMING QUERY
         * ---------------------------------------------------------
         */
        var incoming =
            PerformanceMetrics.Measure(
                $"Incoming / Query ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 29,
                            size - 1) + 1;

                    return graph
                        .GetIncomingRelationships(
                            entityIds[index])
                        .Count();
                });

        PrintMeasurement(
            incoming);

        /*
         * ---------------------------------------------------------
         * GENERAL RELATIONSHIP QUERY
         * ---------------------------------------------------------
         */
        var relationships =
            PerformanceMetrics.Measure(
                $"All Relationships / Query ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 31,
                            size);

                    return graph
                        .GetRelationships(
                            entityIds[index])
                        .Count();
                });

        PrintMeasurement(
            relationships);

        /*
         * ---------------------------------------------------------
         * NEIGHBOR QUERY
         * ---------------------------------------------------------
         */
        var neighbors =
            PerformanceMetrics.Measure(
                $"Neighbors / Query ({size:N0})",
                QuerySamples,
                () =>
                {
                    var index =
                        SampleIndex(
                            seed + 37,
                            size);

                    return graph
                        .GetNeighbors(
                            entityIds[index])
                        .Count();
                });

        PrintMeasurement(
            neighbors);

        /*
         * ---------------------------------------------------------
         * VALIDATION
         * ---------------------------------------------------------
         */
        ResourceMetrics.Stabilize();

        var validationBefore =
            ResourceMetrics.Capture();

        var validationWatch =
            Stopwatch.StartNew();

        var errors =
            graph.Validate();

        validationWatch.Stop();

        var validationAfter =
            ResourceMetrics.Capture();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Graph validation failed for " +
                $"{size:N0} entities / {size - 1:N0} relationships." +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    errors.Take(20)));
        }

        var validationMeasurement =
            new PerformanceMeasurement(
                $"Graph / Validate ({size:N0})",
                1,
                validationWatch.Elapsed,
                validationAfter.TotalAllocatedBytes -
                validationBefore.TotalAllocatedBytes,
                validationBefore,
                validationAfter);

        PrintMeasurement(
            validationMeasurement);

        /*
         * ---------------------------------------------------------
         * RESOURCE SNAPSHOT
         * ---------------------------------------------------------
         */
        var resources =
            ResourceMetrics.Capture();

        return new BenchmarkResult(
            Measurements:
                new[]
                {
                    entityLookup,
                    relationshipLookup,
                    outgoing,
                    incoming,
                    relationships,
                    neighbors,
                    validationMeasurement
                },

            Resources:
                resources,

            Scale:
                new ScalePoint(
                    size,
                    outgoing.OperationsPerSecond,
                    incoming.OperationsPerSecond,
                    relationships.OperationsPerSecond,
                    neighbors.OperationsPerSecond));
    }

    private static void PrintScalingAnalysis(
        IReadOnlyList<ScalePoint> scaleResults)
    {
        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                    SCALING ANALYSIS");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine(
            "Graph Size | Outgoing ops/s | Incoming ops/s | " +
            "All-Rel ops/s | Neighbors ops/s");

        Console.WriteLine(
            "-----------+----------------+----------------+---------------+----------------");

        foreach (var point in scaleResults)
        {
            Console.WriteLine(
                $"{point.EntityCount,10:N0} | " +
                $"{point.OutgoingOpsPerSecond,14:N0} | " +
                $"{point.IncomingOpsPerSecond,14:N0} | " +
                $"{point.AllRelationshipsOpsPerSecond,13:N0} | " +
                $"{point.NeighborsOpsPerSecond,14:N0}");
        }

        if (scaleResults.Count >= 2)
        {
            var first =
                scaleResults[0];

            var last =
                scaleResults[^1];

            Console.WriteLine();

            Console.WriteLine(
                $"Outgoing scaling: " +
                $"{ComputeGrowthFactor(first.OutgoingOpsPerSecond, last.OutgoingOpsPerSecond):F2}x");

            Console.WriteLine(
                $"Incoming scaling: " +
                $"{ComputeGrowthFactor(first.IncomingOpsPerSecond, last.IncomingOpsPerSecond):F2}x");

            Console.WriteLine(
                $"All-rel scaling:   " +
                $"{ComputeGrowthFactor(first.AllRelationshipsOpsPerSecond, last.AllRelationshipsOpsPerSecond):F2}x");

            Console.WriteLine(
                $"Neighbor scaling:  " +
                $"{ComputeGrowthFactor(first.NeighborsOpsPerSecond, last.NeighborsOpsPerSecond):F2}x");

            Console.WriteLine();

            Console.WriteLine(
                "Interpretation:");

            Console.WriteLine(
                "  • This campaign does not declare a performance failure.");
            
            Console.WriteLine(
                "  • It establishes empirical scaling behavior.");
            
            Console.WriteLine(
                "  • Large throughput degradation with tiny node degree is " +
                "evidence for total-edge-dependent query work.");
        }
    }

    private static void PrintResourceSummary(
        ResourceSnapshot resources)
    {
        Console.WriteLine();
        Console.WriteLine(
            "RESOURCE SNAPSHOT");

        Console.WriteLine(
            $"  Working set:      {resources.WorkingSetMegabytes:F1} MB");

        Console.WriteLine(
            $"  Private memory:   {resources.PrivateMemoryMegabytes:F1} MB");

        Console.WriteLine(
            $"  Managed heap:     {resources.ManagedHeapMegabytes:F1} MB");

        Console.WriteLine(
            $"  Peak working set: {resources.PeakWorkingSetMegabytes:F1} MB");

        Console.WriteLine(
            $"  Gen 0:             {resources.Gen0Collections:N0}");

        Console.WriteLine(
            $"  Gen 1:             {resources.Gen1Collections:N0}");

        Console.WriteLine(
            $"  Gen 2:             {resources.Gen2Collections:N0}");
    }

    private static string BuildSummaryMessage(
        IReadOnlyList<ScalePoint> scaleResults,
        ResourceSnapshot resources)
    {
        if (scaleResults.Count == 0)
        {
            return
                "Relationship scaling campaign completed.";
        }

        var first =
            scaleResults[0];

        var last =
            scaleResults[^1];

        return
            $"Measured {scaleResults.Count} graph sizes. " +
            $"Outgoing query throughput changed " +
            $"{ComputeGrowthFactor(first.OutgoingOpsPerSecond, last.OutgoingOpsPerSecond):F2}x " +
            $"from {first.EntityCount:N0} to {last.EntityCount:N0} entities. " +
            $"Peak working set: " +
            $"{resources.PeakWorkingSetMegabytes:F1} MB.";
    }

    private static double ComputeGrowthFactor(
        double first,
        double last)
    {
        if (first <= 0 ||
            last <= 0)
        {
            return 0;
        }

        return last / first;
    }

    private static int[] GetGraphSizes(
        TestCampaignContext context)
    {
        return context.Profile switch
        {
            "quick" =>
                new[]
                {
                    1_000,
                    QuickLargeSize
                },

            "full" =>
                new[]
                {
                    1_000,
                    10_000,
                    50_000,
                    FullLargeSize
                },

            "extreme" =>
                new[]
                {
                    1_000,
                    10_000,
                    50_000,
                    FullLargeSize,
                    ExtremeLargeSize
                },

            _ =>
                new[]
                {
                    1_000,
                    QuickLargeSize
                }
        };
    }

    private static Guid[] CreateEntityIds(
        int seed,
        int count)
    {
        var ids =
            new Guid[count];

        for (var index = 0;
             index < count;
             index++)
        {
            ids[index] =
                DeterministicGuid(
                    seed,
                    index);
        }

        return ids;
    }

    private static Guid[] CreateRelationshipIds(
        int seed,
        int count)
    {
        var ids =
            new Guid[Math.Max(count, 0)];

        for (var index = 0;
             index < ids.Length;
             index++)
        {
            ids[index] =
                DeterministicGuid(
                    seed ^ 0x5A5A5A5A,
                    index);
        }

        return ids;
    }

    private static int SampleIndex(
        int seed,
        int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        unchecked
        {
            var value =
                (uint)(
                    seed *
                    1_664_525 +
                    1_013_904_223);

            return (int)(
                value %
                (uint)count);
        }
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
            $"    {measurement.Name,-45} " +
            $"{measurement.OperationsPerSecond,12:N0} ops/s  " +
            $"{measurement.AverageMicroseconds,10:N2} µs/op");
    }

    private sealed record BenchmarkResult(
        IReadOnlyList<PerformanceMeasurement> Measurements,
        ResourceSnapshot Resources,
        ScalePoint Scale);

    private sealed record ScalePoint(
        int EntityCount,
        double OutgoingOpsPerSecond,
        double IncomingOpsPerSecond,
        double AllRelationshipsOpsPerSecond,
        double NeighborsOpsPerSecond);
}