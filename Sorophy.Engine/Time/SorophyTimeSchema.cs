namespace Sorophy.Engine.Time;

/// <summary>
/// Defines the temporal rules for a single timeline.
/// </summary>
/// <remarks>
/// An <see cref="SorophyTimeSchema"/> establishes the temporal contract for a
/// timeline by defining its available units and their hierarchical order.
///
/// For example:
///
/// Era → Age → Year → Season → Day
///
/// The schema is responsible for validating the structure of the temporal
/// system. It does not represent an individual point in time; that is the
/// responsibility of <see cref="SorophyTime"/>.
/// </remarks>
public sealed class SorophyTimeSchema : IEquatable<SorophyTimeSchema>
{
    private readonly IReadOnlyList<SorophyTimeUnit> _units;
    private readonly Dictionary<string, SorophyTimeUnit> _unitsByName;

    /// <summary>
    /// Gets the name of the timeline represented by this schema.
    /// </summary>
    public string Timeline { get; }

    /// <summary>
    /// Gets the temporal units defined by this schema in hierarchical order.
    /// </summary>
    public IReadOnlyList<SorophyTimeUnit> Units => _units;

    /// <summary>
    /// Gets the number of temporal units defined by this schema.
    /// </summary>
    public int UnitCount => _units.Count;

    /// <summary>
    /// Creates a new temporal schema.
    /// </summary>
    /// <param name="timeline">The name of the timeline.</param>
    /// <param name="units">The temporal units belonging to the timeline.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the timeline is null, empty, or whitespace, when no units
    /// are supplied, or when duplicate unit names or orders are detected.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="units"/> is null or contains a null unit.
    /// </exception>
    public SorophyTimeSchema(
        string timeline,
        IEnumerable<SorophyTimeUnit> units)
    {
        if (string.IsNullOrWhiteSpace(timeline))
        {
            throw new ArgumentException(
                "A timeline name cannot be null, empty, or whitespace.",
                nameof(timeline));
        }

        ArgumentNullException.ThrowIfNull(units);

        var materializedUnits = units.ToList();

        if (materializedUnits.Count == 0)
        {
            throw new ArgumentException(
                "A temporal schema must contain at least one unit.",
                nameof(units));
        }

        var unitsByName = new Dictionary<string, SorophyTimeUnit>(
            StringComparer.Ordinal);

        var orders = new HashSet<int>();

        foreach (var unit in materializedUnits)
        {
            ArgumentNullException.ThrowIfNull(unit, nameof(units));

            if (!unitsByName.TryAdd(unit.Name, unit))
            {
                throw new ArgumentException(
                    $"The temporal schema contains duplicate unit name '{unit.Name}'.",
                    nameof(units));
            }

            if (!orders.Add(unit.Order))
            {
                throw new ArgumentException(
                    $"The temporal schema contains duplicate unit order '{unit.Order}'.",
                    nameof(units));
            }
        }

        materializedUnits.Sort(
            static (left, right) => left.Order.CompareTo(right.Order));

        Timeline = timeline;
        _units = materializedUnits.AsReadOnly();
        _unitsByName = unitsByName;
    }

    /// <summary>
    /// Determines whether the schema contains a temporal unit with the
    /// specified name.
    /// </summary>
    /// <param name="unitName">The unit name to search for.</param>
    /// <returns>
    /// <see langword="true"/> when the unit exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool ContainsUnit(string unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
        {
            return false;
        }

        return _unitsByName.ContainsKey(unitName);
    }

    /// <summary>
    /// Gets a temporal unit by name.
    /// </summary>
    /// <param name="unitName">The unit name to search for.</param>
    /// <returns>The matching temporal unit, or <see langword="null"/>.</returns>
    public SorophyTimeUnit? GetUnit(string unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
        {
            return null;
        }

        return _unitsByName.TryGetValue(unitName, out var unit)
            ? unit
            : null;
    }

    /// <summary>
    /// Gets a temporal unit by its hierarchical order.
    /// </summary>
    /// <param name="order">The hierarchical order to search for.</param>
    /// <returns>The matching temporal unit, or <see langword="null"/>.</returns>
    public SorophyTimeUnit? GetUnitByOrder(int order)
    {
        foreach (var unit in _units)
        {
            if (unit.Order == order)
            {
                return unit;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a temporal unit is larger than another unit
    /// according to this schema.
    /// </summary>
    /// <param name="firstUnitName">The first unit name.</param>
    /// <param name="secondUnitName">The second unit name.</param>
    /// <returns>
    /// <see langword="true"/> when the first unit has a lower order than the
    /// second unit.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when either unit does not exist in the schema.
    /// </exception>
    public bool IsLargerUnit(
        string firstUnitName,
        string secondUnitName)
    {
        var firstUnit = GetRequiredUnit(firstUnitName);
        var secondUnit = GetRequiredUnit(secondUnitName);

        return firstUnit.Order < secondUnit.Order;
    }

    /// <summary>
    /// Determines whether a temporal unit is smaller than another unit
    /// according to this schema.
    /// </summary>
    /// <param name="firstUnitName">The first unit name.</param>
    /// <param name="secondUnitName">The second unit name.</param>
    /// <returns>
    /// <see langword="true"/> when the first unit has a greater order than the
    /// second unit.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when either unit does not exist in the schema.
    /// </exception>
    public bool IsSmallerUnit(
        string firstUnitName,
        string secondUnitName)
    {
        var firstUnit = GetRequiredUnit(firstUnitName);
        var secondUnit = GetRequiredUnit(secondUnitName);

        return firstUnit.Order > secondUnit.Order;
    }

    /// <summary>
    /// Gets the unit and throws when it is not part of this schema.
    /// </summary>
    private SorophyTimeUnit GetRequiredUnit(string unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
        {
            throw new ArgumentException(
                "A unit name cannot be null, empty, or whitespace.",
                nameof(unitName));
        }

        if (!_unitsByName.TryGetValue(unitName, out var unit))
        {
            throw new ArgumentException(
                $"The temporal unit '{unitName}' does not exist in timeline '{Timeline}'.",
                nameof(unitName));
        }

        return unit;
    }

    /// <summary>
    /// Determines whether this schema is structurally equivalent to another
    /// temporal schema.
    /// </summary>
    /// <param name="other">The schema to compare against.</param>
    /// <returns>
    /// <see langword="true"/> when the timelines and all units are equivalent;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(SorophyTimeSchema? other)
    {
        if (other is null)
        {
            return false;
        }

        if (!string.Equals(
                Timeline,
                other.Timeline,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (_units.Count != other._units.Count)
        {
            return false;
        }

        for (var i = 0; i < _units.Count; i++)
        {
            if (_units[i] != other._units[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SorophyTimeSchema other &&
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Timeline, StringComparer.Ordinal);

        foreach (var unit in _units)
        {
            hash.Add(unit);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two temporal schemas are structurally equivalent.
    /// </summary>
    public static bool operator ==(
        SorophyTimeSchema? left,
        SorophyTimeSchema? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two temporal schemas are structurally different.
    /// </summary>
    public static bool operator !=(
        SorophyTimeSchema? left,
        SorophyTimeSchema? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Timeline;
    }
}