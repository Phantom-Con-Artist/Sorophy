using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.StressTests.Infrastructure;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests.Campaigns;

[TestCampaign(
    "Memory Benchmark",
    "memory",
    order: 500)]
public static class MemoryBenchmarkTests
{
    private const int QuickSmallSize = 1_000;
    private const int QuickLargeSize = 10_000;

    private const int FullMediumSize = 10_000;
    private const int FullLargeSize = 100_000;

    private const int PropertiesPerEntityPercent = 10;

    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var overall =
            Stopwatch.StartNew();

        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                    SOROPHY MEMORY                       ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine(
            $"║ Seed:       {context.Seed,-45}║");
        Console.WriteLine(
            $"║ Operations: {context.Operations,-45}║");
        Console.WriteLine(
            $"║ Profile:    {context.Profile,-45}║");
        Console.WriteLine(
            "║ Purpose:    Establish foundational memory baseline         ║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var sizes =
                GetGraphSizes(
                    context.Profile);

            var measurements =
                new List<MemoryMeasurement>();

            ResourceMetrics.Stabilize();

            var processBaseline =
                ResourceMetrics.Capture();

            Console.WriteLine(
                "PROCESS BASELINE");

            PrintResources(
                processBaseline);

            Console.WriteLine();

            foreach (var size in sizes)
            {
                Console.WriteLine(
                    "────────────────────────────────────────────────────────────");
                Console.WriteLine(
                    $"MEMORY SCALE POINT: {size:N0} ENTITIES");
                Console.WriteLine(
                    "────────────────────────────────────────────────────────────");
                Console.WriteLine();

                var entityScenario =
                    MeasureEntities(
                        context.Seed,
                        size);

                measurements.Add(
                    entityScenario);

                var relationshipScenario =
                    MeasureRelationships(
                        context.Seed,
                        size);

                measurements.Add(
                    relationshipScenario);

                var propertyScenario =
                    MeasureSparseProperties(
                        context.Seed,
                        size);

                measurements.Add(
                    propertyScenario);

                var churnScenario =
                    MeasureRelationshipChurn(
                        context.Seed,
                        size);

                measurements.Add(
                    churnScenario);

                Console.WriteLine();
            }

            PrintSummary(
                measurements);

            overall.Stop();

            var failed =
                measurements
                    .Any(
                        x => x.Failed);

            var passedChecks =
                failed
                    ? 0
                    : 10;

            Console.WriteLine();
            Console.WriteLine(
                $"Total memory benchmark time: {overall.Elapsed}");

            if (failed)
            {
                return TestCampaignResult.Fail(
                    "Memory Benchmark",
                    "memory",
                    overall.Elapsed,
                    "One or more memory baseline assertions failed.",
                    null,
                    passedChecks: passedChecks,
                    totalChecks: 10);
            }

            return TestCampaignResult.Pass(
                "Memory Benchmark",
                "memory",
                passedChecks: 10,
                totalChecks: 10,
                overall.Elapsed,
                BuildSummary(
                    measurements));
        }
        catch (Exception exception)
        {
            overall.Stop();

            Console.WriteLine();
            Console.WriteLine(
                "  💥 MEMORY BENCHMARK FAILED");
            Console.WriteLine();
            Console.WriteLine(
                $"  {exception.Message}");
            Console.WriteLine();

            return TestCampaignResult.Fail(
                "Memory Benchmark",
                "memory",
                overall.Elapsed,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 10);
        }
    }

    private static MemoryMeasurement MeasureEntities(
        int seed,
        int entityCount)
    {
        ResourceMetrics.Stabilize();

        var before =
            ResourceMetrics.Capture();

        var graph =
            new SorophyGraph();

        var ids =
            CreateEntityIds(
                seed,
                entityCount);

        var watch =
            Stopwatch.StartNew();

        for (var index = 0;
             index < entityCount;
             index++)
        {
            graph.AddEntity(
                new SorophyEntity
                {
                    Id =
                        ids[index],

                    Name =
                        $"MemoryEntity-{index}",

                    Type =
                        "MemoryBenchmark"
                });
        }

        watch.Stop();

        ResourceMetrics.Stabilize();

        var after =
            ResourceMetrics.Capture();

        if (graph.Entities.Count !=
            entityCount)
        {
            throw new InvalidOperationException(
                $"Entity memory scenario count mismatch. " +
                $"Expected={entityCount}, " +
                $"Actual={graph.Entities.Count}.");
        }

        var managedDelta =
            after.ManagedHeapBytes -
            before.ManagedHeapBytes;

        var workingSetDelta =
            after.WorkingSetBytes -
            before.WorkingSetBytes;

        var bytesPerEntity =
            managedDelta /
            Math.Max(
                entityCount,
                1);

        Console.WriteLine(
            $"  Entities only:");
        Console.WriteLine(
            $"    Build time:          {watch.Elapsed.TotalMilliseconds,10:F2} ms");
        Console.WriteLine(
            $"    Managed heap delta:  {FormatBytes(managedDelta),10}");
        Console.WriteLine(
            $"    Working set delta:   {FormatBytes(workingSetDelta),10}");
        Console.WriteLine(
            $"    Managed bytes/entity:{bytesPerEntity,10:N1}");

        /*
         * Keep graph alive until after resource capture. The local
         * variable is intentionally still in scope here.
         */
        GC.KeepAlive(graph);

        return new MemoryMeasurement(
            Name:
                $"Entities only ({entityCount:N0})",

            EntityCount:
                entityCount,

            RelationshipCount:
                0,

            ManagedDeltaBytes:
                managedDelta,

            WorkingSetDeltaBytes:
                workingSetDelta,

            BuildTime:
                watch.Elapsed,

            ReferenceDeltaBytes:
                0,

            Failed:
                managedDelta < 0);
    }

    private static MemoryMeasurement MeasureRelationships(
        int seed,
        int entityCount)
    {
        ResourceMetrics.Stabilize();

        var graph =
            new SorophyGraph();

        var entityIds =
            CreateEntityIds(
                seed,
                entityCount);

        AddEntities(
            graph,
            entityIds);

        ResourceMetrics.Stabilize();

        var before =
            ResourceMetrics.Capture();

        var relationshipCount =
            Math.Max(
                entityCount - 1,
                0);

        var relationshipIds =
            CreateRelationshipIds(
                seed,
                relationshipCount);

        var watch =
            Stopwatch.StartNew();

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

        watch.Stop();

        ResourceMetrics.Stabilize();

        var after =
            ResourceMetrics.Capture();

        if (graph.Relationships.Count !=
            relationshipCount)
        {
            throw new InvalidOperationException(
                $"Relationship memory scenario count mismatch. " +
                $"Expected={relationshipCount}, " +
                $"Actual={graph.Relationships.Count}.");
        }

        var managedDelta =
            after.ManagedHeapBytes -
            before.ManagedHeapBytes;

        var workingSetDelta =
            after.WorkingSetBytes -
            before.WorkingSetBytes;

        var bytesPerRelationship =
            relationshipCount == 0
                ? 0
                : managedDelta /
                  (double)relationshipCount;

        Console.WriteLine(
            $"  Relationships + indexes:");
        Console.WriteLine(
            $"    Build time:             {watch.Elapsed.TotalMilliseconds,10:F2} ms");
        Console.WriteLine(
            $"    Managed heap delta:     {FormatBytes(managedDelta),10}");
        Console.WriteLine(
            $"    Working set delta:      {FormatBytes(workingSetDelta),10}");
        Console.WriteLine(
            $"    Managed bytes/relation:{bytesPerRelationship,11:N1}");

        GC.KeepAlive(graph);

        return new MemoryMeasurement(
            Name:
                $"Relationships + indexes ({entityCount:N0})",

            EntityCount:
                entityCount,

            RelationshipCount:
                relationshipCount,

            ManagedDeltaBytes:
                managedDelta,

            WorkingSetDeltaBytes:
                workingSetDelta,

            BuildTime:
                watch.Elapsed,

            ReferenceDeltaBytes:
                managedDelta,

            Failed:
                managedDelta < 0);
    }

    private static MemoryMeasurement MeasureSparseProperties(
        int seed,
        int entityCount)
    {
        ResourceMetrics.Stabilize();

        var graph =
            new SorophyGraph();

        var entityIds =
            CreateEntityIds(
                seed ^ 0x12345678,
                entityCount);

        AddEntities(
            graph,
            entityIds);

        ResourceMetrics.Stabilize();

        var before =
            ResourceMetrics.Capture();

        var propertyEntityCount =
            Math.Max(
                1,
                entityCount *
                PropertiesPerEntityPercent /
                100);

        var watch =
            Stopwatch.StartNew();

        for (var index = 0;
             index < propertyEntityCount;
             index++)
        {
            var entity =
                graph.Entities[
                    entityIds[index]];

            entity.Properties["memory_marker"] =
                new SorophyProperty
                {
                    Name =
                        "memory_marker",

                    Value =
                        new SorophyValue(
                            SorophyValueType.Integer,
                            (long)index)
                };
        }

        watch.Stop();

        ResourceMetrics.Stabilize();

        var after =
            ResourceMetrics.Capture();

        var managedDelta =
            after.ManagedHeapBytes -
            before.ManagedHeapBytes;

        var workingSetDelta =
            after.WorkingSetBytes -
            before.WorkingSetBytes;

        var bytesPerPropertyEntity =
            managedDelta /
            Math.Max(
                propertyEntityCount,
                1);

        Console.WriteLine(
            $"  Sparse properties:");
        Console.WriteLine(
            $"    Populated entities:       {propertyEntityCount:N0}");
        Console.WriteLine(
            $"    Build time:               {watch.Elapsed.TotalMilliseconds,10:F2} ms");
        Console.WriteLine(
            $"    Managed heap delta:       {FormatBytes(managedDelta),10}");
        Console.WriteLine(
            $"    Working set delta:        {FormatBytes(workingSetDelta),10}");
        Console.WriteLine(
            $"    Managed bytes/property:   {bytesPerPropertyEntity,10:N1}");

        GC.KeepAlive(graph);

        return new MemoryMeasurement(
            Name:
                $"Sparse properties ({entityCount:N0})",

            EntityCount:
                entityCount,

            RelationshipCount:
                0,

            ManagedDeltaBytes:
                managedDelta,

            WorkingSetDeltaBytes:
                workingSetDelta,

            BuildTime:
                watch.Elapsed,

            ReferenceDeltaBytes:
                managedDelta,

            Failed:
                managedDelta < 0);
    }

    private static MemoryMeasurement MeasureRelationshipChurn(
        int seed,
        int entityCount)
    {
        ResourceMetrics.Stabilize();

        var graph =
            new SorophyGraph();

        var entityIds =
            CreateEntityIds(
                seed ^ 0x55AA55AA,
                entityCount);

        AddEntities(
            graph,
            entityIds);

        var relationshipCount =
            Math.Max(
                entityCount - 1,
                0);

        var relationshipIds =
            CreateRelationshipIds(
                seed ^ unchecked((int)0xAA55AA55),
                relationshipCount);

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
                        "churn",

                    SourceId =
                        entityIds[index],

                    TargetId =
                        entityIds[index + 1]
                });
        }

        ResourceMetrics.Stabilize();

        var populated =
            ResourceMetrics.Capture();

        var watch =
            Stopwatch.StartNew();

        for (var index = 0;
             index < relationshipCount;
             index++)
        {
            graph.RemoveRelationship(
                relationshipIds[index]);
        }

        watch.Stop();

        ResourceMetrics.Stabilize();

        var afterRemoval =
            ResourceMetrics.Capture();

        if (graph.Relationships.Count != 0)
        {
            throw new InvalidOperationException(
                $"Relationship churn failed to remove all relationships. " +
                $"Remaining={graph.Relationships.Count}.");
        }

        var retainedManagedBytes =
            afterRemoval.ManagedHeapBytes;

        var activeGraphManagedBytes =
            populated.ManagedHeapBytes;

        var retainedRatio =
            activeGraphManagedBytes <= 0
                ? 0
                : retainedManagedBytes /
                  (double)activeGraphManagedBytes;

        Console.WriteLine(
            $"  Relationship churn / retention:");
        Console.WriteLine(
            $"    Removal time:             {watch.Elapsed.TotalMilliseconds,10:F2} ms");
        Console.WriteLine(
            $"    Managed heap before:      {FormatBytes(activeGraphManagedBytes),10}");
        Console.WriteLine(
            $"    Managed heap after:       {FormatBytes(retainedManagedBytes),10}");
        Console.WriteLine(
            $"    Retained heap ratio:      {retainedRatio,10:P2}");

        Console.WriteLine(
            $"    Working set after:        {FormatBytes(afterRemoval.WorkingSetBytes),10}");

        /*
         * This is informational rather than a failure condition.
         *
         * A future slab allocator may intentionally retain freed
         * capacity for reuse. High retained memory can therefore be
         * GOOD if the memory is reusable rather than fragmented garbage.
         */
        GC.KeepAlive(graph);

        return new MemoryMeasurement(
            Name:
                $"Relationship churn ({entityCount:N0})",

            EntityCount:
                entityCount,

            RelationshipCount:
                0,

            ManagedDeltaBytes:
                retainedManagedBytes,

            WorkingSetDeltaBytes:
                afterRemoval.WorkingSetBytes,

            BuildTime:
                watch.Elapsed,

            ReferenceDeltaBytes:
                activeGraphManagedBytes,

            Failed:
                false);
    }

    private static void AddEntities(
        SorophyGraph graph,
        Guid[] entityIds)
    {
        for (var index = 0;
             index < entityIds.Length;
             index++)
        {
            graph.AddEntity(
                new SorophyEntity
                {
                    Id =
                        entityIds[index],

                    Name =
                        $"MemoryEntity-{index}",

                    Type =
                        "MemoryBenchmark"
                });
        }
    }

    private static int[] GetGraphSizes(
        string profile)
    {
        return profile switch
        {
            "quick" =>
                new[]
                {
                    QuickSmallSize,
                    QuickLargeSize
                },

            "full" =>
                new[]
                {
                    QuickSmallSize,
                    FullMediumSize,
                    FullLargeSize
                },

            "extreme" =>
                new[]
                {
                    QuickSmallSize,
                    FullMediumSize,
                    FullLargeSize
                },

            _ =>
                new[]
                {
                    QuickSmallSize,
                    QuickLargeSize
                }
        };
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
        var result =
            new Guid[
                Math.Max(
                    count,
                    0)];

        for (var index = 0;
             index < result.Length;
             index++)
        {
            result[index] =
                DeterministicGuid(
                    seed,
                    index);
        }

        return result;
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

    private static string FormatBytes(
        long bytes)
    {
        var value =
            Math.Abs(
                (double)bytes);

        var suffix =
            "B";

        if (value >= 1024 * 1024)
        {
            value /=
                1024 * 1024;

            suffix =
                "MB";
        }
        else if (value >= 1024)
        {
            value /=
                1024;

            suffix =
                "KB";
        }

        return
            bytes < 0
                ? $"-{value:F2} {suffix}"
                : $"{value:F2} {suffix}";
    }

    private static void PrintResources(
        ResourceSnapshot snapshot)
    {
        Console.WriteLine(
            $"  Working set:      {FormatBytes(snapshot.WorkingSetBytes)}");

        Console.WriteLine(
            $"  Private memory:   {FormatBytes(snapshot.PrivateMemoryBytes)}");

        Console.WriteLine(
            $"  Managed heap:     {FormatBytes(snapshot.ManagedHeapBytes)}");

        Console.WriteLine(
            $"  Gen 0:            {snapshot.Gen0Collections:N0}");

        Console.WriteLine(
            $"  Gen 1:            {snapshot.Gen1Collections:N0}");

        Console.WriteLine(
            $"  Gen 2:            {snapshot.Gen2Collections:N0}");
    }

    private static void PrintSummary(
        IReadOnlyList<MemoryMeasurement> measurements)
    {
        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                    MEMORY SUMMARY");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        foreach (var measurement in measurements)
        {
            Console.WriteLine(
                $"{measurement.Name,-42} " +
                $"Managed={FormatBytes(measurement.ManagedDeltaBytes),10} " +
                $"Working={FormatBytes(measurement.WorkingSetDeltaBytes),10}");
        }

        Console.WriteLine();

        var entityMeasurements =
            measurements
                .Where(
                    x =>
                        x.Name.StartsWith(
                            "Entities only",
                            StringComparison.Ordinal))
                .ToArray();

        var relationshipMeasurements =
            measurements
                .Where(
                    x =>
                        x.Name.StartsWith(
                            "Relationships + indexes",
                            StringComparison.Ordinal))
                .ToArray();

        var propertyMeasurements =
            measurements
                .Where(
                    x =>
                        x.Name.StartsWith(
                            "Sparse properties",
                            StringComparison.Ordinal))
                .ToArray();

        if (entityMeasurements.Length > 0)
        {
            Console.WriteLine(
                "ENTITY FOOTPRINT");

            foreach (var measurement in entityMeasurements)
            {
                var bytesPerEntity =
                    measurement.ManagedDeltaBytes /
                    Math.Max(
                        measurement.EntityCount,
                        1);

                Console.WriteLine(
                    $"  {measurement.EntityCount,10:N0} entities → " +
                    $"{bytesPerEntity,10:N1} managed bytes/entity");
            }

            Console.WriteLine();
        }

        if (relationshipMeasurements.Length > 0)
        {
            Console.WriteLine(
                "RELATIONSHIP / INDEX FOOTPRINT");

            foreach (var measurement in relationshipMeasurements)
            {
                var bytesPerRelationship =
                    measurement.ReferenceDeltaBytes /
                    Math.Max(
                        measurement.RelationshipCount,
                        1);

                Console.WriteLine(
                    $"  {measurement.EntityCount,10:N0} entities → " +
                    $"{bytesPerRelationship,10:N1} managed bytes/relationship");
            }

            Console.WriteLine();
        }

        if (propertyMeasurements.Length > 0)
        {
            Console.WriteLine(
                "PROPERTY FOOTPRINT");

            foreach (var measurement in propertyMeasurements)
            {
                Console.WriteLine(
                    $"  {measurement.EntityCount,10:N0} entities → " +
                    $"{FormatBytes(measurement.ManagedDeltaBytes)} total");
            }

            Console.WriteLine();
        }

        Console.WriteLine(
            "Interpretation:");

        Console.WriteLine(
            "  • This campaign measures memory; it does not impose an arbitrary");
        Console.WriteLine(
            "    memory limit on Sorophy.Engine.");

        Console.WriteLine(
            "  • Managed-heap deltas are the primary comparison metric.");

        Console.WriteLine(
            "  • Working-set values are secondary because the OS/runtime may");
        Console.WriteLine(
            "    retain pages for reuse.");

        Console.WriteLine(
            "  • Retained memory after relationship deletion is informational.");
        Console.WriteLine(
            "    A pooled allocator may intentionally keep reusable capacity.");
    }

    private static string BuildSummary(
        IReadOnlyList<MemoryMeasurement> measurements)
    {
        var largest =
            measurements
                .OrderByDescending(
                    x => x.ManagedDeltaBytes)
                .FirstOrDefault();

        if (largest is null)
        {
            return
                "Memory baseline completed.";
        }

        return
            $"Largest measured managed-memory footprint: " +
            $"{largest.Name} at " +
            $"{FormatBytes(largest.ManagedDeltaBytes)}.";
    }

    private sealed record MemoryMeasurement(
        string Name,
        int EntityCount,
        int RelationshipCount,
        long ManagedDeltaBytes,
        long WorkingSetDeltaBytes,
        TimeSpan BuildTime,
        long ReferenceDeltaBytes,
        bool Failed);
}