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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Provides focused, deterministic deep cloning for SorophyProperty and SorophyValue objects.
/// </summary>
internal static class SorophyValueCloner
{
    /// <summary>
    /// Creates a deep copy of a <see cref="SorophyProperty"/>, ensuring that nested
    /// values and collections share no references with the original.
    /// </summary>
    public static SorophyProperty CloneProperty(SorophyProperty source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new SorophyProperty
        {
            Name = source.Name,
            Value = CloneValue(source.Value)
        };
    }

    /// <summary>
    /// Creates a deep copy of a property dictionary.
    /// </summary>
    public static Dictionary<string, SorophyProperty> CloneProperties(
        IReadOnlyDictionary<string, SorophyProperty>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, SorophyProperty>(StringComparer.Ordinal);
        }

        var destination = new Dictionary<string, SorophyProperty>(
            source.Count,
            StringComparer.Ordinal);

        foreach (var (key, property) in source)
        {
            destination[key] = CloneProperty(property);
        }

        return destination;
    }

    /// <summary>
    /// Creates a deep copy of a <see cref="SorophyValue"/>.
    /// </summary>
    public static SorophyValue CloneValue(SorophyValue source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Type switch
        {
            SorophyValueType.List => new SorophyValue(
                SorophyValueType.List,
                CloneList((List<object?>)source.Value!)),

            SorophyValueType.Object => new SorophyValue(
                SorophyValueType.Object,
                CloneDictionary((Dictionary<string, object?>)source.Value!)),

            // Primitive types (Null, String, Boolean, Integer, Decimal, DateTime, Guid) are immutable
            _ => new SorophyValue(source.Type, source.Value)
        };
    }

    private static List<object?> CloneList(List<object?> sourceList)
    {
        var clonedList = new List<object?>(sourceList.Count);

        foreach (var item in sourceList)
        {
            clonedList.Add(CloneObjectGraph(item));
        }

        return clonedList;
    }

    private static Dictionary<string, object?> CloneDictionary(
        Dictionary<string, object?> sourceDict)
    {
        var clonedDict = new Dictionary<string, object?>(
            sourceDict.Count,
            StringComparer.Ordinal);

        foreach (var (key, val) in sourceDict)
        {
            clonedDict[key] = CloneObjectGraph(val);
        }

        return clonedDict;
    }

    private static object? CloneObjectGraph(object? item)
    {
        return item switch
        {
            null => null,
            SorophyValue nestedValue => CloneValue(nestedValue),
            List<object?> nestedList => CloneList(nestedList),
            Dictionary<string, object?> nestedDict => CloneDictionary(nestedDict),
            _ => item // primitives / strings / value types are immutable
        };
    }
}

