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

public sealed class SorophyEntityFactTests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Year", 0, NumericPosition)
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position = "100")
    {
        return new SorophyTime(schema, position, "Year", SorophyTimePrecision.Exact);
    }

    [Fact]
    public void Constructor_ValidParameters_InitializesProperties()
    {
        var schema = CreateSchema();
        var at = CreateTime(schema, "100");
        var entityId = Guid.NewGuid();

        var fact = new SorophyEntityFact(
            at,
            entityId,
            SorophyEntityFactKind.Created,
            "Created fact");

        Assert.Equal(at, fact.At);
        Assert.Equal(entityId, fact.EntityId);
        Assert.Equal(SorophyEntityFactKind.Created, fact.Kind);
        Assert.Equal("Created fact", fact.Description);
    }

    [Fact]
    public void Create_FactoryMethod_InitializesProperties()
    {
        var schema = CreateSchema();
        var at = CreateTime(schema, "200");
        var entityId = Guid.NewGuid();

        var fact = SorophyEntityFact.Create(
            at,
            entityId,
            SorophyEntityFactKind.Retired,
            "Retired fact");

        Assert.Equal(at, fact.At);
        Assert.Equal(entityId, fact.EntityId);
        Assert.Equal(SorophyEntityFactKind.Retired, fact.Kind);
        Assert.Equal("Retired fact", fact.Description);
    }

    [Fact]
    public void Constructor_NullAt_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SorophyEntityFact(
                null!,
                Guid.NewGuid(),
                SorophyEntityFactKind.Created));
    }

    [Fact]
    public void Constructor_EmptyEntityId_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var at = CreateTime(schema, "100");

        var ex = Assert.Throws<ArgumentException>(() =>
            new SorophyEntityFact(
                at,
                Guid.Empty,
                SorophyEntityFactKind.Created));

        Assert.Contains("Entity ID cannot be empty", ex.Message);
    }

    [Fact]
    public void Constructor_UndefinedKind_ThrowsArgumentException()
    {
        var schema = CreateSchema();
        var at = CreateTime(schema, "100");

        var ex = Assert.Throws<ArgumentException>(() =>
            new SorophyEntityFact(
                at,
                Guid.NewGuid(),
                (SorophyEntityFactKind)999));

        Assert.Contains("Undefined entity fact kind", ex.Message);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var schema = CreateSchema();
        var at1 = CreateTime(schema, "100");
        var at2 = CreateTime(schema, "100");
        var entityId = Guid.NewGuid();

        var fact1 = new SorophyEntityFact(at1, entityId, SorophyEntityFactKind.Created, "Desc");
        var fact2 = new SorophyEntityFact(at2, entityId, SorophyEntityFactKind.Created, "Desc");

        Assert.True(fact1.Equals(fact2));
        Assert.True(fact1.Equals((object)fact2));
        Assert.Equal(fact1.GetHashCode(), fact2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var schema = CreateSchema();
        var at1 = CreateTime(schema, "100");
        var at2 = CreateTime(schema, "200");
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        var fact1 = new SorophyEntityFact(at1, entityId1, SorophyEntityFactKind.Created, "Desc");
        var fact2 = new SorophyEntityFact(at2, entityId1, SorophyEntityFactKind.Created, "Desc");
        var fact3 = new SorophyEntityFact(at1, entityId2, SorophyEntityFactKind.Created, "Desc");
        var fact4 = new SorophyEntityFact(at1, entityId1, SorophyEntityFactKind.Retired, "Desc");
        var fact5 = new SorophyEntityFact(at1, entityId1, SorophyEntityFactKind.Created, "Different");

        Assert.False(fact1.Equals(fact2));
        Assert.False(fact1.Equals(fact3));
        Assert.False(fact1.Equals(fact4));
        Assert.False(fact1.Equals(fact5));
        Assert.False(fact1.Equals(null));
    }

    [Fact]
    public void ToString_ContainsEntityIdKindAndAt()
    {
        var schema = CreateSchema();
        var at = CreateTime(schema, "100");
        var entityId = Guid.NewGuid();

        var fact = new SorophyEntityFact(at, entityId, SorophyEntityFactKind.Created);
        var str = fact.ToString();

        Assert.Contains(entityId.ToString(), str);
        Assert.Contains("Created", str);
        Assert.Contains("100", str);
    }
}

