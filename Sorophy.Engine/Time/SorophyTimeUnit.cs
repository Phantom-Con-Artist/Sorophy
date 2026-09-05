namespace Sorophy.Engine.Time;

/// <summary>
/// Defines a temporal unit within an <see cref="SorophyTimeSchema"/>.
/// </summary>
/// <remarks>
/// An <see cref="SorophyTimeUnit"/> establishes the semantic identity, hierarchy,
/// and position rules of a unit of time.
///
/// The <see cref="Order"/> determines the unit's position in the temporal
/// hierarchy. Lower values represent larger temporal units.
///
/// For example:
/// Era (0) → Age (1) → Year (2) → Season (3) → Day (4)
///
/// The unit itself does not define how many smaller units it contains.
/// That relationship belongs to the temporal schema.
/// </remarks>
public sealed class SorophyTimeUnit : IEquatable<SorophyTimeUnit>
{
    /// <summary>
    /// Gets the name of the temporal unit.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the hierarchical order of this unit.
    /// </summary>
    /// <remarks>
    /// Lower values represent larger temporal units.
    /// Two units within the same schema must not share the same order.
    /// </remarks>
    public int Order { get; }

    /// <summary>
    /// Gets the definition describing valid positions for this unit.
    /// </summary>
    public SorophyTimePositionDefinition PositionDefinition { get; }

    /// <summary>
    /// Creates a new temporal unit.
    /// </summary>
    /// <param name="name">The name of the temporal unit.</param>
    /// <param name="order">
    /// The hierarchical order of the unit. Lower values represent larger
    /// temporal units.
    /// </param>
    /// <param name="positionDefinition">
    /// The definition describing the structural form of positions accepted
    /// by this unit.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="positionDefinition"/> is null.
    /// </exception>
    public SorophyTimeUnit(
        string name,
        int order,
        SorophyTimePositionDefinition positionDefinition)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A temporal unit name cannot be null, empty, or whitespace.",
                nameof(name));
        }

        ArgumentNullException.ThrowIfNull(positionDefinition);

        Name = name;
        Order = order;
        PositionDefinition = positionDefinition;
    }

    /// <summary>
    /// Determines whether this temporal unit is structurally equivalent to
    /// another temporal unit.
    /// </summary>
    /// <param name="other">The temporal unit to compare against.</param>
    /// <returns>
    /// <see langword="true"/> when both units have the same name, order, and
    /// position definition; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(SorophyTimeUnit? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Order == other.Order &&
               PositionDefinition == other.PositionDefinition;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SorophyTimeUnit other &&
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            Order,
            PositionDefinition);
    }

    /// <summary>
    /// Determines whether two temporal units are structurally equivalent.
    /// </summary>
    public static bool operator ==(
        SorophyTimeUnit? left,
        SorophyTimeUnit? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two temporal units are structurally different.
    /// </summary>
    public static bool operator !=(
        SorophyTimeUnit? left,
        SorophyTimeUnit? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}