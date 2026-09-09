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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Maintains the authoritative ordered collection of currently authored temporal facts for an entity.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SorophyEntityHistory"/> belongs to exactly one entity identified by <see cref="EntityId"/>.
/// It represents the authored temporal world model for that entity.
/// </para>
/// <para>
/// Temporal facts may be added as history is established, or explicitly removed/reverted when
/// an authored change is undone by the author (for instance, reversing a retirement).
/// It is not an audit log of author actions and contains no "Unretired" records.
/// </para>
/// </remarks>
public sealed class SorophyEntityHistory : IReadOnlyCollection<SorophyEntityFact>
{
    private readonly List<SorophyEntityFact> _facts = new();
    private readonly IReadOnlyList<SorophyEntityFact> _readOnlyFacts;

    /// <summary>
    /// Gets the identity of the entity whose temporal history is maintained.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the authored temporal facts for this entity.
    /// </summary>
    public IReadOnlyList<SorophyEntityFact> Facts => _readOnlyFacts;

    /// <summary>
    /// Gets the total number of currently established facts.
    /// </summary>
    public int Count => _facts.Count;

    /// <summary>
    /// Gets the established creation fact for this entity, if any.
    /// </summary>
    public SorophyEntityFact? CreationFact =>
        _facts.FirstOrDefault(f => f.Kind == SorophyEntityFactKind.Created);

    /// <summary>
    /// Gets the single established retirement fact for this entity, if any.
    /// </summary>
    public SorophyEntityFact? RetirementFact =>
        _facts.SingleOrDefault(f => f.Kind == SorophyEntityFactKind.Retired);

    /// <summary>
    /// Gets the temporal coordinate at which the entity was created, if recorded.
    /// </summary>
    public SorophyTime? CreatedAt => CreationFact?.At;

    /// <summary>
    /// Gets the temporal coordinate at which the entity was retired, if retired.
    /// </summary>
    public SorophyTime? RetiredAt => RetirementFact?.At;

    /// <summary>
    /// Gets whether the entity has an established retirement fact.
    /// </summary>
    public bool IsRetired => RetirementFact is not null;

    /// <summary>
    /// Initializes a new entity temporal history container.
    /// </summary>
    /// <param name="entityId">The identity of the entity whose history is maintained.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/> is empty.</exception>
    public SorophyEntityHistory(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity ID cannot be empty.",
                nameof(entityId));
        }

        EntityId = entityId;
        _readOnlyFacts = _facts.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the entity exists in the temporal projection at the specified coordinate.
    /// </summary>
    /// <param name="time">The temporal coordinate to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> when the coordinate is at or after creation and strictly prior to retirement;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool ExistsAt(SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (CreatedAt is not null)
        {
            if (!SorophyTime.CanCompare(time, CreatedAt))
            {
                return false;
            }

            if (SorophyTime.Compare(time, CreatedAt) < 0)
            {
                return false;
            }
        }

        if (RetiredAt is not null)
        {
            if (SorophyTime.CanCompare(time, RetiredAt) &&
                SorophyTime.Compare(time, RetiredAt) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates the temporal lifecycle status of the entity at the specified coordinate.
    /// </summary>
    /// <param name="time">The temporal coordinate to evaluate.</param>
    /// <returns>
    /// <see cref="SorophyEntityLifecycleStatus.Uncreated"/> if prior to creation or on an incomparable timeline;
    /// <see cref="SorophyEntityLifecycleStatus.Retired"/> if at or after retirement;
    /// otherwise, <see cref="SorophyEntityLifecycleStatus.Active"/>.
    /// </returns>
    public SorophyEntityLifecycleStatus GetStatusAt(SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (CreatedAt is not null)
        {
            if (!SorophyTime.CanCompare(time, CreatedAt))
            {
                return SorophyEntityLifecycleStatus.Uncreated;
            }

            if (SorophyTime.Compare(time, CreatedAt) < 0)
            {
                return SorophyEntityLifecycleStatus.Uncreated;
            }
        }

        if (RetiredAt is not null)
        {
            if (SorophyTime.CanCompare(time, RetiredAt) &&
                SorophyTime.Compare(time, RetiredAt) >= 0)
            {
                return SorophyEntityLifecycleStatus.Retired;
            }
        }

        return SorophyEntityLifecycleStatus.Active;
    }

    private long _nextSequence = 1;

    /// <summary>
    /// Adds an authored temporal fact to this entity's history.
    /// </summary>
    /// <param name="fact">The fact to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fact"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fact"/> belongs to another entity.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to add a duplicate creation or retirement fact.
    /// </exception>
    internal void Add(SorophyEntityFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        if (fact.EntityId != EntityId)
        {
            throw new ArgumentException(
                $"Historical fact belongs to entity '{fact.EntityId}', but this history belongs to entity '{EntityId}'.",
                nameof(fact));
        }

        if (fact.Kind == SorophyEntityFactKind.Created && CreationFact is not null)
        {
            throw new InvalidOperationException(
                $"Entity '{EntityId}' already has an established creation fact at '{CreationFact.At}'.");
        }

        if (fact.Kind == SorophyEntityFactKind.Retired && RetirementFact is not null)
        {
            throw new InvalidOperationException(
                $"Entity '{EntityId}' already has an established retirement fact at '{RetirementFact.At}'.");
        }

        if (fact.Sequence == 0)
        {
            fact.Sequence = _nextSequence++;
        }
        else if (fact.Sequence >= _nextSequence)
        {
            _nextSequence = fact.Sequence + 1;
        }

        _facts.Add(fact);
        SortFacts();
    }

    /// <summary>
    /// Removes a specific authored temporal fact from history.
    /// </summary>
    /// <param name="kind">The kind of fact to remove.</param>
    /// <param name="at">The temporal coordinate of the fact to remove.</param>
    /// <returns><see langword="true"/> if the fact was found and removed; otherwise, <see langword="false"/>.</returns>
    internal bool RemoveFact(SorophyEntityFactKind kind, SorophyTime at)
    {
        ArgumentNullException.ThrowIfNull(at);

        for (int i = 0; i < _facts.Count; i++)
        {
            if (_facts[i].Kind == kind && _facts[i].At.Equals(at))
            {
                _facts.RemoveAt(i);
                long maxSeq = 0;
                for (int j = 0; j < _facts.Count; j++)
                {
                    if (_facts[j].Sequence > maxSeq)
                    {
                        maxSeq = _facts[j].Sequence;
                    }
                }
                _nextSequence = maxSeq + 1;
                SortFacts();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes a specific authored temporal fact from history.
    /// </summary>
    internal bool Remove(SorophyEntityFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        var removed = _facts.Remove(fact);
        if (removed)
        {
            long maxSeq = 0;
            for (int i = 0; i < _facts.Count; i++)
            {
                if (_facts[i].Sequence > maxSeq)
                {
                    maxSeq = _facts[i].Sequence;
                }
            }
            _nextSequence = maxSeq + 1;
            SortFacts();
        }

        return removed;
    }

    private void SortFacts()
    {
        _facts.Sort((a, b) =>
        {
            if (SorophyTime.CanCompare(a.At, b.At))
            {
                var cmp = SorophyTime.Compare(a.At, b.At);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            else
            {
                var timelineCmp = string.Compare(a.At.Timeline, b.At.Timeline, StringComparison.Ordinal);
                if (timelineCmp != 0)
                {
                    return timelineCmp;
                }
            }

            var seqCmp = a.Sequence.CompareTo(b.Sequence);
            if (seqCmp != 0)
            {
                return seqCmp;
            }

            var kindCmp = a.Kind.CompareTo(b.Kind);
            if (kindCmp != 0)
            {
                return kindCmp;
            }

            return string.Compare(a.PropertyName, b.PropertyName, StringComparison.Ordinal);
        });
    }

    /// <inheritdoc />
    public IEnumerator<SorophyEntityFact> GetEnumerator() => _facts.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
