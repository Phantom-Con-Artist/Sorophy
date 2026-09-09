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
using Sorophy.Engine.Types;

namespace Sorophy.Engine.TemporalEdit;

/// <summary>
/// Defines the temporal editing contract for modifying canonical graph state
/// while positioned at an authoritative temporal coordinate.
/// </summary>
public interface ISorophyTemporalEditor
{
    /// <summary>
    /// Gets the canonical graph being edited.
    /// </summary>
    SorophyGraph Graph { get; }

    /// <summary>
    /// Gets the temporal coordinate at which edits are authored.
    /// </summary>
    SorophyTime Time { get; }

    /// <summary>
    /// Creates an entity in the graph at this editor's temporal coordinate.
    /// </summary>
    void CreateEntity(SorophyEntity entity);

    /// <summary>
    /// Temporally retires an entity at this editor's temporal coordinate without mutating canonical entity or relationship storage.
    /// </summary>
    bool RetireEntity(Guid entityId, string? description = null);

    /// <summary>
    /// Reverts an established temporal retirement for an entity, resolving and removing its established retirement fact.
    /// </summary>
    bool RevertEntityRetirement(Guid entityId);

    /// <summary>
    /// Reverts an established temporal retirement for an entity at the specified retirement coordinate.
    /// </summary>
    bool RevertEntityRetirement(Guid entityId, SorophyTime retirementCoordinate);

    /// <summary>
    /// Creates a relationship in the graph at this editor's temporal coordinate.
    /// </summary>
    void CreateRelationship(SorophyRelationship relationship);

    /// <summary>
    /// Creates a relationship in the graph at this editor's temporal coordinate.
    /// </summary>
    void CreateRelationship(
        Guid sourceId,
        Guid targetId,
        string type,
        Guid? relationshipId = null,
        IDictionary<string, SorophyProperty>? properties = null);

    /// <summary>
    /// Temporally retires a relationship at this editor's temporal coordinate without mutating canonical relationship storage.
    /// </summary>
    bool RetireRelationship(Guid relationshipId, string? description = null);

    /// <summary>
    /// Reverts an established temporal retirement for a relationship, resolving and removing its established retirement fact.
    /// </summary>
    bool RevertRelationshipRetirement(Guid relationshipId);

    /// <summary>
    /// Reverts an established temporal retirement for a relationship at the specified retirement coordinate.
    /// </summary>
    bool RevertRelationshipRetirement(Guid relationshipId, SorophyTime retirementCoordinate);

    /// <summary>
    /// Sets an entity property at this editor's temporal coordinate.
    /// </summary>
    void SetEntityProperty(Guid entityId, string propertyName, SorophyValue value, string? description = null);

    /// <summary>
    /// Removes an entity property at this editor's temporal coordinate.
    /// </summary>
    bool RemoveEntityProperty(Guid entityId, string propertyName, string? description = null);

    /// <summary>
    /// Sets a relationship property at this editor's temporal coordinate.
    /// </summary>
    void SetRelationshipProperty(Guid relationshipId, string propertyName, SorophyValue value, string? description = null);

    /// <summary>
    /// Removes a relationship property at this editor's temporal coordinate.
    /// </summary>
    bool RemoveRelationshipProperty(Guid relationshipId, string propertyName, string? description = null);

    /// <summary>
    /// Changes the semantic type of a relationship at this editor's temporal coordinate.
    /// </summary>
    void ChangeRelationshipType(Guid relationshipId, string newType, string? description = null);
}

