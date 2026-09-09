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
using Sorophy.Engine.Diff;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Represents an authored temporal lifecycle or mutation fact for an entity within a Sorophy graph.
/// </summary>
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
    /// Gets the lifecycle transition or mutation kind represented by this fact.
    /// </summary>
    public SorophyEntityFactKind Kind { get; }

    /// <summary>
    /// Gets the authored sequence order within this entity's history.
    /// </summary>
    public long Sequence { get; internal set; }

    /// <summary>
    /// Gets the name of the property that changed, if this fact represents a property mutation.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the previous value of the property prior to this mutation, if applicable.
    /// </summary>
    public SorophyValue? PreviousValue { get; internal set; }

    /// <summary>
    /// Gets the new value of the property established by this mutation, if applicable.
    /// </summary>
    public SorophyValue? NewValue { get; }

    /// <summary>
    /// Gets an optional authored description or rationale for this transition.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new temporal entity fact.
    /// </summary>
    public SorophyEntityFact(
        SorophyTime at,
        Guid entityId,
        SorophyEntityFactKind kind,
        string? description = null,
        long sequence = 0,
        string? propertyName = null,
        SorophyValue? previousValue = null,
        SorophyValue? newValue = null)
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
        Sequence = sequence;
        PropertyName = propertyName;
        PreviousValue = previousValue is not null ? SorophyValueCloner.CloneValue(previousValue) : null;
        NewValue = newValue is not null ? SorophyValueCloner.CloneValue(newValue) : null;
    }

    /// <summary>
    /// Factory method to create a new temporal entity lifecycle fact.
    /// </summary>
    public static SorophyEntityFact Create(
        SorophyTime at,
        Guid entityId,
        SorophyEntityFactKind kind,
        string? description = null,
        long sequence = 0) =>
        new(at, entityId, kind, description, sequence);

    /// <summary>
    /// Factory method to create a new temporal entity property change fact.
    /// </summary>
    public static SorophyEntityFact CreatePropertyChange(
        SorophyTime at,
        Guid entityId,
        string propertyName,
        SorophyValue? previousValue,
        SorophyValue? newValue,
        long sequence = 0,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException(
                "Property name cannot be null, empty, or whitespace.",
                nameof(propertyName));
        }

        return new SorophyEntityFact(
            at,
            entityId,
            SorophyEntityFactKind.PropertyChanged,
            description,
            sequence,
            propertyName,
            previousValue,
            newValue);
    }

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
               Sequence == other.Sequence &&
               string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal) &&
               SorophyStructuralEquality.ValueEquals(PreviousValue, other.PreviousValue) &&
               SorophyStructuralEquality.ValueEquals(NewValue, other.NewValue) &&
               string.Equals(Description, other.Description, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is SorophyEntityFact other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EntityId);
        hash.Add(Kind);
        hash.Add(At);
        hash.Add(Sequence);
        hash.Add(PropertyName, StringComparer.Ordinal);
        hash.Add(Description, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() =>
        Kind == SorophyEntityFactKind.PropertyChanged
            ? $"{EntityId} PropertyChanged '{PropertyName}' @ {At} (Seq={Sequence})"
            : $"{EntityId} {Kind} @ {At} (Seq={Sequence})";
}
