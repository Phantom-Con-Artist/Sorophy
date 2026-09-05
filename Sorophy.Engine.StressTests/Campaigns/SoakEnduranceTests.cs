using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Storage;

namespace Sorophy.Engine.StressTests;

public static class SoakEnduranceTests
{
    /*
     * =============================================================
     * TEST SHAPE
     * =============================================================
     *
     * The soak test keeps one stable graph alive and repeatedly:
     *
     *   mutate
     *   query
     *   remove
     *   validate
     *   serialize
     *   deserialize
     *   save
     *   load
     *
     * The live graph returns to its exact canonical state after every
     * mutation cycle.
     *
     * This is intentionally different from Crash/Recovery:
     *
     *   Crash/Recovery -> fault injection / recovery behavior
     *   Soak           -> cumulative degradation over time
     */

    private const int BaseEntityCount =
        16;

    private const int ProgressInterval =
        10_000;

    private const int RecentCycleCapacity =
        16;

    /*
     * In-memory round-trip every 100 cycles.
     *
     * At 1,000,000 cycles this gives 10,000 full
     * serialize/deserialize cycles.
     */
    private const int InMemoryPersistenceInterval =
        100;

    /*
     * Real disk round-trip every 1,000 cycles.
     *
     * At 1,000,000 cycles this gives exactly 1,000
     * disk-backed persistence cycles.
     */
    private const int DiskPersistenceInterval =
        1_000;

    /*
     * Memory is measured after a forced collection at the start/end.
     *
     * The tolerance is deliberately conservative enough to catch
     * sustained retention without treating normal runtime allocator
     * behavior as a leak.
     */
    private const long RetainedMemoryAbsoluteToleranceBytes =
        8L * 1024L * 1024L;

    private const double RetainedMemoryRelativeTolerance =
        0.50;

    public static int Run(
        int seed = 12345,
        int operations = 10_000,
        int auditInterval = 1_000)
    {
        if (operations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "Operation count must be greater than zero.");
        }

        if (auditInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(auditInterval),
                "Audit interval must be greater than zero.");
        }

        var started =
            Stopwatch.GetTimestamp();

        var rootDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "SorophyEngineSoak",
                $"{seed}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            rootDirectory);

        try
        {
            PrintHeader(
                seed,
                operations,
                auditInterval,
                rootDirectory);

            /*
             * =========================================================
             * BASE GRAPH
             * =========================================================
             */

            var graph =
                BuildBaseGraph(
                    seed);

            Require(
                graph.Validate().Count == 0,
                "Initial soak graph failed validation.");

            var canonicalJson =
                LoreSerializer.Serialize(
                    graph);

            var canonicalFingerprint =
                ComputeFingerprint(
                    canonicalJson);

            var entityIds =
                graph.Entities.Keys
                    .OrderBy(
                        static id => id)
                    .ToArray();

            Require(
                entityIds.Length >= 2,
                "Base graph must contain at least two entities.");

            var anchorEntityId =
                entityIds[0];

            var secondEntityId =
                entityIds[1];

            /*
             * =========================================================
             * DISK FIXTURE
             * =========================================================
             */

            var diskPath =
                Path.Combine(
                    rootDirectory,
                    "soak.lore");

            LoreStorage.Save(
                graph,
                diskPath);

            /*
             * =========================================================
             * BASELINE RESOURCE SNAPSHOT
             * =========================================================
             */

            ForceCollection();

            var baselineManagedBytes =
                GC.GetTotalMemory(
                    forceFullCollection: true);

            var baselineWorkingSet =
                Process.GetCurrentProcess()
                    .WorkingSet64;

            var baselineGen0 =
                GC.CollectionCount(0);

            var baselineGen1 =
                GC.CollectionCount(1);

            var baselineGen2 =
                GC.CollectionCount(2);

            Console.WriteLine();

            Console.WriteLine(
                "BASELINE");

            Console.WriteLine(
                $"  Entities:              {graph.Entities.Count:N0}");

            Console.WriteLine(
                $"  Relationships:         {graph.Relationships.Count:N0}");

            Console.WriteLine(
                $"  Managed heap:          {FormatBytes(baselineManagedBytes)}");

            Console.WriteLine(
                $"  Working set:           {FormatBytes(baselineWorkingSet)}");

            Console.WriteLine(
                $"  Canonical fingerprint: {canonicalFingerprint}");

            Console.WriteLine();

            /*
             * =========================================================
             * ENDURANCE LOOP
             * =========================================================
             */

            var random =
                new DeterministicRandom(
                    seed ^ unchecked((int)0x5A17C0DE));

            var counters =
                new SoakCounters();

            var recentKinds =
                new SoakOperationKind[
                    RecentCycleCapacity];

            var peakManagedBytes =
                baselineManagedBytes;

            var peakWorkingSet =
                baselineWorkingSet;

            var stopwatch =
                Stopwatch.StartNew();

            var lastAuditManagedBytes =
                baselineManagedBytes;

            var lastAuditWorkingSet =
                baselineWorkingSet;

            for (var cycle = 1;
                 cycle <= operations;
                 cycle++)
            {
                var operationKind =
                    SelectOperationKind(
                        random);

                recentKinds[
                    (cycle - 1) %
                    recentKinds.Length] =
                    operationKind;

                try
                {
                    switch (operationKind)
                    {
                        case SoakOperationKind.MutateAndRestore:

                            ExecuteMutationCycle(
                                graph,
                                anchorEntityId,
                                secondEntityId,
                                random,
                                cycle,
                                counters);

                            break;

                        case SoakOperationKind.QueryHeavy:

                            ExecuteQueryCycle(
                                graph,
                                anchorEntityId,
                                secondEntityId,
                                cycle,
                                counters);

                            break;

                        default:

                            throw new InvalidOperationException(
                                $"Unknown soak operation '{operationKind}'.");
                    }

                    counters.CyclesExecuted++;

                    /*
                     * =================================================
                     * IN-MEMORY PERSISTENCE
                     * =================================================
                     */

                    if (cycle %
                            InMemoryPersistenceInterval ==
                        0)
                    {
                        var serialized =
                            LoreSerializer.Serialize(
                                graph);

                        Require(
                            string.Equals(
                                serialized,
                                canonicalJson,
                                StringComparison.Ordinal),
                            $"Canonical serialization drift detected at cycle {cycle:N0}.");

                        var restored =
                            LoreSerializer.Deserialize(
                                serialized);

                        RequireGraphMatchesCanonical(
                            restored,
                            canonicalJson,
                            cycle,
                            "In-memory persistence");

                        graph =
                            restored;

                        counters.InMemoryPersistenceRoundTrips++;
                    }

                    /*
                     * =================================================
                     * DISK PERSISTENCE
                     * =================================================
                     */

                    if (cycle %
                            DiskPersistenceInterval ==
                        0)
                    {
                        LoreStorage.Save(
                            graph,
                            diskPath);

                        var restored =
                            LoreStorage.Load(
                                diskPath);

                        RequireGraphMatchesCanonical(
                            restored,
                            canonicalJson,
                            cycle,
                            "Disk persistence");

                        graph =
                            restored;

                        counters.DiskPersistenceRoundTrips++;
                    }

                    /*
                     * =================================================
                     * PERIODIC AUDIT
                     * =================================================
                     */

                    if (cycle %
                            auditInterval ==
                        0 ||
                        cycle == operations)
                    {
                        PerformAudit(
                            graph,
                            canonicalJson,
                            canonicalFingerprint,
                            anchorEntityId,
                            secondEntityId,
                            cycle,
                            operations,
                            stopwatch,
                            baselineManagedBytes,
                            baselineWorkingSet,
                            ref peakManagedBytes,
                            ref peakWorkingSet,
                            ref lastAuditManagedBytes,
                            ref lastAuditWorkingSet,
                            counters);
                    }

                    /*
                     * =================================================
                     * PROGRESS
                     * =================================================
                     */

                    if (cycle %
                            ProgressInterval ==
                        0 ||
                        cycle == operations)
                    {
                        PrintProgress(
                            cycle,
                            operations,
                            stopwatch.Elapsed,
                            graph,
                            counters,
                            baselineManagedBytes,
                            peakManagedBytes,
                            peakWorkingSet);
                    }
                }
                catch (Exception exception)
                {
                    PrintFailureContext(
                        cycle,
                        operations,
                        operationKind,
                        graph,
                        exception,
                        recentKinds);

                    throw;
                }
            }

            stopwatch.Stop();

            /*
             * =========================================================
             * FINAL RESOURCE SNAPSHOT
             * =========================================================
             */

            ForceCollection();

            var finalManagedBytes =
                GC.GetTotalMemory(
                    forceFullCollection: true);

            var finalWorkingSet =
                Process.GetCurrentProcess()
                    .WorkingSet64;

            peakManagedBytes =
                Math.Max(
                    peakManagedBytes,
                    finalManagedBytes);

            peakWorkingSet =
                Math.Max(
                    peakWorkingSet,
                    finalWorkingSet);

            var finalGen0 =
                GC.CollectionCount(0);

            var finalGen1 =
                GC.CollectionCount(1);

            var finalGen2 =
                GC.CollectionCount(2);

            var retainedManagedDelta =
                finalManagedBytes -
                baselineManagedBytes;

            var retainedManagedLimit =
                Math.Max(
                    RetainedMemoryAbsoluteToleranceBytes,
                    (long)(
                        baselineManagedBytes *
                        RetainedMemoryRelativeTolerance));

            var memoryHealthy =
                retainedManagedDelta <=
                retainedManagedLimit;

            /*
             * =========================================================
             * FINAL STATE
             * =========================================================
             */

            var finalJson =
                LoreSerializer.Serialize(
                    graph);

            var finalFingerprint =
                ComputeFingerprint(
                    finalJson);

            var canonicalStateHealthy =
                string.Equals(
                    finalFingerprint,
                    canonicalFingerprint,
                    StringComparison.Ordinal);

            Require(
                canonicalStateHealthy,
                "Final canonical fingerprint drift detected.");

            Require(
                string.Equals(
                    finalJson,
                    canonicalJson,
                    StringComparison.Ordinal),
                "Final canonical serialized state drift detected.");

            Require(
                graph.Entities.Count ==
                BaseEntityCount,
                $"Final entity count drifted. Expected={BaseEntityCount}, Actual={graph.Entities.Count}.");

            Require(
                graph.Relationships.Count ==
                BaseRelationshipCount(),
                $"Final relationship count drifted. Expected={BaseRelationshipCount()}, Actual={graph.Relationships.Count}.");

            Require(
                graph.Validate().Count == 0,
                "Final graph validation failed.");

            /*
             * =========================================================
             * SUMMARY
             * =========================================================
             */

            var throughput =
                counters.CyclesExecuted /
                Math.Max(
                    stopwatch.Elapsed.TotalSeconds,
                    0.001);

            Console.WriteLine();

            Console.WriteLine(
                "════════════════════════════════════════════════════════════════");

            Console.WriteLine(
                "                    SOAK / ENDURANCE SUMMARY");

            Console.WriteLine(
                "════════════════════════════════════════════════════════════════");

            Console.WriteLine();

            Console.WriteLine(
                $"  Cycles executed:              {counters.CyclesExecuted:N0}");

            Console.WriteLine(
                $"  Mutation cycles:              {counters.MutationCycles:N0}");

            Console.WriteLine(
                $"  Query-heavy cycles:           {counters.QueryHeavyCycles:N0}");

            Console.WriteLine(
                $"  Relationship adds:            {counters.RelationshipAdds:N0}");

            Console.WriteLine(
                $"  Relationship removals:        {counters.RelationshipRemovals:N0}");

            Console.WriteLine(
                $"  Entity adds:                  {counters.EntityAdds:N0}");

            Console.WriteLine(
                $"  Entity removals:              {counters.EntityRemovals:N0}");

            Console.WriteLine(
                $"  Validations:                  {counters.Validations:N0}");

            Console.WriteLine(
                $"  In-memory persistence:        {counters.InMemoryPersistenceRoundTrips:N0}");

            Console.WriteLine(
                $"  Disk persistence:             {counters.DiskPersistenceRoundTrips:N0}");

            Console.WriteLine();

            Console.WriteLine(
                $"  Runtime:                      {stopwatch.Elapsed.TotalSeconds:F3} s");

            Console.WriteLine(
                $"  Throughput:                   {throughput:N0} cycles/s");

            Console.WriteLine();

            Console.WriteLine(
                $"  Baseline managed heap:        {FormatBytes(baselineManagedBytes)}");

            Console.WriteLine(
                $"  Final managed heap:           {FormatBytes(finalManagedBytes)}");

            Console.WriteLine(
                $"  Peak managed heap:            {FormatBytes(peakManagedBytes)}");

            Console.WriteLine(
                $"  Retained managed delta:       {FormatSignedBytes(retainedManagedDelta)}");

            Console.WriteLine(
                $"  Allowed retained delta:       {FormatBytes(retainedManagedLimit)}");

            Console.WriteLine();

            Console.WriteLine(
                $"  Baseline working set:         {FormatBytes(baselineWorkingSet)}");

            Console.WriteLine(
                $"  Final working set:            {FormatBytes(finalWorkingSet)}");

            Console.WriteLine(
                $"  Peak working set:             {FormatBytes(peakWorkingSet)}");

            Console.WriteLine();

            Console.WriteLine(
                $"  Gen 0 collections:            {finalGen0 - baselineGen0:N0}");

            Console.WriteLine(
                $"  Gen 1 collections:            {finalGen1 - baselineGen1:N0}");

            Console.WriteLine(
                $"  Gen 2 collections:            {finalGen2 - baselineGen2:N0}");

            Console.WriteLine();

            Console.WriteLine(
                $"  Canonical state:              {(canonicalStateHealthy ? "PASS" : "FAIL")}");

            Console.WriteLine(
                $"  Memory retention:             {(memoryHealthy ? "PASS" : "FAIL")}");

            Console.WriteLine(
                "  Final validation:             PASS");

            Console.WriteLine();

            if (!memoryHealthy)
            {
                Console.WriteLine(
                    "  FAILURE: retained managed memory exceeded soak tolerance.");

                Console.WriteLine();

                return 1;
            }

            Console.WriteLine(
                $"  PASS  Soak endurance           {counters.CyclesExecuted:N0}/{operations:N0}");

            Console.WriteLine();

            Console.WriteLine(
                "                     100% PASS");

            Console.WriteLine(
                "STATUS: SOAK / ENDURANCE VERIFIED");

            Console.WriteLine();

            return 0;
        }
        finally
        {
            try
            {
                if (Directory.Exists(
                        rootDirectory))
                {
                    Directory.Delete(
                        rootDirectory,
                        recursive: true);
                }
            }
            catch
            {
                /*
                 * Cleanup failure must never hide the real result.
                 */
            }
        }
    }

    /*
     * =============================================================
     * MUTATION CYCLE
     * =============================================================
     */

    private static void ExecuteMutationCycle(
        SorophyGraph graph,
        Guid anchorEntityId,
        Guid secondEntityId,
        DeterministicRandom random,
        int cycle,
        SoakCounters counters)
    {
        counters.MutationCycles++;

        var temporaryEntityId =
            random.NextGuid();

        while (graph.ContainsEntity(
                   temporaryEntityId))
        {
            temporaryEntityId =
                random.NextGuid();
        }

        var temporaryEntity =
            new SorophyEntity
            {
                Id =
                    temporaryEntityId,

                Name =
                    $"SoakTemp-{cycle:N0}",

                Type =
                    "Temporary"
            };

        graph.AddEntity(
            temporaryEntity);

        counters.EntityAdds++;

        var firstRelationshipId =
            random.NextGuid();

        var secondRelationshipId =
            random.NextGuid();

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    firstRelationshipId,

                Type =
                    "soak-forward",

                SourceId =
                    anchorEntityId,

                TargetId =
                    temporaryEntityId
            });

        counters.RelationshipAdds++;

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    secondRelationshipId,

                Type =
                    "soak-return",

                SourceId =
                    temporaryEntityId,

                TargetId =
                    secondEntityId
            });

        counters.RelationshipAdds++;

        /*
         * Query while the temporary subgraph exists.
         */
        var anchorOutgoing =
            graph.GetOutgoingRelationships(
                    anchorEntityId)
                .Any(
                    relationship =>
                        relationship.Id ==
                        firstRelationshipId);

        Require(
            anchorOutgoing,
            $"New outgoing relationship not visible at cycle {cycle:N0}.");

        var secondIncoming =
            graph.GetIncomingRelationships(
                    secondEntityId)
                .Any(
                    relationship =>
                        relationship.Id ==
                        secondRelationshipId);

        Require(
            secondIncoming,
            $"New incoming relationship not visible at cycle {cycle:N0}.");

        var neighbors =
            graph.GetNeighbors(
                    anchorEntityId)
                .Select(
                    entity =>
                        entity.Id)
                .ToHashSet();

        Require(
            neighbors.Contains(
                temporaryEntityId),
            $"Temporary neighbor not visible at cycle {cycle:N0}.");

        /*
         * Exercise reachability through the temporary node.
         */
        Require(
            graph.IsReachable(
                anchorEntityId,
                secondEntityId),
            $"Reachability failed at cycle {cycle:N0}.");

        /*
         * Remove edges first, then entity.
         *
         * This heavily exercises adjacency unlink/reuse behavior.
         */
        Require(
            graph.RemoveRelationship(
                firstRelationshipId),
            $"First relationship removal failed at cycle {cycle:N0}.");

        counters.RelationshipRemovals++;

        Require(
            graph.RemoveRelationship(
                secondRelationshipId),
            $"Second relationship removal failed at cycle {cycle:N0}.");

        counters.RelationshipRemovals++;

        Require(
            graph.RemoveEntity(
                temporaryEntityId),
            $"Temporary entity removal failed at cycle {cycle:N0}.");

        counters.EntityRemovals++;

        /*
         * Make sure no temporary state survived.
         */
        Require(
            !graph.ContainsEntity(
                temporaryEntityId),
            $"Temporary entity survived cycle {cycle:N0}.");

        Require(
            !graph.ContainsRelationship(
                firstRelationshipId),
            $"First relationship survived cycle {cycle:N0}.");

        Require(
            !graph.ContainsRelationship(
                secondRelationshipId),
            $"Second relationship survived cycle {cycle:N0}.");
    }

    /*
     * =============================================================
     * QUERY-HEAVY CYCLE
     * =============================================================
     */

    private static void ExecuteQueryCycle(
        SorophyGraph graph,
        Guid anchorEntityId,
        Guid secondEntityId,
        int cycle,
        SoakCounters counters)
    {
        counters.QueryHeavyCycles++;

        var outgoing =
            graph.GetOutgoingRelationships(
                    anchorEntityId)
                .ToList();

        var incoming =
            graph.GetIncomingRelationships(
                    secondEntityId)
                .ToList();

        var incident =
            graph.GetRelationships(
                    anchorEntityId)
                .ToList();

        var neighbors =
            graph.GetNeighbors(
                    anchorEntityId)
                .ToList();

        Require(
            outgoing.Count > 0,
            $"Outgoing query returned no relationships at cycle {cycle:N0}.");

        Require(
            incoming.Count > 0,
            $"Incoming query returned no relationships at cycle {cycle:N0}.");

        Require(
            incident.Count >= outgoing.Count,
            $"Incident query became inconsistent at cycle {cycle:N0}.");

        Require(
            neighbors.Count > 0,
            $"Neighbor query returned no neighbors at cycle {cycle:N0}.");

        Require(
            graph.IsReachable(
                anchorEntityId,
                secondEntityId),
            $"Reachability failed at cycle {cycle:N0}.");
    }

    /*
     * =============================================================
     * AUDIT
     * =============================================================
     */

    private static void PerformAudit(
        SorophyGraph graph,
        string canonicalJson,
        string canonicalFingerprint,
        Guid anchorEntityId,
        Guid secondEntityId,
        int cycle,
        int operations,
        Stopwatch stopwatch,
        long baselineManagedBytes,
        long baselineWorkingSet,
        ref long peakManagedBytes,
        ref long peakWorkingSet,
        ref long lastAuditManagedBytes,
        ref long lastAuditWorkingSet,
        SoakCounters counters)
    {
        var errors =
            graph.Validate();

        counters.Validations++;

        Require(
            errors.Count == 0,
            $"SorophyGraph.Validate() failed at cycle {cycle:N0} with {errors.Count} error(s)." +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                errors.Take(20)));

        Require(
            graph.Entities.Count ==
            BaseEntityCount,
            $"Entity count drift at cycle {cycle:N0}. " +
            $"Expected={BaseEntityCount}, Actual={graph.Entities.Count}.");

        Require(
            graph.Relationships.Count ==
            BaseRelationshipCount(),
            $"Relationship count drift at cycle {cycle:N0}. " +
            $"Expected={BaseRelationshipCount()}, Actual={graph.Relationships.Count}.");

        Require(
            graph.IsReachable(
                anchorEntityId,
                secondEntityId),
            $"Anchor reachability failed at cycle {cycle:N0}.");

        var serialized =
            LoreSerializer.Serialize(
                graph);

        var fingerprint =
            ComputeFingerprint(
                serialized);

        Require(
            string.Equals(
                fingerprint,
                canonicalFingerprint,
                StringComparison.Ordinal),
            $"Canonical fingerprint drift at cycle {cycle:N0}.");

        Require(
            string.Equals(
                serialized,
                canonicalJson,
                StringComparison.Ordinal),
            $"Canonical serialization drift at cycle {cycle:N0}.");

        var managedBytes =
            GC.GetTotalMemory(
                forceFullCollection: false);

        var workingSet =
            Process.GetCurrentProcess()
                .WorkingSet64;

        peakManagedBytes =
            Math.Max(
                peakManagedBytes,
                managedBytes);

        peakWorkingSet =
            Math.Max(
                peakWorkingSet,
                workingSet);

        var managedDelta =
            managedBytes -
            baselineManagedBytes;

        var workingSetDelta =
            workingSet -
            baselineWorkingSet;

        var auditManagedDelta =
            managedBytes -
            lastAuditManagedBytes;

        var auditWorkingSetDelta =
            workingSet -
            lastAuditWorkingSet;

        lastAuditManagedBytes =
            managedBytes;

        lastAuditWorkingSet =
            workingSet;

        var rate =
            cycle /
            Math.Max(
                stopwatch.Elapsed.TotalSeconds,
                0.001);

        Console.WriteLine(
            $"  Audit {cycle,10:N0}/{operations:N0} → PASS | " +
            $"Rate={rate,9:N0} cycles/s | " +
            $"ManagedΔ={FormatSignedBytes(managedDelta),10} | " +
            $"WSΔ={FormatSignedBytes(workingSetDelta),10} | " +
            $"AuditΔ={FormatSignedBytes(auditManagedDelta),10} | " +
            $"WS AuditΔ={FormatSignedBytes(auditWorkingSetDelta),10}");
    }

    /*
     * =============================================================
     * OPERATION SELECTION
     * =============================================================
     */

    private static SoakOperationKind SelectOperationKind(
        DeterministicRandom random)
    {
        /*
         * 70% mutation churn
         * 30% query-heavy access
         */
        return random.NextInt(100) < 70
            ? SoakOperationKind.MutateAndRestore
            : SoakOperationKind.QueryHeavy;
    }

    /*
     * =============================================================
     * BASE GRAPH
     * =============================================================
     */

    private static SorophyGraph BuildBaseGraph(
        int seed)
    {
        var random =
            new DeterministicRandom(
                seed);

        var graph =
            new SorophyGraph();

        var ids =
            new Guid[BaseEntityCount];

        /*
         * Entities.
         */
        for (var index = 0;
             index < BaseEntityCount;
             index++)
        {
            var id =
                random.NextGuid();

            ids[index] =
                id;

            graph.AddEntity(
                new SorophyEntity
                {
                    Id =
                        id,

                    Name =
                        $"SoakBase-{index:D3}",

                    Type =
                        (index % 4) switch
                        {
                            0 =>
                                "Character",

                            1 =>
                                "Location",

                            2 =>
                                "Faction",

                            _ =>
                                "Concept"
                        }
                });
        }

        /*
         * Ring.
         */
        for (var index = 0;
             index < BaseEntityCount;
             index++)
        {
            var next =
                (index + 1) %
                BaseEntityCount;

            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id =
                        random.NextGuid(),

                    Type =
                        "soak-ring",

                    SourceId =
                        ids[index],

                    TargetId =
                        ids[next]
                });
        }

        /*
         * Two chords.
         */
        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    random.NextGuid(),

                Type =
                    "soak-chord",

                SourceId =
                    ids[0],

                TargetId =
                    ids[BaseEntityCount / 2]
            });

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    random.NextGuid(),

                Type =
                    "soak-chord",

                SourceId =
                    ids[BaseEntityCount / 2],

                TargetId =
                    ids[0]
            });

        /*
         * Self-link.
         */
        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    random.NextGuid(),

                Type =
                    "soak-self",

                SourceId =
                    ids[0],

                TargetId =
                    ids[0]
            });

        return graph;
    }

    private static int BaseRelationshipCount()
    {
        return
            BaseEntityCount +
            2 +
            1;
    }

    /*
     * =============================================================
     * CANONICAL FINGERPRINT
     * =============================================================
     */

    private static string ComputeFingerprint(
        string value)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                value);

        var hash =
            SHA256.HashData(
                bytes);

        return
            Convert.ToHexString(
                hash);
    }

    /*
     * =============================================================
     * GRAPH ASSERTION
     * =============================================================
     */

    private static void RequireGraphMatchesCanonical(
        SorophyGraph graph,
        string canonicalJson,
        int cycle,
        string context)
    {
        Require(
            graph.Entities.Count ==
            BaseEntityCount,
            $"{context}: entity count changed at cycle {cycle:N0}.");

        Require(
            graph.Relationships.Count ==
            BaseRelationshipCount(),
            $"{context}: relationship count changed at cycle {cycle:N0}.");

        Require(
            graph.Validate().Count == 0,
            $"{context}: validation failed at cycle {cycle:N0}.");

        var serialized =
            LoreSerializer.Serialize(
                graph);

        Require(
            string.Equals(
                serialized,
                canonicalJson,
                StringComparison.Ordinal),
            $"{context}: canonical state changed at cycle {cycle:N0}.");
    }

    /*
     * =============================================================
     * COLLECTION CONTROL
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
     * PROGRESS
     * =============================================================
     */

    private static void PrintProgress(
        int cycle,
        int operations,
        TimeSpan elapsed,
        SorophyGraph graph,
        SoakCounters counters,
        long baselineManagedBytes,
        long peakManagedBytes,
        long peakWorkingSet)
    {
        var rate =
            cycle /
            Math.Max(
                elapsed.TotalSeconds,
                0.001);

        var managedBytes =
            GC.GetTotalMemory(
                forceFullCollection: false);

        Console.WriteLine();

        Console.WriteLine(
            $"  [{cycle,10:N0}/{operations:N0}] " +
            $"PASS | " +
            $"Rate={rate,9:N0} cycles/s | " +
            $"Entities={graph.Entities.Count,3} | " +
            $"Relationships={graph.Relationships.Count,3} | " +
            $"Managed={FormatBytes(managedBytes),10} | " +
            $"ManagedΔ={FormatSignedBytes(managedBytes - baselineManagedBytes),10}");

        Console.WriteLine(
            $"      Mutations={counters.MutationCycles,10:N0} | " +
            $"Queries={counters.QueryHeavyCycles,10:N0} | " +
            $"MemoryRT={counters.InMemoryPersistenceRoundTrips,8:N0} | " +
            $"DiskRT={counters.DiskPersistenceRoundTrips,6:N0} | " +
            $"PeakManaged={FormatBytes(peakManagedBytes),10} | " +
            $"PeakWS={FormatBytes(peakWorkingSet),10}");
    }

    /*
     * =============================================================
     * FAILURE REPORT
     * =============================================================
     */

    private static void PrintFailureContext(
        int cycle,
        int operations,
        SoakOperationKind operationKind,
        SorophyGraph graph,
        Exception exception,
        SoakOperationKind[] recentKinds)
    {
        Console.WriteLine();

        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

        Console.WriteLine(
            "SOAK / ENDURANCE FAILURE");

        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

        Console.WriteLine();

        Console.WriteLine(
            $"Cycle:              {cycle:N0}/{operations:N0}");

        Console.WriteLine(
            $"Operation kind:     {operationKind}");

        Console.WriteLine(
            $"Entities:           {graph.Entities.Count:N0}");

        Console.WriteLine(
            $"Relationships:      {graph.Relationships.Count:N0}");

        Console.WriteLine(
            $"Message:            {exception.Message}");

        Console.WriteLine();

        Console.WriteLine(
            "Recent operation kinds:");

        var count =
            Math.Min(
                cycle,
                recentKinds.Length);

        for (var offset = 0;
             offset < count;
             offset++)
        {
            var index =
                (cycle -
                 count +
                 offset) %
                recentKinds.Length;

            Console.WriteLine(
                $"  {offset + 1,2}: {recentKinds[index]}");
        }

        Console.WriteLine();
    }

    /*
     * =============================================================
     * ASSERTION
     * =============================================================
     */

    private static void Require(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                message);
        }
    }

    private static void PrintHeader(
    int seed,
    int operations,
    int auditInterval,
    string temporaryDirectory)
{
    Console.WriteLine();

    Console.WriteLine(
        "╔══════════════════════════════════════════════════════════════╗");

    Console.WriteLine(
        "║                 SOROPHY SOAK / ENDURANCE                ║");

    Console.WriteLine(
        "╠══════════════════════════════════════════════════════════════╣");

    Console.WriteLine(
        $"║ Seed:           {seed,-43}║");

    Console.WriteLine(
        $"║ Operations:     {operations,-43}║");

    Console.WriteLine(
        $"║ Audit interval: {auditInterval,-43}║");

    Console.WriteLine(
        "║ Mode:           MUTATION + QUERY + PERSISTENCE + SOAK    ║");

    Console.WriteLine(
        "╚══════════════════════════════════════════════════════════════╝");

    Console.WriteLine();

    Console.WriteLine(
        $"Temporary sandbox: {temporaryDirectory}");

    Console.WriteLine();

    Console.WriteLine(
        "Workload design:");

    Console.WriteLine(
        "  • Repeated entity/relationship mutation");

    Console.WriteLine(
        "  • Relationship query and reachability checks");

    Console.WriteLine(
        "  • In-memory serialization round-trips");

    Console.WriteLine(
        "  • Periodic disk persistence round-trips");

    Console.WriteLine(
        "  • Periodic full graph validation");

    Console.WriteLine(
        "  • Canonical-state fingerprint verification");

    Console.WriteLine(
        "  • Managed-memory and working-set monitoring");

    Console.WriteLine();
}

    /*
     * =============================================================
     * FORMATTERS
     * =============================================================
     */

    private static string FormatBytes(
        long bytes)
    {
        const double kilobyte =
            1024.0;

        const double megabyte =
            kilobyte *
            1024.0;

        const double gigabyte =
            megabyte *
            1024.0;

        if (bytes >= gigabyte)
        {
            return
                $"{bytes / gigabyte:F2} GB";
        }

        if (bytes >= megabyte)
        {
            return
                $"{bytes / megabyte:F2} MB";
        }

        if (bytes >= kilobyte)
        {
            return
                $"{bytes / kilobyte:F2} KB";
        }

        return
            $"{bytes:N0} B";
    }

    private static string FormatSignedBytes(
        long bytes)
    {
        if (bytes > 0)
        {
            return
                $"+{FormatBytes(bytes)}";
        }

        if (bytes < 0)
        {
            return
                $"-{FormatBytes(
                    Math.Abs(bytes))}";
        }

        return
            "0 B";
    }

    /*
     * =============================================================
     * TYPES
     * =============================================================
     */

    private sealed class SoakCounters
    {
        public long CyclesExecuted { get; set; }

        public long MutationCycles { get; set; }

        public long QueryHeavyCycles { get; set; }

        public long InMemoryPersistenceRoundTrips { get; set; }

        public long DiskPersistenceRoundTrips { get; set; }

        public long Validations { get; set; }

        public long RelationshipAdds { get; set; }

        public long RelationshipRemovals { get; set; }

        public long EntityAdds { get; set; }

        public long EntityRemovals { get; set; }
    }

    private enum SoakOperationKind
    {
        MutateAndRestore,

        QueryHeavy
    }

    /*
     * =============================================================
     * DETERMINISTIC RANDOM
     * =============================================================
     */

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

            return
                z ^
                (z >> 31);
        }

        public int NextInt(
            int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMaximum));
            }

            return
                (int)(
                    NextUInt64() %
                    (uint)exclusiveMaximum);
        }

        public Guid NextGuid()
        {
            Span<byte> bytes =
                stackalloc byte[16];

            for (var index = 0;
                 index < bytes.Length;
                 index += 8)
            {
                var value =
                    NextUInt64();

                for (var offset = 0;
                     offset < 8;
                     offset++)
                {
                    bytes[index + offset] =
                        (byte)(
                            value >>
                            (offset * 8));
                }
            }

            return new Guid(
                bytes);
        }
    }
}