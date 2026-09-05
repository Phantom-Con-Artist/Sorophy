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
/// Represents a first-class semantic relationship between two entities.
/// </summary>
public sealed class SorophyRelationship
{
    private SorophyTime? _validFrom;
    private SorophyTime? _validTill;

    /// <summary>
    /// Stable identity of the relationship.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Semantic type of the relationship.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Entity from which the relationship originates.
    /// </summary>
    public Guid SourceId { get; set; }

    /// <summary>
    /// Entity toward which the relationship points.
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// Optional temporal point from which the relationship is valid.
    /// </summary>
    /// <remarks>
    /// When both <see cref="ValidFrom"/> and <see cref="ValidTill"/> are
    /// specified, they must belong to the same <see cref="SorophyTimeSchema"/>.
    /// </remarks>
    public SorophyTime? ValidFrom
    {
        get => _validFrom;

        set
        {
            ValidateTemporalBoundary(
                value,
                _validTill,
                nameof(ValidFrom));

            _validFrom = value;
        }
    }

    /// <summary>
    /// Optional temporal point until which the relationship is valid.
    /// </summary>
    /// <remarks>
    /// When both <see cref="ValidFrom"/> and <see cref="ValidTill"/> are
    /// specified, they must belong to the same <see cref="SorophyTimeSchema"/>.
    /// </remarks>
    public SorophyTime? ValidTill
    {
        get => _validTill;

        set
        {
            ValidateTemporalBoundary(
                _validFrom,
                value,
                nameof(ValidTill));

            _validTill = value;
        }
    }

    /// <summary>
    /// Properties describing the relationship itself.
    /// </summary>
    public Dictionary<string, SorophyProperty> Properties { get; } = new();

    /// <summary>
    /// Ensures that the temporal boundaries use the same temporal schema.
    /// </summary>
    private static void ValidateTemporalBoundary(
        SorophyTime? validFrom,
        SorophyTime? validTill,
        string parameterName)
    {
        if (validFrom is null || validTill is null)
        {
            return;
        }

        if (!Equals(
                validFrom.Schema,
                validTill.Schema))
        {
            throw new ArgumentException(
                "ValidFrom and ValidTill must belong to the same temporal schema.",
                parameterName);
        }
    }
}