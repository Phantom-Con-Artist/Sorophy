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
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;

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

        return SorophyCanonValidator.ValidateRelationshipRetirementReversal(
            this,
            relationshipId,
            retirementTime);
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
            RemoveRelationship(relationship.Id);
            throw;
        }
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

        if (relationshipId == Guid.Empty || !_relationships.TryGetValue(relationshipId, out var rel))
        {
            return false;
        }

        if (!EntityExistsAt(rel.SourceId, time) || !EntityExistsAt(rel.TargetId, time))
        {
            return false;
        }

        if (_relationshipHistories.TryGetValue(relationshipId, out var history) && history is not null)
        {
            return history.ExistsAt(time);
        }

        // Legacy / unversioned baseline relationship: exists across all coordinates where endpoints exist
        return true;
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