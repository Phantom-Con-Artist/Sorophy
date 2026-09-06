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
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph;

/// <summary>
/// Represents a uniquely identified structured object within the Sorophyis ecosystem.
///
/// A SorophyEntity contains its own identity, descriptive information, typed
/// classification, structured properties, user-defined tags, and optional
/// embedded documents.
///
/// Relationships are intentionally not stored on the entity. Connections
/// between entities belong to the graph and contextual layers.
/// </summary>
public sealed class SorophyEntity
{
    /// <summary>
    /// Stable identity of the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name of the entity.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional classification of the entity.
    ///
    /// Examples:
    /// Character, Location, Organization, Event, Document, etc.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets whether this entity is classified as an Event.
    ///
    /// Event classification is derived entirely from <see cref="Type"/>.
    /// The comparison is case-insensitive so that "Event", "event", and
    /// "EVENT" all represent the same semantic classification.
    /// </summary>
    public bool IsEvent =>
        string.Equals(
            Type,
            "Event",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the authoritative temporal coordinate of this entity
    /// when it represents an Event.
    /// </summary>
    public SorophyTime? OccurredAt { get; set; }

    /// <summary>
    /// Optional human-readable description of the entity.
    ///
    /// This is intended for concise descriptive information directly
    /// associated with the entity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User-defined structured properties associated with the entity.
    /// </summary>
    public Dictionary<string, SorophyProperty> Properties { get; } = new();

    /// <summary>
    /// User-defined tags assigned to the entity.
    ///
    /// Tags behave as flags: an entity either has a tag or it does not.
    /// Duplicate tags are therefore not permitted.
    ///
    /// Examples:
    /// Protagonist
    /// Antagonist
    /// Important
    /// Root Document
    /// Character
    /// Location
    /// </summary>
    public HashSet<string> Tags { get; } =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Documents embedded directly inside the entity.
    ///
    /// Each document is owned by the entity and is identified by its name.
    /// The document contains its own content type and textual content.
    ///
    /// Example:
    /// "History.md" -> SorophyEntityDocument
    /// </summary>
    public Dictionary<string, SorophyEntityDocument> Documents { get; } =
        new(StringComparer.Ordinal);
}