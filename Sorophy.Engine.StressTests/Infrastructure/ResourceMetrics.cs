using System;
using System.Diagnostics;

namespace Sorophy.Engine.StressTests.Infrastructure;

public sealed record ResourceSnapshot(
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long ManagedHeapBytes,
    long TotalAllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    long PeakWorkingSetBytes)
{
    public double WorkingSetMegabytes =>
        WorkingSetBytes /
        (1024.0 * 1024.0);

    public double PrivateMemoryMegabytes =>
        PrivateMemoryBytes /
        (1024.0 * 1024.0);

    public double ManagedHeapMegabytes =>
        ManagedHeapBytes /
        (1024.0 * 1024.0);

    public double PeakWorkingSetMegabytes =>
        PeakWorkingSetBytes /
        (1024.0 * 1024.0);
}

public static class ResourceMetrics
{
    public static ResourceSnapshot Capture()
    {
        using var process =
            Process.GetCurrentProcess();

        process.Refresh();

        return new ResourceSnapshot(
            WorkingSetBytes:
                process.WorkingSet64,

            PrivateMemoryBytes:
                process.PrivateMemorySize64,

            ManagedHeapBytes:
                GC.GetTotalMemory(
                    forceFullCollection: false),

            TotalAllocatedBytes:
                GC.GetTotalAllocatedBytes(
                    precise: false),

            Gen0Collections:
                GC.CollectionCount(0),

            Gen1Collections:
                GC.CollectionCount(1),

            Gen2Collections:
                GC.CollectionCount(2),

            PeakWorkingSetBytes:
                process.PeakWorkingSet64);
    }

    public static void Stabilize()
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
}