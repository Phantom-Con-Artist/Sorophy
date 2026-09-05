namespace Sorophy.Engine.Time;

/// <summary>
/// Defines the structural rules for positions belonging to a temporal unit.
/// </summary>
/// <remarks>
/// A position definition describes the shape of a temporal position, not its
/// world-specific meaning. The temporal schema is responsible for determining
/// whether a position is valid within a particular timeline.
/// </remarks>
public sealed class SorophyTimePositionDefinition
{
    /// <summary>
    /// Gets the structural kind of position this definition accepts.
    /// </summary>
    public SorophyTimePositionKind Kind { get; }

    /// <summary>
    /// Gets the optional pattern used when <see cref="Kind"/> is
    /// <see cref="SorophyTimePositionKind.Pattern"/>.
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    /// Creates a new temporal position definition.
    /// </summary>
    /// <param name="kind">The structural kind of position.</param>
    /// <param name="pattern">
    /// An optional pattern used by pattern-based positions.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when a pattern is required but missing, or when a pattern is
    /// supplied for a non-pattern position kind.
    /// </exception>
    public SorophyTimePositionDefinition(
        SorophyTimePositionKind kind,
        string? pattern = null)
    {
        if (kind == SorophyTimePositionKind.Pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException(
                    "A pattern is required when the position kind is Pattern.",
                    nameof(pattern));
            }

            Pattern = pattern;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException(
                    "A pattern can only be specified when the position kind is Pattern.",
                    nameof(pattern));
            }

            Pattern = null;
        }

        Kind = kind;
    }

    /// <summary>
    /// Determines whether this definition is structurally equivalent to
    /// another position definition.
    /// </summary>
    public bool Equals(SorophyTimePositionDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind &&
               string.Equals(Pattern, other.Pattern, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SorophyTimePositionDefinition other &&
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, Pattern);
    }

    /// <summary>
    /// Determines whether two position definitions are structurally equivalent.
    /// </summary>
    public static bool operator ==(
        SorophyTimePositionDefinition? left,
        SorophyTimePositionDefinition? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two position definitions are structurally different.
    /// </summary>
    public static bool operator !=(
        SorophyTimePositionDefinition? left,
        SorophyTimePositionDefinition? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Defines the structural representation of a temporal position.
/// </summary>
public enum SorophyTimePositionKind
{
    /// <summary>
    /// A numeric position such as "110" or "42".
    /// </summary>
    Numeric,

    /// <summary>
    /// An ordinal position such as "First", "Second", or "Third".
    /// </summary>
    Ordinal,

    /// <summary>
    /// A named position such as "Age of Ash" or "Bloodfall".
    /// </summary>
    Named,

    /// <summary>
    /// A position validated against a user-defined pattern.
    /// </summary>
    Pattern,

    /// <summary>
    /// A free-form position whose structure is intentionally unrestricted.
    /// </summary>
    FreeForm
}