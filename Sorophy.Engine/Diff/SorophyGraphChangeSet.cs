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
/// Immutable implementation of <see cref="ISorophyGraphChangeSet"/>.
/// </summary>
public sealed class SorophyGraphChangeSet : ISorophyGraphChangeSet
{
    private static readonly IReadOnlyList<SorophyEntityChange> EmptyEntityChanges =
        Array.Empty<SorophyEntityChange>();

    private static readonly IReadOnlyList<SorophyRelationshipChange> EmptyRelationshipChanges =
        Array.Empty<SorophyRelationshipChange>();

    private static readonly IReadOnlyList<ISorophySnapshotEntity> EmptySnapshotEntities =
        Array.Empty<ISorophySnapshotEntity>();

    private static readonly IReadOnlyList<ISorophySnapshotRelationship> EmptySnapshotRelationships =
        Array.Empty<ISorophySnapshotRelationship>();

    public ISorophySnapshot Before { get; }
    public ISorophySnapshot After { get; }
    public bool HasChanges => TotalChanges > 0;
    public int TotalChanges => EntityChanges.Count + RelationshipChanges.Count;

    public IReadOnlyList<SorophyEntityChange> EntityChanges { get; }
    public IReadOnlyList<SorophyRelationshipChange> RelationshipChanges { get; }

    public IReadOnlyList<ISorophySnapshotEntity> AddedEntities { get; }
    public IReadOnlyList<ISorophySnapshotEntity> RemovedEntities { get; }
    public IReadOnlyList<SorophyEntityChange> ModifiedEntities { get; }

    public IReadOnlyList<ISorophySnapshotRelationship> AddedRelationships { get; }
    public IReadOnlyList<ISorophySnapshotRelationship> RemovedRelationships { get; }
    public IReadOnlyList<SorophyRelationshipChange> ModifiedRelationships { get; }

    public SorophyGraphChangeSet(
        ISorophySnapshot before,
        ISorophySnapshot after,
        IReadOnlyList<SorophyEntityChange> entityChanges,
        IReadOnlyList<SorophyRelationshipChange> relationshipChanges,
        IReadOnlyList<ISorophySnapshotEntity> addedEntities,
        IReadOnlyList<ISorophySnapshotEntity> removedEntities,
        IReadOnlyList<SorophyEntityChange> modifiedEntities,
        IReadOnlyList<ISorophySnapshotRelationship> addedRelationships,
        IReadOnlyList<ISorophySnapshotRelationship> removedRelationships,
        IReadOnlyList<SorophyRelationshipChange> modifiedRelationships)
    {
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));

        EntityChanges = entityChanges ?? EmptyEntityChanges;
        RelationshipChanges = relationshipChanges ?? EmptyRelationshipChanges;

        AddedEntities = addedEntities ?? EmptySnapshotEntities;
        RemovedEntities = removedEntities ?? EmptySnapshotEntities;
        ModifiedEntities = modifiedEntities ?? EmptyEntityChanges;

        AddedRelationships = addedRelationships ?? EmptySnapshotRelationships;
        RemovedRelationships = removedRelationships ?? EmptySnapshotRelationships;
        ModifiedRelationships = modifiedRelationships ?? EmptyRelationshipChanges;
    }

    /// <summary>
    /// Creates an empty change set representing structural identity between two snapshots.
    /// </summary>
    public static SorophyGraphChangeSet Empty(
        ISorophySnapshot before,
        ISorophySnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        return new SorophyGraphChangeSet(
            before,
            after,
            EmptyEntityChanges,
            EmptyRelationshipChanges,
            EmptySnapshotEntities,
            EmptySnapshotEntities,
            EmptyEntityChanges,
            EmptySnapshotRelationships,
            EmptySnapshotRelationships,
            EmptyRelationshipChanges);
    }
}

