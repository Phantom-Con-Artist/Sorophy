/*
 * Sorophy Engine ? a structured knowledge and graph engine
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
using System.Linq;
using System.Numerics;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.StressTests.Infrastructure;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class TemporalLifecycleRoundTripStressTests
{
    internal static void RunLifecycleRoundTripStress(TestCampaignContext context)
    {
        var random = new KronoStressRandom(context.Seed + 303);
        var schema = KronoTestSchemas.CreateDefaultNumericSchema("RoundTripTimeline");
        var executor = new SorophyRelationshipEvolutionExecutor();

        int cycles = Math.Max(200, context.Operations / 50);

        for (var cycle = 0; cycle < cycles; cycle++)
        {
            var graph = new SorophyGraph();

            // 1. Entities
            const int entityCount = 12;
            var entityIds = new List<Guid>(entityCount);
            for (var i = 0; i < entityCount; i++)
            {
                var eId = random.NextGuid();
                var entity = new SorophyEntity
                {
                    Id = eId,
                    Type = "RoundTripNode",
                    Name = $"Node_{cycle}_{i}"
                };
                graph.AddEntity(entity);
                entityIds.Add(eId);
            }

            BigInteger currentTick = 100_000 + (cycle * 1_000);

            // 2. Complex lifecycle scenario per cycle
            // A: Fully evolved through all steps and terminated
            var relAId = random.NextGuid();
            var sA = entityIds[0];
            var tA = entityIds[1];

            var tA_create = new SorophyTime(schema, (currentTick + 10).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tA_mod = new SorophyTime(schema, (currentTick + 20).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tA_type = new SorophyTime(schema, (currentTick + 30).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tA_val = new SorophyTime(schema, (currentTick + 40).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tA_term = new SorophyTime(schema, (currentTick + 50).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

            var validFromA = new SorophyTime(schema, (currentTick + 5).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var validTillA = new SorophyTime(schema, (currentTick + 100).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

            var propsA = new Dictionary<string, SorophyProperty>(StringComparer.Ordinal)
            {
                ["initial"] = new SorophyProperty { Name = "initial", Value = new SorophyValue(SorophyValueType.String, "A0") }
            };

            executor.Execute(graph, new SorophyRelationshipCreation(relAId, sA, tA, "TypeA", tA_create, propsA, validFromA, validTillA));

            var modPropsA = new Dictionary<string, SorophyProperty>(StringComparer.Ordinal)
            {
                ["updated"] = new SorophyProperty { Name = "updated", Value = new SorophyValue(SorophyValueType.Integer, 42L) }
            };
            executor.Execute(graph, new SorophyRelationshipPropertyModification(relAId, tA_mod, propertiesToSet: modPropsA));

            executor.Execute(graph, new SorophyRelationshipTypeChange(relAId, "TypeA_New", tA_type));

            var newValidTillA = new SorophyTime(schema, (currentTick + 200).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            executor.Execute(graph, new SorophyRelationshipValidityChange(relAId, tA_val, validFromA, newValidTillA));

            executor.Execute(graph, new SorophyRelationshipTermination(relAId, tA_term));

            // B: Evolved and remains active
            var relBId = random.NextGuid();
            var sB = entityIds[2];
            var tB = entityIds[3];
            var tB_create = new SorophyTime(schema, (currentTick + 15).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tB_mod = new SorophyTime(schema, (currentTick + 25).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

            executor.Execute(graph, new SorophyRelationshipCreation(relBId, sB, tB, "TypeB", tB_create));
            executor.Execute(graph, new SorophyRelationshipPropertyModification(relBId, tB_mod,
                propertiesToSet: new Dictionary<string, SorophyProperty>
                {
                    ["active_prop"] = new SorophyProperty { Name = "active_prop", Value = new SorophyValue(SorophyValueType.Boolean, true) }
                }));

            // C: Created via evolution, remains active (single creation fact)
            var relCId = random.NextGuid();
            var sC = entityIds[4];
            var tC = entityIds[5];
            var tC_create = new SorophyTime(schema, (currentTick + 18).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            executor.Execute(graph, new SorophyRelationshipCreation(relCId, sC, tC, "TypeC", tC_create));

            // D: Direct Add, then Direct Remove (retired, NO history)
            var relDId = random.NextGuid();
            var sD = entityIds[6];
            var tD = entityIds[7];
            graph.AddRelationship(new SorophyRelationship { Id = relDId, SourceId = sD, TargetId = tD, Type = "TypeD" });
            graph.RemoveRelationship(relDId);

            // E: Direct Add, remains active (active, NO history)
            var relEId = random.NextGuid();
            var sE = entityIds[8];
            var tE = entityIds[9];
            graph.AddRelationship(new SorophyRelationship { Id = relEId, SourceId = sE, TargetId = tE, Type = "TypeE" });

            // F: Evolved, then directly removed (retired, preserves existing facts, no termination fact)
            var relFId = random.NextGuid();
            var sF = entityIds[10];
            var tF = entityIds[11];
            var tF_create = new SorophyTime(schema, (currentTick + 12).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tF_mod = new SorophyTime(schema, (currentTick + 22).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

            executor.Execute(graph, new SorophyRelationshipCreation(relFId, sF, tF, "TypeF", tF_create));
            executor.Execute(graph, new SorophyRelationshipPropertyModification(relFId, tF_mod,
                propertiesToSet: new Dictionary<string, SorophyProperty>
                {
                    ["f_prop"] = new SorophyProperty { Name = "f_prop", Value = new SorophyValue(SorophyValueType.String, "F") }
                }));
            graph.RemoveRelationship(relFId);

            // 3. Pre-serialization validation
            var preErrors = graph.Validate();
            if (preErrors.Count > 0)
            {
                throw new InvalidOperationException($"Pre-serialization graph validation failed at cycle {cycle}: {string.Join("; ", preErrors)}");
            }

            // 4. Serialize to .lore v2 JSON
            string json = LoreSerializer.Serialize(graph);

            // 5. Deserialize
            SorophyGraph restored = LoreSerializer.Deserialize(json);

            // 6. Post-serialization validation
            var postErrors = restored.Validate();
            if (postErrors.Count > 0)
            {
                throw new InvalidOperationException($"Post-deserialization graph validation failed at cycle {cycle}: {string.Join("; ", postErrors)}");
            }

            // 7. Verify Semantic Parity
            VerifyGraphSemanticParity(graph, restored, cycle);

            // 8. Idempotent round-trip check
            if (cycle % 20 == 0)
            {
                string json2 = LoreSerializer.Serialize(restored);
                SorophyGraph restored2 = LoreSerializer.Deserialize(json2);
                var postErrors2 = restored2.Validate();
                if (postErrors2.Count > 0)
                {
                    throw new InvalidOperationException($"Idempotent second round-trip validation failed at cycle {cycle}: {string.Join("; ", postErrors2)}");
                }
                VerifyGraphSemanticParity(restored, restored2, cycle);
            }
        }
    }

    private static void VerifyGraphSemanticParity(SorophyGraph original, SorophyGraph restored, int cycle)
    {
        // Entity parity
        if (restored.Entities.Count != original.Entities.Count)
        {
            throw new InvalidOperationException(
                $"Entity count mismatch at cycle {cycle}: original {original.Entities.Count}, restored {restored.Entities.Count}.");
        }

        // Active relationship count
        if (restored.Relationships.Count != original.Relationships.Count)
        {
            throw new InvalidOperationException(
                $"Relationship count mismatch at cycle {cycle}: original {original.Relationships.Count}, restored {restored.Relationships.Count}.");
        }

        // Active relationships
        foreach (var origRel in original.Relationships.Values)
        {
            if (!restored.TryGetRelationship(origRel.Id, out var rRel) || rRel is null)
            {
                throw new InvalidOperationException($"Active relationship '{origRel.Id}' missing in restored graph at cycle {cycle}.");
            }

            if (rRel.SourceId != origRel.SourceId || rRel.TargetId != origRel.TargetId)
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' endpoint mismatch in restored graph.");
            }

            if (rRel.Type != origRel.Type)
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' type mismatch: expected '{origRel.Type}', got '{rRel.Type}'.");
            }

            // Temporal validities
            if ((rRel.ValidFrom is null) != (origRel.ValidFrom is null))
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' ValidFrom nullness mismatch.");
            }
            if (origRel.ValidFrom is not null && SorophyTime.Compare(rRel.ValidFrom!, origRel.ValidFrom) != 0)
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' ValidFrom coordinate mismatch.");
            }

            if ((rRel.ValidTill is null) != (origRel.ValidTill is null))
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' ValidTill nullness mismatch.");
            }
            if (origRel.ValidTill is not null && SorophyTime.Compare(rRel.ValidTill!, origRel.ValidTill) != 0)
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' ValidTill coordinate mismatch.");
            }

            // Properties
            if (rRel.Properties.Count != origRel.Properties.Count)
            {
                throw new InvalidOperationException($"Relationship '{origRel.Id}' property count mismatch.");
            }
            foreach (var prop in origRel.Properties)
            {
                if (!rRel.Properties.TryGetValue(prop.Key, out var rProp) || rProp is null)
                {
                    throw new InvalidOperationException($"Relationship '{origRel.Id}' missing property '{prop.Key}'.");
                }
                if (rProp.Value.Type != prop.Value.Value.Type)
                {
                    throw new InvalidOperationException($"Relationship '{origRel.Id}' property '{prop.Key}' type mismatch.");
                }
            }
        }

        // Retired relationship IDs parity
        if (restored.RetiredRelationshipIds.Count != original.RetiredRelationshipIds.Count)
        {
            throw new InvalidOperationException(
                $"Retired relationship count mismatch at cycle {cycle}: original {original.RetiredRelationshipIds.Count}, restored {restored.RetiredRelationshipIds.Count}.");
        }

        foreach (var retiredId in original.RetiredRelationshipIds)
        {
            if (!restored.IsRelationshipIdRetired(retiredId))
            {
                throw new InvalidOperationException($"Retired ID '{retiredId}' not reported as retired in restored graph at cycle {cycle}.");
            }

            if (!restored.RetiredRelationshipIds.Contains(retiredId))
            {
                throw new InvalidOperationException($"Retired ID '{retiredId}' missing from RetiredRelationshipIds collection in restored graph.");
            }

            if (restored.ContainsRelationship(retiredId))
            {
                throw new InvalidOperationException($"Retired ID '{retiredId}' unexpectedly present in restored active relationships.");
            }

            // Tombstone behavior holds on restored graph
            try
            {
                restored.AddRelationship(new SorophyRelationship
                {
                    Id = retiredId,
                    SourceId = original.Entities.Keys.First(),
                    TargetId = original.Entities.Keys.Last(),
                    Type = "TombstoneCheck"
                });
                throw new InvalidOperationException($"Restored graph did not reject adding relationship with retired ID '{retiredId}'.");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Relationship histories parity
        if (restored.RelationshipHistories.Count != original.RelationshipHistories.Count)
        {
            throw new InvalidOperationException(
                $"Relationship history count mismatch at cycle {cycle}: original {original.RelationshipHistories.Count}, restored {restored.RelationshipHistories.Count}.");
        }

        foreach (var origHistEntry in original.RelationshipHistories)
        {
            var relId = origHistEntry.Key;
            var origHist = origHistEntry.Value;

            if (!restored.TryGetRelationshipHistory(relId, out var rHist) || rHist is null)
            {
                throw new InvalidOperationException($"History for relationship '{relId}' missing in restored graph at cycle {cycle}.");
            }

            if (rHist.Facts.Count != origHist.Facts.Count)
            {
                throw new InvalidOperationException(
                    $"Fact count mismatch for relationship '{relId}' at cycle {cycle}: original {origHist.Facts.Count}, restored {rHist.Facts.Count}.");
            }

            for (var k = 0; k < origHist.Facts.Count; k++)
            {
                var origFact = origHist.Facts[k];
                var rFact = rHist.Facts[k];

                if (rFact.RelationshipId != origFact.RelationshipId)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' RelationshipId mismatch.");
                }

                if (rFact.SourceId != origFact.SourceId || rFact.TargetId != origFact.TargetId)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' endpoint mismatch.");
                }

                if (rFact.Type != origFact.Type)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' Type mismatch: expected '{origFact.Type}', got '{rFact.Type}'.");
                }

                if (SorophyTime.Compare(rFact.At, origFact.At) != 0)
                {
                    throw new InvalidOperationException(
                        $"Fact {k} for relationship '{relId}' At timestamp mismatch: original {origFact.At}, restored {rFact.At}.");
                }

                if ((rFact.ValidFrom is null) != (origFact.ValidFrom is null))
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' ValidFrom nullness mismatch.");
                }
                if (origFact.ValidFrom is not null && SorophyTime.Compare(rFact.ValidFrom!, origFact.ValidFrom) != 0)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' ValidFrom mismatch.");
                }

                if ((rFact.ValidTill is null) != (origFact.ValidTill is null))
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' ValidTill nullness mismatch.");
                }
                if (origFact.ValidTill is not null && SorophyTime.Compare(rFact.ValidTill!, origFact.ValidTill) != 0)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' ValidTill mismatch.");
                }

                if (rFact.Properties.Count != origFact.Properties.Count)
                {
                    throw new InvalidOperationException($"Fact {k} for relationship '{relId}' property count mismatch.");
                }
            }
        }
    }
}
