using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Storage;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class CrashRecoveryTests
{
    private const int DefaultRecoveryCycles = 25;
    private const int RecentOperationCapacity = 32;

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
                "SorophyEngineCrashRecovery",
                $"{seed}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            rootDirectory);

        var results =
            new List<CheckResult>();

        try
        {
            PrintHeader(
                seed,
                operations,
                auditInterval,
                rootDirectory);

            /*
             * =========================================================
             * BASELINE RECOVERY CONTRACT
             * =========================================================
             */

            RunCheck(
                results,
                "Baseline save/load",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            32,
                            generation: 1);

                    var path =
                        Path.Combine(
                            rootDirectory,
                            "baseline.lore");

                    LoreStorage.Save(
                        graph,
                        path);

                    var restored =
                        LoreStorage.Load(
                            path);

                    RequireGraphMatches(
                        graph,
                        restored,
                        "Baseline save/load");

                    return
                        $"Entities={restored.Entities.Count}, " +
                        $"Relationships={restored.Relationships.Count}";
                });

            RunCheck(
                results,
                "Missing file rejection",
                () =>
                {
                    var path =
                        Path.Combine(
                            rootDirectory,
                            "missing.lore");

                    try
                    {
                        _ =
                            LoreStorage.Load(
                                path);
                    }
                    catch (FileNotFoundException)
                    {
                        return
                            "FileNotFoundException correctly raised";
                    }

                    throw new InvalidOperationException(
                        "Missing file did not raise FileNotFoundException.");
                });

            RunCheck(
                results,
                "Empty file rejection",
                () =>
                {
                    var path =
                        Path.Combine(
                            rootDirectory,
                            "empty.lore");

                    File.WriteAllText(
                        path,
                        string.Empty);

                    RequireLoadFails(
                        path,
                        "Empty lore file was unexpectedly accepted.");

                    return
                        "Empty document rejected";
                });

            RunCheck(
                results,
                "Truncated file rejection",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            64,
                            generation: 2);

                    var json =
                        LoreSerializer.Serialize(
                            graph);

                    var truncatedLength =
                        Math.Max(
                            1,
                            json.Length / 2);

                    var path =
                        Path.Combine(
                            rootDirectory,
                            "truncated.lore");

                    File.WriteAllText(
                        path,
                        json[..truncatedLength]);

                    RequireLoadFails(
                        path,
                        "Truncated JSON was unexpectedly accepted.");

                    return
                        $"Truncated {truncatedLength:N0}/{json.Length:N0} chars";
                });

            RunCheck(
                results,
                "Interrupted write rejection",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            128,
                            generation: 3);

                    var json =
                        LoreSerializer.Serialize(
                            graph);

                    var partialLength =
                        Math.Max(
                            1,
                            json.Length / 3);

                    var path =
                        Path.Combine(
                            rootDirectory,
                            "interrupted.lore");

                    File.WriteAllText(
                        path,
                        json[..partialLength]);

                    RequireLoadFails(
                        path,
                        "Interrupted-write candidate was unexpectedly accepted.");

                    return
                        $"Partial document {partialLength:N0}/{json.Length:N0} chars";
                });

            RunCheck(
                results,
                "Corrupted document rejection",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            128,
                            generation: 4);

                    var json =
                        LoreSerializer.Serialize(
                            graph);

                    var bytes =
                        Encoding.UTF8.GetBytes(
                            json);

                    var openingBrace =
                        Array.IndexOf(
                            bytes,
                            (byte)'{');

                    Require(
                        openingBrace >= 0,
                        "Could not locate JSON opening delimiter.");

                    bytes[openingBrace] =
                        (byte)'[';

                    var path =
                        Path.Combine(
                            rootDirectory,
                            "corrupted.lore");

                    File.WriteAllBytes(
                        path,
                        bytes);

                    RequireLoadFails(
                        path,
                        "Structurally corrupted JSON was unexpectedly accepted.");

                    return
                        $"Mutated structural byte at offset {openingBrace:N0}";
                });

            RunCheck(
                results,
                "Invalid endpoint rejection",
                () =>
                {
                    var sourceId =
                        Guid.NewGuid();

                    var relationshipId =
                        Guid.NewGuid();

                    var missingTargetId =
                        Guid.NewGuid();

                    var json =
                        $$"""
                        {
                          "formatVersion": 1,
                          "entities": [
                            {
                              "id": "{{sourceId}}",
                              "name": "Source",
                              "type": "Entity",
                              "properties": {}
                            }
                          ],
                          "relationships": [
                            {
                              "id": "{{relationshipId}}",
                              "type": "broken",
                              "sourceId": "{{sourceId}}",
                              "targetId": "{{missingTargetId}}",
                              "properties": {}
                            }
                          ]
                        }
                        """;

                    var path =
                        Path.Combine(
                            rootDirectory,
                            "invalid-endpoint.lore");

                    File.WriteAllText(
                        path,
                        json);

                    RequireLoadFails(
                        path,
                        "Invalid relationship endpoint was unexpectedly accepted.");

                    return
                        "Missing relationship endpoint rejected";
                });

            RunCheck(
                results,
                "Last-known-good recovery",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            256,
                            generation: 5);

                    var currentPath =
                        Path.Combine(
                            rootDirectory,
                            "current.lore");

                    var backupPath =
                        Path.Combine(
                            rootDirectory,
                            "current.lore.bak");

                    LoreStorage.Save(
                        graph,
                        currentPath);

                    File.Copy(
                        currentPath,
                        backupPath,
                        overwrite: true);

                    var validJson =
                        File.ReadAllText(
                            currentPath);

                    File.WriteAllText(
                        currentPath,
                        validJson[..Math.Max(
                            1,
                            validJson.Length / 4)]);

                    RequireLoadFails(
                        currentPath,
                        "Damaged current document was unexpectedly accepted.");

                    File.Copy(
                        backupPath,
                        currentPath,
                        overwrite: true);

                    var recovered =
                        LoreStorage.Load(
                            currentPath);

                    RequireGraphMatches(
                        graph,
                        recovered,
                        "Last-known-good recovery");

                    return
                        $"Recovered {recovered.Entities.Count} entities / " +
                        $"{recovered.Relationships.Count} relationships";
                });

            RunCheck(
                results,
                "Repeated overwrite correctness",
                () =>
                {
                    var path =
                        Path.Combine(
                            rootDirectory,
                            "overwrite.lore");

                    var firstGraph =
                        BuildGraph(
                            seed,
                            20,
                            generation: 6);

                    var secondGraph =
                        BuildGraph(
                            seed + 1,
                            73,
                            generation: 7);

                    LoreStorage.Save(
                        firstGraph,
                        path);

                    LoreStorage.Save(
                        secondGraph,
                        path);

                    var restored =
                        LoreStorage.Load(
                            path);

                    RequireGraphMatches(
                        secondGraph,
                        restored,
                        "Repeated overwrite");

                    return
                        $"Final state={restored.Entities.Count} entities / " +
                        $"{restored.Relationships.Count} relationships";
                });

            RunCheck(
                results,
                "Repeated corruption/recovery",
                () =>
                {
                    var graph =
                        BuildGraph(
                            seed,
                            128,
                            generation: 8);

                    var currentPath =
                        Path.Combine(
                            rootDirectory,
                            "recovery-loop.lore");

                    var backupPath =
                        Path.Combine(
                            rootDirectory,
                            "recovery-loop.lore.bak");

                    LoreStorage.Save(
                        graph,
                        currentPath);

                    File.Copy(
                        currentPath,
                        backupPath,
                        overwrite: true);

                    var knownGood =
                        File.ReadAllText(
                            backupPath);

                    for (var round = 0;
                         round < DefaultRecoveryCycles;
                         round++)
                    {
                        var damaged =
                            CreateDamagedDocument(
                                knownGood,
                                round);

                        File.WriteAllText(
                            currentPath,
                            damaged);

                        RequireLoadFails(
                            currentPath,
                            $"Corruption round {round + 1} was unexpectedly accepted.");

                        File.Copy(
                            backupPath,
                            currentPath,
                            overwrite: true);

                        var recovered =
                            LoreStorage.Load(
                                currentPath);

                        RequireGraphMatches(
                            graph,
                            recovered,
                            $"Recovery round {round + 1}");
                    }

                    return
                        $"{DefaultRecoveryCycles} corruption/recovery rounds";
                });

            /*
             * =========================================================
             * MILLION-OPERATION ENDURANCE
             * =========================================================
             */

            Console.WriteLine();

            Console.WriteLine(
                "════════════════════════════════════════════════════════════");

            Console.WriteLine(
                "             MILLION-OPERATION RECOVERY ENDURANCE");

            Console.WriteLine(
                "════════════════════════════════════════════════════════════");

            var endurance =
                RunEndurance(
                    seed,
                    operations,
                    auditInterval,
                    rootDirectory);

            var endurancePassed =
                endurance.OperationsExecuted ==
                    operations &&
                endurance.OperationsPassed ==
                    operations;

            Console.WriteLine();

            Console.WriteLine(
                "ENDURANCE RESULT");

            Console.WriteLine(
                $"  Operations executed:       {endurance.OperationsExecuted:N0}");

            Console.WriteLine(
                $"  Operations passed:         {endurance.OperationsPassed:N0}");

            Console.WriteLine(
                $"  In-memory valid loads:     {endurance.InMemoryValidLoads:N0}");

            Console.WriteLine(
                $"  Truncation attacks:        {endurance.TruncationAttacks:N0}");

            Console.WriteLine(
                $"  Corruption attacks:        {endurance.CorruptionAttacks:N0}");

            Console.WriteLine(
                $"  Invalid endpoint attacks:  {endurance.InvalidEndpointAttacks:N0}");

            Console.WriteLine(
                $"  Empty-document attacks:    {endurance.EmptyDocumentAttacks:N0}");

            Console.WriteLine(
                $"  Disk-backed valid loads:   {endurance.DiskValidLoads:N0}");

            Console.WriteLine(
                $"  Disk-backed recoveries:    {endurance.DiskRecoveries:N0}");

            Console.WriteLine(
                $"  Total fault injections:    {endurance.FaultInjections:N0}");

            Console.WriteLine(
                $"  Elapsed:                   {endurance.Elapsed.TotalSeconds:F3} s");

            Console.WriteLine(
                $"  Throughput:                {endurance.OperationsPerSecond:N0} ops/s");

            Console.WriteLine();

            Console.WriteLine(
                $"  {(endurancePassed ? "PASS" : "FAIL")}  " +
                $"Million-operation endurance");

            /*
             * =========================================================
             * FINAL SCOREBOARD
             * =========================================================
             */

            var elapsed =
                Stopwatch.GetElapsedTime(
                    started);

            var baselinePassed =
                results.Count(
                    static result =>
                        result.Passed);

            var baselineTotal =
                results.Count;

            var totalChecks =
                baselineTotal + 1;

            var passedChecks =
                baselinePassed +
                (endurancePassed ? 1 : 0);

            Console.WriteLine();

            Console.WriteLine(
                "════════════════════════════════════════════════════════════════");

            Console.WriteLine(
                "                 CRASH / RECOVERY SUMMARY");

            Console.WriteLine(
                "════════════════════════════════════════════════════════════════");

            Console.WriteLine();

            foreach (var result in results)
            {
                var status =
                    result.Passed
                        ? "PASS"
                        : "FAIL";

                Console.WriteLine(
                    $"  {status,-4}  " +
                    $"{result.Name,-34} " +
                    $"{result.Detail}");
            }

            Console.WriteLine();

            var enduranceStatus =
                endurancePassed
                    ? "PASS"
                    : "FAIL";

            Console.WriteLine(
                $"  {enduranceStatus,-4}  " +
                $"Million-operation endurance   " +
                $"{endurance.OperationsPassed:N0}/{operations:N0}");

            Console.WriteLine();

            Console.WriteLine(
                $"Checks passed:       {passedChecks}/{totalChecks}");

            Console.WriteLine(
                $"Elapsed:             {elapsed.TotalSeconds:F3} s");

            Console.WriteLine();

            if (passedChecks == totalChecks)
            {
                Console.WriteLine(
                    "                     100% PASS");

                Console.WriteLine(
                    "STATUS: CRASH / RECOVERY TORTURE VERIFIED");

                Console.WriteLine();

                return 0;
            }

            var percentage =
                passedChecks * 100.0 /
                Math.Max(
                    1,
                    totalChecks);

            Console.WriteLine(
                $"                     {percentage:F1}% PASS");

            Console.WriteLine(
                "STATUS: CRASH / RECOVERY TORTURE FAILED");

            Console.WriteLine();

            return 1;
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
                 * Cleanup failure must never hide the actual result.
                 */
            }
        }
    }

    /*
     * =============================================================
     * MILLION-OPERATION ENDURANCE
     * =============================================================
     */

    private static EnduranceResult RunEndurance(
        int seed,
        int operations,
        int auditInterval,
        string rootDirectory)
    {
        var random =
            new DeterministicRandom(
                seed);

        var graph =
            BuildGraph(
                unchecked(
                    seed ^
                    0x13579BDF),
                16,
                generation: 100);

        var goodJson =
            LoreSerializer.Serialize(
                graph);

        var truncatedJson =
            goodJson[..Math.Max(
                1,
                goodJson.Length / 2)];

        var corruptedJson =
            CreateDamagedDocument(
                goodJson,
                5);

        var invalidEndpointJson =
            BuildInvalidEndpointDocument();

        var goodPath =
            Path.Combine(
                rootDirectory,
                "endurance-good.lore");

        var currentPath =
            Path.Combine(
                rootDirectory,
                "endurance-current.lore");

        var backupPath =
            Path.Combine(
                rootDirectory,
                "endurance-backup.lore");

        var truncatedPath =
            Path.Combine(
                rootDirectory,
                "endurance-truncated.lore");

        var corruptedPath =
            Path.Combine(
                rootDirectory,
                "endurance-corrupted.lore");

        var emptyPath =
            Path.Combine(
                rootDirectory,
                "endurance-empty.lore");

        var invalidEndpointPath =
            Path.Combine(
                rootDirectory,
                "endurance-invalid-endpoint.lore");

        /*
         * Prepare disk fixtures once.
         */
        LoreStorage.Save(
            graph,
            goodPath);

        File.Copy(
            goodPath,
            currentPath,
            overwrite: true);

        File.Copy(
            goodPath,
            backupPath,
            overwrite: true);

        File.WriteAllText(
            truncatedPath,
            truncatedJson);

        File.WriteAllText(
            corruptedPath,
            corruptedJson);

        File.WriteAllText(
            emptyPath,
            string.Empty);

        File.WriteAllText(
            invalidEndpointPath,
            invalidEndpointJson);

        var counters =
            new EnduranceCounters();

        var recent =
            new OperationKind[
                RecentOperationCapacity];

        var stopwatch =
            Stopwatch.StartNew();

        for (var operationNumber = 1;
             operationNumber <= operations;
             operationNumber++)
        {
            var roll =
                random.NextInt(
                    100);

            var kind =
                roll switch
                {
                    < 40 =>
                        OperationKind.InMemoryValid,

                    < 60 =>
                        OperationKind.Truncated,

                    < 75 =>
                        OperationKind.Corrupted,

                    < 85 =>
                        OperationKind.InvalidEndpoint,

                    < 90 =>
                        OperationKind.EmptyDocument,

                    < 95 =>
                        OperationKind.DiskValid,

                    _ =>
                        OperationKind.DiskRecovery
                };

            recent[
                (operationNumber - 1) %
                recent.Length] =
                kind;

            try
            {
                switch (kind)
                {
                    case OperationKind.InMemoryValid:
                    {
                        var restored =
                            LoreSerializer.Deserialize(
                                goodJson);

                        RequireGraphMatches(
                            graph,
                            restored,
                            $"In-memory valid load at operation {operationNumber}");

                        counters.InMemoryValidLoads++;

                        break;
                    }

                    case OperationKind.Truncated:
                    {
                        RequireDeserializeFails(
                            truncatedJson,
                            $"Truncation attack accepted at operation {operationNumber}.");

                        counters.TruncationAttacks++;
                        counters.FaultInjections++;

                        break;
                    }

                    case OperationKind.Corrupted:
                    {
                        RequireDeserializeFails(
                            corruptedJson,
                            $"Corruption attack accepted at operation {operationNumber}.");

                        counters.CorruptionAttacks++;
                        counters.FaultInjections++;

                        break;
                    }

                    case OperationKind.InvalidEndpoint:
                    {
                        RequireDeserializeFails(
                            invalidEndpointJson,
                            $"Invalid endpoint accepted at operation {operationNumber}.");

                        counters.InvalidEndpointAttacks++;
                        counters.FaultInjections++;

                        break;
                    }

                    case OperationKind.EmptyDocument:
                    {
                        RequireDeserializeFails(
                            string.Empty,
                            $"Empty document accepted at operation {operationNumber}.");

                        counters.EmptyDocumentAttacks++;
                        counters.FaultInjections++;

                        break;
                    }

                    case OperationKind.DiskValid:
                    {
                        var restored =
                            LoreStorage.Load(
                                goodPath);

                        RequireGraphMatches(
                            graph,
                            restored,
                            $"Disk valid load at operation {operationNumber}");

                        counters.DiskValidLoads++;

                        break;
                    }

                    case OperationKind.DiskRecovery:
                    {
                        var damagedPath =
                            SelectDamagePath(
                                random,
                                truncatedPath,
                                corruptedPath,
                                emptyPath,
                                invalidEndpointPath);

                        File.Copy(
                            damagedPath,
                            currentPath,
                            overwrite: true);

                        RequireLoadFails(
                            currentPath,
                            $"Damaged candidate accepted at operation {operationNumber}.");

                        counters.FaultInjections++;

                        File.Copy(
                            backupPath,
                            currentPath,
                            overwrite: true);

                        var recovered =
                            LoreStorage.Load(
                                currentPath);

                        RequireGraphMatches(
                            graph,
                            recovered,
                            $"Disk recovery at operation {operationNumber}");

                        counters.DiskRecoveries++;

                        break;
                    }

                    default:
                        throw new InvalidOperationException(
                            $"Unknown endurance operation kind: {kind}.");
                }

                counters.OperationsPassed++;
            }
            catch (Exception exception)
            {
                Console.WriteLine();

                Console.WriteLine(
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                Console.WriteLine(
                    "CRASH / RECOVERY ENDURANCE FAILURE");

                Console.WriteLine(
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                Console.WriteLine();

                Console.WriteLine(
                    $"Operation:       {operationNumber:N0}/{operations:N0}");

                Console.WriteLine(
                    $"Operation kind:  {kind}");

                Console.WriteLine(
                    $"Message:         {exception.Message}");

                Console.WriteLine();

                Console.WriteLine(
                    "Recent operation kinds:");

                var recentCount =
                    Math.Min(
                        operationNumber,
                        recent.Length);

                for (var offset = 0;
                     offset < recentCount;
                     offset++)
                {
                    var recentIndex =
                        (operationNumber -
                         recentCount +
                         offset) %
                        recent.Length;

                    Console.WriteLine(
                        $"  {offset + 1,2}: {recent[recentIndex]}");
                }

                throw;
            }

            if (operationNumber %
                    auditInterval == 0 ||
                operationNumber == operations)
            {
                var elapsed =
                    stopwatch.Elapsed;

                var rate =
                    operationNumber /
                    Math.Max(
                        elapsed.TotalSeconds,
                        0.001);

                var workingSet =
                    Process.GetCurrentProcess()
                        .WorkingSet64;

                Console.WriteLine(
                    $"  [{operationNumber,10:N0}/{operations:N0}] " +
                    $"PASS | " +
                    $"Rate={rate,10:N0} ops/s | " +
                    $"Faults={counters.FaultInjections,8:N0} | " +
                    $"DiskRecovery={counters.DiskRecoveries,7:N0} | " +
                    $"WorkingSet={FormatBytes(workingSet)}");
            }
        }

        stopwatch.Stop();

        return new EnduranceResult(
            operations,
            counters.OperationsPassed,
            counters.InMemoryValidLoads,
            counters.TruncationAttacks,
            counters.CorruptionAttacks,
            counters.InvalidEndpointAttacks,
            counters.EmptyDocumentAttacks,
            counters.DiskValidLoads,
            counters.DiskRecoveries,
            counters.FaultInjections,
            stopwatch.Elapsed);
    }

    /*
     * =============================================================
     * GRAPH GENERATOR
     * =============================================================
     */

    private static SorophyGraph BuildGraph(
        int seed,
        int entityCount,
        int generation)
    {
        var random =
            new DeterministicRandom(
                seed);

        var graph =
            new SorophyGraph();

        var ids =
            new Guid[entityCount];

        for (var index = 0;
             index < entityCount;
             index++)
        {
            var id =
                random.NextGuid();

            ids[index] =
                id;

            var entity =
                new SorophyEntity
                {
                    Id =
                        id,

                    Name =
                        $"CrashRecovery-{generation:D3}-Entity-{index:D5}",

                    Type =
                        (index % 4) switch
                        {
                            0 => "Character",
                            1 => "Location",
                            2 => "Faction",
                            _ => "Artifact"
                        }
                };

            /*
             * SorophyValueType.Integer requires Int64 / long.
             */
            entity.Properties["generation"] =
                new SorophyProperty
                {
                    Name =
                        "generation",

                    Value =
                        new SorophyValue(
                            SorophyValueType.Integer,
                            (long)generation)
                };

            entity.Properties["index"] =
                new SorophyProperty
                {
                    Name =
                        "index",

                    Value =
                        new SorophyValue(
                            SorophyValueType.Integer,
                            (long)index)
                };

            if (index % 4 == 0)
            {
                entity.Properties["label"] =
                    new SorophyProperty
                    {
                        Name =
                            "label",

                        Value =
                            new SorophyValue(
                                SorophyValueType.String,
                                $"Payload-{random.NextGuid():N}")
                    };
            }

            graph.AddEntity(
                entity);
        }

        /*
         * Connected backbone.
         */
        for (var index = 0;
             index + 1 < entityCount;
             index++)
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id =
                        random.NextGuid(),

                    Type =
                        "next",

                    SourceId =
                        ids[index],

                    TargetId =
                        ids[index + 1]
                });
        }

        /*
         * A couple of parallel edges.
         */
        if (entityCount >= 2)
        {
            for (var index = 0;
                 index < 2;
                 index++)
            {
                graph.AddRelationship(
                    new SorophyRelationship
                    {
                        Id =
                            random.NextGuid(),

                        Type =
                            "parallel",

                        SourceId =
                            ids[0],

                        TargetId =
                            ids[1]
                    });
            }
        }

        /*
         * Self link.
         */
        graph.AddRelationship(
            new SorophyRelationship
            {
                Id =
                    random.NextGuid(),

                Type =
                    "self",

                SourceId =
                    ids[0],

                TargetId =
                    ids[0]
            });

        return graph;
    }

    /*
     * =============================================================
     * INVALID DOCUMENT
     * =============================================================
     */

    private static string BuildInvalidEndpointDocument()
    {
        var sourceId =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000001");

        var relationshipId =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000002");

        var missingTargetId =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000003");

        return
            $$"""
            {
              "formatVersion": 1,
              "entities": [
                {
                  "id": "{{sourceId}}",
                  "name": "Source",
                  "type": "Entity",
                  "properties": {}
                }
              ],
              "relationships": [
                {
                  "id": "{{relationshipId}}",
                  "type": "broken",
                  "sourceId": "{{sourceId}}",
                  "targetId": "{{missingTargetId}}",
                  "properties": {}
                }
              ]
            }
            """;
    }

    /*
     * =============================================================
     * DAMAGE GENERATORS
     * =============================================================
     */

    private static string CreateDamagedDocument(
        string knownGood,
        int round)
    {
        return round switch
        {
            0 =>
                knownGood[..Math.Max(
                    1,
                    knownGood.Length / 2)],

            1 =>
                knownGood[..Math.Max(
                    1,
                    knownGood.Length - 1)],

            2 =>
                knownGood.Replace(
                    "\"formatVersion\": 1",
                    "\"formatVersion\": 999",
                    StringComparison.Ordinal),

            3 =>
                knownGood.Replace(
                    "\"formatVersion\": 1",
                    "\"formatVersion\": 0",
                    StringComparison.Ordinal),

            4 =>
                knownGood.Replace(
                    "\"entities\"",
                    "\"entities_broken\"",
                    StringComparison.Ordinal),

            5 =>
                ReplaceFirst(
                    knownGood,
                    '{',
                    '['),

            6 =>
                ReplaceLast(
                    knownGood,
                    '}',
                    ']'),

            7 =>
                "THIS IS NOT JSON",

            8 =>
                knownGood +
                "THIS_TRAILING_DATA_IS_CORRUPTION",

            _ =>
                knownGood[..Math.Max(
                    1,
                    knownGood.Length / 4)]
        };
    }

    private static string ReplaceFirst(
        string value,
        char oldCharacter,
        char newCharacter)
    {
        var index =
            value.IndexOf(
                oldCharacter);

        if (index < 0)
        {
            return value;
        }

        return
            value[..index] +
            newCharacter +
            value[(index + 1)..];
    }

    private static string ReplaceLast(
        string value,
        char oldCharacter,
        char newCharacter)
    {
        var index =
            value.LastIndexOf(
                oldCharacter);

        if (index < 0)
        {
            return value;
        }

        return
            value[..index] +
            newCharacter +
            value[(index + 1)..];
    }

    private static string SelectDamagePath(
        DeterministicRandom random,
        string truncatedPath,
        string corruptedPath,
        string emptyPath,
        string invalidEndpointPath)
    {
        return random.NextInt(4) switch
        {
            0 =>
                truncatedPath,

            1 =>
                corruptedPath,

            2 =>
                emptyPath,

            _ =>
                invalidEndpointPath
        };
    }

    /*
     * =============================================================
     * ASSERTIONS
     * =============================================================
     */

    private static void RequireLoadFails(
        string path,
        string message)
    {
        try
        {
            _ =
                LoreStorage.Load(
                    path);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException(
            message);
    }

    private static void RequireDeserializeFails(
        string json,
        string message)
    {
        try
        {
            _ =
                LoreSerializer.Deserialize(
                    json);
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException(
            message);
    }

    private static void RequireGraphMatches(
        SorophyGraph expected,
        SorophyGraph actual,
        string context)
    {
        Require(
            actual.Entities.Count ==
            expected.Entities.Count,
            $"{context}: entity count mismatch.");

        Require(
            actual.Relationships.Count ==
            expected.Relationships.Count,
            $"{context}: relationship count mismatch.");

        Require(
            actual.Validate().Count == 0,
            $"{context}: graph failed validation.");
    }

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

    /*
     * =============================================================
     * CHECK INFRASTRUCTURE
     * =============================================================
     */

    private static void RunCheck(
        List<CheckResult> results,
        string name,
        Func<string> action)
    {
        try
        {
            var detail =
                action();

            results.Add(
                new CheckResult(
                    name,
                    true,
                    detail));
        }
        catch (Exception exception)
        {
            results.Add(
                new CheckResult(
                    name,
                    false,
                    exception.Message));
        }
    }

    private sealed record CheckResult(
        string Name,
        bool Passed,
        string Detail);

    /*
     * =============================================================
     * ENDURANCE TYPES
     * =============================================================
     */

    private sealed class EnduranceCounters
    {
        public long OperationsPassed { get; set; }

        public long InMemoryValidLoads { get; set; }

        public long TruncationAttacks { get; set; }

        public long CorruptionAttacks { get; set; }

        public long InvalidEndpointAttacks { get; set; }

        public long EmptyDocumentAttacks { get; set; }

        public long DiskValidLoads { get; set; }

        public long DiskRecoveries { get; set; }

        public long FaultInjections { get; set; }
    }

    private sealed record EnduranceResult(
        long OperationsExecuted,
        long OperationsPassed,
        long InMemoryValidLoads,
        long TruncationAttacks,
        long CorruptionAttacks,
        long InvalidEndpointAttacks,
        long EmptyDocumentAttacks,
        long DiskValidLoads,
        long DiskRecoveries,
        long FaultInjections,
        TimeSpan Elapsed)
    {
        public double OperationsPerSecond =>
            OperationsExecuted /
            Math.Max(
                Elapsed.TotalSeconds,
                0.001);
    }

    private enum OperationKind
    {
        InMemoryValid,
        Truncated,
        Corrupted,
        InvalidEndpoint,
        EmptyDocument,
        DiskValid,
        DiskRecovery
    }

    /*
     * =============================================================
     * HEADER
     * =============================================================
     */

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
            "║              SOROPHY CRASH / RECOVERY TORTURE           ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");

        Console.WriteLine(
            $"║ Seed:           {seed,-43}║");

        Console.WriteLine(
            $"║ Operations:     {operations,-43}║");

        Console.WriteLine(
            $"║ Audit interval: {auditInterval,-43}║");

        Console.WriteLine(
            "║ Mode:           1M RECOVERY ENDURANCE                     ║");

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");

        Console.WriteLine();

        Console.WriteLine(
            $"Temporary sandbox: {temporaryDirectory}");

        Console.WriteLine();
    }

    /*
     * =============================================================
     * FORMATTER
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
                 index < 16;
                 index += 8)
            {
                var value =
                    NextUInt64();

                var remaining =
                    Math.Min(
                        8,
                        16 - index);

                for (var offset = 0;
                     offset < remaining;
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