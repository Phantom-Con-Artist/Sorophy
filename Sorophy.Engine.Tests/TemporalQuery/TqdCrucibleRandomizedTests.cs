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
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.TemporalQuery;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalQuery;

/// <summary>
/// Domain C14 (Randomized Adversarial Testing) and Domain C15 (Randomized Query Assault).
/// Uses deterministic PRNG seeds: 1, 42, 12345, 987654321.
/// </summary>
public sealed class TqdCrucibleRandomizedTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(12345)]
    [InlineData(987654321)]
    public void Randomized_DeterministicCampaign_InvariantsHold(int seed)
    {
        var rng = new Random(seed);
        var graph = new SorophyGraph();
        var executor = new SorophyRelationshipEvolutionExecutor();

        // Step 1: Generate entities & Events
        var entityIds = new List<Guid>();
        for (int i = 0; i < 15; i++)
        {
            var id = Guid.NewGuid();
            graph.AddEntity(new SorophyEntity { Id = id, Name = $"Entity_{i}" });
            entityIds.Add(id);
        }

        var eventIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var eventTime = TqdCrucibleTestHelper.CreateTime(_schema, rng.Next(1, 100));
            var ev = TqdCrucibleTestHelper.CreateEventEntity(graph, eventTime, $"Event_{i}");
            eventIds.Add(ev.Id);
        }

        // Step 2: Randomly evolve relationships
        var activeRelIds = new List<Guid>();
        var terminatedRelIds = new List<Guid>();
        var allRelIds = new List<Guid>();

        for (int op = 0; op < 60; op++)
        {
            int action = rng.Next(0, 5);
            var coord = rng.Next(1, 200);
            var time = TqdCrucibleTestHelper.CreateTime(_schema, coord);
            Guid? eventAnchor = rng.Next(0, 3) == 0 ? eventIds[rng.Next(eventIds.Count)] : null;

            if (action == 0 || activeRelIds.Count == 0)
            {
                // Create relationship
                var relId = Guid.NewGuid();
                var s = entityIds[rng.Next(entityIds.Count)];
                var t = entityIds[rng.Next(entityIds.Count)];
                executor.Execute(graph, new SorophyRelationshipCreation(
                    relId, s, t, $"Type_{op}", time, eventEntityId: eventAnchor));
                activeRelIds.Add(relId);
                allRelIds.Add(relId);
            }
            else if (action == 1)
            {
                // Property mod
                var targetRel = activeRelIds[rng.Next(activeRelIds.Count)];
                executor.Execute(graph, new SorophyRelationshipPropertyModification(
                    targetRel, time,
                    propertiesToSet: new Dictionary<string, SorophyProperty>
                    {
                        ["K"] = new SorophyProperty { Name = "K", Value = new SorophyValue(SorophyValueType.Integer, (long)op) }
                    },
                    eventEntityId: eventAnchor));
            }
            else if (action == 2)
            {
                // Type change
                var targetRel = activeRelIds[rng.Next(activeRelIds.Count)];
                executor.Execute(graph, new SorophyRelationshipTypeChange(
                    targetRel, $"Evolved_{op}", time, eventEntityId: eventAnchor));
            }
            else if (action == 3)
            {
                // Validity change
                var targetRel = activeRelIds[rng.Next(activeRelIds.Count)];
                var vFrom = TqdCrucibleTestHelper.CreateTime(_schema, Math.Max(0, coord - 10));
                var vTill = TqdCrucibleTestHelper.CreateTime(_schema, coord + 50);
                executor.Execute(graph, new SorophyRelationshipValidityChange(
                    targetRel, time, vFrom, vTill, eventEntityId: eventAnchor));
            }
            else if (action == 4 && activeRelIds.Count > 2)
            {
                // Terminate
                var targetRel = activeRelIds[rng.Next(activeRelIds.Count)];
                executor.Execute(graph, new SorophyRelationshipTermination(
                    targetRel, time, eventEntityId: eventAnchor));
                activeRelIds.Remove(targetRel);
                terminatedRelIds.Add(targetRel);
            }
        }

        // Step 3: Randomized Query Assault (Domain C15)
        var tqd = graph.TemporalQuery;

        for (int q = 0; q < 50; q++)
        {
            int queryKind = rng.Next(0, 6);
            var queryTime = TqdCrucibleTestHelper.CreateTime(_schema, rng.Next(0, 250));

            if (queryKind == 0)
            {
                // Point-in-time snapshot & lookup invariant
                var targetRel = allRelIds[rng.Next(allRelIds.Count)];
                bool exists = tqd.RelationshipExistsAt(targetRel, queryTime);
                var relObj = tqd.GetRelationshipAt(targetRel, queryTime);

                Assert.Equal(exists, relObj is not null);
                if (exists)
                {
                    Assert.Equal(targetRel, relObj!.Id);
                }
            }
            else if (queryKind == 1)
            {
                // Entity existence invariant
                var targetEntity = entityIds[rng.Next(entityIds.Count)];
                bool exists = tqd.EntityExistsAt(targetEntity, queryTime);
                var entObj = tqd.GetEntityAt(targetEntity, queryTime);

                Assert.True(exists);
                Assert.NotNull(entObj);
                Assert.Equal(targetEntity, entObj.Id);
            }
            else if (queryKind == 2)
            {
                // Interval facts & modified IDs invariant
                int tStart = rng.Next(0, 100);
                int tEnd = tStart + rng.Next(0, 100);
                var from = TqdCrucibleTestHelper.CreateTime(_schema, tStart);
                var till = TqdCrucibleTestHelper.CreateTime(_schema, tEnd);

                var facts = tqd.GetFactsInInterval(from, till);
                var modifiedIds = tqd.GetModifiedRelationshipIds(from, till);

                // All facts within boundary
                Assert.All(facts, f =>
                {
                    Assert.True(SorophyTime.Compare(f.At, from) >= 0);
                    Assert.True(SorophyTime.Compare(f.At, till) <= 0);
                });

                // Distinct relationship IDs in facts must equal modifiedIds
                var distinctRelIds = new HashSet<Guid>(facts.Select(f => f.RelationshipId));
                Assert.Equal(distinctRelIds.Count, modifiedIds.Count);
                Assert.All(distinctRelIds, id => Assert.Contains(id, modifiedIds));
            }
            else if (queryKind == 3)
            {
                // Provenance invariant
                var targetEvent = eventIds[rng.Next(eventIds.Count)];
                var factsByEv = tqd.GetFactsByEvent(targetEvent);
                var relsByEv = tqd.GetRelationshipsEvolvedByEvent(targetEvent);

                Assert.All(factsByEv, f => Assert.Equal(targetEvent, f.EventEntityId));
                var distinctEvolved = new HashSet<Guid>(factsByEv.Select(f => f.RelationshipId));
                Assert.Equal(distinctEvolved.Count, relsByEv.Count);
                Assert.All(distinctEvolved, id => Assert.Contains(id, relsByEv));
            }
            else if (queryKind == 4)
            {
                // History query invariant
                var targetRel = allRelIds[rng.Next(allRelIds.Count)];
                var history = tqd.GetRelationshipHistory(targetRel);

                Assert.NotNull(history);
                Assert.NotEmpty(history.Facts);
                Assert.Equal(targetRel, history.RelationshipId);
            }
            else if (queryKind == 5)
            {
                // Outbound/inbound relationships invariant
                var targetEntity = entityIds[rng.Next(entityIds.Count)];
                var outbound = tqd.GetOutboundRelationshipsAt(targetEntity, queryTime).ToList();
                var inbound = tqd.GetInboundRelationshipsAt(targetEntity, queryTime).ToList();

                Assert.All(outbound, r => Assert.Equal(targetEntity, r.SourceId));
                Assert.All(inbound, r => Assert.Equal(targetEntity, r.TargetId));
            }
        }
    }
}

