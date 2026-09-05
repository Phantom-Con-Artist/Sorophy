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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.History;

public sealed class SorophyGraphHistoryTests
{
    /*
     * =============================================================
     * HELPERS
     * =============================================================
     */

    private static SorophyEntity CreateEntity(
        Guid id,
        string name)
    {
        return new SorophyEntity
        {
            Id = id,
            Name = name,
            Type = "Test"
        };
    }

    private static SorophyRelationship CreateRelationship(
        Guid id,
        Guid sourceId,
        Guid targetId)
    {
        return new SorophyRelationship
        {
            Id = id,
            Type = "knows",
            SourceId = sourceId,
            TargetId = targetId
        };
    }

private static SorophyRelationshipFact CreateFact(
    Guid relationshipId)
{
    var schema =
        new SorophyTimeSchema(
            "Test Timeline",
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    new SorophyTimePositionDefinition(
                        SorophyTimePositionKind.Numeric))
            });

    var at =
        new SorophyTime(
            schema,
            "100",
            "Year",
            SorophyTimePrecision.Exact);

    return new SorophyRelationshipFact(
        at,
        relationshipId,
        Guid.NewGuid(),
        Guid.NewGuid(),
        "knows");
}

    private static SorophyGraph CreateGraphWithRelationship(
        out Guid sourceId,
        out Guid targetId,
        out Guid relationshipId)
    {
        sourceId =
            Guid.NewGuid();

        targetId =
            Guid.NewGuid();

        relationshipId =
            Guid.NewGuid();

        var graph =
            new SorophyGraph();

        graph.AddEntity(
            CreateEntity(
                sourceId,
                "Source"));

        graph.AddEntity(
            CreateEntity(
                targetId,
                "Target"));

        graph.AddRelationship(
            CreateRelationship(
                relationshipId,
                sourceId,
                targetId));

        return graph;
    }

    /*
     * =============================================================
     * EMPTY STATE
     * =============================================================
     */

    [Fact]
    public void NewGraph_ShouldHaveNoRelationshipHistories()
    {
        var graph =
            new SorophyGraph();

        Assert.Empty(
            graph.RelationshipHistories);

        Assert.False(
            graph.TryGetRelationshipHistory(
                Guid.NewGuid(),
                out _));
    }

    /*
     * =============================================================
     * HISTORY CREATION
     * =============================================================
     */

    [Fact]
    public void GetOrCreateRelationshipHistory_ShouldCreateHistory()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var history =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        Assert.NotNull(
            history);

        Assert.Equal(
            relationshipId,
            history.RelationshipId);

        Assert.Empty(
            history.Facts);
    }

    [Fact]
    public void GetOrCreateRelationshipHistory_ShouldReturnSameInstance()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var first =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        var second =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        Assert.Same(
            first,
            second);

        Assert.Single(
            graph.RelationshipHistories);
    }

    [Fact]
    public void GetOrCreateRelationshipHistory_ShouldExposeHistoryThroughPublicView()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var history =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        Assert.True(
            graph.RelationshipHistories.ContainsKey(
                relationshipId));

        Assert.Same(
            history,
            graph.RelationshipHistories[relationshipId]);
    }

    [Fact]
    public void GetOrCreateRelationshipHistory_ShouldRejectEmptyRelationshipId()
    {
        var graph =
            new SorophyGraph();

        Assert.Throws<ArgumentException>(() =>
            graph.GetOrCreateRelationshipHistory(
                Guid.Empty));
    }

    /*
     * =============================================================
     * HISTORY LOOKUP
     * =============================================================
     */

    [Fact]
    public void TryGetRelationshipHistory_ShouldReturnExistingHistory()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var created =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        var found =
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history);

        Assert.True(
            found);

        Assert.NotNull(
            history);

        Assert.Same(
            created,
            history);
    }

    [Fact]
    public void TryGetRelationshipHistory_ShouldNotCreateMissingHistory()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var found =
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history);

        Assert.False(
            found);

        Assert.Null(
            history);

        Assert.Empty(
            graph.RelationshipHistories);
    }

    /*
     * =============================================================
     * CACHED READ-ONLY VIEW
     * =============================================================
     */

    [Fact]
    public void RelationshipHistories_ShouldReuseCachedReadOnlyView()
    {
        var graph =
            new SorophyGraph();

        var first =
            graph.RelationshipHistories;

        var second =
            graph.RelationshipHistories;

        Assert.Same(
            first,
            second);
    }

    [Fact]
    public void RelationshipHistories_ShouldReflectLaterHistoryCreation()
    {
        var graph =
            new SorophyGraph();

        var view =
            graph.RelationshipHistories;

        var relationshipId =
            Guid.NewGuid();

        graph.GetOrCreateRelationshipHistory(
            relationshipId);

        Assert.True(
            view.ContainsKey(
                relationshipId));
    }

    [Fact]
    public void RelationshipHistories_ShouldBeReadOnly()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var history =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        var view =
            graph.RelationshipHistories;

        var dictionary =
            Assert.IsAssignableFrom<
                System.Collections.Generic.IReadOnlyDictionary<
                    Guid,
                    SorophyRelationshipHistory>>(
                view);

        Assert.Same(
            history,
            dictionary[relationshipId]);
    }

    /*
     * =============================================================
     * RELATIONSHIP LIFECYCLE
     * =============================================================
     */

    [Fact]
    public void RelationshipRemoval_ShouldNotDeleteExistingHistory()
    {
        var graph =
            CreateGraphWithRelationship(
                out _,
                out _,
                out var relationshipId);

        var history =
            graph.GetOrCreateRelationshipHistory(
                relationshipId);

        var removed =
            graph.RemoveRelationship(
                relationshipId);

        Assert.True(
            removed);

        Assert.False(
            graph.ContainsRelationship(
                relationshipId));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var retainedHistory));

        Assert.Same(
            history,
            retainedHistory);
    }

    [Fact]
    public void RelationshipRemoval_ShouldLeaveRetainedHistoryVisible()
    {
        var graph =
            CreateGraphWithRelationship(
                out _,
                out _,
                out var relationshipId);

        graph.GetOrCreateRelationshipHistory(
            relationshipId);

        graph.RemoveRelationship(
            relationshipId);

        Assert.Contains(
            relationshipId,
            graph.RelationshipHistories.Keys);
    }

    /*
     * =============================================================
     * ID RETIREMENT
     * =============================================================
     */

    [Fact]
    public void RemovedRelationshipId_ShouldBecomeRetired()
    {
        var graph =
            CreateGraphWithRelationship(
                out var sourceId,
                out var targetId,
                out var relationshipId);

        Assert.True(
            graph.ContainsRelationship(
                relationshipId));

        Assert.True(
            graph.RemoveRelationship(
                relationshipId));

        Assert.False(
            graph.ContainsRelationship(
                relationshipId));

        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(
                CreateRelationship(
                    relationshipId,
                    sourceId,
                    targetId)));
    }

    [Fact]
    public void ActiveRelationshipId_ShouldNotBeConsideredRetired()
    {
        var graph =
            CreateGraphWithRelationship(
                out _,
                out _,
                out var relationshipId);

        Assert.False(
            graph.IsRelationshipIdRetired(
                relationshipId));

        Assert.True(
            graph.ContainsRelationship(
                relationshipId));
    }

    /*
     * =============================================================
     * HISTORY / GRAPH VALIDATION
     * =============================================================
     */

    [Fact]
    public void Validate_ShouldRemainCleanWithEmptyHistoryStore()
    {
        var graph =
            new SorophyGraph();

        var errors =
            graph.Validate();

        Assert.Empty(
            errors);
    }

    [Fact]
    public void Validate_ShouldRemainCleanAfterCreatingRelationshipHistory()
    {
        var graph =
            CreateGraphWithRelationship(
                out _,
                out _,
                out var relationshipId);

        graph.GetOrCreateRelationshipHistory(
            relationshipId);

        var errors =
            graph.Validate();

        Assert.Empty(
            errors);
    }

    [Fact]
    public void Validate_ShouldRemainCleanAfterRelationshipRemovalWithHistory()
    {
        var graph =
            CreateGraphWithRelationship(
                out _,
                out _,
                out var relationshipId);

        graph.GetOrCreateRelationshipHistory(
            relationshipId);

        Assert.True(
            graph.RemoveRelationship(
                relationshipId));

        var errors =
            graph.Validate();

        Assert.Empty(
            errors);
    }

    [Fact]
    public void Validate_ShouldDetectNullHistoryInHistoryStore()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var historiesField =
            typeof(SorophyGraph).GetField(
                "_relationshipHistories",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var histories =
            (Dictionary<Guid, SorophyRelationshipHistory>)
                historiesField!.GetValue(graph)!;

        histories[relationshipId] =
            null!;

        var errors =
            graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    $"Relationship history store contains a null history for relationship '{relationshipId}'.",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectMismatchedRelationshipIdInHistoryStore()
    {
        var graph =
            new SorophyGraph();

        var keyId =
            Guid.NewGuid();

        var historyId =
            Guid.NewGuid();

        var historiesField =
            typeof(SorophyGraph).GetField(
                "_relationshipHistories",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var histories =
            (Dictionary<Guid, SorophyRelationshipHistory>)
                historiesField!.GetValue(graph)!;

        histories[keyId] =
            new SorophyRelationshipHistory(historyId);

        var errors =
            graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    $"Relationship history dictionary key '{keyId}' does not match history relationship ID '{historyId}'.",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectEmptyRelationshipIdInHistoryStore()
    {
        var graph =
            new SorophyGraph();

        var historyId =
            Guid.NewGuid();

        var historiesField =
            typeof(SorophyGraph).GetField(
                "_relationshipHistories",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var histories =
            (Dictionary<Guid, SorophyRelationshipHistory>)
                historiesField!.GetValue(graph)!;

        histories[Guid.Empty] =
            new SorophyRelationshipHistory(historyId);

        var errors =
            graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "Relationship history store contains an empty relationship ID.",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectEmptyRetiredRelationshipId()
    {
        var graph =
            new SorophyGraph();

        var retiredField =
            typeof(SorophyGraph).GetField(
                "_retiredRelationshipIds",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var retired =
            (HashSet<Guid>)
                retiredField!.GetValue(graph)!;

        retired.Add(
            Guid.Empty);

        var errors =
            graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "Relationship retirement store contains an empty relationship ID.",
                    StringComparison.Ordinal));
    }

    /*
     * =============================================================
     * MULTIPLE HISTORIES
     * =============================================================
     */

    [Fact]
    public void Graph_ShouldMaintainIndependentHistoryPerRelationship()
    {
        var sourceId =
            Guid.NewGuid();

        var targetId =
            Guid.NewGuid();

        var firstRelationshipId =
            Guid.NewGuid();

        var secondRelationshipId =
            Guid.NewGuid();

        var graph =
            new SorophyGraph();

        graph.AddEntity(
            CreateEntity(
                sourceId,
                "Source"));

        graph.AddEntity(
            CreateEntity(
                targetId,
                "Target"));

        graph.AddRelationship(
            CreateRelationship(
                firstRelationshipId,
                sourceId,
                targetId));

        graph.AddRelationship(
            CreateRelationship(
                secondRelationshipId,
                sourceId,
                targetId));

        var firstHistory =
            graph.GetOrCreateRelationshipHistory(
                firstRelationshipId);

        var secondHistory =
            graph.GetOrCreateRelationshipHistory(
                secondRelationshipId);

        Assert.NotSame(
            firstHistory,
            secondHistory);

        Assert.Equal(
            firstRelationshipId,
            firstHistory.RelationshipId);

        Assert.Equal(
            secondRelationshipId,
            secondHistory.RelationshipId);

        Assert.Equal(
            2,
            graph.RelationshipHistories.Count);
    }

    /*
     * =============================================================
     * COLLECTION CONSISTENCY
     * =============================================================
     */

    [Fact]
    public void RelationshipHistories_ShouldContainExactlyCreatedHistories()
    {
        var graph =
            new SorophyGraph();

        var firstId =
            Guid.NewGuid();

        var secondId =
            Guid.NewGuid();

        graph.GetOrCreateRelationshipHistory(
            firstId);

        graph.GetOrCreateRelationshipHistory(
            secondId);

        var ids =
            graph.RelationshipHistories.Keys
                .ToArray();

        Assert.Equal(
            2,
            ids.Length);

        Assert.Contains(
            firstId,
            ids);

        Assert.Contains(
            secondId,
            ids);
    }

    /*
     * =============================================================
     * HISTORICAL FACT RECORDING
     * =============================================================
     */

    [Fact]
    public void RecordRelationshipFact_ShouldCreateHistoryAndStoreFact()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var fact =
            CreateFact(
                relationshipId);

        graph.RecordRelationshipFact(
            fact);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        Assert.Same(
            fact,
            history.Facts[0]);
    }

    [Fact]
    public void RecordRelationshipFact_ShouldReuseExistingHistory()
    {
        var graph =
            new SorophyGraph();

        var relationshipId =
            Guid.NewGuid();

        var firstFact =
            CreateFact(
                relationshipId);

        var secondFact =
            CreateFact(
                relationshipId);

        graph.RecordRelationshipFact(
            firstFact);

        graph.RecordRelationshipFact(
            secondFact);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Equal(
            2,
            history!.Count);

        Assert.Same(
            firstFact,
            history.Facts[0]);

        Assert.Same(
            secondFact,
            history.Facts[1]);
    }

    [Fact]
    public void RecordRelationshipFact_ShouldNotCreateHistoryForNullFact()
    {
        var graph =
            new SorophyGraph();

        Assert.Throws<ArgumentNullException>(() =>
            graph.RecordRelationshipFact(
                null!));

        Assert.Empty(
            graph.RelationshipHistories);
    }

    [Fact]
    public void IsRelationshipIdRetired_And_RetiredRelationshipIds_ExposePublicState()
    {
        var graph = new SorophyGraph();
        var entityA = new SorophyEntity { Id = Guid.NewGuid(), Name = "A", Type = "Node" };
        var entityB = new SorophyEntity { Id = Guid.NewGuid(), Name = "B", Type = "Node" };
        graph.AddEntity(entityA);
        graph.AddEntity(entityB);

        var relId = Guid.NewGuid();
        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = entityA.Id,
            TargetId = entityB.Id,
            Type = "Connected"
        };
        graph.AddRelationship(rel);

        Assert.False(graph.IsRelationshipIdRetired(relId));
        Assert.DoesNotContain(relId, graph.RetiredRelationshipIds);

        graph.RemoveRelationship(relId);

        Assert.True(graph.IsRelationshipIdRetired(relId));
        Assert.Contains(relId, graph.RetiredRelationshipIds);
        Assert.Single(graph.RetiredRelationshipIds);
    }
}