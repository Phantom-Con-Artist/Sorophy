using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Sorophy.Engine.Time;

/// <summary>
/// Provides validation operations for the Sorophyis temporal model.
/// </summary>
/// <remarks>
/// SorophyTimeValidator is intentionally stateless. It validates temporal values
/// against the structural rules already defined by SorophyTimePositionDefinition,
/// SorophyTimeUnit, and SorophyTimeSchema.
///
/// It does not define calendar semantics and does not mutate temporal objects.
/// </remarks>
public static class SorophyTimeValidator
{
    private static readonly TimeSpan PatternTimeout =
        TimeSpan.FromSeconds(1);

    /// <summary>
    /// Determines whether a position is structurally valid according to the
    /// supplied position definition.
    /// </summary>
    /// <param name="position">The temporal position to validate.</param>
    /// <param name="definition">
    /// The definition describing the position's allowed structure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the position is structurally valid;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValidPosition(
        string? position,
        SorophyTimePositionDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(position) ||
            definition is null)
        {
            return false;
        }

        return definition.Kind switch
        {
            SorophyTimePositionKind.Numeric =>
                IsValidNumericPosition(position),

            SorophyTimePositionKind.Ordinal =>
                IsValidOrdinalPosition(position),

            SorophyTimePositionKind.Named =>
                IsValidNamedPosition(position),

            SorophyTimePositionKind.Pattern =>
                IsValidPatternPosition(
                    position,
                    definition.Pattern),

            SorophyTimePositionKind.FreeForm =>
                IsValidFreeFormPosition(position),

            _ => false
        };
    }

    /// <summary>
    /// Validates a position according to the supplied position definition.
    /// </summary>
    /// <param name="position">The temporal position to validate.</param>
    /// <param name="definition">
    /// The definition describing the position's allowed structure.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the position is invalid.
    /// </exception>
    public static void ValidatePosition(
        string? position,
        SorophyTimePositionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(position))
        {
            throw new ArgumentException(
                "A temporal position cannot be null, empty, or whitespace.",
                nameof(position));
        }

        if (IsValidPosition(position, definition))
        {
            return;
        }

        throw new ArgumentException(
            $"The temporal position '{position}' is invalid for " +
            $"position kind '{definition.Kind}'.",
            nameof(position));
    }

    /// <summary>
    /// Determines whether a SorophyTime instance is structurally valid according
    /// to its schema and position definition.
    /// </summary>
    /// <param name="time">The temporal point to validate.</param>
    /// <returns>
    /// <see langword="true"/> when the temporal point is valid; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsValid(
        SorophyTime? time)
    {
        if (time is null)
        {
            return false;
        }

        var schemaUnit =
            time.Schema.GetUnit(time.Unit.Name);

        if (schemaUnit is null)
        {
            return false;
        }

        if (schemaUnit != time.Unit)
        {
            /*
             * The unit must belong to the schema that governs the SorophyTime.
             */
            return false;
        }

        return IsValidPosition(
            time.Position,
            time.Unit.PositionDefinition);
    }

    /// <summary>
    /// Validates a SorophyTime instance against its schema and position rules.
    /// </summary>
    /// <param name="time">The temporal point to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="time"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the temporal point is structurally invalid.
    /// </exception>
    public static void Validate(
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var schemaUnit =
            time.Schema.GetUnit(time.Unit.Name);

        if (schemaUnit is null)
        {
            throw new InvalidOperationException(
                $"The temporal unit '{time.Unit.Name}' does not exist " +
                $"in timeline '{time.Schema.Timeline}'.");
        }

        if (schemaUnit != time.Unit)
        {
            throw new InvalidOperationException(
                $"The temporal unit '{time.Unit.Name}' does not belong " +
                "to the supplied temporal schema.");
        }

        try
        {
            ValidatePosition(
                time.Position,
                time.Unit.PositionDefinition);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The SorophyTime position '{time.Position}' is invalid " +
                $"for unit '{time.Unit.Name}'.",
                exception);
        }
    }

    /// <summary>
    /// Determines whether a position can be interpreted as an integer.
    /// </summary>
    private static bool IsValidNumericPosition(
        string position)
    {
        return BigInteger.TryParse(
            position,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out _);
    }

    /// <summary>
    /// Determines whether an ordinal position is structurally usable.
    /// </summary>
    /// <remarks>
    /// Sorophy.Engine deliberately does not hard-code a vocabulary of ordinal
    /// names. A temporal schema may later define its own ordinal vocabulary.
    ///
    /// At this layer, ordinal positions are required only to be non-empty.
    /// This preserves support for values such as:
    ///
    /// First
    /// Second
    /// Third
    /// I
    /// II
    /// III
    /// Tertius
    /// Bloodfall
    ///
    /// More restrictive ordinal vocabularies belong to schema-level rules.
    /// </remarks>
    private static bool IsValidOrdinalPosition(
        string position)
    {
        return !string.IsNullOrWhiteSpace(position);
    }

    /// <summary>
    /// Determines whether a named position is structurally valid.
    /// </summary>
    private static bool IsValidNamedPosition(
        string position)
    {
        return !string.IsNullOrWhiteSpace(position);
    }

    /// <summary>
    /// Determines whether a free-form position is structurally valid.
    /// </summary>
    private static bool IsValidFreeFormPosition(
        string position)
    {
        return !string.IsNullOrWhiteSpace(position);
    }

    /// <summary>
    /// Determines whether a position matches its user-defined pattern.
    /// </summary>
    private static bool IsValidPatternPosition(
        string position,
        string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(
                position,
                pattern,
                RegexOptions.CultureInvariant,
                PatternTimeout);
        }
        catch (ArgumentException)
        {
            /*
             * Invalid regular-expression definitions are treated as invalid
             * position rules rather than allowing malformed schemas to pass.
             */
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            /*
             * A user-defined pattern must never be able to stall validation.
             */
            return false;
        }
    }
}