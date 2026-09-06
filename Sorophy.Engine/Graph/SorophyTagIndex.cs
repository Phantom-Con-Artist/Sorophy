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

namespace Sorophy.Engine.Graph;

/// <summary>
/// Provides an indexed lookup structure for Entity tags.
///
/// Tags are treated as flags. Each tag maps to the IDs of the entities
/// carrying that tag.
///
/// Example:
///
///     "Protagonist" -> Entity A, Entity F, Entity X
///
/// Lookup is performed through a dictionary and does not require scanning
/// the complete entity collection.
/// </summary>
public sealed class SorophyTagIndex
{
    private static readonly HashSet<string> EmptyTagSet =
        new(StringComparer.Ordinal);

    private readonly Dictionary<string, HashSet<Guid>> _tagToEntities =
        new(StringComparer.Ordinal);

    /*
     * Keeps track of which tags were indexed for each entity.
     *
     * This allows UpdateEntity and RemoveEntity to efficiently remove
     * stale memberships even if the Entity's current tag collection has
     * already changed.
     */
    private readonly Dictionary<Guid, HashSet<string>> _entityToTags =
        new();

    /// <summary>
    /// Gets the number of distinct tags currently present in the index.
    /// </summary>
    public int TagCount =>
        _tagToEntities.Count;

    /// <summary>
    /// Gets the number of entities currently represented by the index.
    /// </summary>
    public int EntityCount =>
        _entityToTags.Count;

    /// <summary>
    /// Ensures that the internal entity-to-tags dictionary has at least the specified capacity.
    /// </summary>
    internal void EnsureCapacity(
        int capacity)
    {
        if (capacity > 0)
        {
            _entityToTags.EnsureCapacity(capacity);
        }
    }

    /// <summary>
    /// Adds an entity's current tags to the index.
    ///
    /// If the entity is already indexed, its existing tag memberships are
    /// replaced with its current tag state.
    /// </summary>
    public void IndexEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Tags.Count == 0 &&
            _entityToTags.TryAdd(entity.Id, EmptyTagSet))
        {
            return;
        }

        UpdateEntity(entity);
    }

    /// <summary>
    /// Updates an entity's tag memberships.
    ///
    /// Only differences between the previously indexed tag state and the
    /// entity's current tag state are applied.
    /// </summary>
    public void UpdateEntity(
        SorophyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var currentTags =
            CreateValidatedTagSet(entity);

        if (_entityToTags.TryGetValue(
                entity.Id,
                out var previousTags))
        {
            foreach (var previousTag in previousTags)
            {
                if (!currentTags.Contains(previousTag))
                {
                    RemoveMembership(
                        previousTag,
                        entity.Id);
                }
            }
        }

        foreach (var currentTag in currentTags)
        {
            if (previousTags is null ||
                !previousTags.Contains(currentTag))
            {
                AddMembership(
                    currentTag,
                    entity.Id);
            }
        }

        _entityToTags[entity.Id] =
            currentTags;
    }

    /// <summary>
    /// Removes an entity from every tag index.
    /// </summary>
    public bool RemoveEntity(
        Guid entityId)
    {
        if (!_entityToTags.Remove(
                entityId,
                out var indexedTags))
        {
            return false;
        }

        foreach (var tag in indexedTags)
        {
            RemoveMembership(
                tag,
                entityId);
        }

        return true;
    }

    /// <summary>
    /// Returns whether an entity is indexed.
    /// </summary>
    public bool ContainsEntity(
        Guid entityId)
    {
        return _entityToTags.ContainsKey(
            entityId);
    }

    /// <summary>
    /// Returns whether a tag exists in the index.
    /// </summary>
    public bool ContainsTag(
        string tag)
    {
        ValidateTagName(tag);

        return _tagToEntities.ContainsKey(
            tag);
    }

    /// <summary>
    /// Returns whether the specified entity currently has the specified tag
    /// according to the index.
    /// </summary>
    public bool HasTag(
        Guid entityId,
        string tag)
    {
        ValidateTagName(tag);

        return _tagToEntities.TryGetValue(
                   tag,
                   out var entityIds) &&
               entityIds.Contains(
                   entityId);
    }

    /// <summary>
    /// Returns the entity IDs associated with a tag.
    ///
    /// Lookup itself is O(1); enumeration is proportional to the number of
    /// entities carrying the requested tag.
    /// </summary>
    public IReadOnlyCollection<Guid> GetEntityIds(
        string tag)
    {
        ValidateTagName(tag);

        if (_tagToEntities.TryGetValue(
                tag,
                out var entityIds))
        {
            return entityIds;
        }

        return Array.Empty<Guid>();
    }

    /// <summary>
    /// Returns all tags currently represented by the index.
    ///
    /// Tags are returned in deterministic ordinal order.
    /// </summary>
    public IReadOnlyList<string> GetTags()
    {
        var result =
            new List<string>(
                _tagToEntities.Count);

        foreach (var tag in _tagToEntities.Keys)
        {
            result.Add(tag);
        }

        result.Sort(
            StringComparer.Ordinal);

        return result;
    }

    /// <summary>
    /// Clears the complete tag index.
    /// </summary>
    public void Clear()
    {
        _tagToEntities.Clear();
        _entityToTags.Clear();
    }

    /// <summary>
    /// Rebuilds the complete index from a collection of entities.
    ///
    /// This is intended for graph loading, recovery, or explicit rebuilds.
    /// </summary>
    public void Rebuild(
        IEnumerable<SorophyEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(
            entities);

        Clear();

        foreach (var entity in entities)
        {
            IndexEntity(entity);
        }
    }

    /// <summary>
    /// Validates the internal consistency of the tag index.
    ///
    /// An empty collection indicates a valid index.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors =
            new List<string>();

        /*
         * Validate forward index.
         */
        foreach (var pair in _tagToEntities)
        {
            var tag =
                pair.Key;

            var entityIds =
                pair.Value;

            if (string.IsNullOrWhiteSpace(tag))
            {
                errors.Add(
                    "Tag index contains an empty tag.");
            }

            foreach (var entityId in entityIds)
            {
                if (!_entityToTags.TryGetValue(
                        entityId,
                        out var entityTags))
                {
                    errors.Add(
                        $"Tag index contains entity '{entityId}', " +
                        "but the reverse index does not contain the entity.");
                    continue;
                }

                if (!entityTags.Contains(tag))
                {
                    errors.Add(
                        $"Tag index contains entity '{entityId}' under " +
                        $"tag '{tag}', but the reverse index does not.");
                }
            }
        }

        /*
         * Validate reverse index.
         */
        foreach (var pair in _entityToTags)
        {
            var entityId =
                pair.Key;

            var tags =
                pair.Value;

            foreach (var tag in tags)
            {
                if (!_tagToEntities.TryGetValue(
                        tag,
                        out var entityIds))
                {
                    errors.Add(
                        $"Reverse tag index contains tag '{tag}' for " +
                        $"entity '{entityId}', but the forward index does not.");
                    continue;
                }

                if (!entityIds.Contains(
                        entityId))
                {
                    errors.Add(
                        $"Reverse tag index contains entity '{entityId}' " +
                        $"under tag '{tag}', but the forward index does not.");
                }
            }
        }

        return errors;
    }

    private void AddMembership(
        string tag,
        Guid entityId)
    {
        if (!_tagToEntities.TryGetValue(
                tag,
                out var entityIds))
        {
            entityIds =
                new HashSet<Guid>();

            _tagToEntities[tag] =
                entityIds;
        }

        entityIds.Add(
            entityId);
    }

    private void RemoveMembership(
        string tag,
        Guid entityId)
    {
        if (!_tagToEntities.TryGetValue(
                tag,
                out var entityIds))
        {
            return;
        }

        entityIds.Remove(
            entityId);

        if (entityIds.Count == 0)
        {
            _tagToEntities.Remove(
                tag);
        }
    }

    private static HashSet<string> CreateValidatedTagSet(
        SorophyEntity entity)
    {
        if (entity.Tags.Count == 0)
        {
            return EmptyTagSet;
        }

        var result =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var tag in entity.Tags)
        {
            ValidateTagName(tag);

            result.Add(tag);
        }

        return result;
    }

    private static void ValidateTagName(
        string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tag);
    }
}