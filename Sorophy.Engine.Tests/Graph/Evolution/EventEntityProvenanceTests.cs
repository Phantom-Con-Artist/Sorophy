using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.Evolution;

public sealed class EventEntityProvenanceTests
{
    private readonly SorophyRelationshipEvolutionExecutor _executor = new();

    [Fact]
    public void Creation_WithValidEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        var evolution = new SorophyRelationshipCreation(
            relationshipId,
            sourceId,
            targetId,
            "Alliance",
            effectiveTime,
            eventEntityId: eventId);

        _executor.Execute(graph, evolution);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        var fact = Assert.Single(history!.Facts);
        Assert.Equal(eventId, fact.EventEntityId);
        Assert.Equal(eventId, evolution.EventEntityId);
    }

    [Fact]
    public void Creation_WithCaseInsensitiveEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "event");
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        var evolution = new SorophyRelationshipCreation(
            relationshipId,
            sourceId,
            targetId,
            "Alliance",
            effectiveTime,
            eventEntityId: eventId);

        _executor.Execute(graph, evolution);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        var fact = Assert.Single(history!.Facts);
        Assert.Equal(eventId, fact.EventEntityId);
    }

    [Fact]
    public void Creation_WithoutEventEntity_HasNullEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out _, eventType: "Event");
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        var evolution = new SorophyRelationshipCreation(
            relationshipId,
            sourceId,
            targetId,
            "Alliance",
            effectiveTime);

        _executor.Execute(graph, evolution);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        var fact = Assert.Single(history!.Facts);
        Assert.Null(fact.EventEntityId);
        Assert.Null(evolution.EventEntityId);
    }

    [Fact]
    public void Creation_WithNonExistentEventEntity_ThrowsInvalidOperationException()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out _, eventType: "Event");
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();
        var nonExistentEventId = Guid.NewGuid();

        var evolution = new SorophyRelationshipCreation(
            relationshipId,
            sourceId,
            targetId,
            "Alliance",
            effectiveTime,
            eventEntityId: nonExistentEventId);

        Assert.Throws<InvalidOperationException>(() => _executor.Execute(graph, evolution));
    }

    [Fact]
    public void Creation_WithNonEventEntityType_ThrowsInvalidOperationException()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var notAnEventId, eventType: "Person");
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        var evolution = new SorophyRelationshipCreation(
            relationshipId,
            sourceId,
            targetId,
            "Alliance",
            effectiveTime,
            eventEntityId: notAnEventId);

        Assert.Throws<InvalidOperationException>(() => _executor.Execute(graph, evolution));
    }

    [Fact]
    public void Creation_WithEmptyEventEntityGuid_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var effectiveTime = CreateTime(schema, "10");

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipCreation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Alliance",
                effectiveTime,
                eventEntityId: Guid.Empty));
    }

    [Fact]
    public void Termination_WithValidEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1));

        var termination = new SorophyRelationshipTermination(relationshipId, t2, eventEntityId: eventId);
        _executor.Execute(graph, termination);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        Assert.Equal(2, history!.Facts.Count);
        Assert.Null(history.Facts[0].EventEntityId);
        Assert.Equal(eventId, history.Facts[1].EventEntityId);
        Assert.Equal(eventId, termination.EventEntityId);
    }

    [Fact]
    public void TypeChange_WithValidEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1));

        var typeChange = new SorophyRelationshipTypeChange(relationshipId, "Treaty", t2, eventEntityId: eventId);
        _executor.Execute(graph, typeChange);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        Assert.Equal(2, history!.Facts.Count);
        Assert.Null(history.Facts[0].EventEntityId);
        Assert.Equal(eventId, history.Facts[1].EventEntityId);
        Assert.Equal(eventId, typeChange.EventEntityId);
    }

    [Fact]
    public void PropertyModification_WithValidEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1));

        var propMod = new SorophyRelationshipPropertyModification(
            relationshipId,
            t2,
            propertiesToSet: new Dictionary<string, SorophyProperty>
            {
                ["Rank"] = CreateIntegerProperty("Rank", 5)
            },
            eventEntityId: eventId);
        _executor.Execute(graph, propMod);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        Assert.Equal(2, history!.Facts.Count);
        Assert.Null(history.Facts[0].EventEntityId);
        Assert.Equal(eventId, history.Facts[1].EventEntityId);
        Assert.Equal(eventId, propMod.EventEntityId);
    }

    [Fact]
    public void ValidityChange_WithValidEventEntity_SetsEventEntityIdOnFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1));

        var validityChange = new SorophyRelationshipValidityChange(
            relationshipId,
            t2,
            newValidFrom: t1,
            newValidTill: t2,
            eventEntityId: eventId);
        _executor.Execute(graph, validityChange);

        Assert.True(graph.TryGetRelationshipHistory(relationshipId, out var history));
        Assert.Equal(2, history!.Facts.Count);
        Assert.Null(history.Facts[0].EventEntityId);
        Assert.Equal(eventId, history.Facts[1].EventEntityId);
        Assert.Equal(eventId, validityChange.EventEntityId);
    }

    [Fact]
    public void LoreSerialization_RoundTrip_PreservesEventEntityId()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var t2 = CreateTime(schema, "20");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1, eventEntityId: eventId));
        _executor.Execute(graph, new SorophyRelationshipTermination(relationshipId, t2));

        var json = LoreSerializer.Serialize(graph);
        Assert.Contains("\"eventEntityId\":", json);

        var deserialized = LoreSerializer.Deserialize(json);
        Assert.True(deserialized.TryGetRelationshipHistory(relationshipId, out var deserializedHistory));
        Assert.Equal(2, deserializedHistory!.Facts.Count);
        Assert.Equal(eventId, deserializedHistory.Facts[0].EventEntityId);
        Assert.Null(deserializedHistory.Facts[1].EventEntityId);

        var validationErrors = deserialized.Validate();
        Assert.Empty(validationErrors);
    }

    [Fact]
    public void LoreSerialization_RoundTrip_OmitsEventEntityIdWhenNull()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out _, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1));

        var json = LoreSerializer.Serialize(graph);
        Assert.DoesNotContain("\"eventEntityId\"", json);

        var deserialized = LoreSerializer.Deserialize(json);
        Assert.True(deserialized.TryGetRelationshipHistory(relationshipId, out var deserializedHistory));
        Assert.Null(deserializedHistory!.Facts[0].EventEntityId);
    }

    [Fact]
    public void GraphValidate_DetectsDanglingEventEntityIdInHistoryFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1, eventEntityId: eventId));

        // Now remove the event entity from the graph to create a dangling provenance reference
        graph.RemoveEntity(eventId);

        var errors = graph.Validate();
        Assert.Contains(errors, e => e.Contains("references missing event entity"));
    }

    [Fact]
    public void GraphValidate_DetectsNonEventEntityTypeInHistoryFact()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1, eventEntityId: eventId));

        // Change the event entity's type to something other than Event
        graph.Entities[eventId].Type = "Location";

        var errors = graph.Validate();
        Assert.Contains(errors, e => e.Contains("with non-event type"));
    }

    [Fact]
    public void LoreSerializer_ValidateDocument_RejectsDanglingEventEntityId()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1, eventEntityId: eventId));

        var json = LoreSerializer.Serialize(graph);

        // Replace only the fact's event entity ID with a random Guid in the JSON
        var danglingGuid = Guid.NewGuid();
        var corruptedJson = json.Replace(
            $"\"eventEntityId\": \"{eventId}\"",
            $"\"eventEntityId\": \"{danglingGuid}\"");

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(corruptedJson));
    }

    [Fact]
    public void LoreSerializer_ValidateDocument_RejectsNonEventEntityType()
    {
        var graph = CreateGraph(out var sourceId, out var targetId, out var eventId, eventType: "Event");
        var schema = CreateSchema();
        var t1 = CreateTime(schema, "10");
        var relationshipId = Guid.NewGuid();

        _executor.Execute(graph, new SorophyRelationshipCreation(relationshipId, sourceId, targetId, "Alliance", t1, eventEntityId: eventId));

        var json = LoreSerializer.Serialize(graph);

        // Mutate the type of the event entity in the JSON
        var corruptedJson = json.Replace("\"type\": \"Event\"", "\"type\": \"Location\"");

        Assert.Throws<InvalidOperationException>(() => LoreSerializer.Deserialize(corruptedJson));
    }

    [Fact]
    public void SorophyRelationshipFact_EmptyEventEntityId_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var t = CreateTime(schema, "10");

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                t,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Alliance",
                eventEntityId: Guid.Empty));
    }

    // =============================================================
    // HELPERS
    // =============================================================

    private static SorophyGraph CreateGraph(
        out Guid sourceId,
        out Guid targetId,
        out Guid eventId,
        string eventType = "Event")
    {
        var graph = new SorophyGraph();

        sourceId = Guid.NewGuid();
        targetId = Guid.NewGuid();
        eventId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity
        {
            Id = sourceId,
            Name = "Source",
            Type = "Person"
        });

        graph.AddEntity(new SorophyEntity
        {
            Id = targetId,
            Name = "Target",
            Type = "Person"
        });

        graph.AddEntity(new SorophyEntity
        {
            Id = eventId,
            Name = "Signing Ceremony",
            Type = eventType
        });

        return graph;
    }

    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        var numericPosition = new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric);
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Year", 0, numericPosition)
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(
            schema,
            position,
            "Year",
            SorophyTimePrecision.Exact);
    }

    private static SorophyProperty CreateIntegerProperty(string name, long value)
    {
        return new SorophyProperty
        {
            Name = name,
            Value = new SorophyValue(SorophyValueType.Integer, value)
        };
    }
}
