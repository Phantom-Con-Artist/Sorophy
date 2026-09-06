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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Sorophy.Engine.Snapshot;

namespace Sorophy.Engine.Diff;

/// <summary>
/// Authoritative structural comparison engine for Sorophy graph snapshots.
/// </summary>
/// <remarks>
/// <para>
/// Graph Diff computes a deterministic, read-only structural diff between two
/// materialized <see cref="ISorophySnapshot"/> instances.
/// </para>
/// <para>
/// Graph Diff compares endpoint snapshot state only and cannot detect intermediate
/// removal and recreation when the same canonical identity appears in both snapshots.
/// Such historical transition analysis remains exclusively the responsibility of
/// the Temporal Query Domain (TQD) and relationship histories.
/// </para>
/// </remarks>
public static class SorophyGraphDiff
{
    /// <summary>
    /// Computes the structural difference between two graph snapshots.
    /// </summary>
    /// <param name="before">The baseline snapshot representing state A.</param>
    /// <param name="after">The target snapshot representing state B.</param>
    /// <returns>An immutable <see cref="ISorophyGraphChangeSet"/> describing the structural difference.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="before"/> or <paramref name="after"/> is null.</exception>
    public static ISorophyGraphChangeSet Compare(
        ISorophySnapshot before,
        ISorophySnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (ReferenceEquals(before, after))
        {
            return SorophyGraphChangeSet.Empty(before, after);
        }

        // 1. Process Entities
        var entityChanges = new List<SorophyEntityChange>();
        var addedEntities = new List<ISorophySnapshotEntity>();
        var removedEntities = new List<ISorophySnapshotEntity>();
        var modifiedEntities = new List<SorophyEntityChange>();

        foreach (var (id, afterEntity) in after.Entities)
        {
            if (before.Entities.TryGetValue(id, out var beforeEntity))
            {
                if (!SorophyStructuralEquality.EntityEquals(beforeEntity, afterEntity))
                {
                    var change = new SorophyEntityChange(
                        GraphChangeKind.Modified,
                        id,
                        beforeEntity,
                        afterEntity);

                    entityChanges.Add(change);
                    modifiedEntities.Add(change);
                }
            }
            else
            {
                var change = new SorophyEntityChange(
                    GraphChangeKind.Added,
                    id,
                    null,
                    afterEntity);

                entityChanges.Add(change);
                addedEntities.Add(afterEntity);
            }
        }

        foreach (var (id, beforeEntity) in before.Entities)
        {
            if (!after.Entities.ContainsKey(id))
            {
                var change = new SorophyEntityChange(
                    GraphChangeKind.Removed,
                    id,
                    beforeEntity,
                    null);

                entityChanges.Add(change);
                removedEntities.Add(beforeEntity);
            }
        }

        // 2. Process Relationships
        var relationshipChanges = new List<SorophyRelationshipChange>();
        var addedRelationships = new List<ISorophySnapshotRelationship>();
        var removedRelationships = new List<ISorophySnapshotRelationship>();
        var modifiedRelationships = new List<SorophyRelationshipChange>();

        foreach (var (id, afterRel) in after.Relationships)
        {
            if (before.Relationships.TryGetValue(id, out var beforeRel))
            {
                if (!SorophyStructuralEquality.RelationshipEquals(beforeRel, afterRel))
                {
                    var change = new SorophyRelationshipChange(
                        GraphChangeKind.Modified,
                        id,
                        beforeRel,
                        afterRel);

                    relationshipChanges.Add(change);
                    modifiedRelationships.Add(change);
                }
            }
            else
            {
                var change = new SorophyRelationshipChange(
                    GraphChangeKind.Added,
                    id,
                    null,
                    afterRel);

                relationshipChanges.Add(change);
                addedRelationships.Add(afterRel);
            }
        }

        foreach (var (id, beforeRel) in before.Relationships)
        {
            if (!after.Relationships.ContainsKey(id))
            {
                var change = new SorophyRelationshipChange(
                    GraphChangeKind.Removed,
                    id,
                    beforeRel,
                    null);

                relationshipChanges.Add(change);
                removedRelationships.Add(beforeRel);
            }
        }

        // 3. Deterministic Sorting (sorted by canonical Guid ascending)
        entityChanges.Sort(static (a, b) => a.EntityId.CompareTo(b.EntityId));
        addedEntities.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        removedEntities.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        modifiedEntities.Sort(static (a, b) => a.EntityId.CompareTo(b.EntityId));

        relationshipChanges.Sort(static (a, b) => a.RelationshipId.CompareTo(b.RelationshipId));
        addedRelationships.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        removedRelationships.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        modifiedRelationships.Sort(static (a, b) => a.RelationshipId.CompareTo(b.RelationshipId));

        return new SorophyGraphChangeSet(
            before,
            after,
            entityChanges.AsReadOnly(),
            relationshipChanges.AsReadOnly(),
            addedEntities.AsReadOnly(),
            removedEntities.AsReadOnly(),
            modifiedEntities.AsReadOnly(),
            addedRelationships.AsReadOnly(),
            removedRelationships.AsReadOnly(),
            modifiedRelationships.AsReadOnly());
    }

    /// <summary>
    /// Computes the structural difference from this snapshot to the specified target snapshot.
    /// </summary>
    /// <param name="before">The baseline snapshot.</param>
    /// <param name="after">The target snapshot.</param>
    /// <returns>An immutable change set describing the structural transition.</returns>
    public static ISorophyGraphChangeSet Diff(
        this ISorophySnapshot before,
        ISorophySnapshot after) =>
        Compare(before, after);
}

