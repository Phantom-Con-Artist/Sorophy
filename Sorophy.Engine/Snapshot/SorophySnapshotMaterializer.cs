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

        // Authoritative existence check:
        if (!graph.RelationshipExistsAt(relId, targetTime))
        {
            return false;
        }

        graph.TryGetRelationship(relId, out var liveRel);
        graph.TryGetRelationshipHistory(relId, out var history);

        // Check if legacy mutation facts (Kind == null) exist that modify state
        if (history is not null && history.Facts.Count > 0)
        {
            var facts = history.Facts;
            SorophyRelationshipFact? stateFact = null;
            for (int i = 0; i < facts.Count; i++)
            {
                if (facts[i].Kind is null && SorophyTime.Compare(facts[i].At, targetTime) > 0)
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

            // If liveRel is null (legacy evolution termination), find fact active at targetTime
            if (liveRel is null)
            {
                for (int i = facts.Count - 1; i >= 0; i--)
                {
                    if (facts[i].Kind is null && SorophyTime.Compare(facts[i].At, targetTime) <= 0)
                    {
                        relationship = CreateFromFact(facts[i]);
                        return true;
                    }
                }
            }
        }

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

