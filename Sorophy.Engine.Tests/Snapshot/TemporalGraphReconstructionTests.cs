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
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Snapshot;

/// <summary>
/// Authoritative test suite verifying Temporal Graph Reconstruction and Historical State Queries
/// across the 20 test matrix scenarios of Sorophy v2.0.0 (Krono).
/// </summary>
public sealed class TemporalGraphReconstructionTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "KronoTimeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Tick",
                    0,
                    new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, int position)
    {
        return new SorophyTime(schema, position.ToString(), "Tick", SorophyTimePrecision.Exact);
    }

    private static SorophyEntity CreateEntity(string name = "Node", string type = "Item")
    {
        return new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type
        };
    }

    private static SorophyRelationship CreateRelationship(Guid sourceId, Guid targetId, string type = "Related")
    {
        return new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            Type = type
        };
    }

    // =============================================================
    // 1. ENTITY CREATION BOUNDARY
    // =============================================================

    [Fact]
    public void Scenario01_EntityCreated_AbsentBefore_PresentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");
        var t100 = CreateTime(schema, 100);

        graph.CreateEntity(entity, t100);

        var snap99 = graph.CreateSnapshot(CreateTime(schema, 99));
        var snap100 = graph.CreateSnapshot(t100);
        var snap101 = graph.CreateSnapshot(CreateTime(schema, 101));

        Assert.False(snap99.ContainsEntity(entity.Id));
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 99)));
        Assert.False(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 99)));

        Assert.True(snap100.ContainsEntity(entity.Id));
        Assert.True(graph.EntityExistsAt(entity.Id, t100));
        Assert.True(graph.TemporalQuery.EntityExistsAt(entity.Id, t100));

        Assert.True(snap101.ContainsEntity(entity.Id));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 101)));
        Assert.True(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 101)));
    }

    // =============================================================
    // 2. ENTITY RETIREMENT BOUNDARY
    // =============================================================

    [Fact]
    public void Scenario02_EntityRetired_PresentBefore_AbsentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        var snap299 = graph.CreateSnapshot(CreateTime(schema, 299));
        var snap300 = graph.CreateSnapshot(CreateTime(schema, 300));
        var snap301 = graph.CreateSnapshot(CreateTime(schema, 301));

        Assert.True(snap299.ContainsEntity(entity.Id));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 299)));
        Assert.True(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 299)));

        Assert.False(snap300.ContainsEntity(entity.Id));
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 300)));
        Assert.False(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 300)));

        Assert.False(snap301.ContainsEntity(entity.Id));
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 301)));
        Assert.False(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 301)));
    }

    // =============================================================
    // 3. RELATIONSHIP CREATION BOUNDARY
    // =============================================================

    [Fact]
    public void Scenario03_RelationshipCreated_AbsentBefore_PresentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("E1");
        var e2 = CreateEntity("E2");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = CreateRelationship(e1.Id, e2.Id, "Alliance");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        var snap199 = graph.CreateSnapshot(CreateTime(schema, 199));
        var snap200 = graph.CreateSnapshot(CreateTime(schema, 200));
        var snap250 = graph.CreateSnapshot(CreateTime(schema, 250));

        Assert.False(snap199.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 199)));
        Assert.False(graph.TemporalQuery.RelationshipExistsAt(rel.Id, CreateTime(schema, 199)));

        Assert.True(snap200.ContainsRelationship(rel.Id));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 200)));
        Assert.True(graph.TemporalQuery.RelationshipExistsAt(rel.Id, CreateTime(schema, 200)));

        Assert.True(snap250.ContainsRelationship(rel.Id));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 250)));
        Assert.True(graph.TemporalQuery.RelationshipExistsAt(rel.Id, CreateTime(schema, 250)));
    }

    // =============================================================
    // 4. RELATIONSHIP RETIREMENT BOUNDARY
    // =============================================================

    [Fact]
    public void Scenario04_RelationshipRetired_PresentBefore_AbsentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("E1");
        var e2 = CreateEntity("E2");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = CreateRelationship(e1.Id, e2.Id, "Trade");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 400));

        var snap399 = graph.CreateSnapshot(CreateTime(schema, 399));
        var snap400 = graph.CreateSnapshot(CreateTime(schema, 400));
        var snap401 = graph.CreateSnapshot(CreateTime(schema, 401));

        Assert.True(snap399.ContainsRelationship(rel.Id));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 399)));

        Assert.False(snap400.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 400)));

        Assert.False(snap401.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 401)));
    }

    // =============================================================
    // 5. ENDPOINT RETIREMENT
    // =============================================================

    [Fact]
    public void Scenario05_EndpointRetirement_RemovesRelationshipFromProjectionWithoutMutatingHistory()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("Source");
        var e2 = CreateEntity("Target");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = CreateRelationship(e1.Id, e2.Id, "Link");
        graph.CreateRelationship(rel, CreateTime(schema, 150));

        // Source entity retires at 300, but relationship rel was NOT directly retired
        graph.RetireEntity(e1.Id, CreateTime(schema, 300));

        var snap299 = graph.CreateSnapshot(CreateTime(schema, 299));
        var snap300 = graph.CreateSnapshot(CreateTime(schema, 300));
        var snap301 = graph.CreateSnapshot(CreateTime(schema, 301));

        Assert.True(snap299.ContainsRelationship(rel.Id));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 299)));

        // At and after endpoint retirement: relationship is absent
        Assert.False(snap300.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 300)));

        Assert.False(snap301.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 301)));

        // Invariant: Relationship history was NOT mutated with a retirement fact!
        Assert.True(graph.TryGetRelationshipHistory(rel.Id, out var relHistory));
        Assert.NotNull(relHistory);
        Assert.Single(relHistory!.Facts);
        Assert.Equal(SorophyRelationshipFactKind.Created, relHistory.Facts[0].Kind);
        Assert.Null(relHistory.RetiredAt);
    }

    // =============================================================
    // 6. STAGGERED ENTITY CREATION
    // =============================================================

    [Fact]
    public void Scenario06_StaggeredEntityCreation_RelationshipAbsentBeforeCreation()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var eA = CreateEntity("A");
        var eB = CreateEntity("B");

        graph.CreateEntity(eA, CreateTime(schema, 100));
        graph.CreateEntity(eB, CreateTime(schema, 200));

        var rel = CreateRelationship(eA.Id, eB.Id, "R");
        graph.CreateRelationship(rel, CreateTime(schema, 250));

        var snap150 = graph.CreateSnapshot(CreateTime(schema, 150));
        Assert.True(snap150.ContainsEntity(eA.Id));
        Assert.False(snap150.ContainsEntity(eB.Id));
        Assert.False(snap150.ContainsRelationship(rel.Id));

        var snap249 = graph.CreateSnapshot(CreateTime(schema, 249));
        Assert.True(snap249.ContainsEntity(eA.Id));
        Assert.True(snap249.ContainsEntity(eB.Id));
        Assert.False(snap249.ContainsRelationship(rel.Id));

        var snap250 = graph.CreateSnapshot(CreateTime(schema, 250));
        Assert.True(snap250.ContainsEntity(eA.Id));
        Assert.True(snap250.ContainsEntity(eB.Id));
        Assert.True(snap250.ContainsRelationship(rel.Id));
    }

    // =============================================================
    // 7. INVALID RELATIONSHIP ENDPOINT CANON CHECK
    // =============================================================

    [Fact]
    public void Scenario07_AttemptToCreateRelationshipBeforeEndpoint_CanonCheckRejection()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var eA = CreateEntity("A");
        var eB = CreateEntity("B");

        graph.CreateEntity(eA, CreateTime(schema, 100));
        graph.CreateEntity(eB, CreateTime(schema, 200));

        var rel = CreateRelationship(eA.Id, eB.Id, "InvalidEdge");

        // Attempt to create R at 150 (before B exists at 200)
        var canonResult = graph.CanCreateRelationship(rel, CreateTime(schema, 150));
        Assert.False(canonResult.IsValid);

        Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(rel, CreateTime(schema, 150)));
    }

    // =============================================================
    // 8. HISTORICAL PROJECTION ACROSS MULTIPLE T VALUES
    // =============================================================

    [Fact]
    public void Scenario08_HistoricalProjection_MultiEntityAndRelationshipLifecycles()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var n1 = CreateEntity("N1");
        var n2 = CreateEntity("N2");
        var n3 = CreateEntity("N3");

        graph.CreateEntity(n1, CreateTime(schema, 100));
        graph.CreateEntity(n2, CreateTime(schema, 200));
        graph.CreateEntity(n3, CreateTime(schema, 300));

        var r12 = CreateRelationship(n1.Id, n2.Id, "E12");
        var r23 = CreateRelationship(n2.Id, n3.Id, "E23");

        graph.CreateRelationship(r12, CreateTime(schema, 250));
        graph.CreateRelationship(r23, CreateTime(schema, 350));

        graph.RetireRelationship(r12.Id, CreateTime(schema, 500));
        graph.RetireEntity(n1.Id, CreateTime(schema, 600));

        // T=50: empty
        var s50 = graph.CreateSnapshot(CreateTime(schema, 50));
        Assert.Equal(0, s50.EntityCount);
        Assert.Equal(0, s50.RelationshipCount);

        // T=150: only N1
        var s150 = graph.CreateSnapshot(CreateTime(schema, 150));
        Assert.Equal(1, s150.EntityCount);
        Assert.True(s150.ContainsEntity(n1.Id));
        Assert.Equal(0, s150.RelationshipCount);

        // T=260: N1, N2, R12
        var s260 = graph.CreateSnapshot(CreateTime(schema, 260));
        Assert.Equal(2, s260.EntityCount);
        Assert.True(s260.ContainsEntity(n1.Id));
        Assert.True(s260.ContainsEntity(n2.Id));
        Assert.Equal(1, s260.RelationshipCount);
        Assert.True(s260.ContainsRelationship(r12.Id));

        // T=360: N1, N2, N3, R12, R23
        var s360 = graph.CreateSnapshot(CreateTime(schema, 360));
        Assert.Equal(3, s360.EntityCount);
        Assert.Equal(2, s360.RelationshipCount);
        Assert.True(s360.ContainsRelationship(r12.Id));
        Assert.True(s360.ContainsRelationship(r23.Id));

        // T=550: R12 retired => N1, N2, N3, R23
        var s550 = graph.CreateSnapshot(CreateTime(schema, 550));
        Assert.Equal(3, s550.EntityCount);
        Assert.Equal(1, s550.RelationshipCount);
        Assert.False(s550.ContainsRelationship(r12.Id));
        Assert.True(s550.ContainsRelationship(r23.Id));

        // T=650: N1 retired => N2, N3, R23
        var s650 = graph.CreateSnapshot(CreateTime(schema, 650));
        Assert.Equal(2, s650.EntityCount);
        Assert.False(s650.ContainsEntity(n1.Id));
        Assert.True(s650.ContainsEntity(n2.Id));
        Assert.True(s650.ContainsEntity(n3.Id));
        Assert.Equal(1, s650.RelationshipCount);
        Assert.True(s650.ContainsRelationship(r23.Id));
    }

    // =============================================================
    // 9. REVERSE QUERY ORDER INDEPENDENCE
    // =============================================================

    [Fact]
    public void Scenario09_ReverseQueryOrder_ProducesIdenticalProjections()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 150));
        var rel = CreateRelationship(e1.Id, e2.Id, "Edge");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 450));

        // Query sequence: 500 -> 200 -> 400 -> 100 -> 500
        var s500_first = graph.CreateSnapshot(CreateTime(schema, 500));
        var s200 = graph.CreateSnapshot(CreateTime(schema, 200));
        var s400 = graph.CreateSnapshot(CreateTime(schema, 400));
        var s100 = graph.CreateSnapshot(CreateTime(schema, 100));
        var s500_second = graph.CreateSnapshot(CreateTime(schema, 500));

        Assert.Equal(s500_first.EntityCount, s500_second.EntityCount);
        Assert.Equal(s500_first.RelationshipCount, s500_second.RelationshipCount);
        Assert.Equal(s500_first.Entities.Keys.OrderBy(k => k), s500_second.Entities.Keys.OrderBy(k => k));
        Assert.Equal(s500_first.Relationships.Keys.OrderBy(k => k), s500_second.Relationships.Keys.OrderBy(k => k));

        Assert.False(s500_first.ContainsRelationship(rel.Id));
        Assert.False(s500_second.ContainsRelationship(rel.Id));
        Assert.True(s200.ContainsRelationship(rel.Id));
        Assert.True(s400.ContainsRelationship(rel.Id));
        Assert.False(s100.ContainsRelationship(rel.Id));
    }

    // =============================================================
    // 10. CANONICAL IMMUTABILITY GUARANTEES
    // =============================================================

    [Fact]
    public void Scenario10_CanonicalImmutability_ReconstructionDoesNotMutateGraphOrHistory()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("Alpha");
        var e2 = CreateEntity("Beta");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 200));
        var rel = CreateRelationship(e1.Id, e2.Id, "Link");
        graph.CreateRelationship(rel, CreateTime(schema, 300));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        // Baseline canonical state
        int entityCount = graph.Entities.Count;
        int relationshipCount = graph.Relationships.Count;
        int entityHistoryCount = graph.EntityHistories.Count;
        int relHistoryCount = graph.RelationshipHistories.Count;
        int retiredRelsCount = graph.RetiredRelationshipIds.Count;

        // Perform numerous arbitrary historical projections
        for (int t = 50; t <= 1000; t += 25)
        {
            var snap = graph.CreateSnapshot(CreateTime(schema, t));
            Assert.NotNull(snap);
        }

        // Canonical state must remain 100% untouched
        Assert.Equal(entityCount, graph.Entities.Count);
        Assert.Equal(relationshipCount, graph.Relationships.Count);
        Assert.Equal(entityHistoryCount, graph.EntityHistories.Count);
        Assert.Equal(relHistoryCount, graph.RelationshipHistories.Count);
        Assert.Equal(retiredRelsCount, graph.RetiredRelationshipIds.Count);

        // Verify entities and relationships are still present canonically
        Assert.True(graph.ContainsEntity(e1.Id));
        Assert.True(graph.ContainsEntity(e2.Id));
        Assert.True(graph.ContainsRelationship(rel.Id));
    }

    // =============================================================
    // 11. ZERO DANGLING RELATIONSHIPS GUARANTEE
    // =============================================================

    [Fact]
    public void Scenario11_ZeroDanglingRelationships_AllSnapshotEdgesHaveBothEndpointsPresent()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var entities = new List<SorophyEntity>();
        for (int i = 0; i < 10; i++)
        {
            var ent = CreateEntity($"Node_{i}");
            graph.CreateEntity(ent, CreateTime(schema, 100 + i * 20));
            entities.Add(ent);
        }

        var relationships = new List<SorophyRelationship>();
        for (int i = 0; i < 9; i++)
        {
            var r = CreateRelationship(entities[i].Id, entities[i + 1].Id, $"Edge_{i}");
            graph.CreateRelationship(r, CreateTime(schema, 200 + i * 20));
            relationships.Add(r);
        }

        // To retire entities in Canon-compliant order, retire after all established creation coordinates
        graph.RetireRelationship(relationships[0].Id, CreateTime(schema, 400));
        graph.RetireRelationship(relationships[1].Id, CreateTime(schema, 420));
        graph.RetireRelationship(relationships[2].Id, CreateTime(schema, 440));
        graph.RetireEntity(entities[2].Id, CreateTime(schema, 450));

        graph.RetireRelationship(relationships[4].Id, CreateTime(schema, 460));
        graph.RetireRelationship(relationships[5].Id, CreateTime(schema, 480));
        graph.RetireEntity(entities[5].Id, CreateTime(schema, 500));

        // Audit snapshots across wide coordinate range
        for (int t = 50; t <= 600; t += 10)
        {
            var snap = graph.CreateSnapshot(CreateTime(schema, t));

            foreach (var r in snap.Relationships.Values)
            {
                Assert.True(snap.ContainsEntity(r.SourceId),
                    $"Snapshot at T={t} contains dangling relationship '{r.Id}' missing source '{r.SourceId}'.");
                Assert.True(snap.ContainsEntity(r.TargetId),
                    $"Snapshot at T={t} contains dangling relationship '{r.Id}' missing target '{r.TargetId}'.");
            }

            foreach (var ent in snap.Entities.Values)
            {
                foreach (var outRel in snap.GetOutboundRelationships(ent.Id))
                {
                    Assert.Equal(ent.Id, outRel.SourceId);
                    Assert.True(snap.ContainsEntity(outRel.TargetId));
                }

                foreach (var inRel in snap.GetInboundRelationships(ent.Id))
                {
                    Assert.Equal(ent.Id, inRel.TargetId);
                    Assert.True(snap.ContainsEntity(inRel.SourceId));
                }
            }
        }
    }

    // =============================================================
    // 12. RETIREMENT REVERSAL
    // =============================================================

    [Fact]
    public void Scenario12_RetirementReversal_ReconstructedGraphRestoresRelationship()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = CreateRelationship(e1.Id, e2.Id, "Alliance");
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Retire at 300
        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));
        Assert.False(graph.CreateSnapshot(CreateTime(schema, 500)).ContainsRelationship(rel.Id));

        // Revert retirement at 300
        graph.RevertRelationshipRetirement(rel.Id, CreateTime(schema, 300));

        // Snapshot at 500 must show relationship again
        var snap500 = graph.CreateSnapshot(CreateTime(schema, 500));
        Assert.True(snap500.ContainsRelationship(rel.Id));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 500)));
    }

    // =============================================================
    // 13. PERMANENT DELETION
    // =============================================================

    [Fact]
    public void Scenario13_PermanentDeletion_CannotAppearInAnyHistoricalProjection()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = CreateRelationship(e1.Id, e2.Id, "Temp");
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Verify present before deletion
        Assert.True(graph.CreateSnapshot(CreateTime(schema, 150)).ContainsRelationship(rel.Id));

        // Permanently delete relationship
        bool removed = graph.RemoveRelationship(rel.Id);
        Assert.True(removed);

        // Must NOT appear in ANY historical projection!
        for (int t = 50; t <= 500; t += 50)
        {
            var snap = graph.CreateSnapshot(CreateTime(schema, t));
            Assert.False(snap.ContainsRelationship(rel.Id));
            Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, t)));
            Assert.False(graph.TemporalQuery.RelationshipExistsAt(rel.Id, CreateTime(schema, t)));
        }
    }

    // =============================================================
    // 14. LEGACY BASELINE COMPATIBILITY
    // =============================================================

    [Fact]
    public void Scenario14_LegacyBaseline_RelationshipWithoutHistoryRemainsActiveAcrossCoordinates()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        // Add legacy unversioned entities and relationship
        graph.RestoreEntity(e1);
        graph.RestoreEntity(e2);

        var rel = CreateRelationship(e1.Id, e2.Id, "BaselineRel");
        graph.AddRelationship(rel);

        // Must remain active across arbitrary coordinates
        for (int t = 0; t <= 1000; t += 100)
        {
            var snap = graph.CreateSnapshot(CreateTime(schema, t));
            Assert.True(snap.ContainsEntity(e1.Id));
            Assert.True(snap.ContainsEntity(e2.Id));
            Assert.True(snap.ContainsRelationship(rel.Id));
            Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, t)));
        }
    }

    // =============================================================
    // 15. PERSISTENCE ROUND-TRIP PRESERVES RECONSTRUCTION
    // =============================================================

    [Fact]
    public void Scenario15_PersistenceRoundTrip_HistoricalProjectionsMatchExactly()
    {
        var schema = CreateSchema();
        var originalGraph = new SorophyGraph();

        var e1 = CreateEntity("E1");
        var e2 = CreateEntity("E2");
        var e3 = CreateEntity("E3");

        originalGraph.CreateEntity(e1, CreateTime(schema, 100));
        originalGraph.CreateEntity(e2, CreateTime(schema, 150));
        originalGraph.CreateEntity(e3, CreateTime(schema, 200));

        var r12 = CreateRelationship(e1.Id, e2.Id, "Link12");
        var r23 = CreateRelationship(e2.Id, e3.Id, "Link23");

        originalGraph.CreateRelationship(r12, CreateTime(schema, 250));
        originalGraph.CreateRelationship(r23, CreateTime(schema, 300));

        originalGraph.RetireRelationship(r12.Id, CreateTime(schema, 450));
        originalGraph.RetireEntity(e2.Id, CreateTime(schema, 600));

        // Save to .lore JSON and reload
        var json = LoreSerializer.Serialize(originalGraph);
        var loadedGraph = LoreSerializer.Deserialize(json);

        int[] testCoords = [50, 100, 175, 250, 350, 450, 550, 600, 750];

        foreach (var t in testCoords)
        {
            var time = CreateTime(schema, t);
            var origSnap = originalGraph.CreateSnapshot(time);
            var loadedSnap = loadedGraph.CreateSnapshot(time);

            Assert.Equal(origSnap.EntityCount, loadedSnap.EntityCount);
            Assert.Equal(origSnap.RelationshipCount, loadedSnap.RelationshipCount);

            foreach (var id in origSnap.Entities.Keys)
            {
                Assert.True(loadedSnap.ContainsEntity(id));
            }

            foreach (var id in origSnap.Relationships.Keys)
            {
                Assert.True(loadedSnap.ContainsRelationship(id));
            }
        }
    }

    // =============================================================
    // 16. DETERMINISM
    // =============================================================

    [Fact]
    public void Scenario16_Determinism_IdenticalGraphAndHistoryProducesIdenticalSnapshot()
    {
        var schema = CreateSchema();
        var g1 = new SorophyGraph();
        var g2 = new SorophyGraph();

        var e1Id = Guid.NewGuid();
        var e2Id = Guid.NewGuid();
        var rId = Guid.NewGuid();

        var e1 = new SorophyEntity { Id = e1Id, Name = "Alpha" };
        var e2 = new SorophyEntity { Id = e2Id, Name = "Beta" };
        var rel = new SorophyRelationship { Id = rId, SourceId = e1Id, TargetId = e2Id, Type = "Conn" };

        g1.CreateEntity(e1, CreateTime(schema, 100));
        g1.CreateEntity(e2, CreateTime(schema, 200));
        g1.CreateRelationship(rel, CreateTime(schema, 300));

        g2.CreateEntity(new SorophyEntity { Id = e1Id, Name = "Alpha" }, CreateTime(schema, 100));
        g2.CreateEntity(new SorophyEntity { Id = e2Id, Name = "Beta" }, CreateTime(schema, 200));
        g2.CreateRelationship(new SorophyRelationship { Id = rId, SourceId = e1Id, TargetId = e2Id, Type = "Conn" }, CreateTime(schema, 300));

        var s1 = g1.CreateSnapshot(CreateTime(schema, 350));
        var s2 = g2.CreateSnapshot(CreateTime(schema, 350));

        Assert.Equal(s1.EntityCount, s2.EntityCount);
        Assert.Equal(s1.RelationshipCount, s2.RelationshipCount);
        Assert.Equal(s1.Entities.Keys.OrderBy(k => k), s2.Entities.Keys.OrderBy(k => k));
        Assert.Equal(s1.Relationships.Keys.OrderBy(k => k), s2.Relationships.Keys.OrderBy(k => k));
    }

    // =============================================================
    // 17. MULTI-EDGE HISTORICAL RECONSTRUCTION
    // =============================================================

    [Fact]
    public void Scenario17_MultiEdgeHistoricalReconstruction_DifferentLifecyclesBetweenSameEndpoints()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("CityA");
        var e2 = CreateEntity("CityB");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        // R1 active [200, 400)
        var r1 = CreateRelationship(e1.Id, e2.Id, "Treaty");
        graph.CreateRelationship(r1, CreateTime(schema, 200));
        graph.RetireRelationship(r1.Id, CreateTime(schema, 400));

        // R2 active [300, 600)
        var r2 = CreateRelationship(e1.Id, e2.Id, "Trade");
        graph.CreateRelationship(r2, CreateTime(schema, 300));
        graph.RetireRelationship(r2.Id, CreateTime(schema, 600));

        // T=250: only R1
        var s250 = graph.CreateSnapshot(CreateTime(schema, 250));
        Assert.True(s250.ContainsRelationship(r1.Id));
        Assert.False(s250.ContainsRelationship(r2.Id));

        // T=350: both R1 and R2
        var s350 = graph.CreateSnapshot(CreateTime(schema, 350));
        Assert.True(s350.ContainsRelationship(r1.Id));
        Assert.True(s350.ContainsRelationship(r2.Id));

        // T=450: only R2
        var s450 = graph.CreateSnapshot(CreateTime(schema, 450));
        Assert.False(s450.ContainsRelationship(r1.Id));
        Assert.True(s450.ContainsRelationship(r2.Id));

        // T=650: neither
        var s650 = graph.CreateSnapshot(CreateTime(schema, 650));
        Assert.False(s650.ContainsRelationship(r1.Id));
        Assert.False(s650.ContainsRelationship(r2.Id));
    }

    // =============================================================
    // 18. MIXED ENTITY / RELATIONSHIP RETIREMENT
    // =============================================================

    [Fact]
    public void Scenario18_MixedEntityAndRelationshipRetirement_IndependentCoordinates()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = CreateEntity("Source");
        var t = CreateEntity("Target");

        graph.CreateEntity(s, CreateTime(schema, 100));
        graph.CreateEntity(t, CreateTime(schema, 100));

        var rel = CreateRelationship(s.Id, t.Id, "Flow");
        graph.CreateRelationship(rel, CreateTime(schema, 150));

        // Relationship retired at 300
        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));
        // Source entity retired at 400
        graph.RetireEntity(s.Id, CreateTime(schema, 400));
        // Target entity retired at 500
        graph.RetireEntity(t.Id, CreateTime(schema, 500));

        // T=200: all active
        var s200 = graph.CreateSnapshot(CreateTime(schema, 200));
        Assert.True(s200.ContainsEntity(s.Id));
        Assert.True(s200.ContainsEntity(t.Id));
        Assert.True(s200.ContainsRelationship(rel.Id));

        // T=350: entities active, relationship retired
        var s350 = graph.CreateSnapshot(CreateTime(schema, 350));
        Assert.True(s350.ContainsEntity(s.Id));
        Assert.True(s350.ContainsEntity(t.Id));
        Assert.False(s350.ContainsRelationship(rel.Id));

        // T=450: only target entity active
        var s450 = graph.CreateSnapshot(CreateTime(schema, 450));
        Assert.False(s450.ContainsEntity(s.Id));
        Assert.True(s450.ContainsEntity(t.Id));
        Assert.False(s450.ContainsRelationship(rel.Id));

        // T=550: none active
        var s550 = graph.CreateSnapshot(CreateTime(schema, 550));
        Assert.False(s550.ContainsEntity(s.Id));
        Assert.False(s550.ContainsEntity(t.Id));
        Assert.False(s550.ContainsRelationship(rel.Id));
    }

    // =============================================================
    // 19. ESTABLISHED-STATE CANON CHECK INTERACTION
    // =============================================================

    [Fact]
    public void Scenario19_EstablishedStateCanonCheckInteraction_ConsistentWithReconstruction()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = CreateRelationship(e1.Id, e2.Id, "Edge");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        // Canon check: cannot retire relationship before creation
        var canRetireEarly = graph.CanRetireRelationship(rel.Id, CreateTime(schema, 150));
        Assert.False(canRetireEarly.IsValid);

        // Canon check: can retire after creation
        var canRetireValid = graph.CanRetireRelationship(rel.Id, CreateTime(schema, 300));
        Assert.True(canRetireValid.IsValid);

        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));

        // Snapshot reconstruction perfectly matches established boundaries
        Assert.False(graph.CreateSnapshot(CreateTime(schema, 150)).ContainsRelationship(rel.Id));
        Assert.True(graph.CreateSnapshot(CreateTime(schema, 200)).ContainsRelationship(rel.Id));
        Assert.True(graph.CreateSnapshot(CreateTime(schema, 299)).ContainsRelationship(rel.Id));
        Assert.False(graph.CreateSnapshot(CreateTime(schema, 300)).ContainsRelationship(rel.Id));
    }

    // =============================================================
    // 20. STRESS / RANDOMIZED ARBITRARY-TIME RECONSTRUCTION
    // =============================================================

    [Fact]
    public void Scenario20_RandomizedArbitraryTimeReconstruction_ZeroDanglingEdgesAndBoundaryInvariants()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var rng = new Random(42);

        var entities = new List<SorophyEntity>();
        for (int i = 0; i < 20; i++)
        {
            var ent = CreateEntity($"Node_{i}");
            int c = rng.Next(10, 200);
            graph.CreateEntity(ent, CreateTime(schema, c));
            entities.Add(ent);
        }

        var relationships = new List<SorophyRelationship>();
        for (int i = 0; i < 30; i++)
        {
            var s = entities[rng.Next(entities.Count)];
            var t = entities[rng.Next(entities.Count)];
            if (s.Id == t.Id) continue;

            var sHist = graph.EntityHistories[s.Id];
            var tHist = graph.EntityHistories[t.Id];
            int minCreation = Math.Max(int.Parse(sHist.CreatedAt!.Position), int.Parse(tHist.CreatedAt!.Position));
            int relCreation = minCreation + rng.Next(10, 50);

            var rel = CreateRelationship(s.Id, t.Id, $"R_{i}");
            var createResult = graph.CanCreateRelationship(rel, CreateTime(schema, relCreation));
            if (createResult.IsValid)
            {
                graph.CreateRelationship(rel, CreateTime(schema, relCreation));
                relationships.Add(rel);
            }
        }

        // Apply Canon-compliant retirements
        for (int i = 0; i < relationships.Count; i++)
        {
            var rel = relationships[i];
            int rTime = rng.Next(250, 600);
            var time = CreateTime(schema, rTime);
            if (graph.CanRetireRelationship(rel.Id, time).IsValid)
            {
                graph.RetireRelationship(rel.Id, time);
            }
        }

        for (int i = 0; i < entities.Count; i++)
        {
            var ent = entities[i];
            int rTime = rng.Next(300, 700);
            var time = CreateTime(schema, rTime);
            if (graph.CanRetireEntity(ent.Id, time).IsValid)
            {
                graph.RetireEntity(ent.Id, time);
            }
        }

        // Query 100 random coordinates
        for (int q = 0; q < 100; q++)
        {
            int queryT = rng.Next(0, 800);
            var time = CreateTime(schema, queryT);
            var snap = graph.CreateSnapshot(time);

            // Invariant 1: Zero dangling relationships
            foreach (var r in snap.Relationships.Values)
            {
                Assert.True(snap.ContainsEntity(r.SourceId),
                    $"Snapshot at T={queryT} contains dangling relationship '{r.Id}' missing source '{r.SourceId}'.");
                Assert.True(snap.ContainsEntity(r.TargetId),
                    $"Snapshot at T={queryT} contains dangling relationship '{r.Id}' missing target '{r.TargetId}'.");
            }

            // Invariant 2: Agreement with direct graph methods
            foreach (var e in entities)
            {
                bool expected = graph.EntityExistsAt(e.Id, time);
                Assert.Equal(expected, snap.ContainsEntity(e.Id));
                Assert.Equal(expected, graph.TemporalQuery.EntityExistsAt(e.Id, time));
            }

            foreach (var r in relationships)
            {
                bool expected = graph.RelationshipExistsAt(r.Id, time);
                Assert.Equal(expected, snap.ContainsRelationship(r.Id));
                Assert.Equal(expected, graph.TemporalQuery.RelationshipExistsAt(r.Id, time));
            }
        }
    }
}
