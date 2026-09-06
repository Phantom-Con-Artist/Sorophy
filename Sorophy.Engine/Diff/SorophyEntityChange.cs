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
/// Represents a structural change to an entity between two graph snapshots.
/// </summary>
public sealed class SorophyEntityChange
{
    /// <summary>
    /// Gets the structural change category.
    /// </summary>
    public GraphChangeKind Kind { get; }

    /// <summary>
    /// Gets the canonical identity of the entity.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the observable entity state in the baseline snapshot, or <see langword="null"/> if added.
    /// </summary>
    public ISorophySnapshotEntity? Before { get; }

    /// <summary>
    /// Gets the observable entity state in the target snapshot, or <see langword="null"/> if removed.
    /// </summary>
    public ISorophySnapshotEntity? After { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SorophyEntityChange"/>.
    /// </summary>
    /// <param name="kind">The structural change classification.</param>
    /// <param name="entityId">The canonical identifier of the entity.</param>
    /// <param name="before">The baseline entity state, if present.</param>
    /// <param name="after">The target entity state, if present.</param>
    public SorophyEntityChange(
        GraphChangeKind kind,
        Guid entityId,
        ISorophySnapshotEntity? before,
        ISorophySnapshotEntity? after)
    {
        Kind = kind;
        EntityId = entityId;
        Before = before;
        After = after;
    }
}

