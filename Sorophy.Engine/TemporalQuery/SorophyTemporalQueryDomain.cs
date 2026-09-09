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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.TemporalQuery;

/// <summary>
/// Authoritative implementation of the Temporal Query Domain (TQD) for Sorophy graphs.
/// </summary>
/// <remarks>
/// <para>
/// Delegates point-in-time materialization directly to <see cref="SorophyGraph.CreateSnapshot(SorophyTime)"/>
/// and queries append-only relationship histories and event provenance directly.
/// </para>
/// <para>
/// Guarantees deterministic presentation ordering across result sets without asserting temporal
/// causality between co-temporal transitions.
/// </para>
/// </remarks>
public sealed class SorophyTemporalQueryDomain : ITemporalQueryDomain
{
    private readonly SorophyGraph _graph;

    /// <summary>
    /// Initializes a new instance of <see cref="SorophyTemporalQueryDomain"/> bound to the specified graph.
    /// </summary>
    /// <param name="graph">The graph instance to query.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
    public SorophyTemporalQueryDomain(
        SorophyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(
            graph);

        _graph = graph;
    }

    /*
     * =============================================================
     * 1. POINT-IN-TIME SNAPSHOT QUERIES
     * =============================================================
     */

    /// <inheritdoc />
    public ISorophySnapshot At(
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        return _graph.CreateSnapshot(
            time);
    }

    /// <inheritdoc />
    public ISorophySnapshot At(
        SorophyEntity eventEntity)
    {
        ArgumentNullException.ThrowIfNull(
            eventEntity);

        return _graph.CreateSnapshot(
            eventEntity);
    }

    /*
     * =============================================================
     * 2. POINT-IN-TIME ENTITY LOOKUPS
     * =============================================================
     */

    /// <inheritdoc />
    public bool EntityExistsAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (entityId == Guid.Empty)
        {
            return false;
        }

        return _graph.EntityExistsAt(
            entityId,
            time);
    }

    /// <inheritdoc />
    public ISorophySnapshotEntity? GetEntityAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (entityId == Guid.Empty || !_graph.EntityExistsAt(entityId, time))
        {
            return null;
        }

        return _graph
            .CreateSnapshot(time)
            .GetEntity(entityId);
    }

    /*
     * =============================================================
     * 3. POINT-IN-TIME RELATIONSHIP LOOKUPS
     * =============================================================
     */

    /// <inheritdoc />
    public bool RelationshipExistsAt(
        Guid relationshipId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (relationshipId == Guid.Empty)
        {
            return false;
        }

        return _graph.RelationshipExistsAt(
            relationshipId,
            time);
    }

    /// <inheritdoc />
    public ISorophySnapshotRelationship? GetRelationshipAt(
        Guid relationshipId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (relationshipId == Guid.Empty || !_graph.RelationshipExistsAt(relationshipId, time))
        {
            return null;
        }

        return _graph
            .CreateSnapshot(time)
            .GetRelationship(relationshipId);
    }

    /// <inheritdoc />
    public IEnumerable<ISorophySnapshotRelationship> GetOutboundRelationshipsAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (entityId == Guid.Empty || !_graph.EntityExistsAt(entityId, time))
        {
            return [];
        }

        return _graph
            .CreateSnapshot(time)
            .GetOutboundRelationships(entityId);
    }

    /// <inheritdoc />
    public IEnumerable<ISorophySnapshotRelationship> GetInboundRelationshipsAt(
        Guid entityId,
        SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(
            time);

        if (entityId == Guid.Empty || !_graph.EntityExistsAt(entityId, time))
        {
            return [];
        }

        return _graph
            .CreateSnapshot(time)
            .GetInboundRelationships(entityId);
    }

    /*
     * =============================================================
     * 4. INTERVAL QUERIES
     * =============================================================
     */

    /// <inheritdoc />
    public IReadOnlyList<SorophyRelationshipFact> GetFactsInInterval(
        SorophyTime from,
        SorophyTime till)
    {
        ValidateInterval(
            from,
            till);

        var matching = new List<IndexedFact>();

        foreach (var pair in _graph.RelationshipHistories)
        {
            var facts = pair.Value.Facts;
            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                if (SorophyTime.Compare(fact.At, from) >= 0 &&
                    SorophyTime.Compare(fact.At, till) <= 0)
                {
                    matching.Add(
                        new IndexedFact(fact, i));
                }
            }
        }

        SortIndexedFacts(
            matching);

        var result = new List<SorophyRelationshipFact>(matching.Count);
        for (int i = 0; i < matching.Count; i++)
        {
            result.Add(matching[i].Fact);
        }

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> GetModifiedRelationshipIds(
        SorophyTime from,
        SorophyTime till)
    {
        ValidateInterval(
            from,
            till);

        var modifiedIds = new HashSet<Guid>();

        foreach (var pair in _graph.RelationshipHistories)
        {
            var facts = pair.Value.Facts;
            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                if (SorophyTime.Compare(fact.At, from) >= 0 &&
                    SorophyTime.Compare(fact.At, till) <= 0)
                {
                    modifiedIds.Add(pair.Key);
                    break;
                }
            }
        }

        var list = new List<Guid>(modifiedIds);
        list.Sort();
        return list.AsReadOnly();
    }

    /*
     * =============================================================
     * 5. RELATIONSHIP HISTORY QUERIES
     * =============================================================
     */

    /// <inheritdoc />
    public SorophyRelationshipHistory? GetRelationshipHistory(
        Guid relationshipId)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        return _graph.TryGetRelationshipHistory(relationshipId, out var history)
            ? history
            : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<SorophyRelationshipFact> GetRelationshipFactsInInterval(
        Guid relationshipId,
        SorophyTime from,
        SorophyTime till)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        ValidateInterval(
            from,
            till);

        if (!_graph.TryGetRelationshipHistory(relationshipId, out var history) ||
            history is null ||
            history.Facts.Count == 0)
        {
            return [];
        }

        var matching = new List<IndexedFact>();
        var facts = history.Facts;
        for (int i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            if (SorophyTime.Compare(fact.At, from) >= 0 &&
                SorophyTime.Compare(fact.At, till) <= 0)
            {
                matching.Add(
                    new IndexedFact(fact, i));
            }
        }

        SortIndexedFacts(
            matching);

        var result = new List<SorophyRelationshipFact>(matching.Count);
        for (int i = 0; i < matching.Count; i++)
        {
            result.Add(matching[i].Fact);
        }

        return result.AsReadOnly();
    }

    /*
     * =============================================================
     * 6. PROVENANCE / EVENT QUERIES
     * =============================================================
     */

    /// <inheritdoc />
    public IReadOnlyList<SorophyRelationshipFact> GetFactsByEvent(
        Guid eventEntityId)
    {
        if (eventEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event entity ID cannot be empty.",
                nameof(eventEntityId));
        }

        if (_graph.TryGetEntity(eventEntityId, out var entity) &&
            entity is not null &&
            !entity.IsEvent)
        {
            throw new ArgumentException(
                $"Entity '{eventEntityId}' is not classified as an Event (Type is '{entity.Type}').",
                nameof(eventEntityId));
        }

        var matching = new List<IndexedFact>();

        foreach (var pair in _graph.RelationshipHistories)
        {
            var facts = pair.Value.Facts;
            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                if (fact.EventEntityId == eventEntityId)
                {
                    matching.Add(
                        new IndexedFact(fact, i));
                }
            }
        }

        SortIndexedFacts(
            matching);

        var result = new List<SorophyRelationshipFact>(matching.Count);
        for (int i = 0; i < matching.Count; i++)
        {
            result.Add(matching[i].Fact);
        }

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<SorophyRelationshipFact> GetFactsByEvent(
        SorophyEntity eventEntity)
    {
        ArgumentNullException.ThrowIfNull(
            eventEntity);

        if (!eventEntity.IsEvent)
        {
            throw new ArgumentException(
                $"Entity '{eventEntity.Id}' is not classified as an Event (Type is '{eventEntity.Type}').",
                nameof(eventEntity));
        }

        return GetFactsByEvent(
            eventEntity.Id);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> GetRelationshipsEvolvedByEvent(
        Guid eventEntityId)
    {
        if (eventEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event entity ID cannot be empty.",
                nameof(eventEntityId));
        }

        if (_graph.TryGetEntity(eventEntityId, out var entity) &&
            entity is not null &&
            !entity.IsEvent)
        {
            throw new ArgumentException(
                $"Entity '{eventEntityId}' is not classified as an Event (Type is '{entity.Type}').",
                nameof(eventEntityId));
        }

        var evolvedIds = new HashSet<Guid>();

        foreach (var pair in _graph.RelationshipHistories)
        {
            var facts = pair.Value.Facts;
            for (int i = 0; i < facts.Count; i++)
            {
                var fact = facts[i];
                if (fact.EventEntityId == eventEntityId)
                {
                    evolvedIds.Add(pair.Key);
                    break;
                }
            }
        }

        var list = new List<Guid>(evolvedIds);
        list.Sort();
        return list.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> GetRelationshipsEvolvedByEvent(
        SorophyEntity eventEntity)
    {
        ArgumentNullException.ThrowIfNull(
            eventEntity);

        if (!eventEntity.IsEvent)
        {
            throw new ArgumentException(
                $"Entity '{eventEntity.Id}' is not classified as an Event (Type is '{eventEntity.Type}').",
                nameof(eventEntity));
        }

        return GetRelationshipsEvolvedByEvent(
            eventEntity.Id);
    }

    /*
     * =============================================================
     * PRIVATE VALIDATION & DETERMINISTIC SORTING HELPERS
     * =============================================================
     */

    private static void ValidateInterval(
        SorophyTime from,
        SorophyTime till)
    {
        ArgumentNullException.ThrowIfNull(
            from);

        ArgumentNullException.ThrowIfNull(
            till);

        if (SorophyTime.Compare(from, till) > 0)
        {
            throw new ArgumentException(
                $"Interval start 'from' ({from}) cannot succeed interval end 'till' ({till}).",
                nameof(from));
        }
    }

    private static void SortIndexedFacts(
        List<IndexedFact> facts)
    {
        facts.Sort(static (a, b) =>
        {
            int timeComparison = SorophyTime.Compare(a.Fact.At, b.Fact.At);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            int relComparison = a.Fact.RelationshipId.CompareTo(b.Fact.RelationshipId);
            if (relComparison != 0)
            {
                return relComparison;
            }

            return a.FactIndex.CompareTo(b.FactIndex);
        });
    }

    private readonly record struct IndexedFact(
        SorophyRelationshipFact Fact,
        int FactIndex);
}

