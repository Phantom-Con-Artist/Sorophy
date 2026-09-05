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
}