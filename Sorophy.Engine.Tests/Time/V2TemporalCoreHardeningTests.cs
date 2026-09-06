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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Time;

public sealed class V2TemporalCoreHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Main Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Epoch",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTimeSchema CreateMultiUnitSchema(string timeline = "MultiUnit Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Year", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric)),
                new SorophyTimeUnit("Day", 1, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    // =============================================================
    // 1. TEMPORAL BOUNDARIES, PRECISION & POSITIONING
    // =============================================================

    [Fact]
    public void Temporal_BoundaryPositions_HandledAccurately()
    {
        var schema = CreateSchema();

        var zeroTime = new SorophyTime(schema, "0", "Epoch", SorophyTimePrecision.Exact);
        var largeTime = new SorophyTime(schema, "9223372036854775807", "Epoch", SorophyTimePrecision.Exact);

        Assert.Equal("0", zeroTime.Position);
        Assert.Equal("9223372036854775807", largeTime.Position);

        Assert.True(SorophyTime.Compare(zeroTime, largeTime) < 0);
        Assert.True(SorophyTime.Compare(largeTime, zeroTime) > 0);
        Assert.Equal(0, SorophyTime.Compare(zeroTime, zeroTime));
    }

    [Theory]
    [InlineData(SorophyTimePrecision.Exact)]
    [InlineData(SorophyTimePrecision.Approximate)]
    public void Temporal_PrecisionVariants_PreserveStructuralEquality(SorophyTimePrecision precision)
    {
        var schema = CreateSchema();
        var t1 = new SorophyTime(schema, "100", "Epoch", precision);
        var t2 = new SorophyTime(schema, "100", "Epoch", precision);

        Assert.Equal(t1, t2);
        Assert.True(t1.Equals(t2));
        Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
    }

    [Fact]
    public void Temporal_PrecisionDifference_FailsStructuralEquality()
    {
        var schema = CreateSchema();
        var exact = new SorophyTime(schema, "100", "Epoch", SorophyTimePrecision.Exact);
        var approx = new SorophyTime(schema, "100", "Epoch", SorophyTimePrecision.Approximate);

        Assert.False(exact.Equals(approx));
    }

    // =============================================================
    // 2. EXPLICIT INCOMPATIBILITY ENFORCEMENT (NO GUESSING)
    // =============================================================

    [Fact]
    public void Temporal_IncompatibleSchemas_ThrowsArgumentExceptionOnCompare()
    {
        var schema1 = CreateSchema("Timeline_1");
        var schema2 = CreateSchema("Timeline_2");

        var t1 = new SorophyTime(schema1, "100", "Epoch", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema2, "100", "Epoch", SorophyTimePrecision.Exact);

        // Never guess conversion across schemas
        Assert.Throws<ArgumentException>(() =>
            SorophyTime.Compare(t1, t2));

        // Equals returns false safely without throwing
        Assert.False(t1.Equals(t2));
    }

    [Fact]
    public void Temporal_IncompatibleUnits_ThrowsArgumentExceptionOnCompare()
    {
        var schema = CreateMultiUnitSchema();

        var tYear = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var tDay = new SorophyTime(schema, "10", "Day", SorophyTimePrecision.Exact);

        // Never guess conversion across different units without conversion rules
        Assert.Throws<ArgumentException>(() =>
            SorophyTime.Compare(tYear, tDay));

        Assert.False(tYear.Equals(tDay));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidTextNotNumeric")]
    [InlineData("12.34")]
    public void Temporal_InvalidNumericPositions_ThrowsArgumentException(string invalidPosition)
    {
        var schema = CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(schema, invalidPosition, "Epoch", SorophyTimePrecision.Exact));
    }

    // =============================================================
    // 3. EVENT ENTITY SEMANTICS & EQUIVALENCE
    // =============================================================

    [Fact]
    public void EventEntity_IdenticalOccurredAt_AreTemporallyEquivalentWithoutPrecedence()
    {
        var schema = CreateSchema();
        var coord = new SorophyTime(schema, "500", "Epoch", SorophyTimePrecision.Exact);

        var eventA = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Treaty Signed",
            Type = "Event",
            OccurredAt = coord
        };

        var eventB = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Coronation Ceremony",
            Type = "Event",
            OccurredAt = coord
        };

        var graph = new SorophyGraph();
        graph.AddEntity(eventA);
        graph.AddEntity(eventB);

        // Both events are at the exact same coordinate
        Assert.Equal(0, SorophyTime.Compare(eventA.OccurredAt!, eventB.OccurredAt!));

        // Materializing snapshot from either event yields identical snapshot state
        var snapA = graph.CreateSnapshot(eventA);
        var snapB = graph.CreateSnapshot(eventB);
        var snapCoord = graph.CreateSnapshot(coord);

        Assert.Equal(snapCoord.SnapshotTime, snapA.SnapshotTime);
        Assert.Equal(snapA.SnapshotTime, snapB.SnapshotTime);
        Assert.Equal(snapA.EntityCount, snapB.EntityCount);
        Assert.Equal(snapA.RelationshipCount, snapB.RelationshipCount);
    }

    [Fact]
    public void EventEntity_CreationWithoutEvolutions_IsValidPassiveEntity()
    {
        var schema = CreateSchema();
        var coord = new SorophyTime(schema, "250", "Epoch", SorophyTimePrecision.Exact);

        var eventEntity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "ObservationOnlyEvent",
            Type = "Event",
            OccurredAt = coord,
            Description = "An event with no dependent relationship evolutions"
        };

        var graph = new SorophyGraph();
        graph.AddEntity(eventEntity);

        Assert.True(eventEntity.IsEvent);
        Assert.Empty(graph.Validate());

        var snap = graph.CreateSnapshot(eventEntity);
        Assert.True(snap.ContainsEntity(eventEntity.Id));
    }
}
