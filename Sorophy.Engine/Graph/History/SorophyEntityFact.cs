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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Represents an authored temporal lifecycle fact for an entity within a Sorophy graph.
/// </summary>
/// <remarks>
/// An entity fact records an immutable temporal transition (such as creation or retirement)
/// at a specific <see cref="SorophyTime"/> coordinate. It does not clone full property
/// snapshots, keeping temporal history lightweight, deterministic, and accurate across
/// arbitrary-time edits.
/// </remarks>
public sealed class SorophyEntityFact : IEquatable<SorophyEntityFact>
{
    /// <summary>
    /// Gets the temporal coordinate at which this fact took effect.
    /// </summary>
    public SorophyTime At { get; }

    /// <summary>
    /// Gets the identity of the entity this fact applies to.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the lifecycle transition kind represented by this fact.
    /// </summary>
    public SorophyEntityFactKind Kind { get; }

    /// <summary>
    /// Gets an optional authored description or rationale for this transition.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new temporal entity fact.
    /// </summary>
    /// <param name="at">The temporal coordinate at which this fact takes effect.</param>
    /// <param name="entityId">The identity of the entity this fact describes.</param>
    /// <param name="kind">The lifecycle transition kind.</param>
    /// <param name="description">Optional description or authored context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="at"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/> is empty or <paramref name="kind"/> is undefined.</exception>
    public SorophyEntityFact(
        SorophyTime at,
        Guid entityId,
        SorophyEntityFactKind kind,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(at);

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity ID cannot be empty.",
                nameof(entityId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException(
                $"Undefined entity fact kind '{kind}'.",
                nameof(kind));
        }

        At = at;
        EntityId = entityId;
        Kind = kind;
        Description = description;
    }

    /// <summary>
    /// Factory method to create a new temporal entity fact.
    /// </summary>
    public static SorophyEntityFact Create(
        SorophyTime at,
        Guid entityId,
        SorophyEntityFactKind kind,
        string? description = null) =>
        new(at, entityId, kind, description);

    /// <inheritdoc />
    public bool Equals(SorophyEntityFact? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EntityId == other.EntityId &&
               Kind == other.Kind &&
               At.Equals(other.At) &&
               string.Equals(Description, other.Description, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is SorophyEntityFact other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(EntityId, Kind, At, Description);

    /// <inheritdoc />
    public override string ToString() =>
        $"{EntityId} {Kind} @ {At}";
}
