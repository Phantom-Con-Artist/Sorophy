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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.HistoricalFacts;

/// <summary>
/// Immutable read model representing an emergent historical fact derived from authoritative temporal history.
/// </summary>
public sealed class SorophyHistoricalFact : IEquatable<SorophyHistoricalFact>, IComparable<SorophyHistoricalFact>
{
    /// <summary>
    /// Gets the temporal coordinate at which this historical fact occurred.
    /// </summary>
    public SorophyTime Time { get; }

    /// <summary>
    /// Gets the machine-level semantic classification of this historical fact.
    /// </summary>
    public SorophyHistoricalFactKind Kind { get; }

    /// <summary>
    /// Gets the structural target kind (entity or relationship) of this fact.
    /// </summary>
    public SorophyHistoricalFactTarget TargetKind { get; }

    /// <summary>
    /// Gets the entity ID if this fact applies to an entity; otherwise, <see langword="null"/>.
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Gets the relationship ID if this fact applies to a relationship; otherwise, <see langword="null"/>.
    /// </summary>
    public Guid? RelationshipId { get; }

    /// <summary>
    /// Gets the source entity ID if this fact applies to a relationship; otherwise, <see langword="null"/>.
    /// </summary>
    public Guid? SourceEntityId { get; }

    /// <summary>
    /// Gets the target entity ID if this fact applies to a relationship; otherwise, <see langword="null"/>.
    /// </summary>
    public Guid? TargetEntityId { get; }

    /// <summary>
    /// Gets the semantic type of the relationship if this fact applies to a relationship; otherwise, <see langword="null"/>.
    /// </summary>
    public string? RelationshipType { get; }

    /// <summary>
    /// Gets the sequence number within the entity or relationship history container.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Gets the property name if this fact represents a property change; otherwise, <see langword="null"/>.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the previous value before this mutation; or <see langword="null"/> if property was newly added.
    /// </summary>
    public SorophyValue? PreviousValue { get; }

    /// <summary>
    /// Gets the new value after this mutation; or <see langword="null"/> if property was removed.
    /// </summary>
    public SorophyValue? NewValue { get; }

    /// <summary>
    /// Gets the previous relationship type before this type change; otherwise, <see langword="null"/>.
    /// </summary>
    public string? PreviousType { get; }

    /// <summary>
    /// Gets the new relationship type after this type change; otherwise, <see langword="null"/>.
    /// </summary>
    public string? NewType { get; }

    /// <summary>
    /// Creates an emergent historical fact for an entity lifecycle fact.
    /// </summary>
    public static SorophyHistoricalFact CreateForEntity(
        SorophyTime time,
        SorophyHistoricalFactKind kind,
        Guid entityId,
        long sequence = 0)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity ID cannot be empty.",
                nameof(entityId));
        }

        return new SorophyHistoricalFact(
            time,
            kind,
            SorophyHistoricalFactTarget.Entity,
            entityId,
            relationshipId: null,
            sourceEntityId: null,
            targetEntityId: null,
            relationshipType: null,
            sequence: sequence,
            propertyName: null,
            previousValue: null,
            newValue: null,
            previousType: null,
            newType: null);
    }

    /// <summary>
    /// Creates an emergent historical fact for an entity property change.
    /// </summary>
    public static SorophyHistoricalFact CreateEntityPropertyChange(
        SorophyTime time,
        Guid entityId,
        long sequence,
        string propertyName,
        SorophyValue? previousValue,
        SorophyValue? newValue)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
        }
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
        }

        return new SorophyHistoricalFact(
            time,
            SorophyHistoricalFactKind.PropertyChanged,
            SorophyHistoricalFactTarget.Entity,
            entityId,
            relationshipId: null,
            sourceEntityId: null,
            targetEntityId: null,
            relationshipType: null,
            sequence: sequence,
            propertyName: propertyName,
            previousValue: previousValue,
            newValue: newValue,
            previousType: null,
            newType: null);
    }

    /// <summary>
    /// Creates an emergent historical fact for a relationship lifecycle fact.
    /// </summary>
    public static SorophyHistoricalFact CreateForRelationship(
        SorophyTime time,
        SorophyHistoricalFactKind kind,
        Guid relationshipId,
        Guid sourceEntityId,
        Guid targetEntityId,
        string? relationshipType,
        long sequence = 0)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        if (sourceEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Source entity ID cannot be empty.",
                nameof(sourceEntityId));
        }

        if (targetEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Target entity ID cannot be empty.",
                nameof(targetEntityId));
        }

        return new SorophyHistoricalFact(
            time,
            kind,
            SorophyHistoricalFactTarget.Relationship,
            entityId: null,
            relationshipId,
            sourceEntityId,
            targetEntityId,
            relationshipType,
            sequence: sequence,
            propertyName: null,
            previousValue: null,
            newValue: null,
            previousType: null,
            newType: null);
    }

    /// <summary>
    /// Creates an emergent historical fact for a relationship property change.
    /// </summary>
    public static SorophyHistoricalFact CreateRelationshipPropertyChange(
        SorophyTime time,
        Guid relationshipId,
        Guid sourceEntityId,
        Guid targetEntityId,
        string? relationshipType,
        long sequence,
        string propertyName,
        SorophyValue? previousValue,
        SorophyValue? newValue)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException("Relationship ID cannot be empty.", nameof(relationshipId));
        }
        if (sourceEntityId == Guid.Empty)
        {
            throw new ArgumentException("Source entity ID cannot be empty.", nameof(sourceEntityId));
        }
        if (targetEntityId == Guid.Empty)
        {
            throw new ArgumentException("Target entity ID cannot be empty.", nameof(targetEntityId));
        }
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
        }

        return new SorophyHistoricalFact(
            time,
            SorophyHistoricalFactKind.PropertyChanged,
            SorophyHistoricalFactTarget.Relationship,
            entityId: null,
            relationshipId,
            sourceEntityId,
            targetEntityId,
            relationshipType,
            sequence: sequence,
            propertyName: propertyName,
            previousValue: previousValue,
            newValue: newValue,
            previousType: null,
            newType: null);
    }

    /// <summary>
    /// Creates an emergent historical fact for a relationship type change.
    /// </summary>
    public static SorophyHistoricalFact CreateRelationshipTypeChange(
        SorophyTime time,
        Guid relationshipId,
        Guid sourceEntityId,
        Guid targetEntityId,
        long sequence,
        string? previousType,
        string? newType)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException("Relationship ID cannot be empty.", nameof(relationshipId));
        }
        if (sourceEntityId == Guid.Empty)
        {
            throw new ArgumentException("Source entity ID cannot be empty.", nameof(sourceEntityId));
        }
        if (targetEntityId == Guid.Empty)
        {
            throw new ArgumentException("Target entity ID cannot be empty.", nameof(targetEntityId));
        }

        return new SorophyHistoricalFact(
            time,
            SorophyHistoricalFactKind.RelationshipChanged,
            SorophyHistoricalFactTarget.Relationship,
            entityId: null,
            relationshipId,
            sourceEntityId,
            targetEntityId,
            relationshipType: newType ?? previousType,
            sequence: sequence,
            propertyName: null,
            previousValue: null,
            newValue: null,
            previousType: previousType,
            newType: newType);
    }

    private SorophyHistoricalFact(
        SorophyTime time,
        SorophyHistoricalFactKind kind,
        SorophyHistoricalFactTarget targetKind,
        Guid? entityId,
        Guid? relationshipId,
        Guid? sourceEntityId,
        Guid? targetEntityId,
        string? relationshipType,
        long sequence = 0,
        string? propertyName = null,
        SorophyValue? previousValue = null,
        SorophyValue? newValue = null,
        string? previousType = null,
        string? newType = null)
    {
        Time = time;
        Kind = kind;
        TargetKind = targetKind;
        EntityId = entityId;
        RelationshipId = relationshipId;
        SourceEntityId = sourceEntityId;
        TargetEntityId = targetEntityId;
        RelationshipType = relationshipType;
        Sequence = sequence;
        PropertyName = propertyName;
        PreviousValue = previousValue is not null ? SorophyValueCloner.CloneValue(previousValue) : null;
        NewValue = newValue is not null ? SorophyValueCloner.CloneValue(newValue) : null;
        PreviousType = previousType;
        NewType = newType;
    }

    /// <summary>
    /// Compares this historical fact with another for deterministic ordering.
    /// Order: 1. Time ascending; 2. TargetKind ascending; 3. Target identity Guid ordinal; 4. Sequence ascending; 5. Kind ascending; 6. PropertyName ordinal.
    /// </summary>
    public int CompareTo(SorophyHistoricalFact? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        // 1. Primary: SorophyTime
        if (SorophyTime.CanCompare(Time, other.Time))
        {
            var timeCmp = SorophyTime.Compare(Time, other.Time);
            if (timeCmp != 0)
            {
                return timeCmp;
            }
        }
        else
        {
            var timelineCmp = string.Compare(Time.Timeline, other.Time.Timeline, StringComparison.Ordinal);
            if (timelineCmp != 0)
            {
                return timelineCmp;
            }
        }

        // 2. Secondary: TargetKind (Entity before Relationship)
        var targetCmp = TargetKind.CompareTo(other.TargetKind);
        if (targetCmp != 0)
        {
            return targetCmp;
        }

        // 3. Tertiary: Target Identity Guid ordinal comparison
        var idA = TargetKind == SorophyHistoricalFactTarget.Entity ? EntityId!.Value : RelationshipId!.Value;
        var idB = other.TargetKind == SorophyHistoricalFactTarget.Entity ? other.EntityId!.Value : other.RelationshipId!.Value;
        var idCmp = idA.CompareTo(idB);
        if (idCmp != 0)
        {
            return idCmp;
        }

        // 4. Quaternary: Sequence ascending
        var seqCmp = Sequence.CompareTo(other.Sequence);
        if (seqCmp != 0)
        {
            return seqCmp;
        }

        // 5. Quinary: FactKind
        var kindCmp = Kind.CompareTo(other.Kind);
        if (kindCmp != 0)
        {
            return kindCmp;
        }

        // 6. Senary: PropertyName ordinal
        return string.Compare(PropertyName, other.PropertyName, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool Equals(SorophyHistoricalFact? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Time.Equals(other.Time) &&
               Kind == other.Kind &&
               TargetKind == other.TargetKind &&
               EntityId == other.EntityId &&
               RelationshipId == other.RelationshipId &&
               SourceEntityId == other.SourceEntityId &&
               TargetEntityId == other.TargetEntityId &&
               string.Equals(RelationshipType, other.RelationshipType, StringComparison.Ordinal) &&
               Sequence == other.Sequence &&
               string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal) &&
               SorophyStructuralEquality.ValueEquals(PreviousValue, other.PreviousValue) &&
               SorophyStructuralEquality.ValueEquals(NewValue, other.NewValue) &&
               string.Equals(PreviousType, other.PreviousType, StringComparison.Ordinal) &&
               string.Equals(NewType, other.NewType, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SorophyHistoricalFact);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Time);
        hash.Add(Kind);
        hash.Add(TargetKind);
        hash.Add(EntityId);
        hash.Add(RelationshipId);
        hash.Add(SourceEntityId);
        hash.Add(TargetEntityId);
        hash.Add(RelationshipType, StringComparer.Ordinal);
        hash.Add(Sequence);
        hash.Add(PropertyName, StringComparer.Ordinal);
        hash.Add(PreviousType, StringComparer.Ordinal);
        hash.Add(NewType, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}

