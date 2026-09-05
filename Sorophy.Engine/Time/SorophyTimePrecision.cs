namespace Sorophy.Engine.Time;

/// <summary>
/// Defines how precisely a temporal point is known.
/// </summary>
public enum SorophyTimePrecision
{
    /// <summary>
    /// The temporal point is known precisely according to its temporal schema.
    /// </summary>
    Exact,

    /// <summary>
    /// The temporal point is an approximation rather than a precisely known
    /// temporal coordinate.
    /// </summary>
    Approximate
}