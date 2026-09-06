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
/// Immutable implementation of a relationship projection in a graph snapshot.
/// </summary>
public sealed class SorophySnapshotRelationship : ISorophySnapshotRelationship
{
    public Guid Id { get; }
    public Guid SourceId { get; }
    public Guid TargetId { get; }
    public string Type { get; }
    public SorophyTime? ValidFrom { get; }
    public SorophyTime? ValidTill { get; }
    public IReadOnlyDictionary<string, SorophyProperty> Properties { get; }

    public SorophySnapshotRelationship(
        Guid id,
        Guid sourceId,
        Guid targetId,
        string type,
        SorophyTime? validFrom,
        SorophyTime? validTill,
        IReadOnlyDictionary<string, SorophyProperty> properties)
    {
        Id = id;
        SourceId = sourceId;
        TargetId = targetId;
        Type = type ?? string.Empty;
        ValidFrom = validFrom;
        ValidTill = validTill;
        Properties = properties ?? ReadOnlyDictionary<string, SorophyProperty>.Empty;
    }
}

