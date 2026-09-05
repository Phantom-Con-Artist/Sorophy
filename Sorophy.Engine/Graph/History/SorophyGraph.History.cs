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
using System.Collections.Generic;
using Sorophy.Engine.Graph.History;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /*
     * =============================================================
     * RELATIONSHIP HISTORY
     * =============================================================
     *
     * Historical facts are separate from the canonical relationship
     * store.
     *
     * A relationship consumes historical-memory storage only after
     * its first historical fact is recorded.
     */

    private readonly Dictionary<Guid, SorophyRelationshipHistory>
        _relationshipHistories =
            new();

    private readonly HashSet<Guid>
        _retiredRelationshipIds =
            new();

    /*
     * =============================================================
     * PUBLIC HISTORY ACCESS
     * =============================================================
     */

    /// <summary>
    /// Gets the historical relationship histories currently retained
    /// by the graph.
    /// </summary>
    /// <remarks>
    /// A relationship appears here only after at least one historical
    /// fact has been recorded for it.
    ///
    /// Historical histories remain available after a relationship is
    /// removed from the active graph.
    ///
    /// The returned dictionary is a cached read-only view over the
    /// graph's canonical history store.
    /// </remarks>
    public IReadOnlyDictionary<Guid, SorophyRelationshipHistory>
        RelationshipHistories =>
        _readOnlyRelationshipHistories;

    /// <summary>
    /// Attempts to retrieve the historical history of a relationship.
    /// </summary>
    /// <param name="relationshipId">
    /// The relationship identity to look up.
    /// </param>
    /// <param name="history">
    /// The retained historical history when one exists.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when historical facts exist for the
    /// specified relationship; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetRelationshipHistory(
        Guid relationshipId,
        out SorophyRelationshipHistory? history)
    {
        return _relationshipHistories.TryGetValue(
            relationshipId,
            out history);
    }

    /*
     * =============================================================
     * INTERNAL HISTORY CREATION
     * =============================================================
     */

    /// <summary>
    /// Gets the existing historical history for a relationship or
    /// creates one when the first historical fact is being recorded.
    /// </summary>
    /// <param name="relationshipId">
    /// The relationship identity whose history is required.
    /// </param>
    /// <returns>
    /// The existing or newly created relationship history.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationshipId"/> is empty.
    /// </exception>
    internal SorophyRelationshipHistory GetOrCreateRelationshipHistory(
        Guid relationshipId)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        if (_relationshipHistories.TryGetValue(
                relationshipId,
                out var existingHistory))
        {
            return existingHistory;
        }

        var history =
            new SorophyRelationshipHistory(
                relationshipId);

        _relationshipHistories.Add(
            relationshipId,
            history);

        return history;
    }

    /*
     * =============================================================
     * HISTORICAL FACT RECORDING
     * =============================================================
     */

    /// <summary>
    /// Records a historical fact in the history belonging to the
    /// fact's relationship identity.
    /// </summary>
    /// <param name="fact">
    /// The historical fact to record.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fact"/> is null.
    /// </exception>
    internal void RecordRelationshipFact(
        SorophyRelationshipFact fact)
    {
        ArgumentNullException.ThrowIfNull(
            fact);

        var history =
            GetOrCreateRelationshipHistory(
                fact.RelationshipId);

        history.Add(
            fact);
    }

    /*
     * =============================================================
     * RELATIONSHIP ID RETIREMENT
     * =============================================================
     */

    /// <summary>
    /// Determines whether a relationship identity has been permanently
    /// retired.
    /// </summary>
    internal bool IsRelationshipIdRetired(
        Guid relationshipId)
    {
        return _retiredRelationshipIds.Contains(
            relationshipId);
    }

    /// <summary>
    /// Gets the set of retired relationship identities.
    /// </summary>
    internal IReadOnlyCollection<Guid> RetiredRelationshipIds =>
        _retiredRelationshipIds;

    /// <summary>
    /// Permanently retires a relationship identity.
    /// </summary>
    /// <param name="relationshipId">
    /// The relationship identity to retire.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationshipId"/> is empty.
    /// </exception>
    internal void RetireRelationshipId(
        Guid relationshipId)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Relationship ID cannot be empty.",
                nameof(relationshipId));
        }

        _retiredRelationshipIds.Add(
            relationshipId);
    }

    /*
     * =============================================================
     * HISTORY VALIDATION
     * =============================================================
     */

    /// <summary>
    /// Validates the relationship-history store against its identity
    /// invariants.
    /// </summary>
    private void ValidateRelationshipHistoryStore(
        List<string> errors)
    {
        foreach (var pair in
                 _relationshipHistories)
        {
            var relationshipId =
                pair.Key;

            var history =
                pair.Value;

            if (relationshipId == Guid.Empty)
            {
                errors.Add(
                    "Relationship history store contains an empty relationship ID.");
            }

            if (history is null)
            {
                errors.Add(
                    $"Relationship history store contains a null history " +
                    $"for relationship '{relationshipId}'.");

                continue;
            }

            if (history.RelationshipId !=
                relationshipId)
            {
                errors.Add(
                    $"Relationship history dictionary key '{relationshipId}' " +
                    $"does not match history relationship ID " +
                    $"'{history.RelationshipId}'.");
            }
        }

        foreach (var relationshipId in
                 _retiredRelationshipIds)
        {
            if (relationshipId == Guid.Empty)
            {
                errors.Add(
                    "Relationship retirement store contains an empty relationship ID.");
            }
        }
    }
}