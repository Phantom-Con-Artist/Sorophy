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

namespace Sorophy.Engine.Graph.History;

/// <summary>
/// Maintains the append-only historical facts of a single relationship.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="SorophyRelationshipHistory"/> belongs to exactly one
/// relationship identified by <see cref="RelationshipId"/>.
/// </para>
///
/// <para>
/// Historical facts are appended to the history but cannot be removed
/// or replaced through this API. Each fact is expected to remain immutable
/// once created.
/// </para>
///
/// <para>
/// This type stores historical information only. It does not execute
/// events, apply evolution operations, mutate relationships, or determine
/// when a historical fact should be created.
/// </para>
///
/// <para>
/// Historical ordering is the order in which facts are appended. This
/// type does not attempt to interpret or compare time
/// values. Temporal ordering and event semantics belong to higher layers.
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
    /// Thrown when the exact same fact instance has already been added.
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

        /*
         * History is append-only, but accidentally recording the very same
         * fact object twice is still a programming error.
         *
         * ReferenceEquals is intentional here. The rule concerns the exact
         * fact instance, not value equality between separate fact objects.
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

        _facts.Add(
            fact);
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