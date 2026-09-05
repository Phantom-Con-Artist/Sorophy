using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class SerializationTortureTests
{
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

        var scales =
            BuildScalePoints(operations);

        PrintHeader(
            seed,
            operations,
            scales);

        var allPassed = true;

        var entityResult =
            RunEntityRoundTrip(seed);

        allPassed &=
            entityResult;

        foreach (var scale in scales)
        {
            var result =
                RunGraphRoundTrip(
                    seed,
                    scale);

            allPassed &=
                result;
        }

        var malformedResult =
            RunMalformedInputSuite();

        allPassed &=
            malformedResult;

        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine(
            "                 SERIALIZATION RESULT");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine();

        Console.WriteLine(
            $"Entity serializer:      {(entityResult ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"Graph round trips:      {scales.Count} scale point(s)");

        Console.WriteLine(
            $"Malformed input suite:  {(malformedResult ? "PASS" : "FAIL")}");

        Console.WriteLine();

        Console.WriteLine(
            allPassed
                ? "                    100% PASS"
                : "                    FAILURE");

        Console.WriteLine();

        Console.WriteLine(
            allPassed
                ? "STATUS: SERIALIZATION TORTURE VERIFIED"
                : "STATUS: SERIALIZATION TORTURE FAILED");

        Console.WriteLine();

        return allPassed
            ? 0
            : 1;
    }

    private static bool RunEntityRoundTrip(
        int seed)
    {
        Console.WriteLine();

        Console.WriteLine(
            "────────────────────────────────────────────────────────────");

        Console.WriteLine(
            "ENTITY SERIALIZER / RICH VALUE ROUND TRIP");

        Console.WriteLine(
            "────────────────────────────────────────────────────────────");

        var entity =
            CreateRichEntity(seed);

        var firstJson =
            EntitySerializer.Serialize(entity);

        var firstRoundTrip =
            EntitySerializer.Deserialize(firstJson);

        var secondJson =
            EntitySerializer.Serialize(firstRoundTrip);

        var exactRoundTrip =
            string.Equals(
                firstJson,
                secondJson,
                StringComparison.Ordinal);

        var propertyCountPreserved =
            entity.Properties.Count ==
            firstRoundTrip.Properties.Count;

        var idPreserved =
            entity.Id ==
            firstRoundTrip.Id;

        var namePreserved =
            string.Equals(
                entity.Name,
                firstRoundTrip.Name,
                StringComparison.Ordinal);

        var typePreserved =
            string.Equals(
                entity.Type,
                firstRoundTrip.Type,
                StringComparison.Ordinal);

        var result =
            exactRoundTrip &&
            propertyCountPreserved &&
            idPreserved &&
            namePreserved &&
            typePreserved;

        Console.WriteLine();

        Console.WriteLine(
            $"  Properties:             {entity.Properties.Count,10:N0}");

        Console.WriteLine(
            $"  JSON bytes:             {Encoding.UTF8.GetByteCount(firstJson),10:N0}");

        Console.WriteLine(
            $"  ID preserved:           {(idPreserved ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"  Name preserved:         {(namePreserved ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"  Type preserved:         {(typePreserved ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"  Property count:         {(propertyCountPreserved ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"  Serialize stability:    {(exactRoundTrip ? "PASS" : "FAIL")}");

        Console.WriteLine();

        Console.WriteLine(
            $"  Entity round trip:      {(result ? "PASS" : "FAIL")}");

        return result;
    }

    private static bool RunGraphRoundTrip(
        int seed,
        int scale)
    {
        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════");

        Console.WriteLine(
            $"GRAPH SERIALIZATION SCALE: {scale:N0} ENTITIES");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════");

        var buildStarted =
            Stopwatch.GetTimestamp();

        var graph =
            BuildGraph(
                seed,
                scale);

        var buildElapsed =
            Stopwatch.GetElapsedTime(
                buildStarted);

        Console.WriteLine();

        Console.WriteLine(
            $"  Graph entities:        {graph.Entities.Count,10:N0}");

        Console.WriteLine(
            $"  Graph relationships:   {graph.Relationships.Count,10:N0}");

        Console.WriteLine(
            $"  Build time:            {buildElapsed.TotalMilliseconds,10:F2} ms");

        var initialValidation =
            graph.Validate();

        var initialValid =
            initialValidation.Count == 0;

        Console.WriteLine(
            $"  Initial validation:    {(initialValid ? "PASS" : "FAIL")}");

        if (!initialValid)
        {
            PrintErrors(
                initialValidation);

            return false;
        }

        var serializeStarted =
            Stopwatch.GetTimestamp();

        var json =
            LoreSerializer.Serialize(
                graph);

        var serializeElapsed =
            Stopwatch.GetElapsedTime(
                serializeStarted);

        var jsonBytes =
            Encoding.UTF8.GetByteCount(
                json);

        Console.WriteLine();

        Console.WriteLine(
            $"  Serialize time:        {serializeElapsed.TotalMilliseconds,10:F2} ms");

        Console.WriteLine(
            $"  Serialized size:       {FormatBytes(jsonBytes),10}");

        var deterministicJson =
            LoreSerializer.Serialize(
                graph);

        var deterministic =
            string.Equals(
                json,
                deterministicJson,
                StringComparison.Ordinal);

        Console.WriteLine(
            $"  Deterministic output:  {(deterministic ? "PASS" : "FAIL")}");

        if (!deterministic)
        {
            return false;
        }

        ForceCollection();

        var allocatedBefore =
            GC.GetAllocatedBytesForCurrentThread();

        var deserializeStarted =
            Stopwatch.GetTimestamp();

        var restored =
            LoreSerializer.Deserialize(
                json);

        var deserializeElapsed =
            Stopwatch.GetElapsedTime(
                deserializeStarted);

        var allocatedAfter =
            GC.GetAllocatedBytesForCurrentThread();

        var deserializeAllocated =
            Math.Max(
                0L,
                allocatedAfter -
                allocatedBefore);

        Console.WriteLine();

        Console.WriteLine(
            $"  Deserialize time:      {deserializeElapsed.TotalMilliseconds,10:F2} ms");

        Console.WriteLine(
            $"  Managed allocation:    {FormatBytes(deserializeAllocated),10}");

        var entityCountPreserved =
            restored.Entities.Count ==
            graph.Entities.Count;

        var relationshipCountPreserved =
            restored.Relationships.Count ==
            graph.Relationships.Count;

        Console.WriteLine(
            $"  Entity count:          {(entityCountPreserved ? "PASS" : "FAIL")}");

        Console.WriteLine(
            $"  Relationship count:    {(relationshipCountPreserved ? "PASS" : "FAIL")}");

        var restoredJsonStarted =
            Stopwatch.GetTimestamp();

        var restoredJson =
            LoreSerializer.Serialize(
                restored);

        var restoredJsonElapsed =
            Stopwatch.GetElapsedTime(
                restoredJsonStarted);

        var canonicalRoundTrip =
            string.Equals(
                json,
                restoredJson,
                StringComparison.Ordinal);

        Console.WriteLine(
            $"  Re-serialize time:     {restoredJsonElapsed.TotalMilliseconds,10:F2} ms");

        Console.WriteLine(
            $"  Canonical round trip:  {(canonicalRoundTrip ? "PASS" : "FAIL")}");

        var restoredValidation =
            restored.Validate();

        var restoredValid =
            restoredValidation.Count == 0;

        Console.WriteLine(
            $"  Restored validation:   {(restoredValid ? "PASS" : "FAIL")}");

        if (!restoredValid)
        {
            PrintErrors(
                restoredValidation);
        }

        string? tempDirectory =
            null;

        var diskRoundTrip =
            false;

        try
        {
            tempDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "SorophyEngineSerializationTorture",
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(
                tempDirectory);

            var filePath =
                Path.Combine(
                    tempDirectory,
                    $"graph-{scale:N0}.lore");

            var diskWriteStarted =
                Stopwatch.GetTimestamp();

            File.WriteAllText(
                filePath,
                json,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            var diskWriteElapsed =
                Stopwatch.GetElapsedTime(
                    diskWriteStarted);

            var fileInfo =
                new FileInfo(
                    filePath);

            var diskReadStarted =
                Stopwatch.GetTimestamp();

            var diskJson =
                File.ReadAllText(
                    filePath);

            var diskReadElapsed =
                Stopwatch.GetElapsedTime(
                    diskReadStarted);

            var diskRestored =
                LoreSerializer.Deserialize(
                    diskJson);

            var diskRestoredJson =
                LoreSerializer.Serialize(
                    diskRestored);

            diskRoundTrip =
                string.Equals(
                    json,
                    diskJson,
                    StringComparison.Ordinal) &&
                string.Equals(
                    json,
                    diskRestoredJson,
                    StringComparison.Ordinal);

            Console.WriteLine();

            Console.WriteLine(
                $"  Disk write time:       {diskWriteElapsed.TotalMilliseconds,10:F2} ms");

            Console.WriteLine(
                $"  Disk read time:        {diskReadElapsed.TotalMilliseconds,10:F2} ms");

            Console.WriteLine(
                $"  File size:             {FormatBytes(fileInfo.Length),10}");

            Console.WriteLine(
                $"  Disk byte identity:    {(string.Equals(json, diskJson, StringComparison.Ordinal) ? "PASS" : "FAIL")}");

            Console.WriteLine(
                $"  Disk round trip:       {(diskRoundTrip ? "PASS" : "FAIL")}");
        }
        finally
        {
            if (tempDirectory is not null)
            {
                try
                {
                    Directory.Delete(
                        tempDirectory,
                        recursive: true);
                }
                catch
                {
                    /*
                     * Cleanup failure must never hide the actual
                     * serialization result.
                     */
                }
            }
        }

        var result =
            initialValid &&
            entityCountPreserved &&
            relationshipCountPreserved &&
            deterministic &&
            canonicalRoundTrip &&
            restoredValid &&
            diskRoundTrip;

        Console.WriteLine();

        Console.WriteLine(
            $"  Scale-point result:    {(result ? "PASS" : "FAIL")}");

        return result;
    }

    private static SorophyGraph BuildGraph(
        int seed,
        int scale)
    {
        var random =
            new DeterministicRandom(
                seed);

        var graph =
            new SorophyGraph();

        var entityIds =
            new Guid[scale];

        for (var index = 0;
             index < scale;
             index++)
        {
            var id =
                random.NextGuid();

            entityIds[index] =
                id;

            var entity =
                new SorophyEntity
                {
                    Id =
                        id,

                    Name =
                        $"Entity-{index:N6}",

                    Type =
                        (index % 5) switch
                        {
                            0 => "Character",
                            1 => "Location",
                            2 => "Faction",
                            3 => "Artifact",
                            _ => "Concept"
                        }
                };

            if (index % 100 == 0)
            {
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
            }

            if (index % 250 == 0)
            {
                entity.Properties["label"] =
                    new SorophyProperty
                    {
                        Name =
                            "label",

                        Value =
                            new SorophyValue(
                                SorophyValueType.String,
                                $"Label-{index:N6}")
                    };
            }

            if (index == 0)
            {
                AddRichProperties(
                    entity,
                    random);
            }

            graph.AddEntity(
                entity);
        }

        for (var index = 0;
             index + 1 < scale;
             index++)
        {
            var relationship =
                new SorophyRelationship
                {
                    Id =
                        random.NextGuid(),

                    Type =
                        "next",

                    SourceId =
                        entityIds[index],

                    TargetId =
                        entityIds[index + 1]
                };

            if (index % 1_000 == 0)
            {
                relationship.Properties["sequence"] =
                    new SorophyProperty
                    {
                        Name =
                            "sequence",

                        Value =
                            new SorophyValue(
                                SorophyValueType.Integer,
                                (long)index)
                    };
            }

            graph.AddRelationship(
                relationship);
        }

        var selfCount =
            Math.Min(
                25,
                Math.Max(
                    1,
                    scale / 1_000));

        for (var index = 0;
             index < selfCount;
             index++)
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id =
                        random.NextGuid(),

                    Type =
                        "self",

                    SourceId =
                        entityIds[0],

                    TargetId =
                        entityIds[0]
                });
        }

        if (scale >= 2)
        {
            var parallelCount =
                Math.Min(
                    25,
                    Math.Max(
                        1,
                        scale / 1_000));

            for (var index = 0;
                 index < parallelCount;
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
                            entityIds[0],

                        TargetId =
                            entityIds[1]
                    });
            }
        }

        return graph;
    }

    private static SorophyEntity CreateRichEntity(
        int seed)
    {
        var random =
            new DeterministicRandom(
                seed);

        var entity =
            new SorophyEntity
            {
                Id =
                    random.NextGuid(),

                Name =
                    "Serialization Torture Entity",

                Type =
                    "TortureSubject"
            };

        AddRichProperties(
            entity,
            random);

        return entity;
    }

    private static void AddRichProperties(
        SorophyEntity entity,
        DeterministicRandom random)
    {
        entity.Properties["text"] =
            new SorophyProperty
            {
                Name =
                    "text",

                Value =
                    new SorophyValue(
                        SorophyValueType.String,
                        "Sorophy serialization torture payload")
            };

        entity.Properties["enabled"] =
            new SorophyProperty
            {
                Name =
                    "enabled",

                Value =
                    new SorophyValue(
                        SorophyValueType.Boolean,
                        true)
            };

        entity.Properties["population"] =
            new SorophyProperty
            {
                Name =
                    "population",

                Value =
                    new SorophyValue(
                        SorophyValueType.Integer,
                        4_294_967_296L)
            };

        entity.Properties["ratio"] =
            new SorophyProperty
            {
                Name =
                    "ratio",

                Value =
                    new SorophyValue(
                        SorophyValueType.Decimal,
                        123456.7890123456789m)
            };

        entity.Properties["timestamp"] =
            new SorophyProperty
            {
                Name =
                    "timestamp",

                Value =
                    new SorophyValue(
                        SorophyValueType.DateTime,
                        new DateTime(
                            2026,
                            9,
                            2,
                            12,
                            34,
                            56,
                            DateTimeKind.Utc))
            };

        entity.Properties["identifier"] =
            new SorophyProperty
            {
                Name =
                    "identifier",

                Value =
                    new SorophyValue(
                        SorophyValueType.Guid,
                        random.NextGuid())
            };

        entity.Properties["nullable"] =
            new SorophyProperty
            {
                Name =
                    "nullable",

                Value =
                    new SorophyValue(
                        SorophyValueType.Null,
                        null)
            };

        entity.Properties["list"] =
            new SorophyProperty
            {
                Name =
                    "list",

                Value =
                    new SorophyValue(
                        SorophyValueType.List,
                        new List<object?>
                        {
                            "alpha",
                            42L,
                            true,
                            12.5m,
                            null
                        })
            };

        entity.Properties["object"] =
            new SorophyProperty
            {
                Name =
                    "object",

                Value =
                    new SorophyValue(
                        SorophyValueType.Object,
                        new Dictionary<string, object?>
                        {
                            ["name"] =
                                "nested",

                            ["count"] =
                                7L,

                            ["enabled"] =
                                true,

                            ["value"] =
                                19.75m,

                            ["nothing"] =
                                null
                        })
            };
    }

    private static bool RunMalformedInputSuite()
    {
        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════");

        Console.WriteLine(
            "MALFORMED INPUT / CORRUPTION RESISTANCE");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════");

        var cases =
            new[]
            {
                new MalformedCase(
                    "Malformed JSON",
                    "{ completely broken"),

                new MalformedCase(
                    "Unsupported format version",
                    """
                    {
                      "formatVersion": 999,
                      "entities": [],
                      "relationships": []
                    }
                    """),

                new MalformedCase(
                    "Invalid entity GUID",
                    """
                    {
                      "formatVersion": 1,
                      "entities": [
                        {
                          "id": "not-a-guid",
                          "name": "A",
                          "type": "Entity",
                          "properties": {}
                        }
                      ],
                      "relationships": []
                    }
                    """),

                new MalformedCase(
                    "Missing relationship target",
                    """
                    {
                      "formatVersion": 1,
                      "entities": [
                        {
                          "id": "00000000-0000-0000-0000-000000000001",
                          "name": "A",
                          "type": "Entity",
                          "properties": {}
                        }
                      ],
                      "relationships": [
                        {
                          "id": "00000000-0000-0000-0000-000000000002",
                          "type": "points_to",
                          "sourceId": "00000000-0000-0000-0000-000000000001",
                          "targetId": "00000000-0000-0000-0000-000000000003",
                          "properties": {}
                        }
                      ]
                    }
                    """),

                new MalformedCase(
                    "Invalid property type",
                    """
                    {
                      "formatVersion": 1,
                      "entities": [
                        {
                          "id": "00000000-0000-0000-0000-000000000001",
                          "name": "A",
                          "type": "Entity",
                          "properties": {
                            "broken": {
                              "type": "DefinitelyNotAType",
                              "value": 42
                            }
                          }
                        }
                      ],
                      "relationships": []
                    }
                    """),

                new MalformedCase(
                    "Integer stored as string",
                    """
                    {
                      "formatVersion": 1,
                      "id": "00000000-0000-0000-0000-000000000001",
                      "name": "A",
                      "type": "Entity",
                      "properties": {
                        "population": {
                          "type": "Integer",
                          "value": "not-an-integer"
                        }
                      }
                    }
                    """,
                    true),

                new MalformedCase(
                    "Invalid relationship GUID",
                    """
                    {
                      "formatVersion": 1,
                      "entities": [
                        {
                          "id": "00000000-0000-0000-0000-000000000001",
                          "name": "A",
                          "type": "Entity",
                          "properties": {}
                        },
                        {
                          "id": "00000000-0000-0000-0000-000000000002",
                          "name": "B",
                          "type": "Entity",
                          "properties": {}
                        }
                      ],
                      "relationships": [
                        {
                          "id": "not-a-guid",
                          "type": "points_to",
                          "sourceId": "00000000-0000-0000-0000-000000000001",
                          "targetId": "00000000-0000-0000-0000-000000000002",
                          "properties": {}
                        }
                      ]
                    }
                    """)
            };

        var allPassed =
            true;

        for (var index = 0;
             index < cases.Length;
             index++)
        {
            var testCase =
                cases[index];

            var threw =
                false;

            try
            {
                if (testCase.IsEntityDocument)
                {
                    _ =
                        EntitySerializer.Deserialize(
                            testCase.Json);
                }
                else
                {
                    _ =
                        LoreSerializer.Deserialize(
                            testCase.Json);
                }
            }
            catch
            {
                threw =
                    true;
            }

            allPassed &=
                threw;

            Console.WriteLine(
                $"  {testCase.Name,-34} " +
                $"{(threw ? "PASS" : "FAIL")}");
        }

        var failedConstructionDidNotReturnGraph =
            false;

        try
        {
            _ =
                LoreSerializer.Deserialize(
                    """
                    {
                      "formatVersion": 1,
                      "entities": [
                        {
                          "id": "00000000-0000-0000-0000-000000000001",
                          "name": "A",
                          "type": "Entity",
                          "properties": {}
                        }
                      ],
                      "relationships": [
                        {
                          "id": "00000000-0000-0000-0000-000000000002",
                          "type": "broken",
                          "sourceId": "00000000-0000-0000-0000-000000000001",
                          "targetId": "00000000-0000-0000-0000-000000000099",
                          "properties": {}
                        }
                      ]
                    }
                    """);
        }
        catch
        {
            failedConstructionDidNotReturnGraph =
                true;
        }

        allPassed &=
            failedConstructionDidNotReturnGraph;

        Console.WriteLine(
            $"  Failed construction no graph:       " +
            $"{(failedConstructionDidNotReturnGraph ? "PASS" : "FAIL")}");

        Console.WriteLine();

        Console.WriteLine(
            $"  Malformed input suite:               " +
            $"{(allPassed ? "PASS" : "FAIL")}");

        return allPassed;
    }

    private static IReadOnlyList<int> BuildScalePoints(
        int operations)
    {
        var scales =
            new List<int>();

        AddScale(
            scales,
            1_000,
            operations);

        AddScale(
            scales,
            10_000,
            operations);

        if (operations >= 50_000)
        {
            scales.Add(
                50_000);
        }

        if (operations >= 100_000)
        {
            scales.Add(
                100_000);
        }

        if (operations > 100_000)
        {
            scales.Add(
                operations);
        }

        return scales;
    }

    private static void AddScale(
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

    private static void PrintHeader(
        int seed,
        int operations,
        IReadOnlyList<int> scales)
    {
        Console.WriteLine();

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║              SOROPHY ENGINE SERIALIZATION TORTURE             ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");

        Console.WriteLine(
            $"║ Seed:           {seed,-43}║");

        Console.WriteLine(
            $"║ Operations:     {operations,-43}║");

        Console.WriteLine(
            "║ Formats:        .entity + .lore                          ║");

        Console.WriteLine(
            "║ Mode:           ROUND TRIP + DISK I/O + CORRUPTION      ║");

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
                $"  {scales[index]:N0} entities");
        }
    }

    private static void PrintErrors(
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
                $"      ... and {errors.Count - count:N0} more.");
        }
    }

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

    private sealed record MalformedCase(
        string Name,
        string Json,
        bool IsEntityDocument = false);

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

                var remaining =
                    Math.Min(
                        8,
                        bytes.Length -
                        index);

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