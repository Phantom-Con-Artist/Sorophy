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

using System.Collections.Generic;
using Sorophy.Engine.Snapshot;

namespace Sorophy.Engine.Diff;

/// <summary>
/// Represents an immutable set of structural differences between two graph snapshots.
/// </summary>
/// <remarks>
/// <para>
/// Graph Diff compares endpoint snapshot state only and cannot detect intermediate
/// removal and recreation when the same canonical identity appears in both snapshots.
/// Such historical transition analysis remains exclusively the responsibility of
/// the Temporal Query Domain (TQD) and relationship histories.
/// </para>
/// <para>
/// All collections in this change set are exposed in a deterministic order sorted
/// by canonical identifier (<see cref="System.Guid"/>) as a presentation convention.
/// This ordering carries no temporal or causal meaning.
/// </para>
/// </remarks>
public interface ISorophyGraphChangeSet
{
    /// <summary>
    /// Gets the baseline snapshot representing state A.
    /// </summary>
    ISorophySnapshot Before { get; }

    /// <summary>
    /// Gets the target snapshot representing state B.
    /// </summary>
    ISorophySnapshot After { get; }

    /// <summary>
    /// Gets whether any structural differences exist between the snapshots.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Gets the total count of entity and relationship changes across all categories.
    /// </summary>
    int TotalChanges { get; }

    /// <summary>
    /// Gets all entity changes (added, removed, modified), ordered deterministically by EntityId.
    /// </summary>
    IReadOnlyList<SorophyEntityChange> EntityChanges { get; }

    /// <summary>
    /// Gets all relationship changes (added, removed, modified), ordered deterministically by RelationshipId.
    /// </summary>
    IReadOnlyList<SorophyRelationshipChange> RelationshipChanges { get; }

    /// <summary>
    /// Gets all entities added in the target snapshot, ordered deterministically by Id.
    /// </summary>
    IReadOnlyList<ISorophySnapshotEntity> AddedEntities { get; }

    /// <summary>
    /// Gets all entities removed from the baseline snapshot, ordered deterministically by Id.
    /// </summary>
    IReadOnlyList<ISorophySnapshotEntity> RemovedEntities { get; }

    /// <summary>
    /// Gets all modified entities with both before and after states, ordered deterministically by EntityId.
    /// </summary>
    IReadOnlyList<SorophyEntityChange> ModifiedEntities { get; }

    /// <summary>
    /// Gets all relationships added in the target snapshot, ordered deterministically by Id.
    /// </summary>
    IReadOnlyList<ISorophySnapshotRelationship> AddedRelationships { get; }

    /// <summary>
    /// Gets all relationships removed from the baseline snapshot, ordered deterministically by Id.
    /// </summary>
    IReadOnlyList<ISorophySnapshotRelationship> RemovedRelationships { get; }

    /// <summary>
    /// Gets all modified relationships with both before and after states, ordered deterministically by RelationshipId.
    /// </summary>
    IReadOnlyList<SorophyRelationshipChange> ModifiedRelationships { get; }
}

