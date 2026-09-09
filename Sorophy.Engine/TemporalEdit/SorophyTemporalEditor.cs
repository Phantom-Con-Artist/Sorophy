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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.TemporalEdit;

/// <summary>
/// Lightweight scoped temporal editor for authoring graph modifications
/// positioned at an authoritative temporal coordinate.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SorophyTemporalEditor"/> maintains no parallel graph storage, no timeline branches,
/// and no duplicate collections. All operations directly mutate the single canonical graph
/// and authoritative temporal lifecycle histories.
/// </para>
/// </remarks>
public sealed class SorophyTemporalEditor : ISorophyTemporalEditor
{
    /// <inheritdoc />
    public SorophyGraph Graph { get; }

    /// <inheritdoc />
    public SorophyTime Time { get; }

    /// <summary>
    /// Initializes a new scoped temporal editor for the specified canonical graph at the given coordinate.
    /// </summary>
    public SorophyTemporalEditor(
        SorophyGraph graph,
        SorophyTime time)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Time = time ?? throw new ArgumentNullException(nameof(time));
    }

    /// <inheritdoc />
    public void CreateEntity(SorophyEntity entity)
    {
        Graph.CreateEntity(entity, Time);
    }

    /// <inheritdoc />
    public bool RetireEntity(Guid entityId, string? description = null)
    {
        return Graph.RetireEntity(entityId, Time, description);
    }

    /// <inheritdoc />
    public bool RevertEntityRetirement(Guid entityId)
    {
        if (Graph.TryGetEntityHistory(entityId, out var history) &&
            history is not null &&
            history.RetiredAt is not null)
        {
            return Graph.RevertRetirement(entityId, history.RetiredAt);
        }

        throw new InvalidOperationException(
            $"Cannot revert retirement for entity '{entityId}': entity has no established retirement fact.");
    }

    /// <inheritdoc />
    public bool RevertEntityRetirement(Guid entityId, SorophyTime retirementCoordinate)
    {
        ArgumentNullException.ThrowIfNull(retirementCoordinate);
        return Graph.RevertRetirement(entityId, retirementCoordinate);
    }

    /// <inheritdoc />
    public void CreateRelationship(SorophyRelationship relationship)
    {
        Graph.CreateRelationship(relationship, Time);
    }

    /// <inheritdoc />
    public void CreateRelationship(
        Guid sourceId,
        Guid targetId,
        string type,
        Guid? relationshipId = null,
        IDictionary<string, SorophyProperty>? properties = null)
    {
        Graph.CreateRelationship(sourceId, targetId, type, Time, relationshipId, properties);
    }

    /// <inheritdoc />
    public bool RetireRelationship(Guid relationshipId, string? description = null)
    {
        return Graph.RetireRelationship(relationshipId, Time, description);
    }

    /// <inheritdoc />
    public bool RevertRelationshipRetirement(Guid relationshipId)
    {
        if (Graph.TryGetRelationshipHistory(relationshipId, out var history) &&
            history is not null &&
            history.RetiredAt is not null)
        {
            return Graph.RevertRelationshipRetirement(relationshipId, history.RetiredAt);
        }

        throw new InvalidOperationException(
            $"Cannot revert retirement for relationship '{relationshipId}': relationship has no established retirement fact.");
    }

    /// <inheritdoc />
    public bool RevertRelationshipRetirement(Guid relationshipId, SorophyTime retirementCoordinate)
    {
        ArgumentNullException.ThrowIfNull(retirementCoordinate);
        return Graph.RevertRelationshipRetirement(relationshipId, retirementCoordinate);
    }
}

