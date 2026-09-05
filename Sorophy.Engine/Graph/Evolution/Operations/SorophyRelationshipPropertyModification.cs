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

namespace Sorophy.Engine.Graph.Evolution.Operations;

/// <summary>
/// Represents a modification to the properties of an existing relationship.
/// </summary>
/// <remarks>
/// <para>
/// Properties contained in <see cref="PropertiesToSet"/> are added when
/// absent or replace the existing value when present.
/// </para>
///
/// <para>
/// Property names contained in <see cref="PropertiesToRemove"/> are removed.
/// A property cannot appear in both collections because doing so would make
/// the requested transition ambiguous.
/// </para>
/// </remarks>
public sealed class SorophyRelationshipPropertyModification
    : SorophyRelationshipEvolution
{
    /// <summary>
    /// Gets the properties that should be added or replaced.
    /// </summary>
    public IReadOnlyDictionary<string, SorophyProperty>
        PropertiesToSet { get; }

    /// <summary>
    /// Gets the names of properties that should be removed.
    /// </summary>
    public IReadOnlyCollection<string>
        PropertiesToRemove { get; }

    /// <summary>
    /// Initializes a new relationship property-modification operation.
    /// </summary>
    /// <param name="relationshipId">
    /// The identity of the relationship being modified.
    /// </param>
    /// <param name="effectiveTime">
    /// The temporal point at which the modification takes effect.
    /// </param>
    /// <param name="propertiesToSet">
    /// Properties to add or replace.
    /// </param>
    /// <param name="propertiesToRemove">
    /// Property names to remove.
    /// </param>
    /// <param name="eventEntityId">
    /// Optional identity of the event entity that originated this property modification.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when a property name is blank or when the same property is
    /// requested for both setting and removal.
    /// </exception>
    public SorophyRelationshipPropertyModification(
        Guid relationshipId,
        SorophyTime effectiveTime,
        IReadOnlyDictionary<string, SorophyProperty>? propertiesToSet = null,
        IEnumerable<string>? propertiesToRemove = null,
        Guid? eventEntityId = null)
        : base(
            relationshipId,
            effectiveTime,
            eventEntityId)
    {
        PropertiesToSet =
            CopyPropertiesToSet(
                propertiesToSet);

        PropertiesToRemove =
            CopyPropertiesToRemove(
                propertiesToRemove);

        ValidateNoConflictingOperations(
            PropertiesToSet,
            PropertiesToRemove);
    }

    private static IReadOnlyDictionary<string, SorophyProperty>
        CopyPropertiesToSet(
            IReadOnlyDictionary<string, SorophyProperty>? properties)
    {
        var result =
            new Dictionary<string, SorophyProperty>(
                StringComparer.Ordinal);

        if (properties is null)
        {
            return result;
        }

        foreach (var pair in
                 properties)
        {
            if (string.IsNullOrWhiteSpace(
                    pair.Key))
            {
                throw new ArgumentException(
                    "Property names cannot be null, empty, or whitespace.",
                    nameof(properties));
            }

            ArgumentNullException.ThrowIfNull(
                pair.Value);

            result.Add(
                pair.Key,
                pair.Value);
        }

        return result;
    }

    private static IReadOnlyCollection<string>
        CopyPropertiesToRemove(
            IEnumerable<string>? propertyNames)
    {
        var result =
            new HashSet<string>(
                StringComparer.Ordinal);

        if (propertyNames is null)
        {
            return result;
        }

        foreach (var propertyName in
                 propertyNames)
        {
            if (string.IsNullOrWhiteSpace(
                    propertyName))
            {
                throw new ArgumentException(
                    "Property names cannot be null, empty, or whitespace.",
                    nameof(propertyNames));
            }

            result.Add(
                propertyName);
        }

        return result;
    }

    private static void ValidateNoConflictingOperations(
        IReadOnlyDictionary<string, SorophyProperty> propertiesToSet,
        IReadOnlyCollection<string> propertiesToRemove)
    {
        foreach (var propertyName in
                 propertiesToRemove)
        {
            if (propertiesToSet.ContainsKey(
                    propertyName))
            {
                throw new ArgumentException(
                    $"Property '{propertyName}' cannot be both set and removed " +
                    "by the same relationship property modification.");
            }
        }
    }
}