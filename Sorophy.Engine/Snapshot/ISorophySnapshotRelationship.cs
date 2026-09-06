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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Represents an immutable point-in-time projection of a relationship within a graph snapshot.
/// </summary>
public interface ISorophySnapshotRelationship
{
    /// <summary>
    /// Gets the unique identity of the relationship.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the source entity identity.
    /// </summary>
    Guid SourceId { get; }

    /// <summary>
    /// Gets the target entity identity.
    /// </summary>
    Guid TargetId { get; }

    /// <summary>
    /// Gets the semantic type of the relationship.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the time from which the relationship state was semantically valid.
    /// </summary>
    SorophyTime? ValidFrom { get; }

    /// <summary>
    /// Gets the time until which the relationship state was semantically valid.
    /// </summary>
    SorophyTime? ValidTill { get; }

    /// <summary>
    /// Gets an immutable view of user-defined properties associated with the relationship.
    /// </summary>
    IReadOnlyDictionary<string, SorophyProperty> Properties { get; }
}

