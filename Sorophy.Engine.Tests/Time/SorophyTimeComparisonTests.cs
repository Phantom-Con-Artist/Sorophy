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
using System.Numerics;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Time;

public sealed class SorophyTimeComparisonTests
{
    private static SorophyTimeSchema CreateNumericSchema(string timeline = "Default")
    {
        var yearUnit = new SorophyTimeUnit(
            "Year",
            0,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));

        var dayUnit = new SorophyTimeUnit(
            "Day",
            1,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));

        return new SorophyTimeSchema(timeline, new[] { yearUnit, dayUnit });
    }

    private static SorophyTimeSchema CreateMixedSchema(string timeline = "Mixed")
    {
        var eraUnit = new SorophyTimeUnit(
            "Era",
            0,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Named));

        var yearUnit = new SorophyTimeUnit(
            "Year",
            1,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));

        return new SorophyTimeSchema(timeline, new[] { eraUnit, yearUnit });
    }

    [Fact]
    public void Compare_SameCoordinate_ReturnsZero()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);

        Assert.Equal(0, SorophyTime.Compare(t1, t2));
        Assert.Equal(0, t1.CompareTo(t2));
        Assert.False(t1.IsBefore(t2));
        Assert.False(t1.IsAfter(t2));
        Assert.True(t1.IsAtOrBefore(t2));
        Assert.True(t1.IsAtOrAfter(t2));
    }

    [Fact]
    public void Compare_PaddedSameValue_ReturnsZero()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "05", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "5", "Year", SorophyTimePrecision.Exact);

        Assert.Equal(0, SorophyTime.Compare(t1, t2));
        Assert.Equal(0, t1.CompareTo(t2));
    }

    [Fact]
    public void Compare_EarlierPrecedesLater()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "50", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);

        Assert.True(SorophyTime.Compare(t1, t2) < 0);
        Assert.True(SorophyTime.Compare(t2, t1) > 0);
        Assert.True(t1.IsBefore(t2));
        Assert.False(t1.IsAfter(t2));
        Assert.True(t2.IsAfter(t1));
        Assert.False(t2.IsBefore(t1));
    }

    [Fact]
    public void Compare_NegativeAndPositiveCoordinates_OrdersCorrectly()
    {
        var schema = CreateNumericSchema();
        var bce = new SorophyTime(schema, "-500", "Year", SorophyTimePrecision.Exact);
        var zero = new SorophyTime(schema, "0", "Year", SorophyTimePrecision.Exact);
        var ce = new SorophyTime(schema, "500", "Year", SorophyTimePrecision.Exact);

        Assert.True(bce.IsBefore(zero));
        Assert.True(zero.IsBefore(ce));
        Assert.True(bce.IsBefore(ce));
        Assert.True(ce.IsAfter(bce));
    }

    [Fact]
    public void Compare_LargeBigIntegerValues_OrdersCorrectly()
    {
        var schema = CreateNumericSchema();
        var large1 = BigInteger.Parse("999999999999999999999999999999");
        var large2 = BigInteger.Parse("1000000000000000000000000000000");

        var t1 = new SorophyTime(schema, large1.ToString(), "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, large2.ToString(), "Year", SorophyTimePrecision.Exact);

        Assert.True(t1.IsBefore(t2));
        Assert.True(t2.IsAfter(t1));
    }

    [Fact]
    public void CanCompare_CompatiblePoints_ReturnsTrue()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "20", "Year", SorophyTimePrecision.Approximate);

        Assert.True(SorophyTime.CanCompare(t1, t2));
    }

    [Fact]
    public void CanCompare_Null_ReturnsFalse()
    {
        var schema = CreateNumericSchema();
        var t = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);

        Assert.False(SorophyTime.CanCompare(null, t));
        Assert.False(SorophyTime.CanCompare(t, null));
        Assert.False(SorophyTime.CanCompare(null, null));
    }

    [Fact]
    public void CanCompare_DifferentSchemas_ReturnsFalse()
    {
        var schema1 = CreateNumericSchema("Timeline1");
        var schema2 = CreateNumericSchema("Timeline2");
        var t1 = new SorophyTime(schema1, "10", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema2, "10", "Year", SorophyTimePrecision.Exact);

        Assert.False(SorophyTime.CanCompare(t1, t2));
    }

    [Fact]
    public void CanCompare_DifferentUnits_ReturnsFalse()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "10", "Day", SorophyTimePrecision.Exact);

        Assert.False(SorophyTime.CanCompare(t1, t2));
    }

    [Fact]
    public void CanCompare_NonNumericPositions_ReturnsFalse()
    {
        var schema = CreateMixedSchema();
        var t1 = new SorophyTime(schema, "Age of Ash", "Era", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "Age of Light", "Era", SorophyTimePrecision.Exact);

        Assert.False(SorophyTime.CanCompare(t1, t2));
    }

    [Fact]
    public void Compare_ThrowsOnDifferentSchemas()
    {
        var schema1 = CreateNumericSchema("Timeline1");
        var schema2 = CreateNumericSchema("Timeline2");
        var t1 = new SorophyTime(schema1, "10", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema2, "10", "Year", SorophyTimePrecision.Exact);

        var ex = Assert.Throws<ArgumentException>(() => SorophyTime.Compare(t1, t2));
        Assert.Contains("different schemas", ex.Message);
    }

    [Fact]
    public void Compare_ThrowsOnDifferentUnits()
    {
        var schema = CreateNumericSchema();
        var t1 = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "10", "Day", SorophyTimePrecision.Exact);

        var ex = Assert.Throws<ArgumentException>(() => SorophyTime.Compare(t1, t2));
        Assert.Contains("different units", ex.Message);
    }

    [Fact]
    public void Compare_ThrowsOnNonNumericPositions()
    {
        var schema = CreateMixedSchema();
        var t1 = new SorophyTime(schema, "First Era", "Era", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "Second Era", "Era", SorophyTimePrecision.Exact);

        var ex = Assert.Throws<InvalidOperationException>(() => SorophyTime.Compare(t1, t2));
        Assert.Contains("numeric position definitions", ex.Message);
    }

    [Fact]
    public void Compare_ThrowsOnNull()
    {
        var schema = CreateNumericSchema();
        var t = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);

        Assert.Throws<ArgumentNullException>(() => SorophyTime.Compare(null!, t));
        Assert.Throws<ArgumentNullException>(() => SorophyTime.Compare(t, null!));
    }

    [Fact]
    public void SorophyTimeComparer_SortsCorrectly()
    {
        var schema = CreateNumericSchema();
        var t10 = new SorophyTime(schema, "10", "Year", SorophyTimePrecision.Exact);
        var tNegative = new SorophyTime(schema, "-5", "Year", SorophyTimePrecision.Exact);
        var t100 = new SorophyTime(schema, "100", "Year", SorophyTimePrecision.Exact);
        var t2 = new SorophyTime(schema, "2", "Year", SorophyTimePrecision.Exact);

        var list = new List<SorophyTime> { t10, t100, tNegative, t2 };
        list.Sort(SorophyTimeComparer.Instance);

        Assert.Equal(tNegative, list[0]);
        Assert.Equal(t2, list[1]);
        Assert.Equal(t10, list[2]);
        Assert.Equal(t100, list[3]);
    }
}

