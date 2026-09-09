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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.HistoricalFacts;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Graph;

public sealed partial class SorophyGraph
{
    /* =============================================================
     * EMERGENT HISTORICAL FACTS (EHG)
     * =============================================================
     */

    private static readonly IReadOnlyList<SorophyHistoricalFact> EmptyHistoricalFacts =
        Array.Empty<SorophyHistoricalFact>();

    /// <summary>
    /// Retrieves all emergent historical facts for a specific entity within an optional temporal range.
    /// Derived directly and deterministically from authoritative entity history, and optionally including incident relationship facts.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="from">Optional inclusive lower bound of the temporal range.</param>
    /// <param name="to">Optional inclusive upper bound of the temporal range.</param>
    /// <param name="includeRelationships">When true, also includes all relationship facts where this entity is source or target.</param>
    /// <returns>A deterministically ordered read-only list of historical facts for the entity.</returns>
    public IReadOnlyList<SorophyHistoricalFact> GetEntityHistoricalFacts(
        Guid entityId,
        SorophyTime? from = null,
        SorophyTime? to = null,
        bool includeRelationships = false)
    {
        if (entityId == Guid.Empty || IsRangeInverted(from, to))
        {
            return EmptyHistoricalFacts;
        }

        var facts = new List<SorophyHistoricalFact>();

        if (_entityHistories.TryGetValue(entityId, out var history) && history is not null && history.Facts.Count > 0)
        {
            for (int i = 0; i < history.Facts.Count; i++)
            {
                var f = history.Facts[i];
                if (!IsTimeInRange(f.At, from, to))
                {
                    continue;
                }

                switch (f.Kind)
                {
                    case SorophyEntityFactKind.Created:
                        facts.Add(SorophyHistoricalFact.CreateForEntity(
                            f.At,
                            SorophyHistoricalFactKind.Created,
                            entityId,
                            f.Sequence));
                        break;
                    case SorophyEntityFactKind.Retired:
                        facts.Add(SorophyHistoricalFact.CreateForEntity(
                            f.At,
                            SorophyHistoricalFactKind.Retired,
                            entityId,
                            f.Sequence));
                        break;
                    case SorophyEntityFactKind.PropertyChanged:
                        facts.Add(SorophyHistoricalFact.CreateEntityPropertyChange(
                            f.At,
                            entityId,
                            f.Sequence,
                            f.PropertyName!,
                            f.PreviousValue,
                            f.NewValue));
                        break;
                }
            }
        }

        if (includeRelationships)
        {
            foreach (var relHistory in _relationshipHistories.Values)
            {
                for (int i = 0; i < relHistory.Facts.Count; i++)
                {
                    var f = relHistory.Facts[i];
                    if (!f.Kind.HasValue)
                    {
                        continue;
                    }

                    if (f.SourceId != entityId && f.TargetId != entityId)
                    {
                        continue;
                    }

                    if (!IsTimeInRange(f.At, from, to))
                    {
                        continue;
                    }

                    switch (f.Kind.Value)
                    {
                        case SorophyRelationshipFactKind.Created:
                            facts.Add(SorophyHistoricalFact.CreateForRelationship(
                                f.At,
                                SorophyHistoricalFactKind.Created,
                                relHistory.RelationshipId,
                                f.SourceId,
                                f.TargetId,
                                f.Type,
                                f.Sequence));
                            break;
                        case SorophyRelationshipFactKind.Retired:
                            facts.Add(SorophyHistoricalFact.CreateForRelationship(
                                f.At,
                                SorophyHistoricalFactKind.Retired,
                                relHistory.RelationshipId,
                                f.SourceId,
                                f.TargetId,
                                f.Type,
                                f.Sequence));
                            break;
                        case SorophyRelationshipFactKind.PropertyChanged:
                            facts.Add(SorophyHistoricalFact.CreateRelationshipPropertyChange(
                                f.At,
                                relHistory.RelationshipId,
                                f.SourceId,
                                f.TargetId,
                                f.Type,
                                f.Sequence,
                                f.PropertyName!,
                                f.PreviousValue,
                                f.NewValue));
                            break;
                        case SorophyRelationshipFactKind.RelationshipChanged:
                            facts.Add(SorophyHistoricalFact.CreateRelationshipTypeChange(
                                f.At,
                                relHistory.RelationshipId,
                                f.SourceId,
                                f.TargetId,
                                f.Sequence,
                                f.PreviousType,
                                f.NewType));
                            break;
                    }
                }
            }
        }

        facts.Sort();
        return facts.AsReadOnly();
    }

    /// <summary>
    /// Retrieves an entity-centric timeline containing all direct emergent historical facts
    /// for the specified entity as well as all relationship facts where the entity is source or target.
    /// </summary>
    public IReadOnlyList<SorophyHistoricalFact> GetEntityTimelineFacts(
        Guid entityId,
        SorophyTime? from = null,
        SorophyTime? to = null)
    {
        return GetEntityHistoricalFacts(entityId, from, to, includeRelationships: true);
    }

    /// <summary>
    /// Retrieves all emergent historical facts for a specific relationship within an optional temporal range.
    /// Derived directly and deterministically from authoritative relationship history.
    /// </summary>
    /// <param name="relationshipId">The unique identifier of the relationship.</param>
    /// <param name="from">Optional inclusive lower bound of the temporal range.</param>
    /// <param name="to">Optional inclusive upper bound of the temporal range.</param>
    /// <returns>A deterministically ordered read-only list of historical facts for the relationship.</returns>
    public IReadOnlyList<SorophyHistoricalFact> GetRelationshipHistoricalFacts(
        Guid relationshipId,
        SorophyTime? from = null,
        SorophyTime? to = null)
    {
        if (relationshipId == Guid.Empty || IsRangeInverted(from, to))
        {
            return EmptyHistoricalFacts;
        }

        if (!_relationshipHistories.TryGetValue(relationshipId, out var history) || history is null || history.Facts.Count == 0)
        {
            return EmptyHistoricalFacts;
        }

        var facts = new List<SorophyHistoricalFact>(history.Facts.Count);

        for (int i = 0; i < history.Facts.Count; i++)
        {
            var f = history.Facts[i];

            if (!f.Kind.HasValue)
            {
                // Ignore legacy pre-Krono evolution facts without authoritative lifecycle kind
                continue;
            }

            if (!IsTimeInRange(f.At, from, to))
            {
                continue;
            }

            switch (f.Kind.Value)
            {
                case SorophyRelationshipFactKind.Created:
                    facts.Add(SorophyHistoricalFact.CreateForRelationship(
                        f.At,
                        SorophyHistoricalFactKind.Created,
                        relationshipId,
                        f.SourceId,
                        f.TargetId,
                        f.Type,
                        f.Sequence));
                    break;
                case SorophyRelationshipFactKind.Retired:
                    facts.Add(SorophyHistoricalFact.CreateForRelationship(
                        f.At,
                        SorophyHistoricalFactKind.Retired,
                        relationshipId,
                        f.SourceId,
                        f.TargetId,
                        f.Type,
                        f.Sequence));
                    break;
                case SorophyRelationshipFactKind.PropertyChanged:
                    facts.Add(SorophyHistoricalFact.CreateRelationshipPropertyChange(
                        f.At,
                        relationshipId,
                        f.SourceId,
                        f.TargetId,
                        f.Type,
                        f.Sequence,
                        f.PropertyName!,
                        f.PreviousValue,
                        f.NewValue));
                    break;
                case SorophyRelationshipFactKind.RelationshipChanged:
                    facts.Add(SorophyHistoricalFact.CreateRelationshipTypeChange(
                        f.At,
                        relationshipId,
                        f.SourceId,
                        f.TargetId,
                        f.Sequence,
                        f.PreviousType,
                        f.NewType));
                    break;
            }
        }

        facts.Sort();
        return facts.AsReadOnly();
    }

    /// <summary>
    /// Retrieves all emergent historical facts across the entire graph within an optional temporal range.
    /// Derived directly and deterministically from authoritative entity and relationship histories.
    /// </summary>
    /// <param name="from">Optional inclusive lower bound of the temporal range.</param>
    /// <param name="to">Optional inclusive upper bound of the temporal range.</param>
    /// <returns>A deterministically ordered read-only list of all historical facts across the graph.</returns>
    public IReadOnlyList<SorophyHistoricalFact> GetHistoricalFacts(
        SorophyTime? from = null,
        SorophyTime? to = null)
    {
        if (IsRangeInverted(from, to))
        {
            return EmptyHistoricalFacts;
        }

        var facts = new List<SorophyHistoricalFact>();

        foreach (var history in _entityHistories.Values)
        {
            for (int i = 0; i < history.Facts.Count; i++)
            {
                var f = history.Facts[i];
                if (!IsTimeInRange(f.At, from, to))
                {
                    continue;
                }

                switch (f.Kind)
                {
                    case SorophyEntityFactKind.Created:
                        facts.Add(SorophyHistoricalFact.CreateForEntity(
                            f.At,
                            SorophyHistoricalFactKind.Created,
                            history.EntityId,
                            f.Sequence));
                        break;
                    case SorophyEntityFactKind.Retired:
                        facts.Add(SorophyHistoricalFact.CreateForEntity(
                            f.At,
                            SorophyHistoricalFactKind.Retired,
                            history.EntityId,
                            f.Sequence));
                        break;
                    case SorophyEntityFactKind.PropertyChanged:
                        facts.Add(SorophyHistoricalFact.CreateEntityPropertyChange(
                            f.At,
                            history.EntityId,
                            f.Sequence,
                            f.PropertyName!,
                            f.PreviousValue,
                            f.NewValue));
                        break;
                }
            }
        }

        foreach (var history in _relationshipHistories.Values)
        {
            for (int i = 0; i < history.Facts.Count; i++)
            {
                var f = history.Facts[i];

                if (!f.Kind.HasValue)
                {
                    continue;
                }

                if (!IsTimeInRange(f.At, from, to))
                {
                    continue;
                }

                switch (f.Kind.Value)
                {
                    case SorophyRelationshipFactKind.Created:
                        facts.Add(SorophyHistoricalFact.CreateForRelationship(
                            f.At,
                            SorophyHistoricalFactKind.Created,
                            history.RelationshipId,
                            f.SourceId,
                            f.TargetId,
                            f.Type,
                            f.Sequence));
                        break;
                    case SorophyRelationshipFactKind.Retired:
                        facts.Add(SorophyHistoricalFact.CreateForRelationship(
                            f.At,
                            SorophyHistoricalFactKind.Retired,
                            history.RelationshipId,
                            f.SourceId,
                            f.TargetId,
                            f.Type,
                            f.Sequence));
                        break;
                    case SorophyRelationshipFactKind.PropertyChanged:
                        facts.Add(SorophyHistoricalFact.CreateRelationshipPropertyChange(
                            f.At,
                            history.RelationshipId,
                            f.SourceId,
                            f.TargetId,
                            f.Type,
                            f.Sequence,
                            f.PropertyName!,
                            f.PreviousValue,
                            f.NewValue));
                        break;
                    case SorophyRelationshipFactKind.RelationshipChanged:
                        facts.Add(SorophyHistoricalFact.CreateRelationshipTypeChange(
                            f.At,
                            history.RelationshipId,
                            f.SourceId,
                            f.TargetId,
                            f.Sequence,
                            f.PreviousType,
                            f.NewType));
                        break;
                }
            }
        }

        facts.Sort();
        return facts.AsReadOnly();
    }

    private static bool IsRangeInverted(SorophyTime? from, SorophyTime? to)
    {
        if (from is not null && to is not null)
        {
            if (SorophyTime.CanCompare(from, to) && SorophyTime.Compare(from, to) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTimeInRange(SorophyTime time, SorophyTime? from, SorophyTime? to)
    {
        if (from is not null)
        {
            if (!SorophyTime.CanCompare(time, from) || SorophyTime.Compare(time, from) < 0)
            {
                return false;
            }
        }

        if (to is not null)
        {
            if (!SorophyTime.CanCompare(time, to) || SorophyTime.Compare(time, to) > 0)
            {
                return false;
            }
        }

        return true;
    }
}

