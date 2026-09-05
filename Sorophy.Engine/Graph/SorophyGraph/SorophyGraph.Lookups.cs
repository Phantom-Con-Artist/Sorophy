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
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * DIRECT LOOKUPS
     * =============================================================
     */

    /// <summary>
    /// Determines whether the graph currently contains an entity with
    /// the specified identifier.
    /// </summary>
    public bool ContainsEntity(
        Guid entityId)
    {
        return _entities.ContainsKey(
            entityId);
    }

    /// <summary>
    /// Determines whether the graph currently contains a relationship
    /// with the specified identifier.
    /// </summary>
    public bool ContainsRelationship(
        Guid relationshipId)
    {
        return _relationships.ContainsKey(
            relationshipId);
    }

    /// <summary>
    /// Attempts to retrieve an entity by identifier.
    /// </summary>
    /// <param name="entityId">
    /// The entity identifier to look up.
    /// </param>
    /// <param name="entity">
    /// The entity when found; otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the entity exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetEntity(
        Guid entityId,
        out SorophyEntity? entity)
    {
        return _entities.TryGetValue(
            entityId,
            out entity);
    }

    /// <summary>
    /// Attempts to retrieve a relationship by identifier.
    /// </summary>
    /// <param name="relationshipId">
    /// The relationship identifier to look up.
    /// </param>
    /// <param name="relationship">
    /// The relationship when found; otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the relationship exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetRelationship(
        Guid relationshipId,
        out SorophyRelationship? relationship)
    {
        return _relationships.TryGetValue(
            relationshipId,
            out relationship);
    }
}