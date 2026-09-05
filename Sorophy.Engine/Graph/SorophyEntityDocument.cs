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

namespace Sorophy.Engine.Graph;

/// <summary>
/// Represents a text document embedded inside a SorophyEntity.
///
/// Embedded documents are owned by the entity and are intended to remain
/// part of the entity's self-contained document representation.
/// </summary>
public sealed class SorophyEntityDocument
{
    /// <summary>
    /// Creates a new embedded entity document.
    /// </summary>
    /// <param name="name">
    /// Document name or filename, for example "History.md".
    /// </param>
    /// <param name="content">
    /// Text content of the document.
    /// </param>
    /// <param name="contentType">
    /// MIME-style content type of the document.
    /// Defaults to Markdown.
    /// </param>
    public SorophyEntityDocument(
        string name,
        string content,
        string contentType = "text/markdown")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Name = name;
        Content = content;
        ContentType = contentType;
    }

    /// <summary>
    /// Document name or filename.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// MIME-style content type of the document.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Text content of the embedded document.
    /// </summary>
    public string Content { get; }
}