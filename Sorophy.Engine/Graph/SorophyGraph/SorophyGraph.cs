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
using System.Collections.ObjectModel;
using Sorophy.Engine.Graph.History;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /*
     * =============================================================
     * CANONICAL GRAPH STATE
     * =============================================================
     *
     * These are the authoritative graph stores.
     *
     * Serialization uses these public collections, so the adjacency
     * index remains an internal acceleration structure only.
     */

    private readonly Dictionary<Guid, SorophyEntity> _entities =
        new();

    private readonly Dictionary<Guid, SorophyRelationship> _relationships =
        new();

    /*
     * =============================================================
     * COMPACT ADJACENCY INDEX
     * =============================================================
     *
     * One compact value per entity.
     *
     * The physical adjacency records live in the slab pool below.
     */

    private readonly Dictionary<Guid, EntityAdjacency> _entityAdjacency =
        new();

    /*
     * =============================================================
     * RELATIONSHIP INDEX
     * =============================================================
     *
     * Relationship ID ->
     *
     *   outgoing adjacency node
     *   incoming adjacency node
     *   global insertion sequence
     */

    private readonly Dictionary<Guid, RelationshipIndex> _relationshipIndex =
        new();

    /*
     * =============================================================
     * ADJACENCY POOL
     * =============================================================
     */

    private readonly AdjacencySlabPool _adjacencyPool =
        new();

    /*
     * =============================================================
     * TAG INDEX
     * =============================================================
     *
     * Secondary acceleration index for Entity tags.
     *
     * Canonical tag state remains on SorophyEntity.Tags.
     * This index exists only to make tag-based lookup fast.
     *
     * Tag operations themselves live in SorophyGraph.Tags.cs.
     */

    private readonly SorophyTagIndex _tagIndex =
        new();

    private long _nextRelationshipSequence;

    /*
     * =============================================================
     * READ-ONLY PUBLIC VIEWS
     * =============================================================
     */

    private readonly ReadOnlyDictionary<Guid, SorophyEntity>
        _readOnlyEntities;

    private readonly ReadOnlyDictionary<Guid, SorophyRelationship>
        _readOnlyRelationships;

    private readonly ReadOnlyDictionary<Guid, SorophyRelationshipHistory>
        _readOnlyRelationshipHistories;

    /*
     * =============================================================
     * CONSTRUCTOR
     * =============================================================
     */

    public SorophyGraph()
    {
        _readOnlyEntities =
            new ReadOnlyDictionary<Guid, SorophyEntity>(
                _entities);

        _readOnlyRelationships =
            new ReadOnlyDictionary<Guid, SorophyRelationship>(
                _relationships);

        _readOnlyRelationshipHistories =
            new ReadOnlyDictionary<Guid, SorophyRelationshipHistory>(
                _relationshipHistories);
    }

    /*
     * =============================================================
     * CANONICAL GRAPH ACCESS
     * =============================================================
     */

    public IReadOnlyDictionary<Guid, SorophyEntity> Entities =>
        _readOnlyEntities;

    public IReadOnlyDictionary<Guid, SorophyRelationship> Relationships =>
        _readOnlyRelationships;

    /*
     * =============================================================
     * INTERNAL CAPACITY OPTIMIZATION
     * =============================================================
     */

    internal void EnsureCapacity(
        int entityCapacity,
        int relationshipCapacity)
    {
        if (entityCapacity > 0)
        {
            _entities.EnsureCapacity(entityCapacity);
            _entityAdjacency.EnsureCapacity(entityCapacity);
            _tagIndex.EnsureCapacity(entityCapacity);
        }

        if (relationshipCapacity > 0)
        {
            _relationships.EnsureCapacity(relationshipCapacity);
            _relationshipIndex.EnsureCapacity(relationshipCapacity);
        }
    }
}