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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.History;

public sealed class SorophyEntityHistoryTests
{
    private static readonly Guid EntityId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Year", SorophyTimePrecision.Exact);
    }

    [Fact]
    public void Constructor_ValidId_InitializesEmptyHistory()
    {
        var history = new SorophyEntityHistory(EntityId);

        Assert.Equal(EntityId, history.EntityId);
        Assert.Empty(history.Facts);
        Assert.Null(history.CreationFact);
        Assert.Null(history.RetirementFact);
        Assert.Null(history.CreatedAt);
        Assert.Null(history.RetiredAt);
        Assert.False(history.IsRetired);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SorophyEntityHistory(Guid.Empty));
        Assert.Contains("Entity ID cannot be empty", ex.Message);
    }

    [Fact]
    public void Add_FactForDifferentEntity_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);
        var fact = new SorophyEntityFact(CreateTime(schema, "100"), Guid.NewGuid(), SorophyEntityFactKind.Created);

        var ex = Assert.Throws<ArgumentException>(() => history.Add(fact));
        Assert.Contains("Historical fact belongs to entity", ex.Message);
    }

    [Fact]
    public void Add_ChronologicalOrdering_SortsFactsByTime()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        var fact300 = new SorophyEntityFact(CreateTime(schema, "300"), EntityId, SorophyEntityFactKind.Retired);
        var fact100 = new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created);

        history.Add(fact300);
        history.Add(fact100);

        Assert.Equal(2, history.Facts.Count);
        Assert.Equal(CreateTime(schema, "100"), history.Facts[0].At);
        Assert.Equal(CreateTime(schema, "300"), history.Facts[1].At);
    }

    [Fact]
    public void Add_MultipleRetirementFacts_ThrowsInvalidOperationException()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        history.Add(new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created));
        history.Add(new SorophyEntityFact(CreateTime(schema, "300"), EntityId, SorophyEntityFactKind.Retired));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            history.Add(new SorophyEntityFact(CreateTime(schema, "400"), EntityId, SorophyEntityFactKind.Retired)));

        Assert.Contains("already has an established retirement fact", ex.Message);
    }

    [Fact]
    public void ExistsAt_TemporalLifecycleBehavior_HonorsTransitions()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        history.Add(new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created));
        history.Add(new SorophyEntityFact(CreateTime(schema, "300"), EntityId, SorophyEntityFactKind.Retired));

        // Prior to creation: absent
        Assert.False(history.ExistsAt(CreateTime(schema, "50")));
        Assert.False(history.ExistsAt(CreateTime(schema, "99")));

        // At creation: present (inclusive)
        Assert.True(history.ExistsAt(CreateTime(schema, "100")));

        // Active lifespan: present
        Assert.True(history.ExistsAt(CreateTime(schema, "200")));
        Assert.True(history.ExistsAt(CreateTime(schema, "299")));

        // At retirement: absent (post-transition semantics)
        Assert.False(history.ExistsAt(CreateTime(schema, "300")));

        // After retirement: absent
        Assert.False(history.ExistsAt(CreateTime(schema, "301")));
        Assert.False(history.ExistsAt(CreateTime(schema, "500")));
    }

    [Fact]
    public void GetStatusAt_TemporalLifecycleStatus_ReturnsCorrectStatus()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        history.Add(new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created));
        history.Add(new SorophyEntityFact(CreateTime(schema, "300"), EntityId, SorophyEntityFactKind.Retired));

        Assert.Equal(SorophyEntityLifecycleStatus.Uncreated, history.GetStatusAt(CreateTime(schema, "50")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, history.GetStatusAt(CreateTime(schema, "100")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, history.GetStatusAt(CreateTime(schema, "200")));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, history.GetStatusAt(CreateTime(schema, "300")));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, history.GetStatusAt(CreateTime(schema, "400")));
    }

    [Fact]
    public void ExistsAt_IncompatibleTimeline_ReturnsFalseSafely()
    {
        var schema1 = CreateSchema("Timeline1");
        var schema2 = CreateSchema("Timeline2");

        var history = new SorophyEntityHistory(EntityId);
        history.Add(new SorophyEntityFact(CreateTime(schema1, "100"), EntityId, SorophyEntityFactKind.Created));

        // Incomparable timeline query should return false rather than throw
        Assert.False(history.ExistsAt(CreateTime(schema2, "200")));
        Assert.Equal(SorophyEntityLifecycleStatus.Uncreated, history.GetStatusAt(CreateTime(schema2, "200")));
    }

    [Fact]
    public void RemoveFact_RetirementFactRemoval_EnablesReversal()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        history.Add(new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created));
        history.Add(new SorophyEntityFact(CreateTime(schema, "300"), EntityId, SorophyEntityFactKind.Retired));

        Assert.True(history.IsRetired);
        Assert.False(history.ExistsAt(CreateTime(schema, "350")));

        // Revert retirement fact
        var removed = history.RemoveFact(SorophyEntityFactKind.Retired, CreateTime(schema, "300"));
        Assert.True(removed);

        Assert.False(history.IsRetired);
        Assert.Null(history.RetiredAt);
        Assert.Single(history.Facts);
        Assert.Equal(SorophyEntityFactKind.Created, history.Facts[0].Kind);

        // Entity is active again across all T >= 100
        Assert.True(history.ExistsAt(CreateTime(schema, "350")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, history.GetStatusAt(CreateTime(schema, "350")));
    }

    [Fact]
    public void RemoveFact_NonExistentFact_ReturnsFalse()
    {
        var schema = CreateSchema();
        var history = new SorophyEntityHistory(EntityId);

        history.Add(new SorophyEntityFact(CreateTime(schema, "100"), EntityId, SorophyEntityFactKind.Created));

        var removed = history.RemoveFact(SorophyEntityFactKind.Retired, CreateTime(schema, "300"));
        Assert.False(removed);
    }
}
