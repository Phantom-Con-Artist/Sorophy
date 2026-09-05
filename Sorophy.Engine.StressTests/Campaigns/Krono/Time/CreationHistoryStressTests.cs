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
using System.Numerics;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.StressTests.Infrastructure;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class CreationHistoryStressTests
{
    internal static void RunCreationHistoryStress(TestCampaignContext context)
    {
        var random = new KronoStressRandom(context.Seed + 101);
        var schema = KronoTestSchemas.CreateDefaultNumericSchema("CreationHistoryTimeline");
        var executor = new SorophyRelationshipEvolutionExecutor();
        var graph = new SorophyGraph();

        // 1. Create a pool of entities
        const int entityPoolSize = 50;
        var entityIds = new List<Guid>(entityPoolSize);
        for (var i = 0; i < entityPoolSize; i++)
        {
            var entityId = random.NextGuid();
            var entity = new SorophyEntity
            {
                Id = entityId,
                Type = "Actor",
                Name = $"Entity_{i}"
            };
            graph.AddEntity(entity);
            entityIds.Add(entityId);
        }

        int operations = Math.Max(2000, context.Operations / 4);
        var activeEvolvedRelationships = new List<Guid>();
        BigInteger currentTick = 1000;

        for (var op = 0; op < operations; op++)
        {
            currentTick += random.Next(1, 10);
            int action = random.Next(10);

            if (action < 5 || activeEvolvedRelationships.Count == 0)
            {
                // A. Creation via Evolution
                var relId = random.NextGuid();
                var sourceId = entityIds[random.Next(entityPoolSize)];
                var targetId = entityIds[random.Next(entityPoolSize)];

                var effectiveTime = new SorophyTime(schema, currentTick.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
                var validFrom = random.NextBoolean()
                    ? new SorophyTime(schema, (currentTick - 10).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact)
                    : null;
                var validTill = random.NextBoolean()
                    ? new SorophyTime(schema, (currentTick + 500).ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact)
                    : null;

                var initialProps = new Dictionary<string, SorophyProperty>(StringComparer.Ordinal)
                {
                    ["created_op"] = new SorophyProperty
                    {
                        Name = "created_op",
                        Value = new SorophyValue(SorophyValueType.Integer, (long)op)
                    },
                    ["tag"] = new SorophyProperty
                    {
                        Name = "tag",
                        Value = new SorophyValue(SorophyValueType.String, $"rel_{op}")
                    }
                };

                var creation = new SorophyRelationshipCreation(
                    relId,
                    sourceId,
                    targetId,
                    "EvolvedEdge",
                    effectiveTime,
                    initialProps,
                    validFrom,
                    validTill);

                executor.Execute(graph, creation);
                activeEvolvedRelationships.Add(relId);

                // Verify immediate creation history invariants
                if (!graph.ContainsRelationship(relId))
                {
                    throw new InvalidOperationException($"Relationship '{relId}' was not added to active graph by creation evolution.");
                }

                if (!graph.TryGetRelationshipHistory(relId, out var history) || history is null)
                {
                    throw new InvalidOperationException($"Relationship '{relId}' must have a history recorded upon creation evolution.");
                }

                if (history.Facts.Count != 1)
                {
                    throw new InvalidOperationException($"Relationship '{relId}' history must contain exactly 1 fact after creation; found {history.Facts.Count}.");
                }

                var creationFact = history.Facts[0];
                if (creationFact.At != effectiveTime && SorophyTime.Compare(creationFact.At, effectiveTime) != 0)
                {
                    throw new InvalidOperationException($"Creation fact At ({creationFact.At}) must match creation EffectiveTime ({effectiveTime}).");
                }

                if (creationFact.RelationshipId != relId)
                {
                    throw new InvalidOperationException($"Creation fact RelationshipId mismatch.");
                }

                if (creationFact.SourceId != sourceId || creationFact.TargetId != targetId)
                {
                    throw new InvalidOperationException($"Creation fact endpoint mismatch.");
                }

                if (creationFact.Type != "EvolvedEdge")
                {
                    throw new InvalidOperationException($"Creation fact Type mismatch: expected 'EvolvedEdge', got '{creationFact.Type}'.");
                }

                if (!Equals(creationFact.ValidFrom, validFrom) || !Equals(creationFact.ValidTill, validTill))
                {
                    throw new InvalidOperationException($"Creation fact validity mismatch.");
                }

                if (creationFact.Properties.Count != initialProps.Count ||
                    !Equals(creationFact.Properties["tag"].Value.Value, $"rel_{op}"))
                {
                    throw new InvalidOperationException($"Creation fact properties mismatch.");
                }
            }
            else if (action < 7)
            {
                // B. CRITICAL NEGATIVE INVARIANT: Ordinary graph.AddRelationship must NOT fabricate history
                var directRelId = random.NextGuid();
                var sourceId = entityIds[random.Next(entityPoolSize)];
                var targetId = entityIds[random.Next(entityPoolSize)];

                var directRel = new SorophyRelationship
                {
                    Id = directRelId,
                    SourceId = sourceId,
                    TargetId = targetId,
                    Type = "DirectEdge"
                };
                directRel.Properties["ordinary"] = new SorophyProperty
                {
                    Name = "ordinary",
                    Value = new SorophyValue(SorophyValueType.Boolean, true)
                };

                graph.AddRelationship(directRel);

                if (!graph.ContainsRelationship(directRelId))
                {
                    throw new InvalidOperationException($"Direct relationship '{directRelId}' not found in active graph.");
                }

                if (graph.TryGetRelationshipHistory(directRelId, out var directHistory))
                {
                    throw new InvalidOperationException(
                        $"CRITICAL NEGATIVE INVARIANT VIOLATED: Ordinary AddRelationship fabricated history for '{directRelId}'! (Fact count: {directHistory?.Facts.Count})");
                }

                if (graph.RelationshipHistories.ContainsKey(directRelId))
                {
                    throw new InvalidOperationException(
                        $"CRITICAL NEGATIVE INVARIANT VIOLATED: Ordinary relationship '{directRelId}' present in RelationshipHistories dictionary!");
                }
            }
            else if (action < 9)
            {
                // C. Subsequent evolution on existing evolved relationship
                var targetIdx = random.Next(activeEvolvedRelationships.Count);
                var targetRelId = activeEvolvedRelationships[targetIdx];

                if (!graph.TryGetRelationshipHistory(targetRelId, out var existingHist) || existingHist is null)
                {
                    throw new InvalidOperationException($"Active evolved relationship '{targetRelId}' missing history.");
                }

                int prevFactCount = existingHist.Facts.Count;
                var modTime = new SorophyTime(schema, currentTick.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

                if (random.NextBoolean())
                {
                    // Property modification
                    var newProps = new Dictionary<string, SorophyProperty>(StringComparer.Ordinal)
                    {
                        [$"mod_{op}"] = new SorophyProperty
                        {
                            Name = $"mod_{op}",
                            Value = new SorophyValue(SorophyValueType.Integer, (long)op)
                        }
                    };

                    var propMod = new SorophyRelationshipPropertyModification(
                        targetRelId,
                        modTime,
                        propertiesToSet: newProps);

                    executor.Execute(graph, propMod);

                    if (existingHist.Facts.Count != prevFactCount + 1)
                    {
                        throw new InvalidOperationException(
                            $"Expected fact count {prevFactCount + 1} after property modification, got {existingHist.Facts.Count}.");
                    }

                    var latestFact = existingHist.Facts[^1];
                    if (SorophyTime.Compare(latestFact.At, modTime) != 0)
                    {
                        throw new InvalidOperationException("Latest fact At does not match property modification EffectiveTime.");
                    }
                }
                else
                {
                    // Type change
                    var newType = $"EvolvedType_{op % 5}";
                    var typeChange = new SorophyRelationshipTypeChange(targetRelId, newType, modTime);
                    executor.Execute(graph, typeChange);

                    if (existingHist.Facts.Count != prevFactCount + 1)
                    {
                        throw new InvalidOperationException(
                            $"Expected fact count {prevFactCount + 1} after type change, got {existingHist.Facts.Count}.");
                    }

                    if (graph.Relationships[targetRelId].Type != newType)
                    {
                        throw new InvalidOperationException("Active relationship type was not updated after type change.");
                    }
                }
            }
            else
            {
                // D. Termination
                var targetIdx = random.Next(activeEvolvedRelationships.Count);
                var targetRelId = activeEvolvedRelationships[targetIdx];
                activeEvolvedRelationships.RemoveAt(targetIdx);

                if (!graph.TryGetRelationshipHistory(targetRelId, out var existingHist) || existingHist is null)
                {
                    throw new InvalidOperationException($"Active evolved relationship '{targetRelId}' missing history before termination.");
                }

                int prevFactCount = existingHist.Facts.Count;
                var termTime = new SorophyTime(schema, currentTick.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
                var term = new SorophyRelationshipTermination(targetRelId, termTime);

                executor.Execute(graph, term);

                // Relationship must be removed from active graph
                if (graph.ContainsRelationship(targetRelId))
                {
                    throw new InvalidOperationException($"Terminated relationship '{targetRelId}' still present in active graph.");
                }

                // History must be preserved and incremented by 1 termination fact
                if (!graph.TryGetRelationshipHistory(targetRelId, out var termHist) || termHist is null)
                {
                    throw new InvalidOperationException($"Terminated relationship '{targetRelId}' lost its history.");
                }

                if (termHist.Facts.Count != prevFactCount + 1)
                {
                    throw new InvalidOperationException($"Expected fact count {prevFactCount + 1} after termination, got {termHist.Facts.Count}.");
                }

                if (SorophyTime.Compare(termHist.Facts[^1].At, termTime) != 0)
                {
                    throw new InvalidOperationException("Termination fact At does not match termination EffectiveTime.");
                }
            }

            if ((op + 1) % context.AuditInterval == 0)
            {
                var errors = graph.Validate();
                if (errors.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Graph validation failed during creation history stress at op {op + 1}: {string.Join("; ", errors)}");
                }
            }
        }

        // Final validation
        var finalErrors = graph.Validate();
        if (finalErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Final graph validation failed in creation history stress: {string.Join("; ", finalErrors)}");
        }
    }
}
