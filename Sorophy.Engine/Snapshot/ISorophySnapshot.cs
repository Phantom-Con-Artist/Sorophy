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
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Represents an immutable point-in-time structural projection of a Sorophy graph.
/// </summary>
public interface ISorophySnapshot
{
    /// <summary>
    /// Gets the authoritative temporal coordinate at which this snapshot was materialized.
    /// </summary>
    SorophyTime SnapshotTime { get; }

    /// <summary>
    /// Gets an immutable dictionary of entities active at the snapshot coordinate.
    /// </summary>
    IReadOnlyDictionary<Guid, ISorophySnapshotEntity> Entities { get; }

    /// <summary>
    /// Gets an immutable dictionary of relationships active at the snapshot coordinate.
    /// </summary>
    IReadOnlyDictionary<Guid, ISorophySnapshotRelationship> Relationships { get; }

    /// <summary>
    /// Gets the number of entities in the snapshot.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Gets the number of relationships in the snapshot.
    /// </summary>
    int RelationshipCount { get; }

    /// <summary>
    /// Determines whether an entity with the specified ID exists in the snapshot.
    /// </summary>
    bool ContainsEntity(Guid entityId);

    /// <summary>
    /// Determines whether a relationship with the specified ID exists in the snapshot.
    /// </summary>
    bool ContainsRelationship(Guid relationshipId);

    /// <summary>
    /// Retrieves the snapshot entity with the specified ID, or null if it does not exist.
    /// </summary>
    ISorophySnapshotEntity? GetEntity(Guid entityId);

    /// <summary>
    /// Retrieves the snapshot relationship with the specified ID, or null if it does not exist.
    /// </summary>
    ISorophySnapshotRelationship? GetRelationship(Guid relationshipId);

    /// <summary>
    /// Gets all outbound relationships originating from the specified entity in the snapshot.
    /// </summary>
    IEnumerable<ISorophySnapshotRelationship> GetOutboundRelationships(Guid entityId);

    /// <summary>
    /// Gets all inbound relationships terminating at the specified entity in the snapshot.
    /// </summary>
    IEnumerable<ISorophySnapshotRelationship> GetInboundRelationships(Guid entityId);
}

