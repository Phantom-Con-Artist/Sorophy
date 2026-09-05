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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.History;

public sealed class SorophyRelationshipHistoryTests
{
    private static readonly Guid RelationshipId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid SourceId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly Guid TargetId =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly Guid OtherRelationshipId =
        Guid.Parse(
            "44444444-4444-4444-4444-444444444444");

    private static SorophyTimeSchema CreateSchema()
    {
        return new SorophyTimeSchema(
            "Test Timeline",
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    new SorophyTimePositionDefinition(
                        SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(
        string position = "100")
    {
        return new SorophyTime(
            CreateSchema(),
            position,
            "Year",
            SorophyTimePrecision.Exact);
    }

    [Fact]
    public void Constructor_StoresRelationshipId()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        Assert.Equal(
            RelationshipId,
            history.RelationshipId);
    }

    [Fact]
    public void Constructor_RejectsEmptyRelationshipId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new SorophyRelationshipHistory(
                    Guid.Empty));
    }

    [Fact]
    public void Constructor_CreatesEmptyHistory()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        Assert.Empty(
            history.Facts);
    }

    [Fact]
    public void Add_AddsFact()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact();

        history.Add(
            fact);

        Assert.Single(
            history.Facts);

        Assert.Contains(
            fact,
            history.Facts);
    }

    [Fact]
    public void Add_PreservesInsertionOrder()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var first =
            CreateFact(
                type: "member_of");

        var second =
            CreateFact(
                type: "leader_of");

        var third =
            CreateFact(
                type: "commander_of");

        history.Add(first);
        history.Add(second);
        history.Add(third);

        Assert.Equal(
            new[]
            {
                first,
                second,
                third
            },
            history.Facts);
    }

    [Fact]
    public void Add_AllowsMultipleDistinctFacts()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var first =
            CreateFact(
                type: "member_of");

        var second =
            CreateFact(
                type: "leader_of");

        history.Add(first);
        history.Add(second);

        Assert.Equal(
            2,
            history.Count);

        Assert.Same(
            first,
            history.Facts[0]);

        Assert.Same(
            second,
            history.Facts[1]);
    }

    [Fact]
    public void Add_RejectsNullFact()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        Assert.Throws<ArgumentNullException>(
            () =>
                history.Add(
                    null!));
    }

    [Fact]
    public void Add_RejectsFactBelongingToDifferentRelationship()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact(
                OtherRelationshipId);

        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    history.Add(
                        fact));

        Assert.Contains(
            OtherRelationshipId.ToString(),
            exception.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            RelationshipId.ToString(),
            exception.Message,
            StringComparison.Ordinal);

        Assert.Empty(
            history.Facts);
    }

    [Fact]
    public void Add_RejectsSameFactInstanceTwice()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact();

        history.Add(
            fact);

        Assert.Throws<InvalidOperationException>(
            () =>
                history.Add(
                    fact));

        Assert.Single(
            history.Facts);

        Assert.Same(
            fact,
            history.Facts[0]);
    }

    [Fact]
    public void Add_AllowsDistinctFactInstancesWithSameState()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var first =
            CreateFact(
                type: "member_of");

        var second =
            CreateFact(
                type: "member_of");

        Assert.NotSame(
            first,
            second);

        history.Add(first);
        history.Add(second);

        Assert.Equal(
            2,
            history.Count);

        Assert.Same(
            first,
            history.Facts[0]);

        Assert.Same(
            second,
            history.Facts[1]);
    }

    [Fact]
    public void Contains_ReturnsTrueForAddedFact()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact();

        history.Add(
            fact);

        Assert.True(
            history.Contains(
                fact));
    }

    [Fact]
    public void Contains_ReturnsFalseForMissingFact()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact();

        Assert.False(
            history.Contains(
                fact));
    }

    [Fact]
    public void Contains_UsesFactInstanceIdentity()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var storedFact =
            CreateFact(
                type: "member_of");

        var differentFact =
            CreateFact(
                type: "member_of");

        history.Add(
            storedFact);

        Assert.True(
            history.Contains(
                storedFact));

        Assert.False(
            history.Contains(
                differentFact));
    }

    [Fact]
    public void Contains_RejectsNullFact()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        Assert.Throws<ArgumentNullException>(
            () =>
                history.Contains(
                    null!));
    }

    [Fact]
    public void Facts_ExposesReadOnlyList()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var fact =
            CreateFact();

        history.Add(
            fact);

        IReadOnlyList<SorophyRelationshipFact> facts =
            history.Facts;

        Assert.Single(
            facts);

        Assert.Same(
            fact,
            facts[0]);
    }

    [Fact]
    public void Facts_ReflectsSubsequentAppendOperations()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var first =
            CreateFact(
                type: "member_of");

        var second =
            CreateFact(
                type: "leader_of");

        history.Add(
            first);

        var observedFacts =
            history.Facts;

        Assert.Single(
            observedFacts);

        history.Add(
            second);

        Assert.Equal(
            2,
            observedFacts.Count);

        Assert.Same(
            first,
            observedFacts[0]);

        Assert.Same(
            second,
            observedFacts[1]);
    }

    [Fact]
    public void Enumerator_ReturnsFactsInInsertionOrder()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var first =
            CreateFact(
                type: "member_of");

        var second =
            CreateFact(
                type: "leader_of");

        var third =
            CreateFact(
                type: "commander_of");

        history.Add(first);
        history.Add(second);
        history.Add(third);

        var enumerated =
            history.ToList();

        Assert.Equal(
            3,
            enumerated.Count);

        Assert.Same(
            first,
            enumerated[0]);

        Assert.Same(
            second,
            enumerated[1]);

        Assert.Same(
            third,
            enumerated[2]);
    }

    [Fact]
    public void History_ImplementsIReadOnlyCollection()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        Assert.IsAssignableFrom<
            IReadOnlyCollection<SorophyRelationshipFact>>(
            history);
    }

    [Fact]
    public void History_DoesNotExposeMutableCollectionOperations()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var factsProperty =
            typeof(SorophyRelationshipHistory)
                .GetProperty(
                    nameof(
                        SorophyRelationshipHistory.Facts));

        Assert.NotNull(
            factsProperty);

        Assert.False(
            factsProperty!.CanWrite);
    }

    [Fact]
    public void History_OnlyAcceptsFactsForItsRelationship()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var validFact =
            CreateFact();

        var invalidFact =
            CreateFact(
                OtherRelationshipId);

        history.Add(
            validFact);

        Assert.Single(
            history.Facts);

        Assert.Throws<ArgumentException>(
            () =>
                history.Add(
                    invalidFact));

        Assert.Single(
            history.Facts);

        Assert.Same(
            validFact,
            history.Facts[0]);
    }

    [Fact]
    public void History_CanAccumulateManyFacts()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var facts =
            new List<SorophyRelationshipFact>();

        for (var i = 0; i < 25; i++)
        {
            var fact =
                CreateFact(
                    type: $"state_{i}");

            facts.Add(
                fact);

            history.Add(
                fact);
        }

        Assert.Equal(
            25,
            history.Count);

        Assert.Equal(
            facts,
            history.Facts);
    }

    [Fact]
    public void History_RemainsValidAfterFailedAdd()
    {
        var history =
            new SorophyRelationshipHistory(
                RelationshipId);

        var validFact =
            CreateFact(
                type: "member_of");

        var invalidFact =
            CreateFact(
                OtherRelationshipId,
                "member_of");

        history.Add(
            validFact);

        Assert.Throws<ArgumentException>(
            () =>
                history.Add(
                    invalidFact));

        Assert.Single(
            history.Facts);

        Assert.Same(
            validFact,
            history.Facts[0]);
    }

    private static SorophyRelationshipFact CreateFact(
        Guid? relationshipId = null,
        string type = "related_to")
    {
        var id =
            relationshipId ??
            RelationshipId;

        return new SorophyRelationshipFact(
            CreateTime(),
            id,
            SourceId,
            TargetId,
            type);
    }
}