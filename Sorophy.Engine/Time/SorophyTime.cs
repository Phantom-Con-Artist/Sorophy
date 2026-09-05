using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Sorophy.Engine.Time;

/// <summary>
/// Represents a temporal point within a defined temporal schema.
/// </summary>
/// <remarks>
/// An <see cref="SorophyTime"/> identifies a position within a timeline using
/// a specific temporal unit and precision.
///
/// The temporal schema defines the rules that give the position and unit
/// their meaning.
///
/// SorophyTime itself represents the temporal coordinate; it does not define
/// the calendar or timeline rules.
/// </remarks>
public sealed class SorophyTime : IEquatable<SorophyTime>
{
    /// <summary>
    /// Gets the temporal schema this point belongs to.
    /// </summary>
    public SorophyTimeSchema Schema { get; }

    /// <summary>
    /// Gets the name of the timeline this temporal point belongs to.
    /// </summary>
    public string Timeline => Schema.Timeline;

    /// <summary>
    /// Gets the position of this temporal point within its timeline.
    /// </summary>
    public string Position { get; }

    /// <summary>
    /// Gets the temporal unit associated with the position.
    /// </summary>
    public SorophyTimeUnit Unit { get; }

    /// <summary>
    /// Gets the precision with which this temporal point is known.
    /// </summary>
    public SorophyTimePrecision Precision { get; }

    /// <summary>
    /// Creates a new temporal point.
    /// </summary>
    /// <param name="schema">
    /// The temporal schema governing this point.
    /// </param>
    /// <param name="position">
    /// The position within the timeline.
    /// </param>
    /// <param name="unitName">
    /// The name of the temporal unit associated with the position.
    /// </param>
    /// <param name="precision">
    /// The precision of the temporal point.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="schema"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the position is null, empty, or whitespace, when the
    /// specified unit does not exist in the schema, or when the position does
    /// not satisfy the structural rules defined by the unit.
    /// </exception>
    public SorophyTime(
        SorophyTimeSchema schema,
        string position,
        string unitName,
        SorophyTimePrecision precision)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (string.IsNullOrWhiteSpace(position))
        {
            throw new ArgumentException(
                "A temporal position cannot be null, empty, or whitespace.",
                nameof(position));
        }

        if (string.IsNullOrWhiteSpace(unitName))
        {
            throw new ArgumentException(
                "A temporal unit name cannot be null, empty, or whitespace.",
                nameof(unitName));
        }

        var unit = schema.GetUnit(unitName);

        if (unit is null)
        {
            throw new ArgumentException(
                $"The temporal unit '{unitName}' does not exist in timeline '{schema.Timeline}'.",
                nameof(unitName));
        }

        SorophyTimeValidator.ValidatePosition(
            position,
            unit.PositionDefinition);

        Schema = schema;
        Position = position;
        Unit = unit;
        Precision = precision;
    }

    /// <summary>
    /// Determines whether this temporal point is structurally equivalent
    /// to another temporal point.
    /// </summary>
    /// <param name="other">The temporal point to compare against.</param>
    /// <returns>
    /// <see langword="true"/> when both temporal points belong to equivalent
    /// schemas and have the same position, unit, and precision; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(SorophyTime? other)
    {
        if (other is null)
        {
            return false;
        }

        return Schema == other.Schema &&
               string.Equals(
                   Position,
                   other.Position,
                   StringComparison.Ordinal) &&
               Unit == other.Unit &&
               Precision == other.Precision;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SorophyTime other &&
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Schema,
            Position,
            Unit,
            Precision);
    }

    /// <summary>
    /// Determines whether two temporal points are structurally equivalent.
    /// </summary>
    public static bool operator ==(
        SorophyTime? left,
        SorophyTime? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two temporal points are structurally different.
    /// </summary>
    public static bool operator !=(
        SorophyTime? left,
        SorophyTime? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Timeline}: {Position} {Unit.Name} ({Precision})";
    }

    /*
     * =============================================================
     * TEMPORAL COMPARISON
     * =============================================================
     */

    /// <summary>
    /// Determines whether two temporal points can be deterministically compared
    /// under the existing temporal model without undefined unit conversion.
    /// </summary>
    /// <param name="left">The first temporal point.</param>
    /// <param name="right">The second temporal point.</param>
    /// <returns>
    /// <see langword="true"/> when both points belong to the same schema, share the
    /// same temporal unit, and use a numeric position definition; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool CanCompare(
        SorophyTime? left,
        SorophyTime? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        if (!Equals(left.Schema, right.Schema))
        {
            return false;
        }

        if (left.Unit != right.Unit)
        {
            return false;
        }

        if (left.Unit.PositionDefinition.Kind != SorophyTimePositionKind.Numeric)
        {
            return false;
        }

        return BigInteger.TryParse(
                   left.Position,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out _) &&
               BigInteger.TryParse(
                   right.Position,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out _);
    }

    /// <summary>
    /// Deterministically compares two temporal coordinates belonging to the same
    /// schema and unit.
    /// </summary>
    /// <param name="left">The first temporal point.</param>
    /// <param name="right">The second temporal point.</param>
    /// <returns>
    /// A signed integer indicating relative order: less than zero if <paramref name="left"/>
    /// precedes <paramref name="right"/>; zero if they represent the same coordinate;
    /// greater than zero if <paramref name="left"/> succeeds <paramref name="right"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="left"/> or <paramref name="right"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the points belong to different schemas or different units.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the points use a non-numeric position definition or cannot be parsed.
    /// </exception>
    public static int Compare(
        SorophyTime left,
        SorophyTime right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!Equals(left.Schema, right.Schema))
        {
            throw new ArgumentException(
                $"Cannot compare temporal coordinates from different schemas: '{left.Timeline}' and '{right.Timeline}'.",
                nameof(right));
        }

        if (left.Unit != right.Unit)
        {
            throw new ArgumentException(
                $"Cannot compare temporal coordinates with different units ('{left.Unit.Name}' and '{right.Unit.Name}') without schema-defined unit conversion.",
                nameof(right));
        }

        if (left.Unit.PositionDefinition.Kind != SorophyTimePositionKind.Numeric)
        {
            throw new InvalidOperationException(
                $"Temporal ordering is supported only for numeric position definitions; found '{left.Unit.PositionDefinition.Kind}'.");
        }

        if (!BigInteger.TryParse(
                left.Position,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var leftVal))
        {
            throw new InvalidOperationException(
                $"Temporal position '{left.Position}' could not be parsed as a numeric coordinate.");
        }

        if (!BigInteger.TryParse(
                right.Position,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var rightVal))
        {
            throw new InvalidOperationException(
                $"Temporal position '{right.Position}' could not be parsed as a numeric coordinate.");
        }

        return leftVal.CompareTo(rightVal);
    }

    /// <summary>
    /// Compares this temporal coordinate to another temporal coordinate.
    /// </summary>
    public int CompareTo(
        SorophyTime other)
    {
        return Compare(this, other);
    }

    /// <summary>
    /// Determines whether this temporal coordinate precedes another coordinate.
    /// </summary>
    public bool IsBefore(
        SorophyTime other)
    {
        return Compare(this, other) < 0;
    }

    /// <summary>
    /// Determines whether this temporal coordinate succeeds another coordinate.
    /// </summary>
    public bool IsAfter(
        SorophyTime other)
    {
        return Compare(this, other) > 0;
    }

    /// <summary>
    /// Determines whether this temporal coordinate is equal to or precedes another coordinate.
    /// </summary>
    public bool IsAtOrBefore(
        SorophyTime other)
    {
        return Compare(this, other) <= 0;
    }

    /// <summary>
    /// Determines whether this temporal coordinate is equal to or succeeds another coordinate.
    /// </summary>
    public bool IsAtOrAfter(
        SorophyTime other)
    {
        return Compare(this, other) >= 0;
    }
}

/// <summary>
/// Provides comparison for compatible <see cref="SorophyTime"/> instances.
/// </summary>
public sealed class SorophyTimeComparer : IComparer<SorophyTime>
{
    /// <summary>
    /// Gets the singleton comparer instance.
    /// </summary>
    public static SorophyTimeComparer Instance { get; } = new();

    /// <inheritdoc />
    public int Compare(
        SorophyTime? x,
        SorophyTime? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return SorophyTime.Compare(x, y);
    }
}