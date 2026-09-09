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
    /// Validates whether an entity can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            this,
            entity.Id,
            creationTime);
    }

    /// <summary>
    /// Validates whether an entity with the specified ID can be canonically created at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanCreateEntity(
        Guid entityId,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(creationTime);

        return SorophyCanonValidator.ValidateEntityCreation(
            this,
            entityId,
            creationTime);
    }

    /// <summary>
    /// Validates whether an entity can be canonically retired at the specified temporal coordinate.
    /// </summary>
    public SorophyCanonResult CanRetireEntity(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateEntityRetirement(
            this,
            entityId,
            retirementTime);
    }

    /// <summary>
    /// Validates whether an established retirement fact for an entity can be canonically reverted.
    /// </summary>
    public SorophyCanonResult CanRevertRetirement(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        return SorophyCanonValidator.ValidateRetirementReversal(
            this,
            entityId,
            retirementTime);
    }

    /* =============================================================
     * ENTITY LIFECYCLE OPERATIONS
     * =============================================================
     */

    /// <summary>
    /// Restores an entity directly into canonical storage and the tag index without authoring a lifecycle fact.
    /// Used by deserialization and test fixtures to restore entities (including legacy timeless entities).
    /// </summary>
    internal void RestoreEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException(
                $"An entity with ID '{entity.Id}' already exists.");
        }

        try
        {
            /*
             * The Entity becomes visible to the tag index only after the
             * canonical store accepts it. If tag indexing fails, roll the
             * canonical insertion back so RestoreEntity remains atomic.
             */
            _tagIndex.IndexEntity(entity);
        }
        catch
        {
            _entities.Remove(entity.Id);
            _tagIndex.RemoveEntity(entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Creates an entity in the graph at the specified temporal coordinate, executing Canon Check
    /// validation and recording an authored <see cref="SorophyEntityFactKind.Created"/> fact.
    /// </summary>
    public void CreateEntity(
        SorophyEntity entity,
        SorophyTime creationTime)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(creationTime);

        var canonResult = CanCreateEntity(entity, creationTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot create entity '{entity.Id}': {canonResult.ErrorMessage}");
        }

        RestoreEntity(entity);

        try
        {
            RecordEntityFact(
                SorophyEntityFact.Create(
                    creationTime,
                    entity.Id,
                    SorophyEntityFactKind.Created,
                    $"Entity '{entity.Name}' created at {creationTime}"));
        }
        catch
        {
            _entities.Remove(entity.Id);
            _tagIndex.RemoveEntity(entity.Id);
            _entityHistories.Remove(entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Temporally retires an entity at the specified temporal coordinate without mutating canonical entity or relationship storage.
    /// </summary>
    public bool RetireEntity(
        Guid entityId,
        SorophyTime retirementTime,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRetireEntity(entityId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot retire entity '{entityId}': {canonResult.ErrorMessage}");
        }

        RecordEntityFact(
            SorophyEntityFact.Create(
                retirementTime,
                entityId,
                SorophyEntityFactKind.Retired,
                description ?? $"Entity '{entityId}' retired at {retirementTime}"));

        return true;
    }

    /// <summary>
    /// Reverts an established temporal retirement for an entity, removing the retirement fact
    /// from the authoritative temporal history.
    /// </summary>
    public bool RevertRetirement(
        Guid entityId,
        SorophyTime retirementTime)
    {
        ArgumentNullException.ThrowIfNull(retirementTime);

        var canonResult = CanRevertRetirement(entityId, retirementTime);
        if (!canonResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot revert retirement for entity '{entityId}': {canonResult.ErrorMessage}");
        }

        return RevertEntityRetirementFact(entityId, retirementTime);
    }

    /* =============================================================
     * TEMPORAL ENTITY PROPERTY MUTATION OPERATIONS
     * =============================================================
     */

    /// <summary>
    /// Sets an entity property at the specified temporal coordinate, recording an authoritative
    /// PropertyChanged fact, maintaining past/future continuity, and updating canonical storage if at or after latest time.
    /// </summary>
    public void SetEntityProperty(
        Guid entityId,
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

        if (!_entities.TryGetValue(entityId, out var entity))
        {
            throw new InvalidOperationException($"Entity '{entityId}' does not exist in the graph.");
        }

        var status = GetEntityLifecycleStatus(entityId, time);
        if (status == SorophyEntityLifecycleStatus.Uncreated)
        {
            throw new InvalidOperationException(
                $"Cannot mutate entity '{entityId}' at '{time}': entity is uncreated at this coordinate.");
        }
        if (status == SorophyEntityLifecycleStatus.Retired)
        {
            throw new InvalidOperationException(
                $"Cannot mutate entity '{entityId}' at '{time}': entity is retired at this coordinate.");
        }

        var history = GetOrCreateEntityHistory(entityId);

        var previousValue = GetEffectiveEntityPropertyValue(entity, history, propertyName, time);
        var immediateFutureFact = GetImmediateFutureEntityPropertyFact(history, propertyName, time);
        var oldImmediateFuturePreviousValue = immediateFutureFact?.PreviousValue;
        var isLatest = IsAtOrAfterLatestEntityPropertyMutation(history, propertyName, time);

        var hadLiveProp = entity.Properties.TryGetValue(propertyName, out var oldLiveProp);

        SorophyEntityFact newFact;
        try
        {
            newFact = SorophyEntityFact.CreatePropertyChange(
                time,
                entityId,
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
                entity.Properties[propertyName] = new SorophyProperty
                {
                    Name = propertyName,
                    Value = SorophyValueCloner.CloneValue(value)
                };
            }
        }
        catch
        {
            if (hadLiveProp)
            {
                entity.Properties[propertyName] = oldLiveProp!;
            }
            else
            {
                entity.Properties.Remove(propertyName);
            }

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = oldImmediateFuturePreviousValue;
            }

            throw;
        }
    }

    /// <summary>
    /// Removes an entity property at the specified temporal coordinate, recording an authoritative
    /// PropertyChanged fact (with NewValue = null), maintaining continuity, and removing from canonical store if latest.
    /// </summary>
    public bool RemoveEntityProperty(
        Guid entityId,
        string propertyName,
        SorophyTime time,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null, empty, or whitespace.", nameof(propertyName));
        }

        if (!_entities.TryGetValue(entityId, out var entity))
        {
            throw new InvalidOperationException($"Entity '{entityId}' does not exist in the graph.");
        }

        var status = GetEntityLifecycleStatus(entityId, time);
        if (status == SorophyEntityLifecycleStatus.Uncreated)
        {
            throw new InvalidOperationException(
                $"Cannot mutate entity '{entityId}' at '{time}': entity is uncreated at this coordinate.");
        }
        if (status == SorophyEntityLifecycleStatus.Retired)
        {
            throw new InvalidOperationException(
                $"Cannot mutate entity '{entityId}' at '{time}': entity is retired at this coordinate.");
        }

        var history = GetOrCreateEntityHistory(entityId);

        var previousValue = GetEffectiveEntityPropertyValue(entity, history, propertyName, time);
        if (previousValue is null)
        {
            return false;
        }

        var immediateFutureFact = GetImmediateFutureEntityPropertyFact(history, propertyName, time);
        var oldImmediateFuturePreviousValue = immediateFutureFact?.PreviousValue;
        var isLatest = IsAtOrAfterLatestEntityPropertyMutation(history, propertyName, time);

        var hadLiveProp = entity.Properties.TryGetValue(propertyName, out var oldLiveProp);

        SorophyEntityFact newFact;
        try
        {
            newFact = SorophyEntityFact.CreatePropertyChange(
                time,
                entityId,
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
                entity.Properties.Remove(propertyName);
            }

            return true;
        }
        catch
        {
            if (hadLiveProp)
            {
                entity.Properties[propertyName] = oldLiveProp!;
            }

            if (immediateFutureFact is not null)
            {
                immediateFutureFact.PreviousValue = oldImmediateFuturePreviousValue;
            }

            throw;
        }
    }

    internal static SorophyValue? GetEffectiveEntityPropertyValue(
        SorophyEntity entity,
        SorophyEntityHistory? history,
        string propertyName,
        SorophyTime time)
    {
        if (history is null || history.Facts.Count == 0)
        {
            return entity.Properties.TryGetValue(propertyName, out var prop)
                ? prop.Value
                : null;
        }

        var facts = history.Facts
            .Where(f => f.Kind == SorophyEntityFactKind.PropertyChanged &&
                        string.Equals(f.PropertyName, propertyName, StringComparison.Ordinal))
            .ToList();

        if (facts.Count == 0)
        {
            return entity.Properties.TryGetValue(propertyName, out var prop)
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

        SorophyEntityFact? lastAtOrBefore = null;
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

    internal static SorophyEntityFact? GetImmediateFutureEntityPropertyFact(
        SorophyEntityHistory history,
        string propertyName,
        SorophyTime time)
    {
        var futureFacts = new List<SorophyEntityFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyEntityFactKind.PropertyChanged &&
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

    internal static bool IsAtOrAfterLatestEntityPropertyMutation(
        SorophyEntityHistory history,
        string propertyName,
        SorophyTime time)
    {
        var facts = new List<SorophyEntityFact>();
        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];
            if (f.Kind == SorophyEntityFactKind.PropertyChanged &&
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
    /// Determines whether an entity exists at the specified temporal coordinate.
    /// </summary>
    public bool EntityExistsAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (entityId == Guid.Empty || !_entities.ContainsKey(entityId))
        {
            return false;
        }

        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.ExistsAt(time);
        }

        // Legacy / unversioned baseline entity: exists across all coordinates
        return true;
    }

    /// <summary>
    /// Gets the lifecycle status of an entity at the specified temporal coordinate.
    /// </summary>
    public SorophyEntityLifecycleStatus GetEntityLifecycleStatus(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (entityId == Guid.Empty || !_entities.ContainsKey(entityId))
        {
            return SorophyEntityLifecycleStatus.Uncreated;
        }

        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.GetStatusAt(time);
        }

        // Legacy / unversioned baseline entity: Active across all coordinates
        return SorophyEntityLifecycleStatus.Active;
    }

    /// <summary>
    /// Determines whether an entity is currently retired in its authoritative temporal history.
    /// </summary>
    public bool IsEntityRetired(
        Guid entityId)
    {
        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null)
        {
            return history.IsRetired;
        }

        return false;
    }

    /* =============================================================
     * PERMANENT ENTITY DELETION
     * =============================================================
     */

    /// <summary>
    /// Permanently deletes an entity from the graph, removing all incident relationships,
    /// tag index entries, and authoritative temporal history.
    /// </summary>
    public bool RemoveEntity(
        Guid entityId)
    {
        if (!_entities.Remove(
                entityId))
        {
            return false;
        }

        /*
         * Remove relationships connected to this entity if any adjacency
         * records exist.
         */
        if (_entityAdjacency.ContainsKey(
                entityId))
        {
            while (_entityAdjacency.TryGetValue(
                       entityId,
                       out var outgoingAdjacency) &&
                   outgoingAdjacency.OutgoingHead !=
                       AdjacencySlabPool.None)
            {
                var nodeIndex =
                    outgoingAdjacency.OutgoingHead;

                var node =
                    _adjacencyPool.GetNode(
                        nodeIndex);

                var relationshipId =
                    node.RelationshipId;

                if (!RemoveRelationship(
                        relationshipId))
                {
                    throw new InvalidOperationException(
                        $"Adjacency index references relationship '{relationshipId}', " +
                        "but the relationship could not be removed.");
                }
            }

            while (_entityAdjacency.TryGetValue(
                       entityId,
                       out var incomingAdjacency) &&
                   incomingAdjacency.IncomingHead !=
                       AdjacencySlabPool.None)
            {
                var nodeIndex =
                    incomingAdjacency.IncomingHead;

                var node =
                    _adjacencyPool.GetNode(
                        nodeIndex);

                var relationshipId =
                    node.RelationshipId;

                if (!RemoveRelationship(
                        relationshipId))
                {
                    throw new InvalidOperationException(
                        $"Adjacency index references relationship '{relationshipId}', " +
                        "but the relationship could not be removed.");
                }
            }

            _entityAdjacency.Remove(
                entityId);
        }

        /*
         * Remove the entity from every tag bucket.
         */
        _tagIndex.RemoveEntity(
            entityId);

        /*
         * Permanent deletion purges the entity's temporal history.
         */
        _entityHistories.Remove(
            entityId);

        return true;
    }

    /// <summary>
    /// Permanently deletes an entity from the graph, removing all incident relationships,
    /// tag index entries, and authoritative temporal history.
    /// </summary>
    public bool DeleteEntity(
        Guid entityId)
    {
        return RemoveEntity(entityId);
    }
}