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
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;

namespace Sorophy.Engine.Snapshot;

/// <summary>
/// Authoritative temporal materialization engine for Sorophy graphs.
/// </summary>
internal static class SorophySnapshotMaterializer
{
    /// <summary>
    /// Materializes an immutable point-in-time snapshot of the graph at the specified coordinate.
    /// </summary>
    public static ISorophySnapshot Materialize(
        SorophyGraph graph,
        SorophyTime targetTime)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(targetTime);

        // Phase 1: Materialize Entities
        var snapshotEntities = new Dictionary<Guid, ISorophySnapshotEntity>();
        var activeEntityIds = new HashSet<Guid>();

        foreach (var (id, entity) in graph.Entities)
        {
            if (!graph.EntityExistsAt(id, targetTime))
            {
                continue;
            }

            var clonedProperties = SorophyValueCloner.CloneProperties(entity.Properties);
            var clonedTags = new HashSet<string>(entity.Tags, StringComparer.Ordinal);
            var clonedDocs = new Dictionary<string, SorophyEntityDocument>(entity.Documents, StringComparer.Ordinal);

            var snapshotEntity = new SorophySnapshotEntity(
                entity.Id,
                entity.Name,
                entity.Type,
                entity.IsEvent,
                entity.OccurredAt,
                entity.Description,
                clonedProperties,
                clonedTags,
                clonedDocs);

            snapshotEntities.Add(id, snapshotEntity);
            activeEntityIds.Add(id);
        }

        // Phase 2: Materialize Relationships
        var allRelationshipIds = new HashSet<Guid>(graph.Relationships.Keys);
        foreach (var id in graph.RelationshipHistories.Keys)
        {
            allRelationshipIds.Add(id);
        }

        var candidateRelationships = new List<ISorophySnapshotRelationship>();

        foreach (var relId in allRelationshipIds)
        {
            if (TryMaterializeRelationship(graph, relId, targetTime, out var materializedRel) &&
                materializedRel is not null)
            {
                candidateRelationships.Add(materializedRel);
            }
        }

        // Phase 3: Referential Integrity Validation (Dangling edge pruning)
        var snapshotRelationships = new Dictionary<Guid, ISorophySnapshotRelationship>();
        var outbound = new Dictionary<Guid, List<ISorophySnapshotRelationship>>();
        var inbound = new Dictionary<Guid, List<ISorophySnapshotRelationship>>();

        foreach (var rel in candidateRelationships)
        {
            if (!activeEntityIds.Contains(rel.SourceId) || !activeEntityIds.Contains(rel.TargetId))
            {
                // Prune dangling relationship
                continue;
            }

            snapshotRelationships.Add(rel.Id, rel);

            if (!outbound.TryGetValue(rel.SourceId, out var outList))
            {
                outList = new List<ISorophySnapshotRelationship>();
                outbound[rel.SourceId] = outList;
            }

            outList.Add(rel);

            if (!inbound.TryGetValue(rel.TargetId, out var inList))
            {
                inList = new List<ISorophySnapshotRelationship>();
                inbound[rel.TargetId] = inList;
            }

            inList.Add(rel);
        }

        // Phase 4: Construct Snapshot
        return new SorophySnapshot(
            targetTime,
            snapshotEntities,
            snapshotRelationships,
            outbound,
            inbound);
    }

    private static bool TryMaterializeRelationship(
        SorophyGraph graph,
        Guid relId,
        SorophyTime targetTime,
        out ISorophySnapshotRelationship? relationship)
    {
        relationship = null;

        graph.TryGetRelationship(relId, out var liveRel);

        if (!graph.TryGetRelationshipHistory(relId, out var history) ||
            history is null ||
            history.Facts.Count == 0)
        {
            // Case 1: No history recorded.
            if (liveRel is not null)
            {
                // Baseline un-evolved relationship.
                relationship = CreateFromLive(liveRel);
                return true;
            }

            // Direct removal without history, or nonexistent.
            return false;
        }

        var facts = history.Facts;
        var firstFact = facts[0];

        // Check if first fact was an evolved creation.
        // When created via SorophyRelationshipCreation, fact.ValidTill is operation.ValidTill (or null),
        // whereas in all mutations/terminations, fact.ValidTill is hardcoded to operation.EffectiveTime (fact.At).
        bool isCreation = firstFact.ValidTill is null ||
                          SorophyTime.Compare(firstFact.ValidTill, firstFact.At) != 0;

        if (isCreation && SorophyTime.Compare(targetTime, firstFact.At) < 0)
        {
            // Requested targetTime is prior to creation of the relationship.
            return false;
        }

        // Check if relationship was terminated at or before targetTime.
        bool isRetired = graph.IsRelationshipIdRetired(relId) && liveRel is null;
        if (isRetired)
        {
            var lastFact = facts[^1];
            // Termination took effect at lastFact.At.
            if (SorophyTime.Compare(targetTime, lastFact.At) >= 0)
            {
                // Post-transition semantics: terminated at or before targetTime => absent from Snapshot(targetTime).
                return false;
            }
        }

        // Find state active at targetTime:
        // Any fact F_next whose F_next.At > targetTime captures the state immediately BEFORE F_next.At.
        // Therefore, the first fact with F_next.At > targetTime holds the state active at targetTime.
        SorophyRelationshipFact? stateFact = null;
        for (int i = 0; i < facts.Count; i++)
        {
            if (SorophyTime.Compare(facts[i].At, targetTime) > 0)
            {
                stateFact = facts[i];
                break;
            }
        }

        if (stateFact is not null)
        {
            relationship = CreateFromFact(stateFact);
            return true;
        }

        // No facts occurred strictly after targetTime.
        // If the relationship is currently active in the graph, its live state is active at targetTime.
        if (liveRel is not null)
        {
            relationship = CreateFromLive(liveRel);
            return true;
        }

        return false;
    }

    private static ISorophySnapshotRelationship CreateFromLive(SorophyRelationship live)
    {
        return new SorophySnapshotRelationship(
            live.Id,
            live.SourceId,
            live.TargetId,
            live.Type,
            live.ValidFrom,
            live.ValidTill,
            SorophyValueCloner.CloneProperties(live.Properties));
    }

    private static ISorophySnapshotRelationship CreateFromFact(SorophyRelationshipFact fact)
    {
        return new SorophySnapshotRelationship(
            fact.RelationshipId,
            fact.SourceId,
            fact.TargetId,
            fact.Type,
            fact.ValidFrom,
            fact.ValidTill,
            SorophyValueCloner.CloneProperties(fact.Properties));
    }
}

