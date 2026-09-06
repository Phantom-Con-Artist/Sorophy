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
/// Domain C06 (Provenance Assault).
/// </summary>
public sealed class TqdCrucibleProvenanceTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    [Fact]
    public void Provenance_OneEvent_OneEvolution()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var eventEntity = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Inception");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "CreatedByEvent", t100, eventEntityId: eventEntity.Id));

        var tqd = graph.TemporalQuery;

        var facts = tqd.GetFactsByEvent(eventEntity.Id);
        Assert.Single(facts);
        Assert.Equal(relId, facts[0].RelationshipId);
        Assert.Equal(eventEntity.Id, facts[0].EventEntityId);

        var evolvedRels = tqd.GetRelationshipsEvolvedByEvent(eventEntity);
        Assert.Single(evolvedRels);
        Assert.Contains(relId, evolvedRels);
    }

    [Fact]
    public void Provenance_OneEvent_MultipleEvolutionsOnSameRelationship()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var eventEntity = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "MajorReorganization");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", t100, eventEntityId: eventEntity.Id));
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, t100,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["X"] = new SorophyProperty { Name = "X", Value = new SorophyValue(SorophyValueType.Integer, 1L) }
            },
            eventEntityId: eventEntity.Id));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step3", t100, eventEntityId: eventEntity.Id));

        var tqd = graph.TemporalQuery;

        var facts = tqd.GetFactsByEvent(eventEntity.Id);
        Assert.Equal(3, facts.Count);
        Assert.All(facts, f => Assert.Equal(eventEntity.Id, f.EventEntityId));

        // Distinct relationship IDs count must be 1
        var evolvedRels = tqd.GetRelationshipsEvolvedByEvent(eventEntity.Id);
        Assert.Single(evolvedRels);
        Assert.Contains(relId, evolvedRels);
    }

    [Fact]
    public void Provenance_OneEvent_MultipleRelationships()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t50 = TqdCrucibleTestHelper.CreateTime(_schema, 50);
        var eventEntity = TqdCrucibleTestHelper.CreateEventEntity(graph, t50, "GlobalTreaty");

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relIds = new List<Guid>();

        for (int i = 0; i < 5; i++)
        {
            var relId = Guid.NewGuid();
            relIds.Add(relId);
            executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, $"Treaty_{i}", t50, eventEntityId: eventEntity.Id));
        }

        var tqd = graph.TemporalQuery;

        var facts = tqd.GetFactsByEvent(eventEntity);
        Assert.Equal(5, facts.Count);

        var evolvedRels = tqd.GetRelationshipsEvolvedByEvent(eventEntity);
        Assert.Equal(5, evolvedRels.Count);
        Assert.All(relIds, id => Assert.Contains(id, evolvedRels));
    }

    [Fact]
    public void Provenance_MultipleEvents_SameRelationshipAtDifferentTimes()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);
        var t20 = TqdCrucibleTestHelper.CreateTime(_schema, 20);
        var t30 = TqdCrucibleTestHelper.CreateTime(_schema, 30);

        var event1 = TqdCrucibleTestHelper.CreateEventEntity(graph, t10, "Event 1");
        var event2 = TqdCrucibleTestHelper.CreateEventEntity(graph, t20, "Event 2");
        var event3 = TqdCrucibleTestHelper.CreateEventEntity(graph, t30, "Event 3");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();

        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Initial", t10, eventEntityId: event1.Id));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Modified", t20, eventEntityId: event2.Id));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t30, eventEntityId: event3.Id));

        var tqd = graph.TemporalQuery;

        var factsE1 = tqd.GetFactsByEvent(event1.Id);
        var factsE2 = tqd.GetFactsByEvent(event2.Id);
        var factsE3 = tqd.GetFactsByEvent(event3.Id);

        Assert.Single(factsE1);
        Assert.Single(factsE2);
        Assert.Single(factsE3);

        Assert.Equal(t10, factsE1[0].At);
        Assert.Equal(t20, factsE2[0].At);
        Assert.Equal(t30, factsE3[0].At);
    }

    [Fact]
    public void Provenance_MultipleEvents_SameRelationshipAtSameTime_PartitionedCleanly()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        var eventAlpha = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Alpha");
        var eventBeta = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "Beta");

        var rel1 = Guid.NewGuid();
        var rel2 = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(rel1, s, t, "TypeAlpha", t100, eventEntityId: eventAlpha.Id));
        executor.Execute(graph, new SorophyRelationshipCreation(rel2, s, t, "TypeBeta", t100, eventEntityId: eventBeta.Id));

        var tqd = graph.TemporalQuery;

        var factsAlpha = tqd.GetFactsByEvent(eventAlpha);
        var factsBeta = tqd.GetFactsByEvent(eventBeta);

        Assert.Single(factsAlpha);
        Assert.Equal(rel1, factsAlpha[0].RelationshipId);

        Assert.Single(factsBeta);
        Assert.Equal(rel2, factsBeta[0].RelationshipId);
    }

    [Fact]
    public void Provenance_EventWithZeroEvolutions_ReturnsEmpty()
    {
        var graph = new SorophyGraph();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var idleEvent = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "IdleEvent");

        var tqd = graph.TemporalQuery;

        Assert.Empty(tqd.GetFactsByEvent(idleEvent));
        Assert.Empty(tqd.GetFactsByEvent(idleEvent.Id));
        Assert.Empty(tqd.GetRelationshipsEvolvedByEvent(idleEvent));
        Assert.Empty(tqd.GetRelationshipsEvolvedByEvent(idleEvent.Id));
    }

    [Fact]
    public void Provenance_NullEventEntityId_NeverFalselyAttributed()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var registeredEvent = TqdCrucibleTestHelper.CreateEventEntity(graph, t100, "RegisteredEvent");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        // Execute without an EventEntityId
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Autonomous", t100, eventEntityId: null));

        var tqd = graph.TemporalQuery;

        Assert.Empty(tqd.GetFactsByEvent(registeredEvent.Id));
        Assert.Empty(tqd.GetRelationshipsEvolvedByEvent(registeredEvent.Id));

        // The recorded fact exists in interval, but has null EventEntityId
        var facts = tqd.GetFactsInInterval(t100, t100);
        Assert.Single(facts);
        Assert.Null(facts[0].EventEntityId);
    }

    [Fact]
    public void Provenance_RetiredRelationshipWithProvenance_HistoryRemainsQueryable()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var t10 = TqdCrucibleTestHelper.CreateTime(_schema, 10);
        var t50 = TqdCrucibleTestHelper.CreateTime(_schema, 50);

        var startEvent = TqdCrucibleTestHelper.CreateEventEntity(graph, t10, "Start");
        var endEvent = TqdCrucibleTestHelper.CreateEventEntity(graph, t50, "End");

        var relId = Guid.NewGuid();
        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "ShortLife", t10, eventEntityId: startEvent.Id));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t50, eventEntityId: endEvent.Id));

        var tqd = graph.TemporalQuery;

        // Relationship is retired
        Assert.True(graph.IsRelationshipIdRetired(relId));

        // Provenance queries retrieve facts despite retirement
        var factsStart = tqd.GetFactsByEvent(startEvent);
        Assert.Single(factsStart);
        Assert.Equal(relId, factsStart[0].RelationshipId);

        var factsEnd = tqd.GetFactsByEvent(endEvent);
        Assert.Single(factsEnd);
        Assert.Equal(relId, factsEnd[0].RelationshipId);

        Assert.Contains(relId, tqd.GetRelationshipsEvolvedByEvent(startEvent));
        Assert.Contains(relId, tqd.GetRelationshipsEvolvedByEvent(endEvent));
    }
}

