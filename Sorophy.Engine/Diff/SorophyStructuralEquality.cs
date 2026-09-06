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
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Diff;

/// <summary>
/// Provides deep structural equality comparisons for snapshot entities, relationships,
/// properties, embedded documents, and nested values.
/// </summary>
internal static class SorophyStructuralEquality
{
    public static bool EntityEquals(
        ISorophySnapshotEntity a,
        ISorophySnapshotEntity b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Id == b.Id &&
               string.Equals(a.Name, b.Name, StringComparison.Ordinal) &&
               string.Equals(a.Type, b.Type, StringComparison.Ordinal) &&
               TimeEquals(a.OccurredAt, b.OccurredAt) &&
               string.Equals(a.Description, b.Description, StringComparison.Ordinal) &&
               TagsEquals(a.Tags, b.Tags) &&
               DocumentsEquals(a.Documents, b.Documents) &&
               PropertiesEquals(a.Properties, b.Properties);
    }

    public static bool RelationshipEquals(
        ISorophySnapshotRelationship a,
        ISorophySnapshotRelationship b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Id == b.Id &&
               a.SourceId == b.SourceId &&
               a.TargetId == b.TargetId &&
               string.Equals(a.Type, b.Type, StringComparison.Ordinal) &&
               TimeEquals(a.ValidFrom, b.ValidFrom) &&
               TimeEquals(a.ValidTill, b.ValidTill) &&
               PropertiesEquals(a.Properties, b.Properties);
    }

    public static bool TimeEquals(
        SorophyTime? a,
        SorophyTime? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool TagsEquals(
        IReadOnlySet<string>? a,
        IReadOnlySet<string>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        return a.SetEquals(b);
    }

    public static bool DocumentsEquals(
        IReadOnlyDictionary<string, SorophyEntityDocument>? a,
        IReadOnlyDictionary<string, SorophyEntityDocument>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, docA) in a)
        {
            if (!b.TryGetValue(key, out var docB) || docB is null)
            {
                return false;
            }

            if (!string.Equals(docA.Name, docB.Name, StringComparison.Ordinal) ||
                !string.Equals(docA.ContentType, docB.ContentType, StringComparison.Ordinal) ||
                !string.Equals(docA.Content, docB.Content, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static bool PropertiesEquals(
        IReadOnlyDictionary<string, SorophyProperty>? a,
        IReadOnlyDictionary<string, SorophyProperty>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, propA) in a)
        {
            if (!b.TryGetValue(key, out var propB) || propB is null)
            {
                return false;
            }

            if (!string.Equals(propA.Name, propB.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (!ValueEquals(propA.Value, propB.Value))
            {
                return false;
            }
        }

        return true;
    }

    public static bool ValueEquals(
        SorophyValue? a,
        SorophyValue? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Type != b.Type)
        {
            return false;
        }

        return a.Type switch
        {
            SorophyValueType.Null =>
                b.Value is null,

            SorophyValueType.String =>
                string.Equals((string?)a.Value, (string?)b.Value, StringComparison.Ordinal),

            SorophyValueType.Boolean =>
                (bool)a.Value! == (bool)b.Value!,

            SorophyValueType.Integer =>
                (long)a.Value! == (long)b.Value!,

            SorophyValueType.Decimal =>
                (decimal)a.Value! == (decimal)b.Value!,

            SorophyValueType.DateTime =>
                (DateTime)a.Value! == (DateTime)b.Value!,

            SorophyValueType.Guid =>
                (Guid)a.Value! == (Guid)b.Value!,

            SorophyValueType.List =>
                ListEquals(a.Value as List<object?>, b.Value as List<object?>),

            SorophyValueType.Object =>
                ObjectDictionaryEquals(
                    a.Value as Dictionary<string, object?>,
                    b.Value as Dictionary<string, object?>),

            _ => false
        };
    }

    private static bool ListEquals(
        List<object?>? a,
        List<object?>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (!ObjectGraphEquals(a[i], b[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ObjectDictionaryEquals(
        Dictionary<string, object?>? a,
        Dictionary<string, object?>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, valA) in a)
        {
            if (!b.TryGetValue(key, out var valB))
            {
                return false;
            }

            if (!ObjectGraphEquals(valA, valB))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ObjectGraphEquals(
        object? a,
        object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a is SorophyValue valA && b is SorophyValue valB)
        {
            return ValueEquals(valA, valB);
        }

        if (a is List<object?> listA && b is List<object?> listB)
        {
            return ListEquals(listA, listB);
        }

        if (a is Dictionary<string, object?> dictA && b is Dictionary<string, object?> dictB)
        {
            return ObjectDictionaryEquals(dictA, dictB);
        }

        if (a is string strA && b is string strB)
        {
            return string.Equals(strA, strB, StringComparison.Ordinal);
        }

        if (a is double dblA && b is double dblB)
        {
            return dblA.Equals(dblB);
        }

        if (a is float fltA && b is float fltB)
        {
            return fltA.Equals(fltB);
        }

        return Equals(a, b);
    }
}

