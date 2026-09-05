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
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Tests.Time;

public sealed class SorophyTimeTestSuite
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimePositionDefinition OrdinalPosition =>
        new(SorophyTimePositionKind.Ordinal);

    private static SorophyTimePositionDefinition NamedPosition =>
        new(SorophyTimePositionKind.Named);

    private static SorophyTimePositionDefinition PatternPosition =>
        new(SorophyTimePositionKind.Pattern, @"\d{2}-\d{2}-\d{4}");

    private static SorophyTimePositionDefinition FreeFormPosition =>
        new(SorophyTimePositionKind.FreeForm);

    private static SorophyTimeUnit EraUnit =>
        new("Era", 0, NamedPosition);

    private static SorophyTimeUnit AgeUnit =>
        new("Age", 1, NamedPosition);

    private static SorophyTimeUnit YearUnit =>
        new("Year", 2, NumericPosition);

    private static SorophyTimeUnit SeasonUnit =>
        new("Season", 3, OrdinalPosition);

    private static SorophyTimeUnit DayUnit =>
        new("Day", 4, NumericPosition);

    private static SorophyTimeSchema CreateSchema()
    {
        return new SorophyTimeSchema(
            "Era of Black Pig",
            new[]
            {
                DayUnit,
                YearUnit,
                EraUnit,
                SeasonUnit,
                AgeUnit
            });
    }

    // -------------------------------------------------------------------------
    // SorophyTimePositionDefinition
    // -------------------------------------------------------------------------

    [Fact]
    public void PositionDefinition_AllKindsCanBeCreated()
    {
        var numeric = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Numeric);

        var ordinal = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Ordinal);

        var named = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Named);

        var pattern = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Pattern,
            @"\d{2}-\d{2}-\d{4}");

        var freeForm = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.FreeForm);

        Assert.Equal(SorophyTimePositionKind.Numeric, numeric.Kind);
        Assert.Equal(SorophyTimePositionKind.Ordinal, ordinal.Kind);
        Assert.Equal(SorophyTimePositionKind.Named, named.Kind);
        Assert.Equal(SorophyTimePositionKind.Pattern, pattern.Kind);
        Assert.Equal(SorophyTimePositionKind.FreeForm, freeForm.Kind);

        Assert.Equal(@"\d{2}-\d{2}-\d{4}", pattern.Pattern);
        Assert.Null(numeric.Pattern);
        Assert.Null(ordinal.Pattern);
        Assert.Null(named.Pattern);
        Assert.Null(freeForm.Pattern);
    }

    [Fact]
    public void PositionDefinition_PatternRequiresPattern()
    {
        Assert.Throws<ArgumentException>(() =>
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.Pattern));
    }

    [Fact]
    public void PositionDefinition_NonPatternKindsRejectPattern()
    {
        Assert.Throws<ArgumentException>(() =>
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.Numeric,
                "123"));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.Ordinal,
                "First"));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.Named,
                "Spring"));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.FreeForm,
                "Anything"));
    }

    [Fact]
    public void PositionDefinition_EqualityIsStructural()
    {
        var first = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Pattern,
            @"\d{2}-\d{2}-\d{4}");

        var second = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Pattern,
            @"\d{2}-\d{2}-\d{4}");

        var different = new SorophyTimePositionDefinition(
            SorophyTimePositionKind.Pattern,
            @"\d{4}-\d{2}-\d{2}");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
    }

    // -------------------------------------------------------------------------
    // SorophyTimeUnit
    // -------------------------------------------------------------------------

    [Fact]
    public void TimeUnit_StoresNameOrderAndDefinition()
    {
        var unit = new SorophyTimeUnit(
            "Year",
            2,
            NumericPosition);

        Assert.Equal("Year", unit.Name);
        Assert.Equal(2, unit.Order);
        Assert.Equal(NumericPosition, unit.PositionDefinition);
    }

    [Fact]
    public void TimeUnit_RejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeUnit(
                "",
                0,
                NumericPosition));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeUnit(
                "   ",
                0,
                NumericPosition));
    }

    [Fact]
    public void TimeUnit_RejectsNullPositionDefinition()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SorophyTimeUnit(
                "Year",
                0,
                null!));
    }

    [Fact]
    public void TimeUnit_EqualityIsStructural()
    {
        var first = new SorophyTimeUnit(
            "Year",
            2,
            NumericPosition);

        var second = new SorophyTimeUnit(
            "Year",
            2,
            NumericPosition);

        var differentOrder = new SorophyTimeUnit(
            "Year",
            3,
            NumericPosition);

        var differentName = new SorophyTimeUnit(
            "Sol",
            2,
            NumericPosition);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        Assert.NotEqual(first, differentOrder);
        Assert.NotEqual(first, differentName);
    }

    // -------------------------------------------------------------------------
    // SorophyTimeSchema
    // -------------------------------------------------------------------------

    [Fact]
    public void TimeSchema_StoresTimeline()
    {
        var schema = CreateSchema();

        Assert.Equal(
            "Era of Black Pig",
            schema.Timeline);
    }

    [Fact]
    public void TimeSchema_SortsUnitsByOrder()
    {
        var schema = CreateSchema();

        Assert.Equal(
            new[]
            {
                "Era",
                "Age",
                "Year",
                "Season",
                "Day"
            },
            new[]
            {
                schema.Units[0].Name,
                schema.Units[1].Name,
                schema.Units[2].Name,
                schema.Units[3].Name,
                schema.Units[4].Name
            });
    }

    [Fact]
    public void TimeSchema_ReportsUnitCount()
    {
        var schema = CreateSchema();

        Assert.Equal(5, schema.UnitCount);
        Assert.Equal(5, schema.Units.Count);
    }

    [Fact]
    public void TimeSchema_CanFindUnitsByName()
    {
        var schema = CreateSchema();

        Assert.True(schema.ContainsUnit("Year"));
        Assert.True(schema.ContainsUnit("Day"));
        Assert.False(schema.ContainsUnit("Month"));

        Assert.Equal(
            "Year",
            schema.GetUnit("Year")?.Name);

        Assert.Null(
            schema.GetUnit("Month"));
    }

    [Fact]
    public void TimeSchema_CanFindUnitsByOrder()
    {
        var schema = CreateSchema();

        Assert.Equal(
            "Era",
            schema.GetUnitByOrder(0)?.Name);

        Assert.Equal(
            "Year",
            schema.GetUnitByOrder(2)?.Name);

        Assert.Equal(
            "Day",
            schema.GetUnitByOrder(4)?.Name);

        Assert.Null(
            schema.GetUnitByOrder(99));
    }

    [Fact]
    public void TimeSchema_UnderstandsUnitHierarchy()
    {
        var schema = CreateSchema();

        Assert.True(
            schema.IsLargerUnit(
                "Era",
                "Year"));

        Assert.True(
            schema.IsLargerUnit(
                "Year",
                "Day"));

        Assert.True(
            schema.IsSmallerUnit(
                "Day",
                "Year"));

        Assert.True(
            schema.IsSmallerUnit(
                "Year",
                "Era"));

        Assert.False(
            schema.IsLargerUnit(
                "Day",
                "Year"));

        Assert.False(
            schema.IsSmallerUnit(
                "Era",
                "Year"));
    }

    [Fact]
    public void TimeSchema_RejectsBlankTimeline()
    {
        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeSchema(
                "",
                new[] { YearUnit }));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeSchema(
                "   ",
                new[] { YearUnit }));
    }

    [Fact]
    public void TimeSchema_RejectsNullUnits()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SorophyTimeSchema(
                "Timeline",
                null!));
    }

    [Fact]
    public void TimeSchema_RejectsEmptyUnits()
    {
        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeSchema(
                "Timeline",
                Array.Empty<SorophyTimeUnit>()));
    }

    [Fact]
    public void TimeSchema_RejectsDuplicateUnitNames()
    {
        var first = new SorophyTimeUnit(
            "Year",
            0,
            NumericPosition);

        var second = new SorophyTimeUnit(
            "Year",
            1,
            NumericPosition);

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeSchema(
                "Timeline",
                new[] { first, second }));
    }

    [Fact]
    public void TimeSchema_RejectsDuplicateOrders()
    {
        var first = new SorophyTimeUnit(
            "Year",
            0,
            NumericPosition);

        var second = new SorophyTimeUnit(
            "Season",
            0,
            OrdinalPosition);

        Assert.Throws<ArgumentException>(() =>
            new SorophyTimeSchema(
                "Timeline",
                new[] { first, second }));
    }

    [Fact]
    public void TimeSchema_UnitsAreReadOnly()
    {
        var schema = CreateSchema();

        Assert.IsAssignableFrom<IReadOnlyList<SorophyTimeUnit>>(
            schema.Units);

        Assert.IsType<ReadOnlyCollection<SorophyTimeUnit>>(
            schema.Units);
    }

    [Fact]
    public void TimeSchema_UnitsCannotBeMutatedThroughReturnedCollection()
    {
        var schema = CreateSchema();

        var units =
            schema.Units;

        var collection =
            Assert.IsAssignableFrom<IList<SorophyTimeUnit>>(
                units);

        Assert.Throws<NotSupportedException>(() =>
            collection[0] = YearUnit);

        Assert.Throws<NotSupportedException>(() =>
            collection.Add(
                new SorophyTimeUnit(
                    "Month",
                    5,
                    NumericPosition)));

        Assert.Throws<NotSupportedException>(() =>
            collection.Remove(
                units[0]));
    }

    [Fact]
    public void TimeSchema_RemainsStableAfterConstruction()
    {
        var schema = CreateSchema();

        var originalNames =
            schema.Units
                .Select(unit => unit.Name)
                .ToArray();

        var originalOrders =
            schema.Units
                .Select(unit => unit.Order)
                .ToArray();

        Assert.Equal(
            new[]
            {
                "Era",
                "Age",
                "Year",
                "Season",
                "Day"
            },
            originalNames);

        Assert.Equal(
            new[]
            {
                0,
                1,
                2,
                3,
                4
            },
            originalOrders);

        Assert.Equal(
            "Era",
            schema.GetUnitByOrder(0)?.Name);

        Assert.Equal(
            "Day",
            schema.GetUnitByOrder(4)?.Name);
    }

    [Fact]
    public void TimeSchema_RequiresNewSchemaForDifferentTemporalContract()
    {
        var original =
            new SorophyTimeSchema(
                "Era of Black Pig",
                new[]
                {
                    new SorophyTimeUnit(
                        "Era",
                        0,
                        NamedPosition),

                    new SorophyTimeUnit(
                        "Season",
                        1,
                        OrdinalPosition),

                    new SorophyTimeUnit(
                        "Day",
                        2,
                        NumericPosition)
                });

        var replacement =
            new SorophyTimeSchema(
                "Era of Doom",
                new[]
                {
                    new SorophyTimeUnit(
                        "Era",
                        0,
                        NamedPosition),

                    new SorophyTimeUnit(
                        "Sol",
                        1,
                        NumericPosition)
                });

        Assert.NotEqual(
            original,
            replacement);

        Assert.True(
            original.ContainsUnit("Season"));

        Assert.False(
            original.ContainsUnit("Sol"));

        Assert.True(
            replacement.ContainsUnit("Sol"));

        Assert.False(
            replacement.ContainsUnit("Season"));
    }

    [Fact]
    public void TimeSchema_DoesNotRetroactivelyChangeExistingSorophyTime()
    {
        var original =
            new SorophyTimeSchema(
                "Era of Black Pig",
                new[]
                {
                    new SorophyTimeUnit(
                        "Year",
                        0,
                        NumericPosition)
                });

        var time =
            new SorophyTime(
                original,
                "110",
                "Year",
                SorophyTimePrecision.Exact);

        var replacement =
            new SorophyTimeSchema(
                "Era of Doom",
                new[]
                {
                    new SorophyTimeUnit(
                        "Sol",
                        0,
                        NumericPosition)
                });

        Assert.Equal(
            "Era of Black Pig",
            time.Timeline);

        Assert.Equal(
            "110",
            time.Position);

        Assert.Equal(
            "Year",
            time.Unit.Name);

        Assert.Equal(
            SorophyTimePrecision.Exact,
            time.Precision);

        Assert.Equal(
            "Era of Doom",
            replacement.Timeline);
    }

    [Fact]
    public void TimeSchema_EqualityIsStructural()
    {
        var first = CreateSchema();

        var second = new SorophyTimeSchema(
            "Era of Black Pig",
            new[]
            {
                new SorophyTimeUnit(
                    "Day",
                    4,
                    NumericPosition),

                new SorophyTimeUnit(
                    "Year",
                    2,
                    NumericPosition),

                new SorophyTimeUnit(
                    "Era",
                    0,
                    NamedPosition),

                new SorophyTimeUnit(
                    "Season",
                    3,
                    OrdinalPosition),

                new SorophyTimeUnit(
                    "Age",
                    1,
                    NamedPosition)
            });

        var differentTimeline = new SorophyTimeSchema(
            "Era of Doom",
            new[]
            {
                EraUnit,
                AgeUnit,
                YearUnit,
                SeasonUnit,
                DayUnit
            });

        Assert.Equal(
            first,
            second);

        Assert.Equal(
            first.GetHashCode(),
            second.GetHashCode());

        Assert.NotEqual(
            first,
            differentTimeline);
    }

    // -------------------------------------------------------------------------
    // SorophyTime
    // -------------------------------------------------------------------------

    [Fact]
    public void SorophyTime_StoresSchemaTimelinePositionUnitAndPrecision()
    {
        var schema = CreateSchema();

        var time = new SorophyTime(
            schema,
            "110",
            "Year",
            SorophyTimePrecision.Exact);

        Assert.Same(schema, time.Schema);
        Assert.Equal(
            "Era of Black Pig",
            time.Timeline);

        Assert.Equal(
            "110",
            time.Position);

        Assert.Equal(
            "Year",
            time.Unit.Name);

        Assert.Equal(
            SorophyTimePrecision.Exact,
            time.Precision);
    }

    [Fact]
    public void SorophyTime_SupportsApproximatePrecision()
    {
        var schema = CreateSchema();

        var time = new SorophyTime(
            schema,
            "Third Season",
            "Season",
            SorophyTimePrecision.Approximate);

        Assert.Equal(
            "Third Season",
            time.Position);

        Assert.Equal(
            "Season",
            time.Unit.Name);

        Assert.Equal(
            SorophyTimePrecision.Approximate,
            time.Precision);
    }

    [Fact]
    public void SorophyTime_RejectsNullSchema()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SorophyTime(
                null!,
                "110",
                "Year",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_RejectsBlankPosition()
    {
        var schema = CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "",
                "Year",
                SorophyTimePrecision.Exact));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "   ",
                "Year",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_RejectsBlankUnitName()
    {
        var schema = CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "110",
                "",
                SorophyTimePrecision.Exact));

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "110",
                "   ",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_RejectsUnitNotDefinedBySchema()
    {
        var schema = CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "110",
                "Month",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_RejectsInvalidNumericPosition()
    {
        var schema =
            new SorophyTimeSchema(
                "Numeric Timeline",
                new[]
                {
                    new SorophyTimeUnit(
                        "Year",
                        0,
                        NumericPosition)
                });

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "banana",
                "Year",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_AcceptsValidNumericPosition()
    {
        var schema =
            new SorophyTimeSchema(
                "Numeric Timeline",
                new[]
                {
                    new SorophyTimeUnit(
                        "Year",
                        0,
                        NumericPosition)
                });

        var time =
            new SorophyTime(
                schema,
                "110",
                "Year",
                SorophyTimePrecision.Exact);

        Assert.Equal(
            "110",
            time.Position);
    }

    [Fact]
    public void SorophyTime_RejectsInvalidPatternPosition()
    {
        var schema =
            new SorophyTimeSchema(
                "Date Timeline",
                new[]
                {
                    new SorophyTimeUnit(
                        "Date",
                        0,
                        PatternPosition)
                });

        Assert.Throws<ArgumentException>(() =>
            new SorophyTime(
                schema,
                "03/07/2001",
                "Date",
                SorophyTimePrecision.Exact));
    }

    [Fact]
    public void SorophyTime_AcceptsValidPatternPosition()
    {
        var schema =
            new SorophyTimeSchema(
                "Date Timeline",
                new[]
                {
                    new SorophyTimeUnit(
                        "Date",
                        0,
                        PatternPosition)
                });

        var time =
            new SorophyTime(
                schema,
                "03-07-2001",
                "Date",
                SorophyTimePrecision.Exact);

        Assert.Equal(
            "03-07-2001",
            time.Position);
    }

    [Fact]
    public void SorophyTime_EqualityIsStructural()
    {
        var schema = CreateSchema();

        var first = new SorophyTime(
            schema,
            "110",
            "Year",
            SorophyTimePrecision.Exact);

        var second = new SorophyTime(
            schema,
            "110",
            "Year",
            SorophyTimePrecision.Exact);

        var differentPosition = new SorophyTime(
            schema,
            "111",
            "Year",
            SorophyTimePrecision.Exact);

        var differentPrecision = new SorophyTime(
            schema,
            "110",
            "Year",
            SorophyTimePrecision.Approximate);

        Assert.Equal(first, second);
        Assert.Equal(
            first.GetHashCode(),
            second.GetHashCode());

        Assert.NotEqual(
            first,
            differentPosition);

        Assert.NotEqual(
            first,
            differentPrecision);
    }

    [Fact]
    public void SorophyTime_ToStringContainsTemporalInformation()
    {
        var schema = CreateSchema();

        var time = new SorophyTime(
            schema,
            "110",
            "Year",
            SorophyTimePrecision.Exact);

        var text = time.ToString();

        Assert.Contains(
            "Era of Black Pig",
            text);

        Assert.Contains(
            "110",
            text);

        Assert.Contains(
            "Year",
            text);

        Assert.Contains(
            "Exact",
            text);
    }

    // -------------------------------------------------------------------------
    // Cross-component integration
    // -------------------------------------------------------------------------

    [Fact]
    public void TimeSystem_SupportsWorldbuildingStyleTemporalCoordinates()
    {
        var schema = new SorophyTimeSchema(
            "Era of Black Pig",
            new[]
            {
                new SorophyTimeUnit(
                    "Era",
                    0,
                    new SorophyTimePositionDefinition(
                        SorophyTimePositionKind.Named)),

                new SorophyTimeUnit(
                    "Season",
                    1,
                    new SorophyTimePositionDefinition(
                        SorophyTimePositionKind.Ordinal)),

                new SorophyTimeUnit(
                    "Day",
                    2,
                    new SorophyTimePositionDefinition(
                        SorophyTimePositionKind.Numeric))
            });

        var era = new SorophyTime(
            schema,
            "The Long Dusk",
            "Era",
            SorophyTimePrecision.Approximate);

        var season = new SorophyTime(
            schema,
            "Third",
            "Season",
            SorophyTimePrecision.Exact);

        var day = new SorophyTime(
            schema,
            "110",
            "Day",
            SorophyTimePrecision.Exact);

        Assert.Equal(
            "Era of Black Pig",
            era.Timeline);

        Assert.Equal(
            "The Long Dusk",
            era.Position);

        Assert.Equal(
            "Third",
            season.Position);

        Assert.Equal(
            "Season",
            season.Unit.Name);

        Assert.Equal(
            "110",
            day.Position);

        Assert.Equal(
            "Day",
            day.Unit.Name);

        Assert.True(
            schema.IsLargerUnit(
                "Era",
                "Season"));

        Assert.True(
            schema.IsLargerUnit(
                "Season",
                "Day"));
    }

    [Fact]
    public void TimeSystem_SupportsMultipleIndependentTimelines()
    {
        var worldTimeline = new SorophyTimeSchema(
            "World Calendar",
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    NumericPosition),

                new SorophyTimeUnit(
                    "Day",
                    1,
                    NumericPosition)
            });

        var historicalTimeline = new SorophyTimeSchema(
            "Historical Calendar",
            new[]
            {
                new SorophyTimeUnit(
                    "Age",
                    0,
                    NamedPosition),

                new SorophyTimeUnit(
                    "Year",
                    1,
                    NumericPosition)
            });

        var worldTime = new SorophyTime(
            worldTimeline,
            "110",
            "Year",
            SorophyTimePrecision.Exact);

        var historicalTime = new SorophyTime(
            historicalTimeline,
            "Age of Kings",
            "Age",
            SorophyTimePrecision.Approximate);

        Assert.Equal(
            "World Calendar",
            worldTime.Timeline);

        Assert.Equal(
            "Historical Calendar",
            historicalTime.Timeline);

        Assert.NotEqual(
            worldTime,
            historicalTime);

        Assert.NotEqual(
            worldTimeline,
            historicalTimeline);
    }

    [Fact]
    public void TimeSystem_PatternDefinitionsCanRepresentPreciseWorldDates()
    {
        var schema = new SorophyTimeSchema(
            "Gregorian-Compatible Timeline",
            new[]
            {
                new SorophyTimeUnit(
                    "Date",
                    0,
                    PatternPosition)
            });

        var time = new SorophyTime(
            schema,
            "03-07-2001",
            "Date",
            SorophyTimePrecision.Exact);

        Assert.Equal(
            "03-07-2001",
            time.Position);

        Assert.Equal(
            SorophyTimePositionKind.Pattern,
            time.Unit.PositionDefinition.Kind);

        Assert.Equal(
            @"\d{2}-\d{2}-\d{4}",
            time.Unit.PositionDefinition.Pattern);
    }

    [Fact]
    public void TimeSystem_AllowsFreeFormWorldbuildingPositions()
    {
        var schema = new SorophyTimeSchema(
            "Mythic Timeline",
            new[]
            {
                new SorophyTimeUnit(
                    "Age",
                    0,
                    FreeFormPosition)
            });

        var time = new SorophyTime(
            schema,
            "When the Black Star Fell",
            "Age",
            SorophyTimePrecision.Approximate);

        Assert.Equal(
            "When the Black Star Fell",
            time.Position);

        Assert.Equal(
            SorophyTimePositionKind.FreeForm,
            time.Unit.PositionDefinition.Kind);
    }
}