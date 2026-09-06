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
/// Represents an immutable point-in-time projection of an entity within a graph snapshot.
/// </summary>
public interface ISorophySnapshotEntity
{
    /// <summary>
    /// Gets the unique identity of the entity.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the entity.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the type classification of the entity.
    /// </summary>
    string? Type { get; }

    /// <summary>
    /// Gets whether this entity is classified as an Event.
    /// </summary>
    bool IsEvent { get; }

    /// <summary>
    /// Gets the authoritative occurrence coordinate when the entity is an Event.
    /// </summary>
    SorophyTime? OccurredAt { get; }

    /// <summary>
    /// Gets the description of the entity.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets an immutable view of the user-defined structured properties.
    /// </summary>
    IReadOnlyDictionary<string, SorophyProperty> Properties { get; }

    /// <summary>
    /// Gets an immutable view of user-defined tags assigned to the entity.
    /// </summary>
    IReadOnlySet<string> Tags { get; }

    /// <summary>
    /// Gets an immutable view of documents embedded in the entity.
    /// </summary>
    IReadOnlyDictionary<string, SorophyEntityDocument> Documents { get; }
}

