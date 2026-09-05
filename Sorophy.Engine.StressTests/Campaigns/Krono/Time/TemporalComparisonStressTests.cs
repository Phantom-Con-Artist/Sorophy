/*
 * Sorophy Engine ? a structured knowledge and graph engine
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
using System.Globalization;
using System.Numerics;
using Sorophy.Engine.StressTests.Infrastructure;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Krono Temporal Scope",
    "krono-time",
    order: 150)]
public static class TemporalComparisonStressTests
{
    public static TestCampaignResult Run(TestCampaignContext context)
    {
        var started = DateTime.UtcNow;
        var passedChecks = 0;
        const int totalChecks = 4;

        Console.WriteLine("================================================================");
        Console.WriteLine("                KRONO TEMPORAL SCOPE CAMPAIGN                   ");
        Console.WriteLine("================================================================");
        Console.WriteLine($"  Seed:            {context.Seed}");
        Console.WriteLine($"  Operations:      {context.Operations:N0}");
        Console.WriteLine($"  Audit Interval:  {context.AuditInterval:N0}");
        Console.WriteLine($"  Profile:         {context.Profile}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("[1/4] Running Temporal Comparison Stress...");
            RunComparisonStress(context);
            passedChecks++;
            Console.WriteLine("  Temporal comparison stress ........... PASS");

            Console.WriteLine("[2/4] Running Creation History Stress...");
            CreationHistoryStressTests.RunCreationHistoryStress(context);
            passedChecks++;
            Console.WriteLine("  Creation history stress .............. PASS");

            Console.WriteLine("[3/4] Running Retirement Stress...");
            RetirementStressTests.RunRetirementStress(context);
            passedChecks++;
            Console.WriteLine("  Retirement stress .................... PASS");

            Console.WriteLine("[4/4] Running Temporal Lifecycle Round-Trip Stress...");
            TemporalLifecycleRoundTripStressTests.RunLifecycleRoundTripStress(context);
            passedChecks++;
            Console.WriteLine("  Temporal lifecycle round-trip ........ PASS");

            var duration = DateTime.UtcNow - started;
            Console.WriteLine();
            Console.WriteLine("================================================================");
            Console.WriteLine($"STATUS: PASS ({passedChecks}/{totalChecks} checks in {duration.TotalSeconds:F2}s)");
            Console.WriteLine("================================================================");

            return TestCampaignResult.Pass(
                "Krono Temporal Scope",
                "krono-time",
                passedChecks,
                totalChecks,
                duration,
                "Temporal comparison, creation history, relationship retirement, and lifecycle round-trip passed.");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;
            Console.WriteLine();
            Console.WriteLine($"STATUS: FAIL at check {passedChecks + 1}/{totalChecks} - {ex.Message}");

            return TestCampaignResult.Fail(
                "Krono Temporal Scope",
                "krono-time",
                duration,
                $"Krono temporal scope failure at check {passedChecks + 1}: {ex.Message}",
                ex,
                passedChecks,
                totalChecks);
        }
    }

    internal static void RunComparisonStress(TestCampaignContext context)
    {
        var random = new KronoStressRandom(context.Seed);
        var schemaA = KronoTestSchemas.CreateDefaultNumericSchema("ComparisonTimelineA");
        var schemaB = KronoTestSchemas.CreateDefaultNumericSchema("ComparisonTimelineB");
        var multiUnitSchema = KronoTestSchemas.CreateMultiUnitSchema("ComparisonMultiUnitTimeline");
        var nonNumericSchema = KronoTestSchemas.CreateNonNumericSchema("ComparisonNonNumericTimeline");

        int operationCount = Math.Max(10_000, context.Operations / 2);

        // 1. High-volume randomized numeric comparisons
        for (var i = 0; i < operationCount; i++)
        {
            BigInteger leftVal;
            BigInteger rightVal;
            string leftStr;
            string rightStr;

            int mode = random.Next(6);
            switch (mode)
            {
                case 0:
                    // Small integers (-100,000 to 100,000)
                    leftVal = random.NextLong(-100_000, 100_000);
                    rightVal = random.NextLong(-100_000, 100_000);
                    leftStr = leftVal.ToString(CultureInfo.InvariantCulture);
                    rightStr = rightVal.ToString(CultureInfo.InvariantCulture);
                    break;

                case 1:
                    // Equal coordinates, including padded strings
                    leftVal = random.NextBigInteger(random.Next(1, 35));
                    rightVal = leftVal;
                    leftStr = leftVal.ToString(CultureInfo.InvariantCulture);
                    if (random.NextBoolean())
                    {
                        var absVal = BigInteger.Abs(leftVal).ToString(CultureInfo.InvariantCulture);
                        var padding = new string('0', random.Next(1, 6));
                        rightStr = leftVal.Sign < 0 ? "-" + padding + absVal : padding + absVal;
                    }
                    else
                    {
                        rightStr = leftStr;
                    }
                    break;

                case 2:
                    // Zero comparisons
                    leftVal = BigInteger.Zero;
                    rightVal = random.NextBoolean() ? BigInteger.Zero : random.NextBigInteger(random.Next(1, 20));
                    leftStr = random.NextBoolean() ? "0" : new string('0', random.Next(1, 6));
                    rightStr = rightVal == BigInteger.Zero && random.NextBoolean()
                        ? new string('0', random.Next(1, 5))
                        : rightVal.ToString(CultureInfo.InvariantCulture);
                    break;

                case 3:
                    // Very large BigInteger values (>= 10^30)
                    leftVal = random.NextBigInteger(random.Next(30, 60));
                    rightVal = random.NextBigInteger(random.Next(30, 60));
                    leftStr = leftVal.ToString(CultureInfo.InvariantCulture);
                    rightStr = rightVal.ToString(CultureInfo.InvariantCulture);
                    break;

                case 4:
                    // Negative vs Positive
                    leftVal = -BigInteger.Abs(random.NextBigInteger(random.Next(1, 40)));
                    rightVal = BigInteger.Abs(random.NextBigInteger(random.Next(1, 40)));
                    leftStr = leftVal.ToString(CultureInfo.InvariantCulture);
                    rightStr = rightVal.ToString(CultureInfo.InvariantCulture);
                    break;

                default:
                    // Arbitrary BigIntegers with arbitrary digits
                    leftVal = random.NextBigInteger(random.Next(1, 45));
                    rightVal = random.NextBigInteger(random.Next(1, 45));
                    leftStr = leftVal.ToString(CultureInfo.InvariantCulture);
                    rightStr = rightVal.ToString(CultureInfo.InvariantCulture);
                    break;
            }

            var tLeft = new SorophyTime(schemaA, leftStr, "Tick", SorophyTimePrecision.Exact);
            var tRight = new SorophyTime(schemaA, rightStr, "Tick", SorophyTimePrecision.Exact);

            // CanCompare
            if (!SorophyTime.CanCompare(tLeft, tRight))
            {
                throw new InvalidOperationException(
                    $"CanCompare returned false for compatible numeric coordinates '{leftStr}' and '{rightStr}'.");
            }

            // Compare result
            int cmp = SorophyTime.Compare(tLeft, tRight);
            int expectedCmp = leftVal.CompareTo(rightVal);

            if (Math.Sign(cmp) != Math.Sign(expectedCmp))
            {
                throw new InvalidOperationException(
                    $"SorophyTime.Compare mismatch for '{leftStr}' and '{rightStr}'. Expected sign {Math.Sign(expectedCmp)}, got {Math.Sign(cmp)}.");
            }

            // Instance method CompareTo
            if (tLeft.CompareTo(tRight) != cmp)
            {
                throw new InvalidOperationException("Instance CompareTo did not match static Compare.");
            }

            // Reflexivity
            if (SorophyTime.Compare(tLeft, tLeft) != 0 || !tLeft.IsAtOrBefore(tLeft) || !tLeft.IsAtOrAfter(tLeft) || tLeft.IsBefore(tLeft) || tLeft.IsAfter(tLeft))
            {
                throw new InvalidOperationException($"Reflexivity invariant violated for coordinate '{leftStr}'.");
            }

            // Antisymmetry
            int reverseCmp = SorophyTime.Compare(tRight, tLeft);
            if (Math.Sign(reverseCmp) != -Math.Sign(cmp))
            {
                throw new InvalidOperationException(
                    $"Antisymmetry invariant violated between '{leftStr}' and '{rightStr}'.");
            }

            // Boolean alignment
            if (tLeft.IsBefore(tRight) != (cmp < 0))
            {
                throw new InvalidOperationException($"IsBefore inconsistent with Compare for '{leftStr}' and '{rightStr}'.");
            }

            if (tLeft.IsAfter(tRight) != (cmp > 0))
            {
                throw new InvalidOperationException($"IsAfter inconsistent with Compare for '{leftStr}' and '{rightStr}'.");
            }

            if (tLeft.IsAtOrBefore(tRight) != (cmp <= 0))
            {
                throw new InvalidOperationException($"IsAtOrBefore inconsistent with Compare for '{leftStr}' and '{rightStr}'.");
            }

            if (tLeft.IsAtOrAfter(tRight) != (cmp >= 0))
            {
                throw new InvalidOperationException($"IsAtOrAfter inconsistent with Compare for '{leftStr}' and '{rightStr}'.");
            }
        }

        // 2. Transitivity test
        for (var i = 0; i < 2_000; i++)
        {
            var v1 = random.NextBigInteger(random.Next(1, 40));
            var v2 = random.NextBigInteger(random.Next(1, 40));
            var v3 = random.NextBigInteger(random.Next(1, 40));

            var values = new[] { v1, v2, v3 };
            Array.Sort(values);

            var tA = new SorophyTime(schemaA, values[0].ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tB = new SorophyTime(schemaA, values[1].ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);
            var tC = new SorophyTime(schemaA, values[2].ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact);

            if (!tA.IsAtOrBefore(tB) || !tB.IsAtOrBefore(tC))
            {
                throw new InvalidOperationException("Sort ordering failed in transitivity test.");
            }

            if (!tA.IsAtOrBefore(tC))
            {
                throw new InvalidOperationException(
                    $"Transitivity violated: {values[0]} <= {values[1]} <= {values[2]}, but IsAtOrBefore returned false.");
            }
        }

        // 3. Sorting Invariance using SorophyTimeComparer.Instance
        for (var batch = 0; batch < 50; batch++)
        {
            const int batchSize = 100;
            var timeList = new List<SorophyTime>(batchSize);
            var valueList = new List<BigInteger>(batchSize);

            for (var i = 0; i < batchSize; i++)
            {
                var val = (i % 5 == 0)
                    ? random.NextBigInteger(random.Next(30, 50))
                    : random.NextBigInteger(random.Next(1, 20));
                valueList.Add(val);
                timeList.Add(new SorophyTime(schemaA, val.ToString(CultureInfo.InvariantCulture), "Tick", SorophyTimePrecision.Exact));
            }

            timeList.Sort(SorophyTimeComparer.Instance);
            valueList.Sort();

            for (var i = 0; i < batchSize; i++)
            {
                var parsed = BigInteger.Parse(timeList[i].Position, CultureInfo.InvariantCulture);
                if (parsed != valueList[i])
                {
                    throw new InvalidOperationException(
                        $"Sort invariance failed at index {i}: expected {valueList[i]}, found {parsed}.");
                }

                if (i > 0)
                {
                    if (SorophyTime.Compare(timeList[i - 1], timeList[i]) > 0)
                    {
                        throw new InvalidOperationException(
                            $"Sort ordering violated between index {i - 1} and {i}.");
                    }
                }
            }

            // Comparer null handling
            var sampleTime = timeList[0];
            if (SorophyTimeComparer.Instance.Compare(null, sampleTime) >= 0)
            {
                throw new InvalidOperationException("SorophyTimeComparer: null must precede non-null.");
            }
            if (SorophyTimeComparer.Instance.Compare(sampleTime, null) <= 0)
            {
                throw new InvalidOperationException("SorophyTimeComparer: non-null must succeed null.");
            }
            if (SorophyTimeComparer.Instance.Compare(null, null) != 0)
            {
                throw new InvalidOperationException("SorophyTimeComparer: null and null must be equal.");
            }
            if (SorophyTimeComparer.Instance.Compare(sampleTime, sampleTime) != 0)
            {
                throw new InvalidOperationException("SorophyTimeComparer: identical instances must be equal.");
            }
        }

        // 4. Rejection Guarantees
        // Cross-schema rejection
        var tSchemaA = new SorophyTime(schemaA, "100", "Tick", SorophyTimePrecision.Exact);
        var tSchemaB = new SorophyTime(schemaB, "100", "Tick", SorophyTimePrecision.Exact);
        if (SorophyTime.CanCompare(tSchemaA, tSchemaB))
        {
            throw new InvalidOperationException("CanCompare must return false for coordinates across different schemas.");
        }
        try
        {
            SorophyTime.Compare(tSchemaA, tSchemaB);
            throw new InvalidOperationException("Compare must throw ArgumentException for cross-schema coordinates.");
        }
        catch (ArgumentException) { /* expected */ }

        // Cross-unit rejection
        var tYear = new SorophyTime(multiUnitSchema, "2026", "Year", SorophyTimePrecision.Exact);
        var tDay = new SorophyTime(multiUnitSchema, "100", "Day", SorophyTimePrecision.Exact);
        if (SorophyTime.CanCompare(tYear, tDay))
        {
            throw new InvalidOperationException("CanCompare must return false for coordinates across different units.");
        }
        try
        {
            SorophyTime.Compare(tYear, tDay);
            throw new InvalidOperationException("Compare must throw ArgumentException for cross-unit coordinates.");
        }
        catch (ArgumentException) { /* expected */ }

        // Non-numeric position definitions
        var nonNumericUnits = new[] { "NamedUnit", "OrdinalUnit", "PatternUnit", "FreeUnit" };
        var samplePositions = new[] { "NamedPos", "FirstOrdinal", "2026", "FreePos" };
        for (var i = 0; i < nonNumericUnits.Length; i++)
        {
            var tNonNum1 = new SorophyTime(nonNumericSchema, samplePositions[i], nonNumericUnits[i], SorophyTimePrecision.Exact);
            var tNonNum2 = new SorophyTime(nonNumericSchema, samplePositions[i], nonNumericUnits[i], SorophyTimePrecision.Exact);

            if (SorophyTime.CanCompare(tNonNum1, tNonNum2))
            {
                throw new InvalidOperationException($"CanCompare must return false for non-numeric unit '{nonNumericUnits[i]}'.");
            }
            try
            {
                SorophyTime.Compare(tNonNum1, tNonNum2);
                throw new InvalidOperationException($"Compare must throw InvalidOperationException for non-numeric unit '{nonNumericUnits[i]}'.");
            }
            catch (InvalidOperationException) { /* expected */ }
        }

        // Null argument rejection
        if (SorophyTime.CanCompare(null, tSchemaA) || SorophyTime.CanCompare(tSchemaA, null) || SorophyTime.CanCompare(null, null))
        {
            throw new InvalidOperationException("CanCompare must return false when any operand is null.");
        }

        try
        {
            SorophyTime.Compare(null!, tSchemaA);
            throw new InvalidOperationException("Compare must throw ArgumentNullException when left is null.");
        }
        catch (ArgumentNullException) { /* expected */ }

        try
        {
            SorophyTime.Compare(tSchemaA, null!);
            throw new InvalidOperationException("Compare must throw ArgumentNullException when right is null.");
        }
        catch (ArgumentNullException) { /* expected */ }
    }
}

internal sealed class KronoStressRandom
{
    private ulong _state;

    public KronoStressRandom(int seed)
    {
        _state = unchecked((ulong)(uint)seed + 0x9E3779B97F4A7C15UL);
        if (_state == 0)
        {
            _state = 0x9E3779B97F4A7C15UL;
        }
    }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public int Next(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        }

        return (int)(NextUInt64() % (ulong)exclusiveMax);
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return minInclusive + Next(maxExclusive - minInclusive);
    }

    public long NextLong(long minimumInclusive, long maximumExclusive)
    {
        if (maximumExclusive <= minimumInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
        }

        var range = unchecked((ulong)(maximumExclusive - minimumInclusive));
        return unchecked((long)(NextUInt64() % range)) + minimumInclusive;
    }

    public bool NextBoolean()
    {
        return (NextUInt64() & 1) == 1;
    }

    public Guid NextGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        for (var index = 0; index < bytes.Length; index += 8)
        {
            var value = NextUInt64();
            for (var offset = 0; offset < 8; offset++)
            {
                bytes[index + offset] = (byte)(value >> (offset * 8));
            }
        }
        return new Guid(bytes);
    }

    public BigInteger NextBigInteger(int digits)
    {
        if (digits <= 0) digits = 1;
        var chars = new char[digits];
        chars[0] = (char)('1' + Next(9));
        for (int i = 1; i < digits; i++)
        {
            chars[i] = (char)('0' + Next(10));
        }
        var val = BigInteger.Parse(new string(chars), CultureInfo.InvariantCulture);
        return NextBoolean() ? -val : val;
    }
}

internal static class KronoTestSchemas
{
    public static SorophyTimeSchema CreateDefaultNumericSchema(string timeline = "KronoNumericTimeline")
    {
        var unit = new SorophyTimeUnit(
            "Tick",
            0,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));
        return new SorophyTimeSchema(timeline, new[] { unit });
    }

    public static SorophyTimeSchema CreateMultiUnitSchema(string timeline = "KronoMultiUnitTimeline")
    {
        var year = new SorophyTimeUnit(
            "Year",
            0,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));
        var day = new SorophyTimeUnit(
            "Day",
            1,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric));
        return new SorophyTimeSchema(timeline, new[] { year, day });
    }

    public static SorophyTimeSchema CreateNonNumericSchema(string timeline = "KronoNonNumericTimeline")
    {
        var named = new SorophyTimeUnit(
            "NamedUnit",
            0,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Named));
        var ordinal = new SorophyTimeUnit(
            "OrdinalUnit",
            1,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Ordinal));
        var pattern = new SorophyTimeUnit(
            "PatternUnit",
            2,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.Pattern, @"^\d{4}$"));
        var freeform = new SorophyTimeUnit(
            "FreeUnit",
            3,
            new SorophyTimePositionDefinition(SorophyTimePositionKind.FreeForm));
        return new SorophyTimeSchema(timeline, new[] { named, ordinal, pattern, freeform });
    }
}
