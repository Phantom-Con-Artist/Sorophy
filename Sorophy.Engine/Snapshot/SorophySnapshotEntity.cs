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
using System.Collections.ObjectModel;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Immutable implementation of an entity projection in a graph snapshot.
/// </summary>
public sealed class SorophySnapshotEntity : ISorophySnapshotEntity
{
    public Guid Id { get; }
    public string? Name { get; }
    public string? Type { get; }
    public bool IsEvent { get; }
    public SorophyTime? OccurredAt { get; }
    public string? Description { get; }
    public IReadOnlyDictionary<string, SorophyProperty> Properties { get; }
    public IReadOnlySet<string> Tags { get; }
    public IReadOnlyDictionary<string, SorophyEntityDocument> Documents { get; }

    public SorophySnapshotEntity(
        Guid id,
        string? name,
        string? type,
        bool isEvent,
        SorophyTime? occurredAt,
        string? description,
        IReadOnlyDictionary<string, SorophyProperty> properties,
        IReadOnlySet<string> tags,
        IReadOnlyDictionary<string, SorophyEntityDocument> documents)
    {
        Id = id;
        Name = name;
        Type = type;
        IsEvent = isEvent;
        OccurredAt = occurredAt;
        Description = description;
        Properties = properties ?? ReadOnlyDictionary<string, SorophyProperty>.Empty;
        Tags = tags ?? new HashSet<string>(StringComparer.Ordinal);
        Documents = documents ?? ReadOnlyDictionary<string, SorophyEntityDocument>.Empty;
    }
}

