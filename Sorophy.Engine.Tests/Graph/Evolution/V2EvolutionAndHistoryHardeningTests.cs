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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.Evolution;

public sealed class V2EvolutionAndHistoryHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Evolution Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Tick",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Tick", SorophyTimePrecision.Exact);
    }

    private static (SorophyGraph graph, Guid sId, Guid tId, Guid eventId) CreateGraphWithEvent(
        SorophyTimeSchema schema,
        string eventTime = "100")
    {
        var graph = new SorophyGraph();
        var sId = Guid.NewGuid();
        var tId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = sId, Name = "SourceNode" });
        graph.AddEntity(new SorophyEntity { Id = tId, Name = "TargetNode" });
        graph.AddEntity(new SorophyEntity
        {
            Id = eventId,
            Name = "TreatyEvent",
            Type = "Event",
            OccurredAt = CreateTime(schema, eventTime)
        });

        return (graph, sId, tId, eventId);
    }

    // =============================================================
    // 1. DIRECT MUTATION VS EVOLUTION (HISTORY RECORDING)
    // =============================================================

    [Fact]
    public void DirectMutation_DoesNotFabricateHistory()
    {
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = s.Id,
            TargetId = t.Id,
            Type = "DirectEdge"
        };

        // Direct graph add
        graph.AddRelationship(rel);
        Assert.False(graph.RelationshipHistories.ContainsKey(rel.Id));

        // Direct graph remove
        graph.RemoveRelationship(rel.Id);
        Assert.False(graph.RelationshipHistories.ContainsKey(rel.Id));
    }

    [Fact]
    public void Evolution_RecordsAppendOnlyFactsWithProvenance()
    {
        var schema = CreateSchema();
        var (graph, sId, tId, eventId) = CreateGraphWithEvent(schema, "10");
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        // 1. Creation
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Alliance", CreateTime(schema, "10"), eventEntityId: eventId));

        Assert.True(graph.RelationshipHistories.ContainsKey(relId));
        var history = graph.RelationshipHistories[relId];
        Assert.Single(history.Facts);
        Assert.Equal("Alliance", history.Facts[0].Type);
        Assert.Equal(eventId, history.Facts[0].EventEntityId);
        Assert.Equal(CreateTime(schema, "10"), history.Facts[0].At);

        // 2. Type change
        executor.Execute(graph, new SorophyRelationshipTypeChange(
            relId, "Truce", CreateTime(schema, "20")));

        Assert.Equal(2, history.Facts.Count);
        // Pre-transition fact captures state immediately before type change took effect
        Assert.Equal("Alliance", history.Facts[1].Type);
        Assert.Equal("Truce", graph.Relationships[relId].Type);
        Assert.Equal(CreateTime(schema, "20"), history.Facts[1].At);

        // 3. Property modification
        var prop = new SorophyProperty { Name = "Strength", Value = new SorophyValue(SorophyValueType.Integer, 50L) };
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId, CreateTime(schema, "30"), new Dictionary<string, SorophyProperty> { ["Strength"] = prop }));

        Assert.Equal(3, history.Facts.Count);
        Assert.Equal("Truce", history.Facts[2].Type);
        Assert.False(history.Facts[2].Properties.ContainsKey("Strength"));
        Assert.True(graph.Relationships[relId].Properties.ContainsKey("Strength"));

        // 4. Validity change
        executor.Execute(graph, new SorophyRelationshipValidityChange(
            relId, CreateTime(schema, "40"), newValidFrom: CreateTime(schema, "10"), newValidTill: CreateTime(schema, "50")));

        Assert.Equal(4, history.Facts.Count);
        Assert.True(history.Facts[3].Properties.ContainsKey("Strength"));
        Assert.Null(history.Facts[3].ValidFrom); // Fact captured state before validity change
        Assert.Equal(CreateTime(schema, "10"), graph.Relationships[relId].ValidFrom);
        Assert.Equal(CreateTime(schema, "50"), graph.Relationships[relId].ValidTill);

        // 5. Termination
        executor.Execute(graph, new SorophyRelationshipTermination(
            relId, CreateTime(schema, "50")));

        Assert.Equal(5, history.Facts.Count);
        Assert.Equal(CreateTime(schema, "10"), history.Facts[4].ValidFrom); // Fact 4 captured state before termination
        Assert.False(graph.Relationships.ContainsKey(relId));
        Assert.True(graph.IsRelationshipIdRetired(relId));
    }

    // =============================================================
    // 2. RETIRED IDENTITY RE-CREATION PROHIBITION
    // =============================================================

    [Fact]
    public void Evolution_CreationWithRetiredId_ThrowsInvalidOperationException()
    {
        var schema = CreateSchema();
        var (graph, sId, tId, _) = CreateGraphWithEvent(schema);
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        // Create and terminate
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Edge", CreateTime(schema, "10")));
        executor.Execute(graph, new SorophyRelationshipTermination(
            relId, CreateTime(schema, "20")));

        Assert.True(graph.IsRelationshipIdRetired(relId));

        // Attempting to re-create with retired ID must fail
        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(graph, new SorophyRelationshipCreation(
                relId, sId, tId, "RebornEdge", CreateTime(schema, "30"))));
    }

    // =============================================================
    // 3. PROVENANCE REJECTION OF NON-EVENT ENTITIES
    // =============================================================

    [Fact]
    public void Evolution_NonEventProvenanceEntity_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var sId = Guid.NewGuid();
        var tId = Guid.NewGuid();
        var nonEventId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = sId, Name = "Source" });
        graph.AddEntity(new SorophyEntity { Id = tId, Name = "Target" });
        graph.AddEntity(new SorophyEntity { Id = nonEventId, Name = "NotAnEvent", Type = "Character" });

        var executor = new SorophyRelationshipEvolutionExecutor();

        // Provenance must point to an Event entity
        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(graph, new SorophyRelationshipCreation(
                Guid.NewGuid(), sId, tId, "Edge", CreateTime(schema, "10"), eventEntityId: nonEventId)));
    }

    // =============================================================
    // 4. SAME-TIME FACT ACCUMULATION (NO CAUSALITY INFERRED)
    // =============================================================

    [Fact]
    public void Evolution_SameTimeFacts_AccumulatesDeterministicallyWithoutPrecedence()
    {
        var schema = CreateSchema();
        var (graph, sId, tId, _) = CreateGraphWithEvent(schema);
        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        var timeCoord = CreateTime(schema, "100");

        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Base", timeCoord));

        const int factCount = 100;
        for (int i = 0; i < factCount; i++)
        {
            var prop = new SorophyProperty
            {
                Name = $"P_{i}",
                Value = new SorophyValue(SorophyValueType.Integer, (long)i)
            };

            executor.Execute(graph, new SorophyRelationshipPropertyModification(
                relId, timeCoord, new Dictionary<string, SorophyProperty> { [$"P_{i}"] = prop }));
        }

        var history = graph.RelationshipHistories[relId];
        Assert.Equal(factCount + 1, history.Facts.Count);

        // Every fact has the exact same coordinate
        foreach (var fact in history.Facts)
        {
            Assert.Equal(timeCoord, fact.At);
        }
    }
}
