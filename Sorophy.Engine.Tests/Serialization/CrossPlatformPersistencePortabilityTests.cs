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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Storage;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Serialization;

/// <summary>
/// Targeted cross-platform persistence portability test suite for Sorophy Krono (.lore v2).
/// Validates that persisted graph state is fully portable across operating systems
/// (Windows, Linux, macOS) by verifying:
/// <list type="bullet">
///   <item><description>Culture/locale invariance across global regions</description></item>
///   <item><description>Line-ending invariance (CRLF, LF, mixed)</description></item>
///   <item><description>Encoding invariance (UTF-8 with/without BOM)</description></item>
///   <item><description>Filesystem path separator and relative path handling</description></item>
///   <item><description>Deterministic canonical re-serialization byte identity</description></item>
///   <item><description>Full preservation of entities, relationships, temporal coordinates,
///         evolutions, histories, event entity provenance, and retired IDs</description></item>
///   <item><description>Standalone and chained CI artifact exchange harness</description></item>
/// </list>
/// </summary>
public sealed class CrossPlatformPersistencePortabilityTests
{
    public const string InputArtifactEnvVar = "SOROPHY_PORTABILITY_INPUT_LORE";
    public const string OutputArtifactEnvVar = "SOROPHY_PORTABILITY_OUTPUT_LORE";

    /// <summary>
    /// Constructs a fully representative Sorophy Krono v2 graph exercising all
    /// supported architectural dimensions:
    /// Entities, Normal Relationships, Relationship Properties, ValidFrom/ValidTill,
    /// Relationship History, Relationship Evolution (Creation, Modification, Termination),
    /// Event Entity Provenance, Retired Relationship IDs, Temporal Coordinates, and
    /// Nested/Structured Property Values.
    /// </summary>
    public static SorophyGraph BuildRepresentativeKronoGraph()
    {
        var schema = new SorophyTimeSchema(
            "CosmicTimeline",
            new[]
            {
                new SorophyTimeUnit("Age", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Named)),
                new SorophyTimeUnit("Cycle", 1, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric)),
                new SorophyTimeUnit("Phase", 2, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });

        var graph = new SorophyGraph();

        // 1. Entities with scalar, temporal, and structured properties
        var heroId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var hero = new SorophyEntity
        {
            Id = heroId,
            Name = "Aethelgard",
            Type = "Character",
            Description = "The Guardian of the North"
        };
        hero.Tags.Add("Protagonist");
        hero.Tags.Add("Champion");
        hero.Documents["biography.md"] = new SorophyEntityDocument(
            "biography.md",
            "# Biography\nBorn in the northern frost.",
            "text/markdown");
        hero.Properties["level"] = new SorophyProperty
        {
            Name = "level",
            Value = new SorophyValue(SorophyValueType.Integer, 99L)
        };
        hero.Properties["rating"] = new SorophyProperty
        {
            Name = "rating",
            Value = new SorophyValue(SorophyValueType.Decimal, 98.75m)
        };
        hero.Properties["active"] = new SorophyProperty
        {
            Name = "active",
            Value = new SorophyValue(SorophyValueType.Boolean, true)
        };
        hero.Properties["registeredAt"] = new SorophyProperty
        {
            Name = "registeredAt",
            Value = new SorophyValue(SorophyValueType.DateTime, new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Utc))
        };
        hero.Properties["syncId"] = new SorophyProperty
        {
            Name = "syncId",
            Value = new SorophyValue(SorophyValueType.Guid, Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444"))
        };
        hero.Properties["skills"] = new SorophyProperty
        {
            Name = "skills",
            Value = new SorophyValue(SorophyValueType.List, new List<object?> { "Swordsmanship", "FrostMagic", 42L })
        };
        hero.Properties["attributes"] = new SorophyProperty
        {
            Name = "attributes",
            Value = new SorophyValue(SorophyValueType.Object, new Dictionary<string, object?>
            {
                ["strength"] = 100L,
                ["agility"] = 85.5m,
                ["titles"] = new List<object?> { "Frostborn", "Warden" }
            })
        };

        var castleId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var castle = new SorophyEntity
        {
            Id = castleId,
            Name = "Winterfell Keep",
            Type = "Location",
            Description = "Ancient northern stronghold"
        };
        castle.Tags.Add("Stronghold");
        castle.Properties["defenseRating"] = new SorophyProperty
        {
            Name = "defenseRating",
            Value = new SorophyValue(SorophyValueType.Integer, 5000L)
        };

        var coronationId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var coronation = new SorophyEntity
        {
            Id = coronationId,
            Name = "Grand Coronation",
            Type = "Event",
            Description = "Ascension of the Frost King"
        };
        coronation.Tags.Add("Historic");
        coronation.Tags.Add("Epochal");

        var relicId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        var relic = new SorophyEntity
        {
            Id = relicId,
            Name = "Frostmourn Blade",
            Type = "Item",
            Description = "Legendary runeblade"
        };
        relic.Tags.Add("Relic");

        graph.AddEntity(hero);
        graph.AddEntity(castle);
        graph.AddEntity(coronation);
        graph.AddEntity(relic);

        // 2. Normal Active Relationship with properties and temporal validity
        var timeStart = new SorophyTime(schema, "100", "Cycle", SorophyTimePrecision.Exact);
        var timeEnd = new SorophyTime(schema, "500", "Cycle", SorophyTimePrecision.Exact);

        var r1Id = Guid.Parse("50000000-0000-0000-0000-000000000005");
        var r1 = new SorophyRelationship
        {
            Id = r1Id,
            SourceId = heroId,
            TargetId = castleId,
            Type = "defends",
            ValidFrom = timeStart,
            ValidTill = timeEnd
        };
        r1.Properties["honoraryRank"] = new SorophyProperty
        {
            Name = "honoraryRank",
            Value = new SorophyValue(SorophyValueType.String, "High Marshal")
        };
        r1.Properties["stipend"] = new SorophyProperty
        {
            Name = "stipend",
            Value = new SorophyValue(SorophyValueType.Decimal, 25000.50m)
        };
        r1.Properties["dutyMetadata"] = new SorophyProperty
        {
            Name = "dutyMetadata",
            Value = new SorophyValue(SorophyValueType.Object, new Dictionary<string, object?>
            {
                ["patrolFrequencyDays"] = 7L,
                ["clearanceLevel"] = "TopSecret",
                ["contingencies"] = new List<object?> { "Siege", "Infiltration" }
            })
        };
        graph.AddRelationship(r1);

        // 3. Normal Active Relationship with history & Event Entity provenance
        var r3Id = Guid.Parse("60000000-0000-0000-0000-000000000006");
        var r3 = new SorophyRelationship
        {
            Id = r3Id,
            SourceId = heroId,
            TargetId = relicId,
            Type = "wields",
            ValidFrom = timeStart
        };
        graph.AddRelationship(r3);

        var r3History = graph.GetOrCreateRelationshipHistory(r3Id);
        var factTime1 = new SorophyTime(schema, "100", "Cycle", SorophyTimePrecision.Exact);
        var factTime2 = new SorophyTime(schema, "250", "Cycle", SorophyTimePrecision.Approximate);

        var r3Fact1 = new SorophyRelationshipFact(
            at: factTime1,
            relationshipId: r3Id,
            sourceId: heroId,
            targetId: relicId,
            type: "wields",
            properties: new Dictionary<string, SorophyProperty>
            {
                ["attunement"] = new SorophyProperty { Name = "attunement", Value = new SorophyValue(SorophyValueType.Integer, 50L) }
            },
            validFrom: factTime1,
            validTill: null,
            eventEntityId: coronationId);

        var r3Fact2 = new SorophyRelationshipFact(
            at: factTime2,
            relationshipId: r3Id,
            sourceId: heroId,
            targetId: relicId,
            type: "mastered_wields",
            properties: new Dictionary<string, SorophyProperty>
            {
                ["attunement"] = new SorophyProperty { Name = "attunement", Value = new SorophyValue(SorophyValueType.Integer, 100L) },
                ["awakened"] = new SorophyProperty { Name = "awakened", Value = new SorophyValue(SorophyValueType.Boolean, true) }
            },
            validFrom: factTime2,
            validTill: null,
            eventEntityId: coronationId);

        r3History.Add(r3Fact1);
        r3History.Add(r3Fact2);

        // 4. Relationship Evolution executed via SorophyRelationshipEvolutionExecutor
        var r2Id = Guid.Parse("70000000-0000-0000-0000-000000000007");
        var evoTime1 = new SorophyTime(schema, "120", "Cycle", SorophyTimePrecision.Exact);
        var evoTime2 = new SorophyTime(schema, "200", "Cycle", SorophyTimePrecision.Exact);
        var evoTime3 = new SorophyTime(schema, "350", "Cycle", SorophyTimePrecision.Exact);

        var executor = new SorophyRelationshipEvolutionExecutor();

        // 4a. Evolution: Creation with Event Entity Provenance
        var createEvo = new SorophyRelationshipCreation(
            relationshipId: r2Id,
            sourceId: heroId,
            targetId: castleId,
            type: "allied_with",
            effectiveTime: evoTime1,
            properties: new Dictionary<string, SorophyProperty>
            {
                ["pactStrength"] = new SorophyProperty { Name = "pactStrength", Value = new SorophyValue(SorophyValueType.Integer, 10L) }
            },
            validFrom: evoTime1,
            eventEntityId: coronationId);
        executor.Execute(graph, createEvo);

        // 4b. Evolution: Property Modification
        var propEvo = new SorophyRelationshipPropertyModification(
            relationshipId: r2Id,
            effectiveTime: evoTime2,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["pactStrength"] = new SorophyProperty { Name = "pactStrength", Value = new SorophyValue(SorophyValueType.Integer, 75L) },
                ["ratified"] = new SorophyProperty { Name = "ratified", Value = new SorophyValue(SorophyValueType.Boolean, true) }
            },
            eventEntityId: coronationId);
        executor.Execute(graph, propEvo);

        // 4c. Evolution: Termination (retires relationship r2Id and records final historical fact)
        var termEvo = new SorophyRelationshipTermination(
            relationshipId: r2Id,
            effectiveTime: evoTime3,
            eventEntityId: coronationId);
        executor.Execute(graph, termEvo);

        // 5. Explicitly retired relationship ID
        var retiredIdManual = Guid.Parse("90000000-0000-0000-0000-000000000009");
        graph.RetireRelationshipId(retiredIdManual);

        return graph;
    }

    [Fact]
    public void Portability_01_RepresentativeGraph_ValidationAndStructuralCompleteness()
    {
        var graph = BuildRepresentativeKronoGraph();

        // 1. Validation must pass with zero issues
        var validationErrors = graph.Validate();
        Assert.Empty(validationErrors);

        // 2. Entities completeness
        Assert.Equal(4, graph.Entities.Count);
        Assert.True(graph.ContainsEntity(Guid.Parse("10000000-0000-0000-0000-000000000001")));
        Assert.True(graph.ContainsEntity(Guid.Parse("20000000-0000-0000-0000-000000000002")));
        Assert.True(graph.ContainsEntity(Guid.Parse("30000000-0000-0000-0000-000000000003")));
        Assert.True(graph.ContainsEntity(Guid.Parse("40000000-0000-0000-0000-000000000004")));

        // 3. Active relationships
        Assert.Equal(2, graph.Relationships.Count);
        Assert.True(graph.ContainsRelationship(Guid.Parse("50000000-0000-0000-0000-000000000005")));
        Assert.True(graph.ContainsRelationship(Guid.Parse("60000000-0000-0000-0000-000000000006")));

        // 4. Retired relationship IDs (from termination + manual retirement)
        Assert.Equal(2, graph.RetiredRelationshipIds.Count);
        Assert.True(graph.IsRelationshipIdRetired(Guid.Parse("70000000-0000-0000-0000-000000000007")));
        Assert.True(graph.IsRelationshipIdRetired(Guid.Parse("90000000-0000-0000-0000-000000000009")));

        // 5. Relationship histories
        Assert.Equal(2, graph.RelationshipHistories.Count);
        Assert.True(graph.TryGetRelationshipHistory(Guid.Parse("60000000-0000-0000-0000-000000000006"), out var r3Hist));
        Assert.Equal(2, r3Hist!.Facts.Count);
        Assert.True(graph.TryGetRelationshipHistory(Guid.Parse("70000000-0000-0000-0000-000000000007"), out var r2Hist));
        Assert.Equal(3, r2Hist!.Facts.Count);
    }

    [Fact]
    public void Portability_02_DeterministicCanonicalEquivalence()
    {
        var graph = BuildRepresentativeKronoGraph();

        // Repeated serialization produces byte-for-byte identical output
        var json1 = LoreSerializer.Serialize(graph);
        var json2 = LoreSerializer.Serialize(graph);
        Assert.True(
            string.Equals(json1, json2, StringComparison.Ordinal),
            "Serialization must produce byte-for-byte identical output on repeated calls.");

        // Round trip preserves canonical equivalence
        var restored = LoreSerializer.Deserialize(json1);
        var restoredJson = LoreSerializer.Serialize(restored);
        Assert.True(
            string.Equals(json1, restoredJson, StringComparison.Ordinal),
            "Re-serialization of restored graph must produce byte-for-byte identical canonical output.");

        // Restored graph must be valid
        Assert.Empty(restored.Validate());

        // Restored graph structural equality
        Assert.Equal(graph.Entities.Count, restored.Entities.Count);
        Assert.Equal(graph.Relationships.Count, restored.Relationships.Count);
        Assert.Equal(graph.RelationshipHistories.Count, restored.RelationshipHistories.Count);
        Assert.Equal(graph.RetiredRelationshipIds.Count, restored.RetiredRelationshipIds.Count);
    }

    [Fact]
    public void Portability_03_CultureInvariance_AllWorldLocales()
    {
        var graph = BuildRepresentativeKronoGraph();
        var baselineJson = LoreSerializer.Serialize(graph);

        var testCultures = new[]
        {
            CultureInfo.InvariantCulture,
            new CultureInfo("en-US"),
            new CultureInfo("fr-FR"),  // Comma decimal separator, space grouping
            new CultureInfo("de-DE"),  // Comma decimal separator, dot grouping
            new CultureInfo("tr-TR"),  // Dotted/dotless 'i' casing difference
            new CultureInfo("ar-SA"),  // Arabic locale
            new CultureInfo("ja-JP"),  // East Asian
            new CultureInfo("es-ES"),  // Spanish
            new CultureInfo("ru-RU")   // Cyrillic locale
        };

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            foreach (var culture in testCultures)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                // 1. Serialization in this culture must match baseline byte-for-byte
                var cultureJson = LoreSerializer.Serialize(graph);
                Assert.True(
                    string.Equals(baselineJson, cultureJson, StringComparison.Ordinal),
                    $"Serialization in culture '{culture.Name}' diverged from invariant baseline.");

                // 2. Numeric representations must use decimal period '.', never comma ','
                Assert.Contains("98.75", cultureJson, StringComparison.Ordinal);
                Assert.Contains("25000.5", cultureJson, StringComparison.Ordinal);
                Assert.DoesNotContain("98,75", cultureJson, StringComparison.Ordinal);
                Assert.DoesNotContain("25000,5", cultureJson, StringComparison.Ordinal);

                // 3. Deserialization in this culture must restore full graph and pass validation
                var restored = LoreSerializer.Deserialize(cultureJson);
                Assert.Empty(restored.Validate());

                // 4. Re-serialization in this culture must match baseline byte-for-byte
                var reSerialized = LoreSerializer.Serialize(restored);
                Assert.True(
                    string.Equals(baselineJson, reSerialized, StringComparison.Ordinal),
                    $"Re-serialization in culture '{culture.Name}' diverged from invariant baseline.");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Portability_04_LineEndingInvariance_LF_CRLF_Mixed()
    {
        var graph = BuildRepresentativeKronoGraph();
        var canonicalJson = LoreSerializer.Serialize(graph);

        // Prepare line ending variants
        var lfJson = canonicalJson.Replace("\r\n", "\n").Replace("\r", "\n");
        var crlfJson = lfJson.Replace("\n", "\r\n");

        // Mixed line endings: alternate lines between \r\n and \n
        var lines = lfJson.Split('\n');
        var mixedSb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            mixedSb.Append(lines[i]);
            if (i < lines.Length - 1)
            {
                mixedSb.Append(i % 2 == 0 ? "\r\n" : "\n");
            }
        }
        var mixedJson = mixedSb.ToString();

        // 1. Deserialize LF-only
        var restoredFromLf = LoreSerializer.Deserialize(lfJson);
        Assert.Empty(restoredFromLf.Validate());
        var reserializedLf = LoreSerializer.Serialize(restoredFromLf);
        Assert.True(
            string.Equals(canonicalJson, reserializedLf, StringComparison.Ordinal),
            "Re-serialization from LF source diverged from canonical baseline.");

        // 2. Deserialize CRLF-only
        var restoredFromCrlf = LoreSerializer.Deserialize(crlfJson);
        Assert.Empty(restoredFromCrlf.Validate());
        var reserializedCrlf = LoreSerializer.Serialize(restoredFromCrlf);
        Assert.True(
            string.Equals(canonicalJson, reserializedCrlf, StringComparison.Ordinal),
            "Re-serialization from CRLF source diverged from canonical baseline.");

        // 3. Deserialize Mixed line endings
        var restoredFromMixed = LoreSerializer.Deserialize(mixedJson);
        Assert.Empty(restoredFromMixed.Validate());
        var reserializedMixed = LoreSerializer.Serialize(restoredFromMixed);
        Assert.True(
            string.Equals(canonicalJson, reserializedMixed, StringComparison.Ordinal),
            "Re-serialization from mixed line ending source diverged from canonical baseline.");
    }

    [Fact]
    public void Portability_05_EncodingInvariance_Utf8WithAndWithoutBom()
    {
        var graph = BuildRepresentativeKronoGraph();
        var canonicalJson = LoreSerializer.Serialize(graph);

        var tempDir = Path.Combine(Path.GetTempPath(), "SorophyPortabilityEncodingTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var noBomPath = Path.Combine(tempDir, "no_bom.lore");
            var withBomPath = Path.Combine(tempDir, "with_bom.lore");

            // Write without BOM
            File.WriteAllText(noBomPath, canonicalJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            // Write with BOM
            File.WriteAllText(withBomPath, canonicalJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Assert file bytes differ by exactly the 3-byte UTF-8 BOM preamble
            var noBomBytes = File.ReadAllBytes(noBomPath);
            var withBomBytes = File.ReadAllBytes(withBomPath);
            Assert.Equal(noBomBytes.Length + 3, withBomBytes.Length);
            Assert.Equal(0xEF, withBomBytes[0]);
            Assert.Equal(0xBB, withBomBytes[1]);
            Assert.Equal(0xBF, withBomBytes[2]);

            // Load and verify both via LoreStorage
            var restoredNoBom = LoreStorage.Load(noBomPath);
            var restoredWithBom = LoreStorage.Load(withBomPath);

            Assert.Empty(restoredNoBom.Validate());
            Assert.Empty(restoredWithBom.Validate());

            var reserializedNoBom = LoreSerializer.Serialize(restoredNoBom);
            var reserializedWithBom = LoreSerializer.Serialize(restoredWithBom);

            Assert.True(
                string.Equals(canonicalJson, reserializedNoBom, StringComparison.Ordinal),
                "Restored no-BOM file diverged from canonical baseline.");
            Assert.True(
                string.Equals(canonicalJson, reserializedWithBom, StringComparison.Ordinal),
                "Restored with-BOM file diverged from canonical baseline.");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Portability_06_PathSeparators_CrossPlatformFilesystemHandling()
    {
        var graph = BuildRepresentativeKronoGraph();
        var canonicalJson = LoreSerializer.Serialize(graph);

        var rootTemp = Path.Combine(Path.GetTempPath(), "SorophyPortabilityPaths_" + Guid.NewGuid().ToString("N"));
        var nestedRelativeDir = Path.Combine("nested", "sub", "dir");
        var fullTargetDir = Path.Combine(rootTemp, nestedRelativeDir);
        Directory.CreateDirectory(fullTargetDir);

        try
        {
            // Test 1: Native separator path
            var nativePath = Path.Combine(fullTargetDir, "native.lore");
            LoreStorage.Save(graph, nativePath);
            Assert.True(File.Exists(nativePath));
            var fromNative = LoreStorage.Load(nativePath);
            Assert.Empty(fromNative.Validate());
            Assert.Equal(canonicalJson, LoreSerializer.Serialize(fromNative));

            // Test 2: Normalized forward-slash path (portable across Windows, Linux, macOS)
            var forwardSlashPath = nativePath.Replace('\\', '/');
            var fromForwardSlash = LoreStorage.Load(forwardSlashPath);
            Assert.Empty(fromForwardSlash.Validate());
            Assert.Equal(canonicalJson, LoreSerializer.Serialize(fromForwardSlash));
        }
        finally
        {
            try { Directory.Delete(rootTemp, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Portability_07_GuidRepresentationInvariance()
    {
        var graph = BuildRepresentativeKronoGraph();
        var json = LoreSerializer.Serialize(graph);

        // GUIDs in serialized JSON must follow lowercase hyphenated 'D' format (RFC 4122)
        Assert.Contains("aaaaaaaa-1111-2222-3333-444444444444", json, StringComparison.Ordinal);
        Assert.DoesNotContain("aaaaaaaa111122223333444444444444", json, StringComparison.Ordinal);
        Assert.DoesNotContain("{aaaaaaaa-1111-2222-3333-444444444444}", json, StringComparison.Ordinal);
        Assert.DoesNotContain("AAAAAAAA-1111-2222-3333-444444444444", json, StringComparison.Ordinal);

        // Deserializer must accept uppercase GUIDs without data loss
        var uppercaseJson = json.Replace("aaaaaaaa-1111-2222-3333-444444444444", "AAAAAAAA-1111-2222-3333-444444444444");
        var restored = LoreSerializer.Deserialize(uppercaseJson);
        Assert.Empty(restored.Validate());
        Assert.True(restored.ContainsEntity(Guid.Parse("10000000-0000-0000-0000-000000000001")));
    }

    [Fact]
    public void Portability_08_StandaloneArtifactHarness_RoundTripLoop()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SorophyPortabilityHarness_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var artifactPath = Path.Combine(tempDir, "representative_krono_canonical.lore");

            // Step 1: Export canonical graph to artifact file
            var originalGraph = BuildRepresentativeKronoGraph();
            var expectedJson = LoreSerializer.Serialize(originalGraph);
            var expectedChecksum = ComputeSha256(expectedJson);

            LoreStorage.Save(originalGraph, artifactPath);
            Assert.True(File.Exists(artifactPath));

            // Step 2: Load artifact from disk
            var loadedGraph = LoreStorage.Load(artifactPath);

            // Step 3: Validate
            var validationErrors = loadedGraph.Validate();
            Assert.Empty(validationErrors);

            // Step 4: Re-serialize and verify checksum and byte-for-byte equality
            var reSerializedJson = LoreSerializer.Serialize(loadedGraph);
            var actualChecksum = ComputeSha256(reSerializedJson);

            Assert.Equal(expectedChecksum, actualChecksum);
            Assert.True(
                string.Equals(expectedJson, reSerializedJson, StringComparison.Ordinal),
                "Artifact round-trip did not produce byte-for-byte identical canonical serialization.");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Integration hook for multi-runner CI pipelines.
    /// If <see cref="InputArtifactEnvVar"/> is provided in the runner environment,
    /// this test loads the upstream artifact, asserts validation and canonical equivalence,
    /// and optionally saves it to <see cref="OutputArtifactEnvVar"/> for downstream runner consumption.
    /// When the environment variable is not set, this test validates the local baseline.
    /// </summary>
    [Fact]
    public void Portability_09_ChainedCIArtifact_IntegrationHook()
    {
        var inputPath = Environment.GetEnvironmentVariable(InputArtifactEnvVar);
        var outputPath = Environment.GetEnvironmentVariable(OutputArtifactEnvVar);

        var baselineGraph = BuildRepresentativeKronoGraph();
        var expectedCanonicalJson = LoreSerializer.Serialize(baselineGraph);

        inputPath = ResolvePath(inputPath);
        outputPath = ResolvePath(outputPath);

        if (!string.IsNullOrWhiteSpace(inputPath) && File.Exists(inputPath))
        {
            // Upstream CI stage provided a .lore artifact
            var incomingGraph = LoreStorage.Load(inputPath);
            Assert.Empty(incomingGraph.Validate());

            var reSerialized = LoreSerializer.Serialize(incomingGraph);
            Assert.True(
                string.Equals(expectedCanonicalJson, reSerialized, StringComparison.Ordinal),
                $"Artifact transferred from upstream runner ({inputPath}) diverged from canonical expectation.");

            // If downstream stage requested output artifact, emit it
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                LoreStorage.Save(incomingGraph, outputPath);
            }
        }
        else
        {
            // Standalone / initial export mode: verify self-consistency
            var restored = LoreSerializer.Deserialize(expectedCanonicalJson);
            Assert.Empty(restored.Validate());
            Assert.Equal(expectedCanonicalJson, LoreSerializer.Serialize(restored));

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                LoreStorage.Save(baselineGraph, outputPath);
            }
        }
    }

    private static string? ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
