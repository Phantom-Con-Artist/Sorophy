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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * CANON CHECK INSPECTION
     * =============================================================
     */

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateRelationship(
        SorophyRelationship relationship,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateRelationshipCreation(
            this,
            relationship,
            creationTime);
    }

    /// <summary>
    /// Validates whether a relationship can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateRelationship(
        Guid relationshipId,
        Guid sourceId,
        Guid targetId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateRelationshipCreation(
            this,
            relationshipId,
            sourceId,
            targetId,
            creationTime);
    }

    /// <summary>
    /// Validates whether a relationship can be canonically retired at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanRetireRelationship(
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRelationshipRetirement(
            this,
            relationshipId,
            retirementTime);
    }

    /// <summary>
    /// Validates whether an established retirement fact for a relationship can be canonically reverted.
    /// </summary>
    public SorophyCanonResult CanRevertRelationshipRetirement(
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return CanonCheck.CanRevertRelationshipRetirement(
            relationshipId,
            retirementTime);
    }

    /// <summary>
    /// Validates whether the semantic type of a relationship can be changed at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanChangeRelationshipType(
        Guid relationshipId,
        string newType,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        return CanonCheck.CanChangeRelationshipType(
            relationshipId,
            newType,
            time);
    }

    /// <summary>
    /// Validates whether a property can be mutated on a relationship at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanSetRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyValue value,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(time);

        return CanonCheck.CanSetRelationshipProperty(
            relationshipId,
            propertyName,
            value,
            time);
    }

    /// <summary>
    /// Validates whether a property can be removed from a relationship at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanRemoveRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        return CanonCheck.CanRemoveRelationshipProperty(
            relationshipId,
            propertyName,
            time);
    }

    /* =============================================================
     * RELATIONSHIP LIFECYCLE & MUTATION OPERATIONS
     * =============================================================
     */

    /// <summary>
    /// Restores a relationship directly into canonical storage and the adjacency index without authoring a lifecycle fact.
    /// Used by deserialization and unversioned baseline insertion.
    /// </summary>
    internal void RestoreRelationship(
        SorophyRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (relationship.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationship));
        }

        if (IsRelationshipIdRetired(relationship.Id))
        {
            throw new InvalidOperationException(
                $"Relationship ID '{relationship.Id}' has been retired and cannot be reused.");
        }

        if (!_entities.ContainsKey(relationship.SourceId))
        {
            throw new InvalidOperationException(
                $"Source entity '{relationship.SourceId}' does not exist.");
        }

        if (!_entities.ContainsKey(relationship.TargetId))
        {
            throw new InvalidOperationException(
                $"Target entity '{relationship.TargetId}' does not exist.");
        }

        var outgoingNode = _adjacencyPool.Allocate(relationship.Id);
        var incomingNode = _adjacencyPool.Allocate(relationship.Id);

        var relationshipAdded = false;
        var indexAdded = false;
        var outgoingLinked = false;
        var incomingLinked = false;

        try
        {
            if (!_relationships.TryAdd(relationship.Id, relationship))
            {
                throw new InvalidOperationException(
                    $"A relationship with ID '{relationship.Id}' already exists.");
            }

            relationshipAdded = true;

            _relationshipIndex.Add(
                relationship.Id,
                new RelationshipIndex(
                    outgoingNode,
                    incomingNode,
                    _nextRelationshipSequence));

            indexAdded = true;

            LinkOutgoing(
                relationship.SourceId,
                outgoingNode);

            outgoingLinked = true;

            LinkIncoming(
                relationship.TargetId,
                incomingNode);

            incomingLinked = true;

            _nextRelationshipSequence++;
        }
        catch
        {
            if (incomingLinked)
            {
                UnlinkIncoming(relationship.TargetId, incomingNode);
            }

            if (outgoingLinked)
            {
                UnlinkOutgoing(relationship.SourceId, outgoingNode);
            }

            if (indexAdded)
            {
                _relationshipIndex.Remove(relationship.Id);
            }

            if (relationshipAdded)
            {
                _relationships.Remove(relationship.Id);
            }

            _adjacencyPool.Release(outgoingNode);
            _adjacencyPool.Release(incomingNode);

            throw;
        }
    }

    /// <summary>
    /// Adds an unversioned baseline relationship into the graph without authoring a lifecycle fact.
    /// Preserved for backwards compatibility with legacy fixtures.
    /// </summary>
    public void AddRelationship(
        SorophyRelationship relationship)
    {
        RestoreRelationship(relationship);
    }

    /// <summary>
    /// Creates a relationship in the graph at the specified temporal coordinate, executing Canon Check
    /// validation and recording an authored <see cref="SorophyRelationshipFactKind.Created"/> fact.
    /// </summary>
    public void CreateRelationship(
        SorophyRelationship relationship,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(creationTime);

        var canonResult = CanCreateRelationship(relationship, creationTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot create relationship '{relationship.Id}': {canonResult.ErrorMessage}");
        }

        RestoreRelationship(relationship);

        try
        {
            RecordRelationshipFact(
                SorophyRelationshipFact.CreateLifecycleFact(
                    creationTime,
                    relationship.Id,
                    relationship.SourceId,
                    relationship.TargetId,
                    relationship.Type,
                    SorophyRelationshipFactKind.Created,
                    relationship.Properties,
                    $"Relationship '{relationship.Id}' created at {creationTime}"));
        }
        catch
        {
            RollbackFailedRelationshipCreation(relationship.Id);
            throw;
        }
    }

    /// <summary>
    /// Creates a relationship in the graph at the specified temporal coordinate, executing Canon Check
    /// validation and recording an authored <see cref="SorophyRelationshipFactKind.Created"/> fact.
    /// </summary>
    public void CreateRelationship(
        Guid sourceId,
        Guid targetId,
        string type,
        SorophyTime creationTime,
        Guid? relationshipId = null,
        IDictionary<string, SorophyProperty>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(creationTime);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        var relationship = new SorophyRelationship
        {
            Id = relationshipId ?? Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            Type = type
        };

        if (properties is not null)
        {
            foreach (var (k, v) in properties)
            {
                relationship.Properties[k] = v;
            }
        }

        CreateRelationship(relationship, creationTime);
    }

    private void RollbackFailedRelationshipCreation(Guid relationshipId)
    {
        if (!_relationships.TryGetValue(relationshipId, out var relationship))
        {
            return;
        }

        if (_relationshipIndex.TryGetValue(relationshipId, out var index))
        {
            UnlinkOutgoing(relationship.SourceId, index.OutgoingNode);
            UnlinkIncoming(relationship.TargetId, index.IncomingNode);
            _adjacencyPool.Release(index.OutgoingNode);
            _adjacencyPool.Release(index.IncomingNode);
            _relationshipIndex.Remove(relationshipId);
        }

        _relationships.Remove(relationshipId);
        _relationshipHistories.Remove(relationshipId);
    }

    /// <summary>
    /// Temporally retires a relationship at the specified temporal coordinate without mutating canonical relationship storage.
    /// </summary>
    public bool RetireRelationship(
        Guid relationshipId,
        SorophyTime retirementTime,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRetireRelationship(relationshipId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot retire relationship '{relationshipId}': {canonResult.ErrorMessage}");
        }

        var rel = _relationships[relationshipId];

        RecordRelationshipFact(
            SorophyRelationshipFact.CreateLifecycleFact(
                retirementTime,
                rel.Id,
                rel.SourceId,
                rel.TargetId,
                rel.Type,
                SorophyRelationshipFactKind.Retired,
                rel.Properties,
                description ?? $"Relationship '{relationshipId}' retired at {retirementTime}"));

        return true;
    }

    /// <summary>
    /// Reverts an established temporal retirement for a relationship, removing the retirement fact
    /// from the authoritative temporal history.
    /// </summary>
    public bool RevertRelationshipRetirement(
        Guid relationshipId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRevertRelationshipRetirement(relationshipId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot revert retirement for relationship '{relationshipId}': {canonResult.ErrorMessage}");
        }

        return RevertRelationshipRetirementFact(relationshipId, retirementTime);
    }

    /* =============================================================
     * TEMPORAL RELATIONSHIP MUTATION OPERATIONS
     * =============================================================
     */

    /// <summary>
    /// Changes the semantic type of a relationship at the specified temporal coordinate,
    /// recording an authoritative RelationshipChanged fact with PreviousType and NewType,
    /// maintaining continuity, and updating canonical storage if at or after latest time.
    /// </summary>
    public void ChangeRelationshipType(
        Guid relationshipId,
        string newType,
        SorophyTime time,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (string.IsNullOrWhiteSpace(newType))
        {
            throw new ArgumentException("New relationship type cannot be null, empty, or whitespace.", nameof(newType));
        }

        var canonResult = CanChangeRelationshipType(relationshipId, newType, time);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(canonResult.ErrorMessage);
        }

        var relationship = _relationships[relationshipId];
        var history = GetOrCreateRelationshipHistory(relationshipId);

        var previousType = GetEffectiveRelationshipType(relationship, history, time);
        var immediateFutureFact = GetImmediateFutureRelationshipTypeFact(history, time);
        var oldImmediateFuturePreviousType = immediateFutureFact?.PreviousType;
        var isLatest = IsAtOrAfterLatestRelationshipTypeMutation(history, time);

        var oldLiveType = relationship.Type;

        SorophyRelationshipFact? newFact = null;
        try
        {
            newFact = SorophyRelationshipFact.CreateTypeChangeFact(
                time,
                relationshipId,
                relationship.SourceId,
                relationship.TargetId,
                previousType,
                newType,
                properties: relationship.Properties,
                description: description);

            history.Add(newFact);

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousType = newType;
            }

            if (isLatest)
            {
                relationship.Type = newType;
            }
        }
        catch
        {
            if (newFact is not null)
            {
                history.Remove(newFact);
            }

            relationship.Type = oldLiveType;

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousType = oldImmediateFuturePreviousType;
            }

            throw;
        }
    }

    /// <summary>
    /// Sets a relationship property at the specified temporal coordinate, recording an authoritative
    /// PropertyChanged fact, maintaining continuity, and updating canonical storage if at or after latest time.
    /// </summary>
    public void SetRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyValue value,
        SorophyTime time,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(time);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null, empty, or whitespace.", nameof(propertyName));
        }

        var canonResult = CanSetRelationshipProperty(relationshipId, propertyName, value, time);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(canonResult.ErrorMessage);
        }

        var relationship = _relationships[relationshipId];
        var history = GetOrCreateRelationshipHistory(relationshipId);

        var previousValue = GetEffectiveRelationshipPropertyValue(relationship, history, propertyName, time);
        var immediateFutureFact = GetImmediateFutureRelationshipPropertyFact(history, propertyName, time);
        var oldImmediateFuturePreviousValue = immediateFutureFact?.PreviousValue;
        var isLatest = IsAtOrAfterLatestRelationshipPropertyMutation(history, propertyName, time);

        var hadLiveProp = relationship.Properties.TryGetValue(propertyName, out var oldLiveProp);

        SorophyRelationshipFact? newFact = null;
        try
        {
            newFact = SorophyRelationshipFact.CreatePropertyChangeFact(
                time,
                relationshipId,
                relationship.SourceId,
                relationship.TargetId,
                relationship.Type,
                propertyName,
                previousValue,
                value,
                description: description);

            history.Add(newFact);

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = SorophyValueCloner.CloneValue(value);
            }

            if (isLatest)
            {
                relationship.Properties[propertyName] = new SorophyProperty
                {
                    Name = propertyName,
                    Value = SorophyValueCloner.CloneValue(value)
                };
            }
        }
        catch
        {
            if (newFact is not null)
            {
                history.Remove(newFact);
            }

            if (hadLiveProp)
            {
                relationship.Properties[propertyName] = oldLiveProp!;
            }
            else
            {
                relationship.Properties.Remove(propertyName);
            }

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = oldImmediateFuturePreviousValue;
            }

            throw;
        }
    }

    /// <summary>
    /// Removes a relationship property at the specified temporal coordinate, recording an authoritative
    /// PropertyChanged fact (with NewValue = null), maintaining continuity, and removing from canonical store if latest.
    /// </summary>
    public bool RemoveRelationshipProperty(
        Guid relationshipId,
        string propertyName,
        SorophyTime time,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null, empty, or whitespace.", nameof(propertyName));
        }

        var canonResult = CanRemoveRelationshipProperty(relationshipId, propertyName, time);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(canonResult.ErrorMessage);
        }

        var relationship = _relationships[relationshipId];
        var history = GetOrCreateRelationshipHistory(relationshipId);

        var previousValue = GetEffectiveRelationshipPropertyValue(relationship, history, propertyName, time);
        if (previousValue is null)
        {
            return false;
        }

        var immediateFutureFact = GetImmediateFutureRelationshipPropertyFact(history, propertyName, time);
        var oldImmediateFuturePreviousValue = immediateFutureFact?.PreviousValue;
        var isLatest = IsAtOrAfterLatestRelationshipPropertyMutation(history, propertyName, time);

        var hadLiveProp = relationship.Properties.TryGetValue(propertyName, out var oldLiveProp);

        SorophyRelationshipFact? newFact = null;
        try
        {
            newFact = SorophyRelationshipFact.CreatePropertyChangeFact(
                time,
                relationshipId,
                relationship.SourceId,
                relationship.TargetId,
                relationship.Type,
                propertyName,
                previousValue,
                newValue: null,
                description: description);

            history.Add(newFact);

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = null;
            }

            if (isLatest)
            {
                relationship.Properties.Remove(propertyName);
            }

            return true;
        }
        catch
        {
            if (newFact is not null)
            {
                history.Remove(newFact);
            }

            if (hadLiveProp)
            {
                relationship.Properties[propertyName] = oldLiveProp!;
            }

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = oldImmediateFuturePreviousValue;
            }

            throw;
        }
    }

    internal static string GetEffectiveRelationshipType(
        SorophyRelationship relationship,
        SorophyRelationshipHistory? history,
        SorophyTime time)
    {
        if (history is null || history.Facts.Count == 0)
        {
            return relationship.Type;
        }

        var facts = history.Facts
            .Where(f => f.Kind == SorophyRelationshipFactKind.RelationshipChanged)
            .ToList();

        if (facts.Count == 0)
        {
            return relationship.Type;
        }

        facts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        SorophyRelationshipFact? lastAtOrBefore = null;
        for (int i = 0; i < facts.Count; i++)
        {
            var f = facts[i];
            if (SorophyTime.CanCompare(f.At, time))
            {
                var cmp = SorophyTime.Compare(f.At, time);
                if (cmp <= 0)
                {
                    lastAtOrBefore = f;
                }
                else
                {
                    break;
                }
            }
        }

        if (lastAtOrBefore is not null)
        {
            return lastAtOrBefore.NewType ?? lastAtOrBefore.Type;
        }

        var earliestFuture = facts[0];
        return earliestFuture.PreviousType ?? relationship.Type;
    }

    internal static SorophyRelationshipFact? GetImmediateFutureRelationshipTypeFact(
        SorophyRelationshipHistory history,
        SorophyTime time)
    {
        var futureFacts = new List<SorophyRelationshipFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyRelationshipFactKind.RelationshipChanged)
            {
                if (SorophyTime.CanCompare(f.At, time) && SorophyTime.Compare(f.At, time) > 0)
                {
                    futureFacts.Add(f);
                }
            }
        }

        if (futureFacts.Count == 0)
        {
            return null;
        }

        futureFacts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        return futureFacts[0];
    }

    internal static bool IsAtOrAfterLatestRelationshipTypeMutation(
        SorophyRelationshipHistory history,
        SorophyTime time)
    {
        var facts = new List<SorophyRelationshipFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyRelationshipFactKind.RelationshipChanged)
            {
                facts.Add(f);
            }
        }

        if (facts.Count == 0)
        {
            return true;
        }

        facts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        var latest = facts[^1];
        if (SorophyTime.CanCompare(time, latest.At))
        {
            return SorophyTime.Compare(time, latest.At) >= 0;
        }

        return true;
    }

    internal static SorophyValue? GetEffectiveRelationshipPropertyValue(
        SorophyRelationship relationship,
        SorophyRelationshipHistory? history,
        string propertyName,
        SorophyTime time)
    {
        if (history is null || history.Facts.Count == 0)
        {
            return relationship.Properties.TryGetValue(propertyName, out var prop)
                ? prop.Value
                : null;
        }

        var facts = history.Facts
            .Where(f => f.Kind == SorophyRelationshipFactKind.PropertyChanged &&
                        string.Equals(f.PropertyName, propertyName, StringComparison.Ordinal))
            .ToList();

        if (facts.Count == 0)
        {
            return relationship.Properties.TryGetValue(propertyName, out var prop)
                ? prop.Value
                : null;
        }

        facts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        SorophyRelationshipFact? lastAtOrBefore = null;
        for (int i = 0; i < facts.Count; i++)
        {
            var f = facts[i];
            if (SorophyTime.CanCompare(f.At, time))
            {
                var cmp = SorophyTime.Compare(f.At, time);
                if (cmp <= 0)
                {
                    lastAtOrBefore = f;
                }
                else
                {
                    break;
                }
            }
        }

        if (lastAtOrBefore is not null)
        {
            return lastAtOrBefore.NewValue;
        }

        var earliestFuture = facts[0];
        return earliestFuture.PreviousValue;
    }

    internal static SorophyRelationshipFact? GetImmediateFutureRelationshipPropertyFact(
        SorophyRelationshipHistory history,
        string propertyName,
        SorophyTime time)
    {
        var futureFacts = new List<SorophyRelationshipFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyRelationshipFactKind.PropertyChanged &&
                string.Equals(f.PropertyName, propertyName, StringComparison.Ordinal))
            {
                if (SorophyTime.CanCompare(f.At, time) && SorophyTime.Compare(f.At, time) > 0)
                {
                    futureFacts.Add(f);
                }
            }
        }

        if (futureFacts.Count == 0)
        {
            return null;
        }

        futureFacts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        return futureFacts[0];
    }

    internal static bool IsAtOrAfterLatestRelationshipPropertyMutation(
        SorophyRelationshipHistory history,
        string propertyName,
        SorophyTime time)
    {
        var facts = new List<SorophyRelationshipFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyRelationshipFactKind.PropertyChanged &&
                string.Equals(f.PropertyName, propertyName, StringComparison.Ordinal))
            {
                facts.Add(f);
            }
        }

        if (facts.Count == 0)
        {
            return true;
        }

        facts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Sequence.CompareTo(b.Sequence);
        });

        var latest = facts[^1];
        if (SorophyTime.CanCompare(time, latest.At))
        {
            return SorophyTime.Compare(time, latest.At) >= 0;
        }

        return true;
    }

    /* =============================================================
     * TEMPORAL EXISTENCE & LIFECYCLE QUERIES
     * =============================================================
     */

    /// <summary>
    /// Determines whether a relationship exists at the specified temporal coordinate.
    /// Enforces the hard endpoint existence invariant: a relationship cannot exist at coordinate T
    /// unless both of its endpoint entities exist at T.
    /// </summary>
    public bool RelationshipExistsAt(
        Guid relationshipId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (relationshipId == Guid.Empty)
        {
            return false;
        }

        if (_relationships.TryGetValue(relationshipId, out var rel))
        {
            if (!EntityExistsAt(rel.SourceId, time) || !EntityExistsAt(rel.TargetId, time))
            {
                return false;
            }

            if (_relationshipHistories.TryGetValue(relationshipId, out var history) && history is not null)
            {
                if (history.CreatedAt is not null || history.RetiredAt is not null)
                {
                    return history.ExistsAt(time);
                }

                // Legacy pre-Krono evolution facts (Kind is null)
                if (history.Facts.Count > 0 && history.Facts[0].Kind is null)
                {
                    var firstFact = history.Facts[0];
                    bool isCreation = firstFact.ValidTill is null ||
                                      SorophyTime.Compare(firstFact.ValidTill, firstFact.At) != 0;

                    if (isCreation && SorophyTime.Compare(time, firstFact.At) < 0)
                    {
                        return false;
                    }

                    return true;
                }
            }

            // Legacy / unversioned baseline relationship: exists across all coordinates where endpoints exist
            return true;
        }

        // Legacy compatibility: pre-Krono evolution termination removed the relationship from _relationships,
        // but preserved history with Kind == null and added to _retiredRelationshipIds.
        if (_retiredRelationshipIds.Contains(relationshipId) &&
            _relationshipHistories.TryGetValue(relationshipId, out var legacyHistory) &&
            legacyHistory is not null &&
            legacyHistory.Facts.Count > 0 &&
            legacyHistory.Facts[0].Kind is null)
        {
            var facts = legacyHistory.Facts;
            var firstFact = facts[0];
            var lastFact = facts[^1];

            if (!EntityExistsAt(firstFact.SourceId, time) || !EntityExistsAt(firstFact.TargetId, time))
            {
                return false;
            }

            bool isCreation = firstFact.ValidTill is null ||
                              SorophyTime.Compare(firstFact.ValidTill, firstFact.At) != 0;

            if (isCreation && SorophyTime.Compare(time, firstFact.At) < 0)
            {
                return false;
            }

            if (SorophyTime.Compare(time, lastFact.At) >= 0)
            {
                return false;
            }

            return true;
        }

        // Krono permanently deleted relationship (or non-existent): absent from all temporal coordinates
        return false;
    }

    /// <summary>
    /// Evaluates the temporal lifecycle status of a relationship at the specified coordinate.
    /// </summary>
    public SorophyRelationshipLifecycleStatus GetRelationshipLifecycleStatus(
        Guid relationshipId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (relationshipId == Guid.Empty || !_relationships.TryGetValue(relationshipId, out var rel))
        {
            return SorophyRelationshipLifecycleStatus.Uncreated;
        }

        if (!EntityExistsAt(rel.SourceId, time) || !EntityExistsAt(rel.TargetId, time))
        {
            return SorophyRelationshipLifecycleStatus.EndpointInactive;
        }

        if (_relationshipHistories.TryGetValue(relationshipId, out var history) && history is not null)
        {
            return history.GetStatusAt(time);
        }

        return SorophyRelationshipLifecycleStatus.Active;
    }

    /// <summary>
    /// Determines whether a relationship is currently retired in its authoritative temporal history.
    /// </summary>
    public bool IsRelationshipRetired(
        Guid relationshipId)
    {
        if (_relationshipHistories.TryGetValue(relationshipId, out var history) && history is not null)
        {
            return history.IsRetired;
        }

        return false;
    }

    /* =============================================================
     * PERMANENT RELATIONSHIP DELETION
     * =============================================================
     */

    /// <summary>
    /// Permanently removes a relationship from canonical storage and unlinks its adjacency nodes.
    /// </summary>
    public bool RemoveRelationship(
        Guid relationshipId)
    {
        if (!_relationships.TryGetValue(
                relationshipId,
                out var relationship))
        {
            return false;
        }

        if (!_relationshipIndex.TryGetValue(
                relationshipId,
                out var index))
        {
            throw new InvalidOperationException(
                $"Relationship '{relationshipId}' is missing its adjacency index.");
        }

        /*
         * Unlink while canonical relationship information still exists.
         */
        UnlinkOutgoing(
            relationship.SourceId,
            index.OutgoingNode);

        UnlinkIncoming(
            relationship.TargetId,
            index.IncomingNode);

        /*
         * Return physical nodes to the free list.
         */
        _adjacencyPool.Release(
            index.OutgoingNode);

        _adjacencyPool.Release(
            index.IncomingNode);

        /*
         * Remove canonical/index state.
         */
        _relationshipIndex.Remove(
            relationshipId);

        _relationships.Remove(
            relationshipId);

        /*
         * The relationship no longer exists in the active graph, but its
         * identity is permanently retired.
         *
         * Any historical facts associated with this identity remain
         * preserved in the history store.
         */
        RetireRelationshipId(
            relationshipId);

        return true;
    }

    /// <summary>
    /// Permanently removes a relationship from canonical storage and unlinks its adjacency nodes.
    /// Explicit alias for <see cref="RemoveRelationship(Guid)"/>.
    /// </summary>
    public bool DeleteRelationship(
        Guid relationshipId)
    {
        return RemoveRelationship(relationshipId);
    }
}