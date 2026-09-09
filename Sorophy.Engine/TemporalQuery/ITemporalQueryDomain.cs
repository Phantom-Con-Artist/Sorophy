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
using Sorophy.Engine.HistoricalFacts;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.TemporalQuery;

/// <summary>
/// Defines the read-only query contract of the Temporal Query Domain (TQD)
/// in The Saga Architecture.
/// </summary>
/// <remarks>
/// <para>
/// TQD provides temporal querying capabilities across canonical graph state,
/// relationship history, and event provenance.
/// </para>
/// <para>
/// TQD is strictly read-only. It does not mutate graph state, execute events,
/// fabricate timestamps, or create synthetic history.
/// </para>
/// </remarks>
public interface ITemporalQueryDomain
{
    /*
     * =============================================================
     * 1. POINT-IN-TIME SNAPSHOT QUERIES
     * =============================================================
     */

    /// <summary>
    /// Materializes an immutable point-in-time snapshot of the graph at the
    /// specified authoritative temporal coordinate.
    /// </summary>
    /// <param name="time">The temporal coordinate at which to query the graph.</param>
    /// <returns>An immutable snapshot representing the graph at <paramref name="time"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    ISorophySnapshot At(SorophyTime time);

    /// <summary>
    /// Materializes an immutable point-in-time snapshot of the graph at the
    /// temporal coordinate of the specified Event entity.
    /// </summary>
    /// <param name="eventEntity">The Event entity acting as the temporal anchor.</param>
    /// <returns>An immutable snapshot representing the graph at the Event's temporal coordinate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventEntity"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventEntity"/> is not classified as an Event or lacks an OccurredAt coordinate.
    /// </exception>
    ISorophySnapshot At(SorophyEntity eventEntity);

    /*
     * =============================================================
     * 2. POINT-IN-TIME ENTITY LOOKUPS
     * =============================================================
     */

    /// <summary>
    /// Determines whether an entity with the specified ID was active in the graph
    /// at the given temporal coordinate.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns><see langword="true"/> if the entity existed at <paramref name="time"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    bool EntityExistsAt(Guid entityId, SorophyTime time);

    /// <summary>
    /// Retrieves the entity active at the specified temporal coordinate,
    /// or <see langword="null"/> if it did not exist.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns>The snapshot entity if active at <paramref name="time"/>; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    ISorophySnapshotEntity? GetEntityAt(Guid entityId, SorophyTime time);

    /*
     * =============================================================
     * 3. POINT-IN-TIME RELATIONSHIP LOOKUPS
     * =============================================================
     */

    /// <summary>
    /// Determines whether a relationship with the specified ID was active in the graph
    /// at the given temporal coordinate.
    /// </summary>
    /// <param name="relationshipId">The unique identifier of the relationship.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns><see langword="true"/> if the relationship existed at <paramref name="time"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    bool RelationshipExistsAt(Guid relationshipId, SorophyTime time);

    /// <summary>
    /// Retrieves the relationship active at the specified temporal coordinate,
    /// or <see langword="null"/> if it did not exist.
    /// </summary>
    /// <param name="relationshipId">The unique identifier of the relationship.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns>The snapshot relationship if active at <paramref name="time"/>; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    ISorophySnapshotRelationship? GetRelationshipAt(Guid relationshipId, SorophyTime time);

    /// <summary>
    /// Gets all outbound relationships originating from the specified entity
    /// at the given temporal coordinate.
    /// </summary>
    /// <param name="entityId">The entity from which the relationships originate.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns>An enumerable collection of outbound snapshot relationships.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    IEnumerable<ISorophySnapshotRelationship> GetOutboundRelationshipsAt(Guid entityId, SorophyTime time);

    /// <summary>
    /// Gets all inbound relationships terminating at the specified entity
    /// at the given temporal coordinate.
    /// </summary>
    /// <param name="entityId">The entity at which the relationships terminate.</param>
    /// <param name="time">The temporal coordinate to inspect.</param>
    /// <returns>An enumerable collection of inbound snapshot relationships.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
    IEnumerable<ISorophySnapshotRelationship> GetInboundRelationshipsAt(Guid entityId, SorophyTime time);

    /*
     * =============================================================
     * 4. INTERVAL QUERIES
     * =============================================================
     */

    /// <summary>
    /// Retrieves all historical relationship facts recorded within the inclusive
    /// interval [<paramref name="from"/>, <paramref name="till"/>], ordered
    /// deterministically.
    /// </summary>
    /// <param name="from">The inclusive lower bound of the temporal interval.</param>
    /// <param name="till">The inclusive upper bound of the temporal interval.</param>
    /// <returns>An immutable list of historical facts recorded within the interval.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="till"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="from"/> succeeds <paramref name="till"/> or temporal schemas/units mismatch.
    /// </exception>
    IReadOnlyList<SorophyRelationshipFact> GetFactsInInterval(
        SorophyTime from,
        SorophyTime till);

    /// <summary>
    /// Retrieves the distinct IDs of all relationships modified or created within
    /// the inclusive interval [<paramref name="from"/>, <paramref name="till"/>].
    /// </summary>
    /// <param name="from">The inclusive lower bound of the temporal interval.</param>
    /// <param name="till">The inclusive upper bound of the temporal interval.</param>
    /// <returns>An immutable collection of relationship IDs with facts recorded in the interval.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="till"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="from"/> succeeds <paramref name="till"/> or temporal schemas/units mismatch.
    /// </exception>
    IReadOnlyCollection<Guid> GetModifiedRelationshipIds(
        SorophyTime from,
        SorophyTime till);

    /*
     * =============================================================
     * 5. RELATIONSHIP HISTORY QUERIES
     * =============================================================
     */

    /// <summary>
    /// Retrieves the complete historical log of facts for the specified relationship,
    /// or <see langword="null"/> if no history exists for it.
    /// </summary>
    /// <param name="relationshipId">The unique identifier of the relationship.</param>
    /// <returns>The relationship's history if recorded; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="relationshipId"/> is empty.</exception>
    SorophyRelationshipHistory? GetRelationshipHistory(Guid relationshipId);

    /// <summary>
    /// Retrieves historical facts for a specific relationship recorded within the
    /// inclusive interval [<paramref name="from"/>, <paramref name="till"/>].
    /// </summary>
    /// <param name="relationshipId">The unique identifier of the relationship.</param>
    /// <param name="from">The inclusive lower bound of the temporal interval.</param>
    /// <param name="till">The inclusive upper bound of the temporal interval.</param>
    /// <returns>An immutable list of historical facts for the relationship within the interval.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationshipId"/> is empty, <paramref name="from"/> succeeds <paramref name="till"/>,
    /// or temporal schemas/units mismatch.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="till"/> is null.</exception>
    IReadOnlyList<SorophyRelationshipFact> GetRelationshipFactsInInterval(
        Guid relationshipId,
        SorophyTime from,
        SorophyTime till);

    /*
     * =============================================================
     * 6. PROVENANCE / EVENT QUERIES
     * =============================================================
     */

    /// <summary>
    /// Retrieves all historical relationship facts anchored by the specified Event entity ID.
    /// </summary>
    /// <param name="eventEntityId">The unique identifier of the Event entity.</param>
    /// <returns>An immutable list of historical facts anchored by the Event entity.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventEntityId"/> is empty, or identifies an entity that is not an Event.
    /// </exception>
    IReadOnlyList<SorophyRelationshipFact> GetFactsByEvent(Guid eventEntityId);

    /// <summary>
    /// Retrieves all historical relationship facts anchored by the specified Event entity.
    /// </summary>
    /// <param name="eventEntity">The Event entity acting as the temporal anchor.</param>
    /// <returns>An immutable list of historical facts anchored by the Event entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventEntity"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventEntity"/> is not classified as an Event.</exception>
    IReadOnlyList<SorophyRelationshipFact> GetFactsByEvent(SorophyEntity eventEntity);

    /// <summary>
    /// Retrieves the distinct IDs of all relationships with historical facts
    /// anchored by the specified Event entity ID.
    /// </summary>
    /// <param name="eventEntityId">The unique identifier of the Event entity.</param>
    /// <returns>An immutable collection of relationship IDs evolved under the Event entity.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventEntityId"/> is empty, or identifies an entity that is not an Event.
    /// </exception>
    IReadOnlyCollection<Guid> GetRelationshipsEvolvedByEvent(Guid eventEntityId);

    /// <summary>
    /// Retrieves the distinct IDs of all relationships with historical facts
    /// anchored by the specified Event entity.
    /// </summary>
    /// <param name="eventEntity">The Event entity acting as the temporal anchor.</param>
    /// <returns>An immutable collection of relationship IDs evolved under the Event entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventEntity"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventEntity"/> is not classified as an Event.</exception>
    IReadOnlyCollection<Guid> GetRelationshipsEvolvedByEvent(SorophyEntity eventEntity);

    /*
     * =============================================================
     * 6. EMERGENT HISTORICAL FACTS (EHG)
     * =============================================================
     */

    /// <summary>
    /// Retrieves all emergent historical facts for a specific entity within an optional temporal range.
    /// Derived directly from authoritative temporal history, optionally including incident relationship facts.
    /// </summary>
    IReadOnlyList<SorophyHistoricalFact> GetEntityHistoricalFacts(
        Guid entityId,
        SorophyTime? from = null,
        SorophyTime? to = null,
        bool includeRelationships = false);

    /// <summary>
    /// Retrieves an entity-centric timeline containing all direct emergent historical facts
    /// for the specified entity as well as all relationship facts where the entity is source or target.
    /// </summary>
    IReadOnlyList<SorophyHistoricalFact> GetEntityTimelineFacts(
        Guid entityId,
        SorophyTime? from = null,
        SorophyTime? to = null);

    /// <summary>
    /// Retrieves all emergent historical facts for a specific relationship within an optional temporal range.
    /// Derived directly from authoritative temporal history.
    /// </summary>
    IReadOnlyList<SorophyHistoricalFact> GetRelationshipHistoricalFacts(
        Guid relationshipId,
        SorophyTime? from = null,
        SorophyTime? to = null);

    /// <summary>
    /// Retrieves all emergent historical facts across the entire graph within an optional temporal range.
    /// Derived directly from authoritative temporal history.
    /// </summary>
    IReadOnlyList<SorophyHistoricalFact> GetHistoricalFacts(
        SorophyTime? from = null,
        SorophyTime? to = null);
}

