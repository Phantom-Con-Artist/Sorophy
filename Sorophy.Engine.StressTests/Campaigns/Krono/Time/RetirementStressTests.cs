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
using Sorophy.Engine.StressTests.Infrastructure;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class RetirementStressTests
{
    internal static void RunRetirementStress(TestCampaignContext context)
    {
        var random = new KronoStressRandom(context.Seed + 202);
        var schema = KronoTestSchemas.CreateDefaultNumericSchema("RetirementTimeline");
        var executor = new SorophyRelationshipEvolutionExecutor();
        var graph = new SorophyGraph();

        const int entityCount = 40;
        var entityIds = new List<Guid>(entityCount);
        for (var i = 0; i < entityCount; i++)
        {
            var entityId = random.NextGuid();
            var entity = new SorophyEntity
            {
                Id = entityId,
                Type = "Node",
                Name = $"Entity_{i}"
            };
            graph.AddEntity(entity);
            entityIds.Add(entityId);
        }

        var retiredIds = new HashSet<Guid>();
        var activeEvolved = new List<Guid>();
        var activeDirect = new List<Guid>();

        int operations = Math.Max(2000, context.Operations / 4);
        BigInteger currentTick = 5000;

        for (var op = 0; op < operations; op++)
        {
            currentTick += random.Next(1, 10);
            int action = random.Next(6);

            if (action == 0 || (activeEvolved.Count == 0 && activeDirect.Count == 0))
            {
                // Add evolved relationship
                var relId = random.NextGuid();
                var s = entityIds[random.Next(entityCount)];
                var t = entityIds[random.Next(entityCount)];
                var time = new SorophyTime(schema, currentTick.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

                var creation = new SorophyRelationshipCreation(relId, s, t, "EvolvedRel", time);
                executor.Execute(graph, creation);
                activeEvolved.Add(relId);
            }
            else if (action == 1)
            {
                // Add direct relationship
                var relId = random.NextGuid();
                var s = entityIds[random.Next(entityCount)];
                var t = entityIds[random.Next(entityCount)];

                var directRel = new SorophyRelationship
                {
                    Id = relId,
                    SourceId = s,
                    TargetId = t,
                    Type = "DirectRel"
                };
                graph.AddRelationship(directRel);
                activeDirect.Add(relId);
            }
            else if (action == 2 && activeEvolved.Count > 0)
            {
                // Temporal Termination via Evolution
                var idx = random.Next(activeEvolved.Count);
                var relId = activeEvolved[idx];
                activeEvolved.RemoveAt(idx);

                var rel = graph.Relationships[relId];
                var s = rel.SourceId;
                var t = rel.TargetId;

                var time = new SorophyTime(schema, currentTick.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
                var term = new SorophyRelationshipTermination(relId, time);
                executor.Execute(graph, term);
                retiredIds.Add(relId);

                // Assert retirement state
                AssertRetiredRelationship(graph, relId, s, t, schema, executor, hasHistoryExpected: true);
            }
            else if (action == 3 && activeDirect.Count > 0)
            {
                // Ordinary Direct Removal
                var idx = random.Next(activeDirect.Count);
                var relId = activeDirect[idx];
                activeDirect.RemoveAt(idx);

                var rel = graph.Relationships[relId];
                var s = rel.SourceId;
                var t = rel.TargetId;

                graph.RemoveRelationship(relId);
                retiredIds.Add(relId);

                // Assert retirement state; ordinary removal MUST NOT create history
                AssertRetiredRelationship(graph, relId, s, t, schema, executor, hasHistoryExpected: false);
            }
            else if (action == 4 && activeEvolved.Count > 0)
            {
                // Direct Removal of an evolved relationship (has history, but direct removal does NOT add a termination fact)
                var idx = random.Next(activeEvolved.Count);
                var relId = activeEvolved[idx];
                activeEvolved.RemoveAt(idx);

                var rel = graph.Relationships[relId];
                var s = rel.SourceId;
                var t = rel.TargetId;

                graph.TryGetRelationshipHistory(relId, out var beforeHist);
                int factsBeforeRemoval = beforeHist?.Facts.Count ?? 0;

                graph.RemoveRelationship(relId);
                retiredIds.Add(relId);

                AssertRetiredRelationship(graph, relId, s, t, schema, executor, hasHistoryExpected: true);

                // Check that direct removal did NOT synthesize a termination fact
                graph.TryGetRelationshipHistory(relId, out var afterHist);
                if (afterHist?.Facts.Count != factsBeforeRemoval)
                {
                    throw new InvalidOperationException(
                        $"Direct removal of evolved relationship '{relId}' fabricated history! Before: {factsBeforeRemoval}, After: {afterHist?.Facts.Count}");
                }
            }
            else
            {
                // Check public retirement collections consistency
                if (retiredIds.Count > 0)
                {
                    var sampleRetired = retiredIds.First();
                    if (!graph.IsRelationshipIdRetired(sampleRetired))
                    {
                        throw new InvalidOperationException($"IsRelationshipIdRetired returned false for retired ID '{sampleRetired}'.");
                    }
                }
            }

            if ((op + 1) % context.AuditInterval == 0)
            {
                AuditRetirementState(graph, retiredIds);
            }
        }

        AuditRetirementState(graph, retiredIds);
    }

    private static void AssertRetiredRelationship(
        SorophyGraph graph,
        Guid retiredId,
        Guid sourceId,
        Guid targetId,
        SorophyTimeSchema schema,
        SorophyRelationshipEvolutionExecutor executor,
        bool hasHistoryExpected)
    {
        // 1. Retirement query APIs
        if (!graph.IsRelationshipIdRetired(retiredId))
        {
            throw new InvalidOperationException($"IsRelationshipIdRetired returned false for retired relationship '{retiredId}'.");
        }

        if (!graph.RetiredRelationshipIds.Contains(retiredId))
        {
            throw new InvalidOperationException($"RetiredRelationshipIds does not contain retired relationship '{retiredId}'.");
        }

        // 2. Active graph queries must return false / missing
        if (graph.ContainsRelationship(retiredId))
        {
            throw new InvalidOperationException($"ContainsRelationship returned true for retired relationship '{retiredId}'.");
        }

        if (graph.TryGetRelationship(retiredId, out _))
        {
            throw new InvalidOperationException($"TryGetRelationship succeeded for retired relationship '{retiredId}'.");
        }

        if (graph.Relationships.ContainsKey(retiredId))
        {
            throw new InvalidOperationException($"Relationships dictionary contains retired relationship '{retiredId}'.");
        }

        // 3. Adjacency traversal must not contain retired relationship
        if (graph.GetOutgoingRelationships(sourceId).Any(r => r.Id == retiredId))
        {
            throw new InvalidOperationException($"GetOutgoingRelationships contains retired relationship '{retiredId}'.");
        }

        if (graph.GetIncomingRelationships(targetId).Any(r => r.Id == retiredId))
        {
            throw new InvalidOperationException($"GetIncomingRelationships contains retired relationship '{retiredId}'.");
        }

        // 4. History check
        bool hasHistory = graph.TryGetRelationshipHistory(retiredId, out _);
        if (hasHistory != hasHistoryExpected)
        {
            throw new InvalidOperationException(
                $"Relationship '{retiredId}' history expectation failed: expected {hasHistoryExpected}, but got {hasHistory}.");
        }

        // 5. Tombstone behavior / ID reuse rejection
        var dummyRel = new SorophyRelationship
        {
            Id = retiredId,
            SourceId = sourceId,
            TargetId = targetId,
            Type = "ReuseAttempt"
        };

        bool addThrew = false;
        try
        {
            graph.AddRelationship(dummyRel);
        }
        catch (InvalidOperationException)
        {
            addThrew = true;
        }

        if (!addThrew)
        {
            throw new InvalidOperationException($"AddRelationship did not reject reuse of retired ID '{retiredId}'.");
        }

        var reuseTime = new SorophyTime(schema, "999999", "Tick", SorophyTimePrecision.Exact);
        var reuseCreation = new SorophyRelationshipCreation(retiredId, sourceId, targetId, "ReuseAttempt", reuseTime);

        bool evolveThrew = false;
        try
        {
            executor.Execute(graph, reuseCreation);
        }
        catch (InvalidOperationException)
        {
            evolveThrew = true;
        }

        if (!evolveThrew)
        {
            throw new InvalidOperationException($"Creation evolution did not reject reuse of retired ID '{retiredId}'.");
        }
    }

    private static void AuditRetirementState(SorophyGraph graph, HashSet<Guid> expectedRetiredIds)
    {
        if (graph.RetiredRelationshipIds.Count != expectedRetiredIds.Count)
        {
            throw new InvalidOperationException(
                $"Retired relationship count mismatch: expected {expectedRetiredIds.Count}, got {graph.RetiredRelationshipIds.Count}.");
        }

        foreach (var id in expectedRetiredIds)
        {
            if (!graph.IsRelationshipIdRetired(id))
            {
                throw new InvalidOperationException($"Audited ID '{id}' was not reported as retired.");
            }

            if (graph.ContainsRelationship(id))
            {
                throw new InvalidOperationException($"Audited retired ID '{id}' was found in active relationships.");
            }
        }

        var errors = graph.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Graph validation failed during retirement audit: {string.Join("; ", errors)}");
        }
    }
}
