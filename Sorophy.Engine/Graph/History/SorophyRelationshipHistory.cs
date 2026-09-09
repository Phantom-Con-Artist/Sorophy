// Sorophyis Project
// Copyright (C) 2026 Phantom-Con-Artist
//
// This file is part of the Sorophyis Project.
//
// The Sorophyis Project is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// The Sorophyis Project is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Maintains the authoritative historical facts of a single relationship.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="SorophyRelationshipHistory"/> belongs to exactly one
/// relationship identified by <see cref="RelationshipId"/>.
/// </para>
/// <para>
/// Historical facts are recorded as history is established, or explicitly
/// removed/reverted when an authored change is undone by the author (for instance,
/// reversing a retirement). It is not an audit log of author actions and contains
/// no "Unretired" records.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipHistory
    : IReadOnlyCollection<SorophyRelationshipFact>
{
    private readonly List<SorophyRelationshipFact> _facts =
        new();

    private readonly IReadOnlyList<SorophyRelationshipFact> _readOnlyFacts;

    /// <summary>
    /// Gets the identity of the relationship whose history is maintained.
    /// </summary>
    public Guid RelationshipId { get; }

    /// <summary>
    /// Gets the historical facts in insertion order.
    /// </summary>
    /// <remarks>
    /// The returned collection is read-only and cannot be used to mutate
    /// the underlying history.
    /// </remarks>
    public IReadOnlyList<SorophyRelationshipFact> Facts =>
        _readOnlyFacts;

    /// <summary>
    /// Gets the number of historical facts currently recorded.
    /// </summary>
    public int Count =>
        _facts.Count;

    /// <summary>
    /// Gets the established creation fact for this relationship, if any.
    /// </summary>
    public SorophyRelationshipFact? CreationFact =>
        _facts.FirstOrDefault(f => f.Kind == SorophyRelationshipFactKind.Created);

    /// <summary>
    /// Gets the single established retirement fact for this relationship, if any.
    /// </summary>
    public SorophyRelationshipFact? RetirementFact =>
        _facts.SingleOrDefault(f => f.Kind == SorophyRelationshipFactKind.Retired);

    /// <summary>
    /// Gets the temporal coordinate at which the relationship was created, if recorded.
    /// </summary>
    public SorophyTime? CreatedAt => CreationFact?.At;

    /// <summary>
    /// Gets the temporal coordinate at which the relationship was retired, if retired.
    /// </summary>
    public SorophyTime? RetiredAt => RetirementFact?.At;

    /// <summary>
    /// Gets whether the relationship has an established retirement fact.
    /// </summary>
    public bool IsRetired => RetirementFact is not null;

    /// <summary>
    /// Initializes a new relationship history.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity of the relationship whose history is being maintained.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationshipId"/> is empty.
    /// </exception>
    public SorophyRelationshipHistory(
        Guid relationshipId)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        RelationshipId =
            relationshipId;

        _readOnlyFacts =
            _facts.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the relationship's own lifecycle exists at the specified temporal coordinate.
    /// </summary>
    /// <remarks>
    /// Note: This checks relationship lifecycle only. For full existence including endpoint checks,
    /// use <see cref="SorophyGraph.RelationshipExistsAt(Guid, SorophyTime)"/>.
    /// </remarks>
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
    /// Evaluates the relationship's own temporal lifecycle status at the specified coordinate.
    /// </summary>
    /// <remarks>
    /// Note: This checks relationship lifecycle only. For endpoint-aware status,
    /// use <see cref="SorophyGraph.GetRelationshipLifecycleStatus(Guid, SorophyTime)"/>.
    /// </remarks>
    /// <param name="time">The temporal coordinate to evaluate.</param>
    /// <returns>
    /// <see cref="SorophyRelationshipLifecycleStatus.Uncreated"/> if prior to creation or on an incomparable timeline;
    /// <see cref="SorophyRelationshipLifecycleStatus.Retired"/> if at or after retirement;
    /// otherwise, <see cref="SorophyRelationshipLifecycleStatus.Active"/>.
    /// </returns>
    public SorophyRelationshipLifecycleStatus GetStatusAt(SorophyTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        if (CreatedAt is not null)
        {
            if (!SorophyTime.CanCompare(time, CreatedAt))
            {
                return SorophyRelationshipLifecycleStatus.Uncreated;
            }

            if (SorophyTime.Compare(time, CreatedAt) < 0)
            {
                return SorophyRelationshipLifecycleStatus.Uncreated;
            }
        }

        if (RetiredAt is not null)
        {
            if (SorophyTime.CanCompare(time, RetiredAt) &&
                SorophyTime.Compare(time, RetiredAt) >= 0)
            {
                return SorophyRelationshipLifecycleStatus.Retired;
            }
        }

        return SorophyRelationshipLifecycleStatus.Active;
    }

    private long _nextSequence = 1;

    /// <summary>
    /// Appends a historical fact to this relationship's history.
    /// </summary>
    /// <param name="fact">
    /// The historical fact to append.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fact"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the fact belongs to a different relationship.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to add duplicate creation/retirement facts or the exact same fact instance.
    /// </exception>
    public void Add(
        SorophyRelationshipFact fact)
    {
        ArgumentNullException.ThrowIfNull(
            fact);

        if (fact.RelationshipId !=
            RelationshipId)
        {
            throw new ArgumentException(
                $"Historical fact belongs to relationship " +
                $"'{fact.RelationshipId}', but this history belongs to " +
                $"relationship '{RelationshipId}'.",
                nameof(fact));
        }

        if (fact.Kind == SorophyRelationshipFactKind.Created && CreationFact is not null)
        {
            throw new InvalidOperationException(
                $"Relationship '{RelationshipId}' already has an established creation fact at '{CreationFact.At}'.");
        }

        if (fact.Kind == SorophyRelationshipFactKind.Retired && RetirementFact is not null)
        {
            throw new InvalidOperationException(
                $"Relationship '{RelationshipId}' already has an established retirement fact at '{RetirementFact.At}'.");
        }

        /*
         * Accidental recording of the very same fact object twice is a programming error.
         */
        foreach (var existing in
                 _facts)
        {
            if (ReferenceEquals(
                    existing,
                    fact))
            {
                throw new InvalidOperationException(
                    "The specified historical fact has already been added " +
                    "to this relationship history.");
            }
        }

        if (fact.Sequence == 0)
        {
            fact.Sequence = _nextSequence++;
        }
        else if (fact.Sequence >= _nextSequence)
        {
            _nextSequence = fact.Sequence + 1;
        }

        _facts.Add(
            fact);
    }

    /// <summary>
    /// Removes a specific authored temporal fact from history.
    /// </summary>
    internal bool Remove(SorophyRelationshipFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        return _facts.Remove(fact);
    }

    /// <summary>
    /// Removes a specific authored temporal fact from history.
    /// </summary>
    /// <param name="kind">The kind of fact to remove.</param>
    /// <param name="at">The temporal coordinate of the fact to remove.</param>
    /// <returns><see langword="true"/> if the fact was found and removed; otherwise, <see langword="false"/>.</returns>
    internal bool RemoveFact(SorophyRelationshipFactKind kind, SorophyTime at)
    {
        ArgumentNullException.ThrowIfNull(at);

        for (int i = 0; i < _facts.Count; i++)
        {
            if (_facts[i].Kind == kind && _facts[i].At.Equals(at))
            {
                _facts.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether this history contains the specified historical
    /// fact instance.
    /// </summary>
    /// <param name="fact">
    /// The historical fact to search for.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the exact fact instance is present;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(
        SorophyRelationshipFact fact)
    {
        ArgumentNullException.ThrowIfNull(
            fact);

        foreach (var existing in
                 _facts)
        {
            if (ReferenceEquals(
                    existing,
                    fact))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator over the historical facts in insertion order.
    /// </summary>
    public IEnumerator<SorophyRelationshipFact> GetEnumerator()
    {
        return _facts.GetEnumerator();
    }

    /// <summary>
    /// Returns a non-generic enumerator over the historical facts.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}