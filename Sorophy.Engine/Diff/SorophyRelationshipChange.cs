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
using Sorophy.Engine.Snapshot;

namespace Sorophy.Engine.Diff;

/// <summary>
/// Represents a structural change to a relationship between two graph snapshots.
/// </summary>
public sealed class SorophyRelationshipChange
{
    /// <summary>
    /// Gets the structural change category.
    /// </summary>
    public GraphChangeKind Kind { get; }

    /// <summary>
    /// Gets the canonical identity of the relationship.
    /// </summary>
    public Guid RelationshipId { get; }

    /// <summary>
    /// Gets the observable relationship state in the baseline snapshot, or <see langword="null"/> if added.
    /// </summary>
    public ISorophySnapshotRelationship? Before { get; }

    /// <summary>
    /// Gets the observable relationship state in the target snapshot, or <see langword="null"/> if removed.
    /// </summary>
    public ISorophySnapshotRelationship? After { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SorophyRelationshipChange"/>.
    /// </summary>
    /// <param name="kind">The structural change classification.</param>
    /// <param name="relationshipId">The canonical identifier of the relationship.</param>
    /// <param name="before">The baseline relationship state, if present.</param>
    /// <param name="after">The target relationship state, if present.</param>
    public SorophyRelationshipChange(
        GraphChangeKind kind,
        Guid relationshipId,
        ISorophySnapshotRelationship? before,
        ISorophySnapshotRelationship? after)
    {
        Kind = kind;
        RelationshipId = relationshipId;
        Before = before;
        After = after;
    }
}

