using System;
using System.Diagnostics;

namespace Sorophy.Engine.StressTests.Infrastructure;

public sealed record PerformanceMeasurement(
    string Name,
    int Iterations,
    TimeSpan Elapsed,
    long AllocatedBytes,
    ResourceSnapshot BeforeResources,
    ResourceSnapshot AfterResources)
{
    public double OperationsPerSecond =>
        Iterations /
        Math.Max(
            Elapsed.TotalSeconds,
            0.000000001);

    public double AverageMicroseconds =>
        (Elapsed.TotalMilliseconds * 1000.0) /
        Math.Max(
            Iterations,
            1);

    public long AllocatedBytesPerOperation =>
        AllocatedBytes /
        Math.Max(
            Iterations,
            1);
}

public static class PerformanceMetrics
{
    public static PerformanceMeasurement Measure(
        string name,
        int iterations,
        Func<long> operation)
    {
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iterations),
                "Iterations must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(operation);

        /*
         * IMPORTANT:
         *
         * Do NOT automatically warm up the supplied operation.
         *
         * Many performance tests operate on mutable state
         * such as adding/removing entities or relationships.
         *
         * Running the operation once before measurement would
         * alter the benchmark state and can invalidate iteration
         * counts or even cause indexing errors.
         *
         * If a future benchmark needs JIT warm-up, it should warm
         * up a separate disposable graph/state before the measured
         * graph is constructed.
         */

        var before =
            ResourceMetrics.Capture();

        var allocatedBefore =
            GC.GetTotalAllocatedBytes(
                precise: false);

        var stopwatch =
            Stopwatch.StartNew();

        long sink = 0;

        for (var index = 0;
             index < iterations;
             index++)
        {
            sink += operation();
        }

        stopwatch.Stop();

        GC.KeepAlive(sink);

        var allocatedAfter =
            GC.GetTotalAllocatedBytes(
                precise: false);

        var after =
            ResourceMetrics.Capture();

        return new PerformanceMeasurement(
            name,
            iterations,
            stopwatch.Elapsed,
            allocatedAfter - allocatedBefore,
            before,
            after);
    }
}