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
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Immutable implementation of a Sorophy graph snapshot materialized at an authoritative temporal coordinate.
/// </summary>
public sealed class SorophySnapshot : ISorophySnapshot
{
    private static readonly ISorophySnapshotRelationship[] EmptyRelationships =
        Array.Empty<ISorophySnapshotRelationship>();

    private readonly Dictionary<Guid, ISorophySnapshotEntity> _entities;
    private readonly Dictionary<Guid, ISorophySnapshotRelationship> _relationships;
    private readonly Dictionary<Guid, List<ISorophySnapshotRelationship>> _outbound;
    private readonly Dictionary<Guid, List<ISorophySnapshotRelationship>> _inbound;

    private readonly ReadOnlyDictionary<Guid, ISorophySnapshotEntity> _readOnlyEntities;
    private readonly ReadOnlyDictionary<Guid, ISorophySnapshotRelationship> _readOnlyRelationships;

    public SorophyTime SnapshotTime { get; }

    public IReadOnlyDictionary<Guid, ISorophySnapshotEntity> Entities =>
        _readOnlyEntities;

    public IReadOnlyDictionary<Guid, ISorophySnapshotRelationship> Relationships =>
        _readOnlyRelationships;

    public int EntityCount =>
        _entities.Count;

    public int RelationshipCount =>
        _relationships.Count;

    public SorophySnapshot(
        SorophyTime snapshotTime,
        Dictionary<Guid, ISorophySnapshotEntity> entities,
        Dictionary<Guid, ISorophySnapshotRelationship> relationships,
        Dictionary<Guid, List<ISorophySnapshotRelationship>> outbound,
        Dictionary<Guid, List<ISorophySnapshotRelationship>> inbound)
    {
        SnapshotTime = snapshotTime ?? throw new ArgumentNullException(nameof(snapshotTime));
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        _outbound = outbound ?? throw new ArgumentNullException(nameof(outbound));
        _inbound = inbound ?? throw new ArgumentNullException(nameof(inbound));

        _readOnlyEntities = new ReadOnlyDictionary<Guid, ISorophySnapshotEntity>(_entities);
        _readOnlyRelationships = new ReadOnlyDictionary<Guid, ISorophySnapshotRelationship>(_relationships);
    }

    public bool ContainsEntity(Guid entityId) =>
        _entities.ContainsKey(entityId);

    public bool ContainsRelationship(Guid relationshipId) =>
        _relationships.ContainsKey(relationshipId);

    public ISorophySnapshotEntity? GetEntity(Guid entityId) =>
        _entities.GetValueOrDefault(entityId);

    public ISorophySnapshotRelationship? GetRelationship(Guid relationshipId) =>
        _relationships.GetValueOrDefault(relationshipId);

    public IEnumerable<ISorophySnapshotRelationship> GetOutboundRelationships(Guid entityId)
    {
        if (_outbound.TryGetValue(entityId, out var relationships))
        {
            return relationships;
        }

        return EmptyRelationships;
    }

    public IEnumerable<ISorophySnapshotRelationship> GetInboundRelationships(Guid entityId)
    {
        if (_inbound.TryGetValue(entityId, out var relationships))
        {
            return relationships;
        }

        return EmptyRelationships;
    }
}

