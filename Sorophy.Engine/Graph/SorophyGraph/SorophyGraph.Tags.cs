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
using System.Collections.Generic;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * TAG INDEX ACCESS
     * =============================================================
     */

    /// <summary>
    /// Returns whether the graph currently contains the specified tag in
    /// its tag index.
    /// </summary>
    public bool ContainsTag(
        string tag)
    {
        return _tagIndex.ContainsTag(
            tag);
    }

    /// <summary>
    /// Returns whether the specified entity is currently indexed under
    /// the specified tag.
    /// </summary>
    public bool HasEntityTag(
        Guid entityId,
        string tag)
    {
        return _tagIndex.HasTag(
            entityId,
            tag);
    }

    /// <summary>
    /// Returns the IDs of all entities currently indexed under a tag.
    /// </summary>
    public IReadOnlyCollection<Guid> GetEntityIdsByTag(
        string tag)
    {
        return _tagIndex.GetEntityIds(
            tag);
    }

    /// <summary>
    /// Returns all currently indexed entities carrying a tag.
    ///
    /// The tag lookup is performed through the secondary tag index rather
    /// than scanning the graph's complete entity store.
    /// </summary>
    public IEnumerable<SorophyEntity> GetEntitiesByTag(
        string tag)
    {
        foreach (var entityId in
                 _tagIndex.GetEntityIds(
                     tag))
        {
            if (_entities.TryGetValue(
                    entityId,
                    out var entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Returns all tags currently represented by the graph tag index in
    /// deterministic ordinal order.
    /// </summary>
    public IReadOnlyList<string> GetTags()
    {
        return _tagIndex.GetTags();
    }

    /// <summary>
    /// Re-synchronizes one entity's tags with the graph tag index.
    ///
    /// SorophyEntity.Tags is intentionally mutable, so direct mutations to that
    /// collection are not automatically observable by SorophyGraph. Call this
    /// method after changing an entity's tags.
    /// </summary>
    public void UpdateEntityTags(
        Guid entityId)
    {
        if (!_entities.TryGetValue(
                entityId,
                out var entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entityId}' does not exist.");
        }

        _tagIndex.UpdateEntity(
            entity);
    }

    /// <summary>
    /// Rebuilds the complete tag index from the graph's canonical entity
    /// store.
    ///
    /// This is useful after bulk loading or explicit index recovery.
    /// </summary>
    public void RebuildTagIndex()
    {
        _tagIndex.Rebuild(
            _entities.Values);
    }
}