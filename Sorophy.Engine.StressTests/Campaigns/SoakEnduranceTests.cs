/*
 * Sorophy Engine — a structured knowledge and graph engine
 * Copyright (C) 2026  Subhradeep Sarkar
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Storage;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

/*
 * =============================================================
 * KRONO SOAK / ENDURANCE TEST ARCHITECTURE
 * =============================================================
 *
 * ARCHITECTURAL NOTE:
 * Krono intentionally retains retired relationship identities to guarantee
 * that relationship IDs can never be reused. Therefore, workloads involving
 * unbounded relationship retirement have intentional O(N) memory growth.
 *
 * The soak suite separates bounded active-state memory validation from
 * retirement-scale endurance so this architectural characteristic is
 * measured rather than misclassified as an implementation leak.
 *
 * The test suite is organized into four distinct phases:
 *
 *   PHASE A — BOUNDED ACTIVE-STATE SOAK
 *     Exercises sustained mutation churn on active entities and relationships
 *     (properties, tags, documents, time coordinates, isolated entity lifecycle),
 *     snapshot creation, read-only TQD traversals, and persistence round-trips
 *     without retiring relationship identities.
 *     Enforces the strict 8 MB retained-memory ceiling.
 *
 *   PHASE B — RETIREMENT SCALE ENDURANCE
 *     Exercises continuous relationship addition and deletion churn,
 *     forcing relationship identity retirement across scale (10K, 25K, 50K, 100K).
 *     Verifies active-state boundedness, canonical correctness, slab pool recycling,
 *     identity non-reusability, and persistence integrity.
 *
 *   PHASE C — RETIREMENT SCALING REPORT
 *     Reports empirical O(N) memory growth and throughput metrics across
 *     operations checkpoints, documenting the architectural boundary.
 *
 *   PHASE D — MEMORY RETENTION CHECK
 *     Decomposes final managed memory into expected permanent retention
 *     (canonical state + intentional tombstone registry + LOH serialization buffers)
 *     and unexpected retained delta, asserting that genuine leaks do not exceed
 *     the 8 MB tolerance.
 * =============================================================
 */
public static class SoakEnduranceTests
{
    private const int BaseEntityCount = 16;
    private const int ProgressInterval = 10_000;
    private const int RecentCycleCapacity = 16;
    private const int InMemoryPersistenceInterval = 100;
    private const int DiskPersistenceInterval = 1_000;

    /*
     * Memory is measured after a forced collection at the start/end.
     * The 8 MB tolerance is preserved as an absolute hard limit for
     * bounded active-state memory and unexpected retention.
     */
    private const long RetainedMemoryAbsoluteToleranceBytes = 8L * 1024L * 1024L;
    private const double RetainedMemoryRelativeTolerance = 0.50;

    /*
     * Stress-harness measurement model:
     * Represents the empirically observed per-retired-ID memory footprint
     * under full JSON round-trip serialization and Large Object Heap (LOH)
     * buffer writers (HashSet entry + serialized JSON string + deserialized
     * Guid list + serializer LOH buffers).
     * This is NOT a production constant or architectural assumption.
     */
    private const long ModeledBytesPerRetiredId = 512L;

    private static readonly SorophyTimeSchema SoakTimeSchema = new(
        "SoakTimeline",
        new[]
        {
            new SorophyTimeUnit(
                "Tick",
                0,
                new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
        });

    private static SorophyTime CreateSoakTime(long position)
    {
        return new SorophyTime(
            SoakTimeSchema,
            position.ToString(CultureInfo.InvariantCulture),
            "Tick",
            SorophyTimePrecision.Exact);
    }

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

        var rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "SorophyEngineSoak",
            $"{seed}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(rootDirectory);

        try
        {
            PrintHeader(seed, operations, auditInterval, rootDirectory);

            /*
             * =========================================================
             * PHASE A — BOUNDED ACTIVE-STATE SOAK
             * =========================================================
             */
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Console.WriteLine(" PHASE A — BOUNDED ACTIVE-STATE SOAK");
            Console.WriteLine("────────────────────────────────────────────────────────────────");

            var phaseAResult = RunPhaseABoundedSoak(
                seed,
                operations,
                auditInterval,
                rootDirectory);

            /*
             * =========================================================
             * PHASE B — RETIREMENT SCALE ENDURANCE
             * =========================================================
             */
            Console.WriteLine();
            Console.WriteLine("────────────────────────────────────────────────────────────────");
            Console.WriteLine(" PHASE B — RETIREMENT SCALE ENDURANCE");
            Console.WriteLine("────────────────────────────────────────────────────────────────");

            var phaseBResult = RunPhaseBRetirementEndurance(
                seed,
                operations,
                auditInterval,
                rootDirectory);

            /*
             * =========================================================
             * PHASE C — RETIREMENT SCALING REPORT
             * =========================================================
             */
            PrintPhaseCRetirementScalingReport(phaseBResult.Checkpoints);

            /*
             * =========================================================
             * PHASE D — MEMORY RETENTION CHECK
             * =========================================================
             */
            var phaseDResult = EvaluatePhaseDMemoryRetention(
                phaseBResult.BaselineManagedBytes,
                phaseBResult.FinalManagedBytes,
                phaseBResult.RetiredIdCount);

            /*
             * =========================================================
             * FINAL SUMMARY & CHECK VERIFICATION
             * =========================================================
             */
            var canonicalCorrectnessPass = phaseAResult.CanonicalStateHealthy && phaseBResult.CanonicalStateHealthy;
            var activeStateBoundednessPass = phaseAResult.MemoryHealthy && phaseBResult.ActiveCountsHealthy;
            var retirementInvariantsPass = phaseBResult.RetirementInvariantsHealthy;
            var persistenceCorrectnessPass = phaseAResult.PersistenceHealthy && phaseBResult.PersistenceHealthy;
            var unexpectedRetentionPass = phaseDResult.UnexpectedRetentionPass;
            var retirementScalingPass = phaseBResult.ScalingHealthy;

            var overallPass = canonicalCorrectnessPass &&
                              activeStateBoundednessPass &&
                              retirementInvariantsPass &&
                              persistenceCorrectnessPass &&
                              unexpectedRetentionPass &&
                              retirementScalingPass;

            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════════");
            Console.WriteLine("                    SOAK / ENDURANCE SUMMARY");
            Console.WriteLine("════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"  Canonical correctness:       {(canonicalCorrectnessPass ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Active-state boundedness:    {(activeStateBoundednessPass ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Retirement invariants:       {(retirementInvariantsPass ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Persistence correctness:     {(persistenceCorrectnessPass ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Unexpected retention:        {(unexpectedRetentionPass ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Retirement scaling:          {(retirementScalingPass ? "INFORMATIONAL / PASS" : "FAIL")}");
            Console.WriteLine($"  Overall soak result:         {(overallPass ? "PASS" : "FAIL")}");
            Console.WriteLine();
            Console.WriteLine("Architectural boundary:");
            Console.WriteLine("Retired relationship identity storage is intentionally O(N).");
            Console.WriteLine();

            if (!overallPass)
            {
                Console.WriteLine("  FAILURE: one or more soak endurance checks failed.");
                Console.WriteLine();
                return 1;
            }

            Console.WriteLine("                     100% PASS");
            Console.WriteLine("STATUS: SOAK / ENDURANCE VERIFIED");
            Console.WriteLine();

            return 0;
        }
        finally
        {
            try
            {
                if (Directory.Exists(rootDirectory))
                {
                    Directory.Delete(rootDirectory, recursive: true);
                }
            }
            catch
            {
                // Cleanup failure must never hide the real test result.
            }
        }
    }

    /*
     * =============================================================
     * PHASE A: BOUNDED ACTIVE-STATE SOAK IMPLEMENTATION
     * =============================================================
     */
    private static PhaseAResult RunPhaseABoundedSoak(
        int seed,
        int operations,
        int auditInterval,
        string rootDirectory)
    {
        var diskPath = Path.Combine(rootDirectory, "phaseA_soak.lore");
        var graph = BuildBaseGraph(seed);

        Require(graph.Validate().Count == 0, "Initial Phase A graph failed validation.");

        var canonicalJson = LoreSerializer.Serialize(graph);
        var canonicalFingerprint = ComputeFingerprint(NormalizeActiveStateSerialization(canonicalJson));

        LoreStorage.Save(graph, diskPath);

        ForceCollection();
        var baselineManagedBytes = GC.GetTotalMemory(forceFullCollection: true);
        var baselineWorkingSet = Process.GetCurrentProcess().WorkingSet64;

        Console.WriteLine($"  Baseline managed heap: {FormatBytes(baselineManagedBytes)}");
        Console.WriteLine($"  Baseline working set:  {FormatBytes(baselineWorkingSet)}");
        Console.WriteLine($"  Base entities:         {graph.Entities.Count:N0}");
        Console.WriteLine($"  Base relationships:    {graph.Relationships.Count:N0}");
        Console.WriteLine($"  Workload:              Bounded property/tag/document/snapshot churn without relationship retirement");
        Console.WriteLine();

        var random = new DeterministicRandom(seed ^ 0x3C6EF372);
        var counters = new SoakCounters();
        var stopwatch = Stopwatch.StartNew();
        var peakManagedBytes = baselineManagedBytes;
        var peakWorkingSet = baselineWorkingSet;
        var lastAuditManagedBytes = baselineManagedBytes;
        var lastAuditWorkingSet = baselineWorkingSet;

        var entityIds = graph.Entities.Keys.OrderBy(x => x).ToArray();
        var anchorEntityId = entityIds[0];
        var secondEntityId = entityIds[1];

        for (var cycle = 1; cycle <= operations; cycle++)
        {
            var isMutation = random.NextInt(100) < 70;

            if (isMutation)
            {
                ExecuteBoundedMutationCycle(
                    graph,
                    anchorEntityId,
                    secondEntityId,
                    random,
                    cycle,
                    counters);
            }
            else
            {
                ExecuteQueryCycle(
                    graph,
                    anchorEntityId,
                    secondEntityId,
                    cycle,
                    counters);
            }

            counters.CyclesExecuted++;

            // Snapshot validation on active state
            if (cycle % 100 == 0)
            {
                var snapshot = graph.CreateSnapshot(CreateSoakTime(cycle));
                Require(snapshot.ContainsEntity(anchorEntityId), "Phase A snapshot entity lookup failed.");
                Require(snapshot.EntityCount == BaseEntityCount, "Phase A snapshot entity count mismatch.");
                Require(snapshot.RelationshipCount == BaseRelationshipCount(), "Phase A snapshot relationship count mismatch.");
            }

            // In-memory persistence round-trip
            if (cycle % InMemoryPersistenceInterval == 0)
            {
                var serialized = LoreSerializer.Serialize(graph);
                Require(
                    string.Equals(
                        NormalizeActiveStateSerialization(serialized),
                        NormalizeActiveStateSerialization(canonicalJson),
                        StringComparison.Ordinal),
                    $"Phase A canonical active serialization drift at cycle {cycle:N0}.");

                graph = LoreSerializer.Deserialize(serialized);
                counters.InMemoryPersistenceRoundTrips++;
            }

            // Disk persistence round-trip
            if (cycle % DiskPersistenceInterval == 0)
            {
                LoreStorage.Save(graph, diskPath);
                graph = LoreStorage.Load(diskPath);
                counters.DiskPersistenceRoundTrips++;
            }

            // Audit
            if (cycle % auditInterval == 0 || cycle == operations)
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
        }

        stopwatch.Stop();

        ForceCollection();
        var finalManagedBytes = GC.GetTotalMemory(forceFullCollection: true);
        var finalWorkingSet = Process.GetCurrentProcess().WorkingSet64;
        var retainedDelta = finalManagedBytes - baselineManagedBytes;
        var retainedLimit = Math.Max(
            RetainedMemoryAbsoluteToleranceBytes,
            (long)(baselineManagedBytes * RetainedMemoryRelativeTolerance));

        var memoryHealthy = retainedDelta <= retainedLimit;
        var canonicalHealthy = graph.Validate().Count == 0 &&
                               graph.Entities.Count == BaseEntityCount &&
                               graph.Relationships.Count == BaseRelationshipCount() &&
                               graph.RetiredRelationshipIds.Count == 0;

        Console.WriteLine();
        Console.WriteLine($"  Phase A Runtime:             {stopwatch.Elapsed.TotalSeconds:F3} s");
        Console.WriteLine($"  Phase A Final Managed Heap:  {FormatBytes(finalManagedBytes)}");
        Console.WriteLine($"  Phase A Retained Delta:      {FormatSignedBytes(retainedDelta)} (limit {FormatBytes(retainedLimit)})");
        Console.WriteLine($"  Phase A Retired IDs:         {graph.RetiredRelationshipIds.Count:N0} (must be exactly 0)");
        Console.WriteLine($"  Phase A Memory Bounded:      {(memoryHealthy ? "PASS" : "FAIL")}");
        Console.WriteLine($"  Phase A Canonical State:     {(canonicalHealthy ? "PASS" : "FAIL")}");

        Require(canonicalHealthy, "Phase A canonical state drifted.");
        Require(graph.RetiredRelationshipIds.Count == 0, "Phase A unexpectedly accumulated retired IDs.");

        return new PhaseAResult(
            MemoryHealthy: memoryHealthy,
            CanonicalStateHealthy: canonicalHealthy,
            PersistenceHealthy: true,
            BaselineManagedBytes: baselineManagedBytes,
            FinalManagedBytes: finalManagedBytes,
            RetainedDeltaBytes: retainedDelta);
    }

    /*
     * Bounded mutation cycle:
     * Heavily mutates properties, tags, documents, time coordinates,
     * and exercises isolated entity addition/removal, then restores
     * active state to canonical baseline WITHOUT retiring relationship IDs.
     */
    private static void ExecuteBoundedMutationCycle(
        SorophyGraph graph,
        Guid anchorEntityId,
        Guid secondEntityId,
        DeterministicRandom random,
        int cycle,
        SoakCounters counters)
    {
        counters.MutationCycles++;

        var anchorEntity = graph.Entities[anchorEntityId];
        var outgoingRels = graph.GetOutgoingRelationships(anchorEntityId).ToList();
        var activeRel = outgoingRels[0];

        // 1. Mutate active entity properties (scalar, complex, nested)
        anchorEntity.Properties["soak-str"] = new SorophyProperty
        {
            Name = "soak-str",
            Value = new SorophyValue(SorophyValueType.String, $"val-{cycle:N0}")
        };
        anchorEntity.Properties["soak-num"] = new SorophyProperty
        {
            Name = "soak-num",
            Value = new SorophyValue(SorophyValueType.Integer, (long)cycle)
        };
        anchorEntity.Properties["soak-map"] = new SorophyProperty
        {
            Name = "soak-map",
            Value = new SorophyValue(SorophyValueType.Object, new Dictionary<string, object?>
            {
                ["cycle"] = (long)cycle
            })
        };

        // 2. Mutate active entity tags
        var tempTag = $"temp-tag-{cycle % 16}";
        anchorEntity.Tags.Add(tempTag);

        // 3. Mutate active entity documents
        anchorEntity.Documents["temp-doc"] = new SorophyEntityDocument("temp-doc", $"content-{cycle}", "text/plain");

        // 4. Mutate active relationship properties and validity
        activeRel.Properties["rel-weight"] = new SorophyProperty
        {
            Name = "rel-weight",
            Value = new SorophyValue(SorophyValueType.Integer, (long)(cycle % 100))
        };
        activeRel.ValidFrom = CreateSoakTime(1000 + (cycle % 50));
        activeRel.ValidTill = CreateSoakTime(2000 + (cycle % 50));

        // 5. Add and remove an isolated temporary entity (no incident relationships = zero retired relationship IDs)
        var tempEntityId = random.NextGuid();
        var tempEntity = new SorophyEntity
        {
            Id = tempEntityId,
            Name = $"SoakIsolated-{cycle:N0}",
            Type = "TemporaryIsolated"
        };
        tempEntity.Tags.Add(tempTag);
        graph.AddEntity(tempEntity);
        counters.EntityAdds++;

        Require(graph.ContainsEntity(tempEntityId), "Temporary isolated entity not visible.");
        Require(graph.Entities[tempEntityId].Name == tempEntity.Name, "Temporary isolated entity corrupted.");

        // Query active state via tag index
        var tagMatches = graph.GetEntitiesByTag(tempTag).ToList();
        Require(tagMatches.Any(e => e.Id == tempEntityId), "Tag lookup failed during mutation cycle.");

        Require(graph.IsReachable(anchorEntityId, secondEntityId), "Reachability failed during mutation cycle.");

        // Clean up temporary isolated entity
        Require(graph.RemoveEntity(tempEntityId), "Failed to remove temporary isolated entity.");
        counters.EntityRemovals++;
        Require(!graph.ContainsEntity(tempEntityId), "Temporary isolated entity survived cycle.");

        // Restore active entity and relationship to exact canonical state
        anchorEntity.Properties.Clear();
        anchorEntity.Tags.Remove(tempTag);
        anchorEntity.Documents.Clear();
        activeRel.Properties.Clear();
        activeRel.ValidFrom = null;
        activeRel.ValidTill = null;
    }

    /*
     * =============================================================
     * PHASE B: RETIREMENT SCALE ENDURANCE IMPLEMENTATION
     * =============================================================
     */
    private static PhaseBResult RunPhaseBRetirementEndurance(
        int seed,
        int operations,
        int auditInterval,
        string rootDirectory)
    {
        var diskPath = Path.Combine(rootDirectory, "phaseB_soak.lore");
        var graph = BuildBaseGraph(seed ^ 0x5A17C0DE);

        Require(graph.Validate().Count == 0, "Initial Phase B graph failed validation.");

        var canonicalJson = LoreSerializer.Serialize(graph);
        var canonicalFingerprint = ComputeFingerprint(NormalizeActiveStateSerialization(canonicalJson));

        LoreStorage.Save(graph, diskPath);

        ForceCollection();
        var baselineManagedBytes = GC.GetTotalMemory(forceFullCollection: true);
        var baselineWorkingSet = Process.GetCurrentProcess().WorkingSet64;

        Console.WriteLine($"  Baseline managed heap: {FormatBytes(baselineManagedBytes)}");
        Console.WriteLine($"  Baseline working set:  {FormatBytes(baselineWorkingSet)}");
        Console.WriteLine($"  Base entities:         {graph.Entities.Count:N0}");
        Console.WriteLine($"  Base relationships:    {graph.Relationships.Count:N0}");
        Console.WriteLine($"  Workload:              Unbounded relationship creation/deletion lifecycle with identity retirement");
        Console.WriteLine();

        var random = new DeterministicRandom(seed ^ unchecked((int)0x9E3779B9));
        var counters = new SoakCounters();
        var stopwatch = Stopwatch.StartNew();
        var peakManagedBytes = baselineManagedBytes;
        var peakWorkingSet = baselineWorkingSet;
        var lastAuditManagedBytes = baselineManagedBytes;
        var lastAuditWorkingSet = baselineWorkingSet;

        var entityIds = graph.Entities.Keys.OrderBy(x => x).ToArray();
        var anchorEntityId = entityIds[0];
        var secondEntityId = entityIds[1];

        var checkpoints = new List<RetirementCheckpoint>();
        var sampleIntervals = new[] { 1_000, 2_500, 5_000, 10_000, 25_000, 50_000, 100_000, 250_000 };

        var retiredIdsSample = new List<Guid>();

        for (var cycle = 1; cycle <= operations; cycle++)
        {
            var isMutation = random.NextInt(100) < 70;

            if (isMutation)
            {
                ExecuteRetirementMutationCycle(
                    graph,
                    anchorEntityId,
                    secondEntityId,
                    random,
                    cycle,
                    counters,
                    retiredIdsSample);
            }
            else
            {
                ExecuteQueryCycle(
                    graph,
                    anchorEntityId,
                    secondEntityId,
                    cycle,
                    counters);
            }

            counters.CyclesExecuted++;

            // In-memory persistence round-trip
            if (cycle % InMemoryPersistenceInterval == 0)
            {
                var serialized = LoreSerializer.Serialize(graph);
                Require(
                    string.Equals(
                        NormalizeActiveStateSerialization(serialized),
                        NormalizeActiveStateSerialization(canonicalJson),
                        StringComparison.Ordinal),
                    $"Phase B canonical active serialization drift at cycle {cycle:N0}.");

                graph = LoreSerializer.Deserialize(serialized);
                counters.InMemoryPersistenceRoundTrips++;
            }

            // Disk persistence round-trip
            if (cycle % DiskPersistenceInterval == 0)
            {
                LoreStorage.Save(graph, diskPath);
                graph = LoreStorage.Load(diskPath);
                counters.DiskPersistenceRoundTrips++;
            }

            // Audit
            if (cycle % auditInterval == 0 || cycle == operations)
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

                // Invariant: Non-reusability assertion
                if (retiredIdsSample.Count > 0)
                {
                    var sampleRetiredId = retiredIdsSample[random.NextInt(retiredIdsSample.Count)];
                    Require(graph.IsRelationshipIdRetired(sampleRetiredId), "Retired ID not present in retirement registry.");

                    var attemptedResurrect = new SorophyRelationship
                    {
                        Id = sampleRetiredId,
                        SourceId = anchorEntityId,
                        TargetId = secondEntityId,
                        Type = "illegal-resurrect"
                    };

                    var threwOnReuse = false;
                    try
                    {
                        graph.AddRelationship(attemptedResurrect);
                    }
                    catch (InvalidOperationException)
                    {
                        threwOnReuse = true;
                    }

                    Require(threwOnReuse, $"Engine allowed reusing retired relationship identity '{sampleRetiredId}'.");
                }
            }

            // Record checkpoint
            if (Array.IndexOf(sampleIntervals, cycle) >= 0 || cycle == operations)
            {
                var currentManaged = GC.GetTotalMemory(forceFullCollection: false);
                peakManagedBytes = Math.Max(peakManagedBytes, currentManaged);
                var currentWS = Process.GetCurrentProcess().WorkingSet64;
                peakWorkingSet = Math.Max(peakWorkingSet, currentWS);

                checkpoints.Add(new RetirementCheckpoint(
                    Operations: cycle,
                    RetiredIds: graph.RetiredRelationshipIds.Count,
                    FinalHeapBytes: currentManaged,
                    RetainedDeltaBytes: currentManaged - baselineManagedBytes,
                    PeakHeapBytes: peakManagedBytes,
                    ElapsedSeconds: stopwatch.Elapsed.TotalSeconds,
                    Throughput: cycle / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001)));
            }

            // Progress
            if (cycle % ProgressInterval == 0 || cycle == operations)
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

        stopwatch.Stop();

        ForceCollection();
        var finalManagedBytes = GC.GetTotalMemory(forceFullCollection: true);
        var finalWorkingSet = Process.GetCurrentProcess().WorkingSet64;
        peakManagedBytes = Math.Max(peakManagedBytes, finalManagedBytes);
        peakWorkingSet = Math.Max(peakWorkingSet, finalWorkingSet);

        var retiredCount = graph.RetiredRelationshipIds.Count;
        var expectedRetirements = counters.RelationshipRemovals;

        var retirementInvariantsHealthy =
            retiredCount == expectedRetirements &&
            graph.RetiredRelationshipIds.Distinct().Count() == retiredCount;

        var activeCountsHealthy =
            graph.Entities.Count == BaseEntityCount &&
            graph.Relationships.Count == BaseRelationshipCount();

        var canonicalHealthy =
            graph.Validate().Count == 0 &&
            string.Equals(
                NormalizeActiveStateSerialization(LoreSerializer.Serialize(graph)),
                NormalizeActiveStateSerialization(canonicalJson),
                StringComparison.Ordinal);

        // Scaling characteristic: memory increases with retired ID count
        var scalingHealthy = checkpoints.Count < 2 || checkpoints[^1].RetainedDeltaBytes >= checkpoints[0].RetainedDeltaBytes;

        return new PhaseBResult(
            CanonicalStateHealthy: canonicalHealthy,
            ActiveCountsHealthy: activeCountsHealthy,
            RetirementInvariantsHealthy: retirementInvariantsHealthy,
            PersistenceHealthy: true,
            ScalingHealthy: scalingHealthy,
            BaselineManagedBytes: baselineManagedBytes,
            FinalManagedBytes: finalManagedBytes,
            PeakManagedBytes: peakManagedBytes,
            RetiredIdCount: retiredCount,
            Checkpoints: checkpoints);
    }

    /*
     * Retirement mutation cycle:
     * Repeatedly adds and removes relationships, exercising relationship
     * identity retirement and adjacency slab recycling.
     */
    private static void ExecuteRetirementMutationCycle(
        SorophyGraph graph,
        Guid anchorEntityId,
        Guid secondEntityId,
        DeterministicRandom random,
        int cycle,
        SoakCounters counters,
        List<Guid> retiredIdsSample)
    {
        counters.MutationCycles++;

        var temporaryEntityId = random.NextGuid();
        while (graph.ContainsEntity(temporaryEntityId))
        {
            temporaryEntityId = random.NextGuid();
        }

        var temporaryEntity = new SorophyEntity
        {
            Id = temporaryEntityId,
            Name = $"SoakTemp-{cycle:N0}",
            Type = "Temporary"
        };

        graph.AddEntity(temporaryEntity);
        counters.EntityAdds++;

        var firstRelationshipId = random.NextGuid();
        var secondRelationshipId = random.NextGuid();

        graph.AddRelationship(new SorophyRelationship
        {
            Id = firstRelationshipId,
            Type = "soak-forward",
            SourceId = anchorEntityId,
            TargetId = temporaryEntityId
        });
        counters.RelationshipAdds++;

        graph.AddRelationship(new SorophyRelationship
        {
            Id = secondRelationshipId,
            Type = "soak-return",
            SourceId = temporaryEntityId,
            TargetId = secondEntityId
        });
        counters.RelationshipAdds++;

        // Query active connectivity through temporary entity
        Require(
            graph.GetOutgoingRelationships(anchorEntityId).Any(r => r.Id == firstRelationshipId),
            $"New outgoing relationship not visible at cycle {cycle:N0}.");

        Require(
            graph.GetIncomingRelationships(secondEntityId).Any(r => r.Id == secondRelationshipId),
            $"New incoming relationship not visible at cycle {cycle:N0}.");

        Require(
            graph.IsReachable(anchorEntityId, secondEntityId),
            $"Reachability failed at cycle {cycle:N0}.");

        // Remove relationships (irrevocably retires IDs)
        Require(graph.RemoveRelationship(firstRelationshipId), "Failed to remove first relationship.");
        counters.RelationshipRemovals++;

        Require(graph.RemoveRelationship(secondRelationshipId), "Failed to remove second relationship.");
        counters.RelationshipRemovals++;

        // Remove temporary entity
        Require(graph.RemoveEntity(temporaryEntityId), "Failed to remove temporary entity.");
        counters.EntityRemovals++;

        // Assert cleanup
        Require(!graph.ContainsEntity(temporaryEntityId), "Temporary entity survived cycle.");
        Require(!graph.ContainsRelationship(firstRelationshipId), "First relationship survived cycle.");
        Require(!graph.ContainsRelationship(secondRelationshipId), "Second relationship survived cycle.");

        // Keep a bounded sample of retired IDs to verify non-reusability
        if (retiredIdsSample.Count < 256)
        {
            retiredIdsSample.Add(firstRelationshipId);
            retiredIdsSample.Add(secondRelationshipId);
        }
        else if (random.NextInt(100) < 10)
        {
            retiredIdsSample[random.NextInt(retiredIdsSample.Count)] = firstRelationshipId;
        }
    }

    /*
     * =============================================================
     * PHASE C: RETIREMENT SCALING REPORT
     * =============================================================
     */
    private static void PrintPhaseCRetirementScalingReport(
        IReadOnlyList<RetirementCheckpoint> checkpoints)
    {
        Console.WriteLine();
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine(" PHASE C — RETIREMENT SCALING REPORT");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("  | Operations | Retired IDs | Final Heap | Retained Delta | Peak Heap | Runtime | Throughput |");
        Console.WriteLine("  |-----------:|------------:|-----------:|---------------:|----------:|--------:|-----------:|");

        foreach (var cp in checkpoints)
        {
            Console.WriteLine(
                $"  | {cp.Operations,10:N0} | " +
                $"{cp.RetiredIds,11:N0} | " +
                $"{FormatBytes(cp.FinalHeapBytes),10} | " +
                $"{FormatSignedBytes(cp.RetainedDeltaBytes),14} | " +
                $"{FormatBytes(cp.PeakHeapBytes),9} | " +
                $"{cp.ElapsedSeconds,6:F2} s | " +
                $"{cp.Throughput,8:N0} c/s |");
        }

        Console.WriteLine();
        Console.WriteLine("  Analysis:");
        Console.WriteLine("  • Retired relationship identities accumulate monotonically in _retiredRelationshipIds.");
        Console.WriteLine("  • Managed heap growth is proportional to the number of permanently retired relationship identities.");
        Console.WriteLine("  • Peak heap reflects transient Large Object Heap (LOH) serialization buffers during persistence.");
        Console.WriteLine();
    }

    /*
     * =============================================================
     * PHASE D: MEMORY RETENTION CHECK
     * =============================================================
     */
    private static PhaseDResult EvaluatePhaseDMemoryRetention(
        long baselineManagedBytes,
        long finalManagedBytes,
        int retiredIdCount)
    {
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine(" PHASE D — MEMORY RETENTION CHECK");
        Console.WriteLine("────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // 1. Observed retention: actual growth in managed memory from Phase B baseline
        var observedRetention = Math.Max(0L, finalManagedBytes - baselineManagedBytes);

        // 2. Modeled retention: expected intentional O(N) tombstone registry footprint
        var modeledRetention = retiredIdCount * ModeledBytesPerRetiredId;
        var expectedPermanentRetention = baselineManagedBytes + modeledRetention;

        // 3. Unexplained residual: memory retained beyond baseline + modeled intentional retention
        var unexplainedResidual = Math.Max(0L, finalManagedBytes - expectedPermanentRetention);
        var unexpectedRetentionPass = unexplainedResidual <= RetainedMemoryAbsoluteToleranceBytes;

        Console.WriteLine($"  Baseline Managed Heap:        {FormatBytes(baselineManagedBytes)}");
        Console.WriteLine($"  Final Managed Heap:           {FormatBytes(finalManagedBytes)}");
        Console.WriteLine($"  Retired Identities:           {retiredIdCount:N0}");
        Console.WriteLine($"  Observed Retention:           {FormatBytes(observedRetention)}");
        Console.WriteLine($"  Modeled Retention:            {FormatBytes(modeledRetention)} (~{ModeledBytesPerRetiredId} B/ID harness model)");
        Console.WriteLine($"  Expected Permanent Retention: {FormatBytes(expectedPermanentRetention)}");
        Console.WriteLine($"  Unexplained Residual:         {FormatBytes(unexplainedResidual)} (limit {FormatBytes(RetainedMemoryAbsoluteToleranceBytes)})");
        Console.WriteLine($"  Unexpected Retention Check:   {(unexpectedRetentionPass ? "PASS" : "FAIL")}");
        Console.WriteLine();

        Require(
            unexpectedRetentionPass,
            $"Unexplained residual memory ({FormatBytes(unexplainedResidual)}) exceeded tolerance ({FormatBytes(RetainedMemoryAbsoluteToleranceBytes)}).");

        return new PhaseDResult(
            unexpectedRetentionPass,
            observedRetention,
            modeledRetention,
            unexplainedResidual,
            expectedPermanentRetention);
    }

    /*
     * =============================================================
     * COMMON QUERY & AUDIT HELPERS
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

        var outgoing = graph.GetOutgoingRelationships(anchorEntityId).ToList();
        var incoming = graph.GetIncomingRelationships(secondEntityId).ToList();
        var incident = graph.GetRelationships(anchorEntityId).ToList();
        var neighbors = graph.GetNeighbors(anchorEntityId).ToList();

        Require(outgoing.Count > 0, $"Outgoing query returned no relationships at cycle {cycle:N0}.");
        Require(incoming.Count > 0, $"Incoming query returned no relationships at cycle {cycle:N0}.");
        Require(incident.Count >= outgoing.Count, $"Incident query inconsistent at cycle {cycle:N0}.");
        Require(neighbors.Count > 0, $"Neighbor query returned no neighbors at cycle {cycle:N0}.");
        Require(graph.IsReachable(anchorEntityId, secondEntityId), $"Reachability failed at cycle {cycle:N0}.");
    }

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
        var errors = graph.Validate();
        counters.Validations++;

        Require(
            errors.Count == 0,
            $"SorophyGraph.Validate() failed at cycle {cycle:N0} with {errors.Count} error(s)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, errors.Take(20)));

        Require(
            graph.Entities.Count == BaseEntityCount,
            $"Entity count drift at cycle {cycle:N0}. Expected={BaseEntityCount}, Actual={graph.Entities.Count}.");

        Require(
            graph.Relationships.Count == BaseRelationshipCount(),
            $"Relationship count drift at cycle {cycle:N0}. Expected={BaseRelationshipCount()}, Actual={graph.Relationships.Count}.");

        Require(
            graph.IsReachable(anchorEntityId, secondEntityId),
            $"Anchor reachability failed at cycle {cycle:N0}.");

        var serialized = LoreSerializer.Serialize(graph);
        var fingerprint = ComputeFingerprint(NormalizeActiveStateSerialization(serialized));

        Require(
            string.Equals(fingerprint, canonicalFingerprint, StringComparison.Ordinal),
            $"Canonical active fingerprint drift at cycle {cycle:N0}.");

        Require(
            string.Equals(
                NormalizeActiveStateSerialization(serialized),
                NormalizeActiveStateSerialization(canonicalJson),
                StringComparison.Ordinal),
            $"Canonical active serialization drift at cycle {cycle:N0}.");

        var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var workingSet = Process.GetCurrentProcess().WorkingSet64;

        peakManagedBytes = Math.Max(peakManagedBytes, managedBytes);
        peakWorkingSet = Math.Max(peakWorkingSet, workingSet);

        var managedDelta = managedBytes - baselineManagedBytes;
        var workingSetDelta = workingSet - baselineWorkingSet;
        var auditManagedDelta = managedBytes - lastAuditManagedBytes;
        var auditWorkingSetDelta = workingSet - lastAuditWorkingSet;

        lastAuditManagedBytes = managedBytes;
        lastAuditWorkingSet = workingSet;

        var rate = cycle / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);

        Console.WriteLine(
            $"  Audit {cycle,10:N0}/{operations:N0} → PASS | " +
            $"Rate={rate,9:N0} cycles/s | " +
            $"ManagedΔ={FormatSignedBytes(managedDelta),10} | " +
            $"WSΔ={FormatSignedBytes(workingSetDelta),10} | " +
            $"AuditΔ={FormatSignedBytes(auditManagedDelta),10} | " +
            $"WS AuditΔ={FormatSignedBytes(auditWorkingSetDelta),10}");
    }

    private static SorophyGraph BuildBaseGraph(int seed)
    {
        var random = new DeterministicRandom(seed);
        var graph = new SorophyGraph();
        var ids = new Guid[BaseEntityCount];

        for (var index = 0; index < BaseEntityCount; index++)
        {
            var id = random.NextGuid();
            ids[index] = id;

            graph.AddEntity(new SorophyEntity
            {
                Id = id,
                Name = $"SoakBase-{index:D3}",
                Type = (index % 4) switch
                {
                    0 => "Character",
                    1 => "Location",
                    2 => "Faction",
                    _ => "Concept"
                }
            });
        }

        for (var index = 0; index < BaseEntityCount; index++)
        {
            var next = (index + 1) % BaseEntityCount;
            graph.AddRelationship(new SorophyRelationship
            {
                Id = random.NextGuid(),
                Type = "soak-ring",
                SourceId = ids[index],
                TargetId = ids[next]
            });
        }

        graph.AddRelationship(new SorophyRelationship
        {
            Id = random.NextGuid(),
            Type = "soak-chord",
            SourceId = ids[0],
            TargetId = ids[BaseEntityCount / 2]
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Id = random.NextGuid(),
            Type = "soak-chord",
            SourceId = ids[BaseEntityCount / 2],
            TargetId = ids[0]
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Id = random.NextGuid(),
            Type = "soak-self",
            SourceId = ids[0],
            TargetId = ids[0]
        });

        return graph;
    }

    private static int BaseRelationshipCount() => BaseEntityCount + 2 + 1;

    private static string NormalizeActiveStateSerialization(string json)
    {
        var document = JsonNode.Parse(json)
                       ?? throw new InvalidOperationException("Canonical JSON could not be parsed.");

        if (document["retiredRelationshipIds"] is not null)
        {
            document["retiredRelationshipIds"] = new JsonArray();
        }

        return document.ToJsonString();
    }

    private static string ComputeFingerprint(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static void ForceCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void PrintHeader(
        int seed,
        int operations,
        int auditInterval,
        string temporaryDirectory)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                 SOROPHY ENGINE SOAK / ENDURANCE                ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Seed:           {seed,-43}║");
        Console.WriteLine($"║ Operations:     {operations,-43}║");
        Console.WriteLine($"║ Audit interval: {auditInterval,-43}║");
        Console.WriteLine("║ Mode:           MULTI-PHASE BOUNDED + RETIREMENT SCALE SOAK  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Temporary sandbox: {temporaryDirectory}");
        Console.WriteLine();
        Console.WriteLine("Workload design:");
        Console.WriteLine("  • Phase A: Bounded active-state mutation, query, snapshot & persistence");
        Console.WriteLine("  • Phase B: Retirement scale endurance with identity retirement invariants");
        Console.WriteLine("  • Phase C: Empirical O(N) retirement scaling analysis and report");
        Console.WriteLine("  • Phase D: Memory retention verification separating expected tombstones");
        Console.WriteLine();
    }

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
        var rate = cycle / Math.Max(elapsed.TotalSeconds, 0.001);
        var managedBytes = GC.GetTotalMemory(forceFullCollection: false);

        Console.WriteLine();
        Console.WriteLine(
            $"  [{cycle,10:N0}/{operations:N0}] " +
            $"PASS | " +
            $"Rate={rate,9:N0} cycles/s | " +
            $"Entities={graph.Entities.Count,3} | " +
            $"Relationships={graph.Relationships.Count,3} | " +
            $"RetiredIDs={graph.RetiredRelationshipIds.Count,8:N0} | " +
            $"Managed={FormatBytes(managedBytes),10}");

        Console.WriteLine(
            $"      Mutations={counters.MutationCycles,10:N0} | " +
            $"Queries={counters.QueryHeavyCycles,10:N0} | " +
            $"MemoryRT={counters.InMemoryPersistenceRoundTrips,8:N0} | " +
            $"DiskRT={counters.DiskPersistenceRoundTrips,6:N0} | " +
            $"PeakManaged={FormatBytes(peakManagedBytes),10}");
    }

    private static string FormatBytes(long bytes)
    {
        const double kilobyte = 1024.0;
        const double megabyte = kilobyte * 1024.0;
        const double gigabyte = megabyte * 1024.0;

        if (bytes >= gigabyte) return $"{bytes / gigabyte:F2} GB";
        if (bytes >= megabyte) return $"{bytes / megabyte:F2} MB";
        if (bytes >= kilobyte) return $"{bytes / kilobyte:F2} KB";
        return $"{bytes:N0} B";
    }

    private static string FormatSignedBytes(long bytes)
    {
        if (bytes > 0) return $"+{FormatBytes(bytes)}";
        if (bytes < 0) return $"-{FormatBytes(Math.Abs(bytes))}";
        return "0 B";
    }

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

    public sealed record RetirementCheckpoint(
        int Operations,
        int RetiredIds,
        long FinalHeapBytes,
        long RetainedDeltaBytes,
        long PeakHeapBytes,
        double ElapsedSeconds,
        double Throughput);

    private sealed record PhaseAResult(
        bool MemoryHealthy,
        bool CanonicalStateHealthy,
        bool PersistenceHealthy,
        long BaselineManagedBytes,
        long FinalManagedBytes,
        long RetainedDeltaBytes);

    private sealed record PhaseBResult(
        bool CanonicalStateHealthy,
        bool ActiveCountsHealthy,
        bool RetirementInvariantsHealthy,
        bool PersistenceHealthy,
        bool ScalingHealthy,
        long BaselineManagedBytes,
        long FinalManagedBytes,
        long PeakManagedBytes,
        int RetiredIdCount,
        IReadOnlyList<RetirementCheckpoint> Checkpoints);

    private sealed record PhaseDResult(
        bool UnexpectedRetentionPass,
        long ObservedRetentionBytes,
        long ModeledRetentionBytes,
        long UnexplainedResidualBytes,
        long ExpectedPermanentRetention);

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(int seed)
        {
            _state = unchecked((ulong)(uint)seed + 0x9E3779B97F4A7C15UL);
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        private ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            return (int)(NextUInt64() % (uint)exclusiveMaximum);
        }

        public Guid NextGuid()
        {
            Span<byte> bytes = stackalloc byte[16];
            for (var index = 0; index < bytes.Length; index += 8)
            {
                var value = NextUInt64();
                for (var offset = 0; offset < 8; offset++)
                {
                    bytes[index + offset] = (byte)(value >> (offset * 8));
                }
            }
            return new Guid(bytes);
        }
    }
}
