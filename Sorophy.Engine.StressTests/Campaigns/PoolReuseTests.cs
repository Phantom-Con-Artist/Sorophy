using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sorophy.Engine.Graph;

namespace Sorophy.Engine.StressTests;

public static class PoolReuseTests
{
    private const int MinimumScale = 1_000;

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

        var scalePoints =
            BuildScalePoints(
                operations);

        PrintHeader(
            seed,
            operations,
            scalePoints);

        var generator =
            new WorkloadGenerator(
                seed);

        var results =
            new List<ScaleResult>();

        var allPassed =
            true;

        foreach (var scale in scalePoints)
        {
            Console.WriteLine(
                "────────────────────────────────────────────────────────────");

            Console.WriteLine(
                $"POOL REUSE SCALE POINT: {scale:N0} RELATIONSHIPS");

            Console.WriteLine(
                "────────────────────────────────────────────────────────────");

            /*
             * Each relationship identity has a single active lifetime.
             * Wave 2 therefore receives fresh relationship identities while
             * keeping the exact same topology and relationship count.
             *
             * The objects are created before timing so workload generation
             * and identity generation remain outside both measured Add waves.
             */
            var firstWaveRelationships =
                generator.CreateRelationships(
                    scale,
                    identityNamespace: 1);

            var secondWaveRelationships =
                generator.CreateRelationships(
                    scale,
                    identityNamespace: 2);

            var firstWave =
                MeasureWave(
                    generator,
                    firstWaveRelationships,
                    wave: 1);

            Console.WriteLine();

            Console.WriteLine(
                $"  Wave 1 / Add ({scale:N0})");
            Console.WriteLine(
                $"    Time:                 {firstWave.Elapsed.TotalMilliseconds,10:F2} ms");
            Console.WriteLine(
                $"    Throughput:           {firstWave.OperationsPerSecond,10:N0} ops/s");
            Console.WriteLine(
                $"    Allocated:            {firstWave.AllocatedBytes,10:N0} bytes");
            Console.WriteLine(
                $"    Allocation/op:        {firstWave.AllocatedBytesPerOperation,10:F2} B/op");

            /*
             * The measured graph from wave 1 is returned so we can remove
             * the relationships outside the add timing.
             */
            var graph =
                firstWave.Graph;

            var removalStarted =
                Stopwatch.GetTimestamp();

            RemoveAllRelationships(
                graph,
                firstWaveRelationships);

            var removalElapsedTicks =
                Stopwatch.GetTimestamp() -
                removalStarted;

            var removalElapsed =
                TimeSpan.FromSeconds(
                    (double)removalElapsedTicks /
                    Stopwatch.Frequency);

            var removalThroughput =
                scale /
                Math.Max(
                    removalElapsed.TotalSeconds,
                    double.Epsilon);

            var removalPassed =
                graph.Relationships.Count == 0;

            Console.WriteLine();

            Console.WriteLine(
                $"  Wave 1 / Remove ({scale:N0})");
            Console.WriteLine(
                $"    Time:                 {removalElapsed.TotalMilliseconds,10:F2} ms");
            Console.WriteLine(
                $"    Throughput:           {removalThroughput,10:N0} ops/s");
            Console.WriteLine(
                $"    Final relationships:  {graph.Relationships.Count,10:N0}");
            Console.WriteLine(
                $"    Result:               {(removalPassed ? "PASS" : "FAIL")}");

            /*
             * Make sure the graph is structurally sound after the first
             * complete lifecycle before exercising reuse.
             */
            var firstValidation =
                graph.Validate();

            var firstValidationPassed =
                firstValidation.Count == 0;

            Console.WriteLine(
                $"    Validation:           {(firstValidationPassed ? "PASS" : "FAIL")}");

            if (!firstValidationPassed)
            {
                PrintValidationErrors(
                    firstValidation);
            }

            /*
             * =========================================================
             * SECOND WAVE
             * =========================================================
             *
             * The same topology is added again with fresh relationship
             * identities.
             *
             * This is intentional: workload creation and GUID creation
             * are not part of the allocation measurement, while the
             * adjacency slab storage remains eligible for reuse.
             */
            var secondWave =
                MeasureExistingGraphWave(
                    graph,
                    secondWaveRelationships);

            Console.WriteLine();

            Console.WriteLine(
                $"  Wave 2 / Re-Add ({scale:N0})");
            Console.WriteLine(
                $"    Time:                 {secondWave.Elapsed.TotalMilliseconds,10:F2} ms");
            Console.WriteLine(
                $"    Throughput:           {secondWave.OperationsPerSecond,10:N0} ops/s");
            Console.WriteLine(
                $"    Allocated:            {secondWave.AllocatedBytes,10:N0} bytes");
            Console.WriteLine(
                $"    Allocation/op:        {secondWave.AllocatedBytesPerOperation,10:F2} B/op");

            var wave2CountPassed =
                graph.Relationships.Count ==
                scale;

            Console.WriteLine(
                $"    Relationship count:   {(wave2CountPassed ? "PASS" : "FAIL")}");

            /*
             * The important reuse indicator.
             *
             * We expect the second allocation wave to require no more
             * managed allocation than the first one.
             */
            var reuseObserved =
                secondWave.AllocatedBytes <=
                firstWave.AllocatedBytes;

            var reductionPercent =
                CalculateReductionPercent(
                    firstWave.AllocatedBytes,
                    secondWave.AllocatedBytes);

            Console.WriteLine(
                $"    Allocation reduction: {reductionPercent,9:F2}%");

            Console.WriteLine(
                $"    Slab reuse evidence:   {(reuseObserved ? "PASS" : "FAIL")}");

            /*
             * Remove wave 2 outside the re-add timing.
             */
            var secondRemovalStarted =
                Stopwatch.GetTimestamp();

            RemoveAllRelationships(
                graph,
                secondWaveRelationships);

            var secondRemovalElapsedTicks =
                Stopwatch.GetTimestamp() -
                secondRemovalStarted;

            var secondRemovalElapsed =
                TimeSpan.FromSeconds(
                    (double)secondRemovalElapsedTicks /
                    Stopwatch.Frequency);

            var secondRemovalPassed =
                graph.Relationships.Count == 0;

            Console.WriteLine();

            Console.WriteLine(
                $"  Wave 2 / Remove ({scale:N0})");
            Console.WriteLine(
                $"    Time:                 {secondRemovalElapsed.TotalMilliseconds,10:F2} ms");
            Console.WriteLine(
                $"    Final relationships:  {graph.Relationships.Count,10:N0}");
            Console.WriteLine(
                $"    Result:               {(secondRemovalPassed ? "PASS" : "FAIL")}");

            /*
             * Final structural audit.
             */
            var finalValidation =
                graph.Validate();

            var finalValidationPassed =
                finalValidation.Count == 0;

            Console.WriteLine(
                $"    Final validation:     {(finalValidationPassed ? "PASS" : "FAIL")}");

            if (!finalValidationPassed)
            {
                PrintValidationErrors(
                    finalValidation);
            }

            var scalePassed =
                removalPassed &&
                firstValidationPassed &&
                wave2CountPassed &&
                reuseObserved &&
                secondRemovalPassed &&
                finalValidationPassed;

            allPassed &=
                scalePassed;

            results.Add(
                new ScaleResult(
                    scale,
                    firstWave.AllocatedBytes,
                    secondWave.AllocatedBytes,
                    reductionPercent,
                    firstWave.OperationsPerSecond,
                    secondWave.OperationsPerSecond,
                    scalePassed));
        }

        PrintSummary(
            results);

        return allPassed
            ? 0
            : 1;
    }

    /*
     * =============================================================
     * SCALE POINTS
     * =============================================================
     */

    private static IReadOnlyList<int>
        BuildScalePoints(
            int operations)
    {
        var scales =
            new List<int>();

        AddScaleIfUseful(
            scales,
            MinimumScale,
            operations);

        AddScaleIfUseful(
            scales,
            10_000,
            operations);

        AddScaleIfUseful(
            scales,
            100_000,
            operations);

        /*
         * Always test the requested operation count if it is larger
         * than the standard points.
         */
        if (operations > 100_000 &&
            !scales.Contains(
                operations))
        {
            scales.Add(
                operations);
        }

        return scales;
    }

    private static void AddScaleIfUseful(
        List<int> scales,
        int scale,
        int operations)
    {
        if (operations >= scale &&
            !scales.Contains(scale))
        {
            scales.Add(
                scale);
        }
    }

    /*
     * =============================================================
     * WAVE 1
     * =============================================================
     *
     * The graph is empty except for its endpoint entities.
     *
     * Only AddRelationship() calls are timed.
     */

    private static WaveMeasurement
        MeasureWave(
            WorkloadGenerator generator,
            IReadOnlyList<SorophyRelationship> relationships,
            int wave)
    {
        var graph =
            new SorophyGraph();

        var source =
            new SorophyEntity
            {
                Id =
                    generator.AnchorId,

                Name =
                    $"PoolReuseSource-W{wave}",

                Type =
                    "PoolReuseBenchmark"
            };

        var target =
            new SorophyEntity
            {
                Id =
                    generator.TargetId,

                Name =
                    $"PoolReuseTarget-W{wave}",

                Type =
                    "PoolReuseBenchmark"
            };

        graph.AddEntity(
            source);

        graph.AddEntity(
            target);

        /*
         * JIT warm-up is deliberately outside the measurement.
         *
         * The warm-up relationship must not come from the measured
         * workload because RemoveRelationship() permanently retires
         * relationship identities.
         */
        if (relationships.Count > 0)
        {
            var warmRelationship =
                generator.CreateWarmupRelationship();

            graph.AddRelationship(
                warmRelationship);

            graph.RemoveRelationship(
                warmRelationship.Id);
        }

        ForceCollection();

        var allocatedBefore =
            GC.GetAllocatedBytesForCurrentThread();

        var started =
            Stopwatch.GetTimestamp();

        for (var index = 0;
             index < relationships.Count;
             index++)
        {
            graph.AddRelationship(
                relationships[index]);
        }

        var elapsedTicks =
            Stopwatch.GetTimestamp() -
            started;

        var allocatedAfter =
            GC.GetAllocatedBytesForCurrentThread();

        var allocatedBytes =
            Math.Max(
                0L,
                allocatedAfter -
                allocatedBefore);

        var elapsed =
            TimeSpan.FromSeconds(
                Math.Max(
                    (double)elapsedTicks /
                    Stopwatch.Frequency,
                    double.Epsilon));

        return new WaveMeasurement(
            graph,
            elapsed,
            relationships.Count /
                elapsed.TotalSeconds,
            allocatedBytes,
            (double)allocatedBytes /
                Math.Max(
                    relationships.Count,
                    1));
    }

    /*
     * =============================================================
     * WAVE 2
     * =============================================================
     *
     * The graph has already had all previous relationships removed.
     *
     * The timer therefore measures only the second AddRelationship()
     * wave.
     */

    private static WaveMeasurement
        MeasureExistingGraphWave(
            SorophyGraph graph,
            IReadOnlyList<SorophyRelationship> relationships)
    {
        ForceCollection();

        var allocatedBefore =
            GC.GetAllocatedBytesForCurrentThread();

        var started =
            Stopwatch.GetTimestamp();

        for (var index = 0;
             index < relationships.Count;
             index++)
        {
            graph.AddRelationship(
                relationships[index]);
        }

        var elapsedTicks =
            Stopwatch.GetTimestamp() -
            started;

        var allocatedAfter =
            GC.GetAllocatedBytesForCurrentThread();

        var allocatedBytes =
            Math.Max(
                0L,
                allocatedAfter -
                allocatedBefore);

        var elapsed =
            TimeSpan.FromSeconds(
                Math.Max(
                    (double)elapsedTicks /
                    Stopwatch.Frequency,
                    double.Epsilon));

        return new WaveMeasurement(
            graph,
            elapsed,
            relationships.Count /
                elapsed.TotalSeconds,
            allocatedBytes,
            (double)allocatedBytes /
                Math.Max(
                    relationships.Count,
                    1));
    }

    /*
     * =============================================================
     * REMOVAL
     * =============================================================
     */

    private static void RemoveAllRelationships(
        SorophyGraph graph,
        IReadOnlyList<SorophyRelationship> relationships)
    {
        for (var index = 0;
             index < relationships.Count;
             index++)
        {
            var relationship =
                relationships[index];

            if (!graph.RemoveRelationship(
                    relationship.Id))
            {
                throw new InvalidOperationException(
                    $"Pool reuse removal failed for relationship '{relationship.Id}'.");
            }
        }
    }

    /*
     * =============================================================
     * VALIDATION
     * =============================================================
     */

    private static void PrintValidationErrors(
        IReadOnlyList<string> errors)
    {
        var count =
            Math.Min(
                errors.Count,
                8);

        for (var index = 0;
             index < count;
             index++)
        {
            Console.WriteLine(
                $"      {errors[index]}");
        }

        if (errors.Count > count)
        {
            Console.WriteLine(
                $"      ... and {errors.Count - count:N0} more errors.");
        }
    }

    /*
     * =============================================================
     * RESULT SUMMARY
     * =============================================================
     */

    private static void PrintSummary(
        IReadOnlyList<ScaleResult> results)
    {
        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine(
            "                    POOL REUSE SUMMARY");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine();

        Console.WriteLine(
            "Scale | Wave 1 Alloc | Wave 2 Alloc | Reduction | Reuse | Result");

        Console.WriteLine(
            "------+---------------+---------------+-----------+------+--------");

        for (var index = 0;
             index < results.Count;
             index++)
        {
            var result =
                results[index];

            Console.WriteLine(
                $"{result.Scale,5:N0} |" +
                $"{result.FirstWaveAllocated,13:N0} |" +
                $"{result.SecondWaveAllocated,13:N0} |" +
                $"{result.ReductionPercent,8:F2}% |" +
                $"{(result.ReuseObserved ? "PASS" : "FAIL"),5} |" +
                $"{(result.Passed ? "PASS" : "FAIL"),6}");
        }

        Console.WriteLine();

        Console.WriteLine(
            "Interpretation:");

        Console.WriteLine(
            "  • Wave 1 measures initial adjacency-storage growth.");

        Console.WriteLine(
            "  • All relationship removals occur outside the add timings.");

        Console.WriteLine(
            "  • Wave 2 reuses the same workload after the first wave is removed.");

        Console.WriteLine(
            "  • Lower Wave 2 allocation is evidence that released slab nodes are reused.");

        Console.WriteLine(
            "  • Final validation verifies that reuse does not corrupt graph state.");

        Console.WriteLine();
    }

    /*
     * =============================================================
     * HEADER
     * =============================================================
     */

    private static void PrintHeader(
        int seed,
        int operations,
        IReadOnlyList<int> scales)
    {
        Console.WriteLine();

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                    SOROPHY ENGINE POOL REUSE                  ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");

        Console.WriteLine(
            $"║ Seed:           {seed,-43}║");

        Console.WriteLine(
            $"║ Operations:     {operations,-43}║");

        Console.WriteLine(
            "║ Topology:       Parallel relationship chains              ║");

        Console.WriteLine(
            "║ Purpose:        Prove slab storage reuse after deletion   ║");

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");

        Console.WriteLine();

        Console.WriteLine(
            "Scale points:");

        for (var index = 0;
             index < scales.Count;
             index++)
        {
            Console.WriteLine(
                $"  {scales[index]:N0} relationships");
        }

        Console.WriteLine();
    }

    /*
     * =============================================================
     * GC
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
     * MATH
     * =============================================================
     */

    private static double
        CalculateReductionPercent(
            long first,
            long second)
    {
        if (first <= 0)
        {
            return second == 0
                ? 100.0
                : 0.0;
        }

        return
            (1.0 -
             ((double)second /
              first)) *
            100.0;
    }

    /*
     * =============================================================
     * DATA
     * =============================================================
     */

    private sealed record WaveMeasurement(
        SorophyGraph Graph,
        TimeSpan Elapsed,
        double OperationsPerSecond,
        long AllocatedBytes,
        double AllocatedBytesPerOperation);

    private sealed record ScaleResult(
        int Scale,
        long FirstWaveAllocated,
        long SecondWaveAllocated,
        double ReductionPercent,
        double FirstWaveThroughput,
        double SecondWaveThroughput,
        bool Passed)
    {
        public bool ReuseObserved =>
            SecondWaveAllocated <=
            FirstWaveAllocated;
    }

    /*
     * =============================================================
     * DETERMINISTIC WORKLOAD
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

        public SorophyRelationship[] CreateRelationships(
            int count,
            byte identityNamespace)
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
                            CreateGuid(
                                identityNamespace),

                        Type =
                            "pool-reuse",

                        SourceId =
                            AnchorId,

                        TargetId =
                            TargetId
                    };
            }

            return relationships;
        }

        public SorophyRelationship CreateWarmupRelationship()
        {
            return new SorophyRelationship
            {
                Id =
                    CreateGuid(
                        0x0F),

                Type =
                    "pool-reuse-warmup",

                SourceId =
                    AnchorId,

                TargetId =
                    TargetId
            };
        }

        private Guid CreateGuid(
            byte identityNamespace)
        {
            Span<byte> bytes =
                stackalloc byte[16];

            _random.NextBytes(
                bytes);

            /*
             * Reserve the high nibble of the first byte as the deterministic
             * identity namespace. This guarantees that Wave 1, Wave 2, and
             * the warm-up relationship cannot share an identity.
             */
            bytes[0] =
                (byte)(
                    (bytes[0] & 0x0F) |
                    ((identityNamespace & 0x0F) << 4));

            return new Guid(
                bytes);
        }
    }
}