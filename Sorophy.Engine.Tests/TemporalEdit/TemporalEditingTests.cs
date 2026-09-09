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
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.TemporalEdit;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalEdit;

/// <summary>
/// Authoritative test suite verifying Temporal Editing at arbitrary SorophyTime coordinates
/// in Sorophy v2.0.0 "Krono".
/// </summary>
public sealed class TemporalEditingTests
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
            Type = type,
            Description = $"Entity {name}"
        };
    }

    private static SorophyRelationship CreateRelationship(
        Guid sourceId,
        Guid targetId,
        string type = "Connects")
    {
        return new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            Type = type
        };
    }

    // =========================================================================
    // 1. ENTITY TEMPORAL CREATION
    // =========================================================================

    [Fact]
    public void CreateEntity_AtTimeT_VisibleAtT_AbsentBeforeT_PresentAfterT()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 300));

        // T < 300: Absent
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 200)));
        Assert.False(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 200)));
        var snapPre = graph.CreateSnapshot(CreateTime(schema, 200));
        Assert.False(snapPre.ContainsEntity(entity.Id));

        // T == 300: Present
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 300)));
        Assert.True(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 300)));
        var snapAt = graph.CreateSnapshot(CreateTime(schema, 300));
        Assert.True(snapAt.ContainsEntity(entity.Id));

        // T > 300: Present
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 400)));
        Assert.True(graph.TemporalQuery.EntityExistsAt(entity.Id, CreateTime(schema, 400)));
        var snapPost = graph.CreateSnapshot(CreateTime(schema, 400));
        Assert.True(snapPost.ContainsEntity(entity.Id));
    }

    [Fact]
    public void CreateEntity_HistoricalCoordinate_IntegratesIntoExistingHistory()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        // Later entity already established at 500
        var futureEntity = CreateEntity("FutureCastle");
        graph.CreateEntity(futureEntity, CreateTime(schema, 500));

        // Author edits at historical coordinate 200
        var historicalEntity = CreateEntity("AncientRuins");
        graph.CreateEntity(historicalEntity, CreateTime(schema, 200));

        // At 100: neither exists
        var snap100 = graph.CreateSnapshot(CreateTime(schema, 100));
        Assert.False(snap100.ContainsEntity(historicalEntity.Id));
        Assert.False(snap100.ContainsEntity(futureEntity.Id));

        // At 300: AncientRuins exists, FutureCastle does not
        var snap300 = graph.CreateSnapshot(CreateTime(schema, 300));
        Assert.True(snap300.ContainsEntity(historicalEntity.Id));
        Assert.False(snap300.ContainsEntity(futureEntity.Id));

        // At 500: both exist
        var snap500 = graph.CreateSnapshot(CreateTime(schema, 500));
        Assert.True(snap500.ContainsEntity(historicalEntity.Id));
        Assert.True(snap500.ContainsEntity(futureEntity.Id));
    }

    [Fact]
    public void CreateEntity_FutureCoordinate_AbsentBefore_PresentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("FutureCity");

        graph.CreateEntity(entity, CreateTime(schema, 700));

        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 699)));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 700)));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 800)));
    }

    [Fact]
    public void EditAt_CreateEntity_UsesEditorTime()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var editor = graph.EditAt(CreateTime(schema, 300));

        var entity = CreateEntity("ScopedHero");
        editor.CreateEntity(entity);

        Assert.Equal(CreateTime(schema, 300), graph.EntityHistories[entity.Id].CreatedAt);
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 299)));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 300)));
    }

    // =========================================================================
    // 2. ENTITY TEMPORAL RETIREMENT & REVERSAL
    // =========================================================================

    [Fact]
    public void RetireEntity_AtTimeT_PresentBefore_AbsentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Kingdom");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        // Before retirement: Present
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 200)));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, 200)));

        // At retirement: Absent
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 300)));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, 300)));

        // After retirement: Absent
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, 400)));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, 400)));
    }

    [Fact]
    public void RetireEntity_ContradictsIncidentRelationships_RejectedByCanonCheck()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var kingdom = CreateEntity("Kingdom");
        var ally = CreateEntity("Ally");
        var observatory = CreateEntity("Observatory");

        graph.CreateEntity(kingdom, CreateTime(schema, 100));
        graph.CreateEntity(ally, CreateTime(schema, 100));
        graph.CreateEntity(observatory, CreateTime(schema, 500)); // Establishes coordinate 500 in T_established

        var treaty = CreateRelationship(kingdom.Id, ally.Id, "Treaty");
        graph.CreateRelationship(treaty, CreateTime(schema, 150));

        // Incident relationship established active at coordinate 500
        // Attempting to retire kingdom at 300 must be rejected
        var conflict = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireEntity(kingdom.Id, CreateTime(schema, 300)));

        Assert.Contains("incident relationship", conflict.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(graph.IsEntityRetired(kingdom.Id));
    }

    [Fact]
    public void EditAt_RevertEntityRetirement_CoordinateLess_RevertsFromEarlierCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Empire");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));
        Assert.True(graph.IsEntityRetired(entity.Id));

        // Author is positioned at T = 300 (before the retirement) and reverses the retirement
        var editor300 = graph.EditAt(CreateTime(schema, 300));
        Assert.True(editor300.RevertEntityRetirement(entity.Id));

        // Retirement fact is removed
        Assert.False(graph.IsEntityRetired(entity.Id));
        Assert.Null(graph.EntityHistories[entity.Id].RetiredAt);

        // Entity is now active at 500 and 600
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 500)));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 600)));
    }

    [Fact]
    public void EditAt_RevertEntityRetirement_ExplicitCoordinate_MatchesAndReverts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Empire");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));

        var editor300 = graph.EditAt(CreateTime(schema, 300));
        Assert.True(editor300.RevertEntityRetirement(entity.Id, CreateTime(schema, 500)));

        Assert.False(graph.IsEntityRetired(entity.Id));
    }

    [Fact]
    public void EditAt_RevertEntityRetirement_ExplicitCoordinateMismatch_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Empire");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));

        var editor300 = graph.EditAt(CreateTime(schema, 300));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor300.RevertEntityRetirement(entity.Id, CreateTime(schema, 400)));

        Assert.Contains("does not have a retirement fact at coordinate", ex.Message);
        Assert.True(graph.IsEntityRetired(entity.Id));
    }

    [Fact]
    public void RevertRetirement_LeavesNoSyntheticUnretiredFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Empire");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));
        graph.RevertRetirement(entity.Id, CreateTime(schema, 500));

        var history = graph.EntityHistories[entity.Id];
        Assert.Single(history.Facts);
        Assert.Equal(SorophyEntityFactKind.Created, history.Facts[0].Kind);
    }

    // =========================================================================
    // 3. RELATIONSHIP TEMPORAL CREATION
    // =========================================================================

    [Fact]
    public void CreateRelationship_AtTimeT_VisibleAtT_AbsentBeforeT()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 200));

        var rel = CreateRelationship(source.Id, target.Id, "Ally");
        graph.CreateRelationship(rel, CreateTime(schema, 300));

        // Before 300: Absent
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 250)));
        var snapPre = graph.CreateSnapshot(CreateTime(schema, 250));
        Assert.False(snapPre.ContainsRelationship(rel.Id));

        // At 300: Visible
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 300)));
        var snapAt = graph.CreateSnapshot(CreateTime(schema, 300));
        Assert.True(snapAt.ContainsRelationship(rel.Id));

        // After 300: Visible
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 400)));
        var snapPost = graph.CreateSnapshot(CreateTime(schema, 400));
        Assert.True(snapPost.ContainsRelationship(rel.Id));
    }

    [Fact]
    public void CreateRelationship_SourceNotActiveAtT_Rejected()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        // Source created at 400, target created at 100
        graph.CreateEntity(source, CreateTime(schema, 400));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Ally");

        // Attempting to create relationship at 300 (when source does not exist)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(rel, CreateTime(schema, 300)));

        Assert.Contains("source entity", ex.Message);
        Assert.False(graph.ContainsRelationship(rel.Id));
    }

    [Fact]
    public void CreateRelationship_TargetNotActiveAtT_Rejected()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));
        graph.RetireEntity(target.Id, CreateTime(schema, 250));

        var rel = CreateRelationship(source.Id, target.Id, "Ally");

        // Attempting to create relationship at 300 (when target is retired)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(rel, CreateTime(schema, 300)));

        Assert.Contains("target entity", ex.Message);
        Assert.False(graph.ContainsRelationship(rel.Id));
    }

    [Fact]
    public void EditAt_CreateRelationship_ConvenienceOverload_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("A");
        var target = CreateEntity("B");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var editor = graph.EditAt(CreateTime(schema, 300));
        var relId = Guid.NewGuid();
        editor.CreateRelationship(source.Id, target.Id, "Bond", relId);

        Assert.True(graph.ContainsRelationship(relId));
        Assert.Equal(CreateTime(schema, 300), graph.RelationshipHistories[relId].CreatedAt);
        Assert.True(graph.RelationshipExistsAt(relId, CreateTime(schema, 300)));
    }

    // =========================================================================
    // 4. RELATIONSHIP TEMPORAL RETIREMENT & REVERSAL
    // =========================================================================

    [Fact]
    public void RetireRelationship_AtTimeT_PresentBefore_AbsentAtAndAfter()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("A");
        var target = CreateEntity("B");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Link");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        graph.RetireRelationship(rel.Id, CreateTime(schema, 400));

        // Present before 400
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 300)));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Active, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, 300)));

        // Absent at 400
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 400)));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Retired, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, 400)));

        // Absent after 400
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 500)));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Retired, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, 500)));
    }

    [Fact]
    public void RetireRelationship_ContradictsEstablishedLaterState_Rejected()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");
        var c = CreateEntity("C");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));
        graph.CreateEntity(c, CreateTime(schema, 500)); // Establishes T_established at 500

        var rel = CreateRelationship(a.Id, b.Id, "Treaty");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        // Rel is active at established coordinate 500
        // Retiring at 300 contradicts established active state at 500
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireRelationship(rel.Id, CreateTime(schema, 300)));

        Assert.Contains("established as active at later coordinate", ex.Message);
        Assert.False(graph.IsRelationshipRetired(rel.Id));
    }

    [Fact]
    public void EditAt_RevertRelationshipRetirement_CoordinateLess_RevertsFromEarlierCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));

        var rel = CreateRelationship(a.Id, b.Id, "Treaty");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        Assert.True(graph.IsRelationshipRetired(rel.Id));

        // Author positioned at 300 reverts retirement
        var editor300 = graph.EditAt(CreateTime(schema, 300));
        Assert.True(editor300.RevertRelationshipRetirement(rel.Id));

        Assert.False(graph.IsRelationshipRetired(rel.Id));
        Assert.Null(graph.RelationshipHistories[rel.Id].RetiredAt);
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, 500)));
    }

    [Fact]
    public void EditAt_RevertRelationshipRetirement_ExplicitCoordinate_MatchesAndReverts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));

        var rel = CreateRelationship(a.Id, b.Id, "Treaty");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        var editor300 = graph.EditAt(CreateTime(schema, 300));
        Assert.True(editor300.RevertRelationshipRetirement(rel.Id, CreateTime(schema, 500)));

        Assert.False(graph.IsRelationshipRetired(rel.Id));
    }

    [Fact]
    public void EditAt_RevertRelationshipRetirement_ExplicitCoordinateMismatch_Throws()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));

        var rel = CreateRelationship(a.Id, b.Id, "Treaty");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        var editor300 = graph.EditAt(CreateTime(schema, 300));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            editor300.RevertRelationshipRetirement(rel.Id, CreateTime(schema, 400)));

        Assert.Contains("does not have a retirement fact at coordinate", ex.Message);
        Assert.True(graph.IsRelationshipRetired(rel.Id));
    }

    [Fact]
    public void RevertRelationshipRetirement_LeavesNoSyntheticUnretiredFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));

        var rel = CreateRelationship(a.Id, b.Id, "Treaty");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));
        graph.RevertRelationshipRetirement(rel.Id, CreateTime(schema, 500));

        var history = graph.RelationshipHistories[rel.Id];
        Assert.Single(history.Facts);
        Assert.Equal(SorophyRelationshipFactKind.Created, history.Facts[0].Kind);
    }

    // =========================================================================
    // 5. CANONICAL INTEGRITY & COMPLETE ATOMICITY
    // =========================================================================

    [Fact]
    public void CreateEntity_FailedValidation_LeavesAllStoresEmpty()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        // Empty entity ID rejected by Canon Check
        var invalidEntity = new SorophyEntity { Id = Guid.Empty, Name = "Bad" };
        Assert.Throws<InvalidOperationException>(() =>
            graph.CreateEntity(invalidEntity, CreateTime(schema, 100)));

        Assert.Empty(graph.Entities);
        Assert.Empty(graph.EntityHistories);
        Assert.Empty(graph.GetTags());
    }

    [Fact]
    public void CreateRelationship_FailedValidation_LeavesStoresCleanAndRetiredRelationshipIdsEmpty()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");

        graph.CreateEntity(a, CreateTime(schema, 100));

        var badRel = CreateRelationship(a.Id, Guid.NewGuid(), "Bad"); // Target doesn't exist
        Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(badRel, CreateTime(schema, 200)));

        Assert.Empty(graph.Relationships);
        Assert.Empty(graph.RelationshipHistories);
        Assert.Empty(graph.RetiredRelationshipIds); // MUST NOT be polluted
    }

    [Fact]
    public void FailedCanonCheck_LeavesStateCompletelyUnchanged()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Entity");

        graph.CreateEntity(entity, CreateTime(schema, 300));

        // Attempt retirement that precedes creation
        Assert.Throws<InvalidOperationException>(() =>
            graph.RetireEntity(entity.Id, CreateTime(schema, 200)));

        Assert.False(graph.IsEntityRetired(entity.Id));
        Assert.Single(graph.EntityHistories[entity.Id].Facts);
    }

    [Fact]
    public void NoDanglingRelationships_WhenEndpointRetired()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Edge");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        // Retire target at 400 (retiring relationship prior to target retirement to satisfy Canon Check)
        graph.RetireRelationship(rel.Id, CreateTime(schema, 350));
        graph.RetireEntity(target.Id, CreateTime(schema, 400));

        // At 300: Both alive, edge alive
        var snap300 = graph.CreateSnapshot(CreateTime(schema, 300));
        Assert.True(snap300.ContainsRelationship(rel.Id));

        // At 375: Relationship retired, target still active
        var snap375 = graph.CreateSnapshot(CreateTime(schema, 375));
        Assert.False(snap375.ContainsRelationship(rel.Id));

        // At 450: Target retired
        var snap450 = graph.CreateSnapshot(CreateTime(schema, 450));
        Assert.False(snap450.ContainsRelationship(rel.Id));
        Assert.False(snap450.ContainsEntity(target.Id));
    }

    [Fact]
    public void CanonicalGraphRemainsSingular_AcrossAllEdits()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var editor100 = graph.EditAt(CreateTime(schema, 100));
        var e1 = CreateEntity("E1");
        editor100.CreateEntity(e1);

        var editor500 = graph.EditAt(CreateTime(schema, 500));
        var e2 = CreateEntity("E2");
        editor500.CreateEntity(e2);

        // Both editors modified the single canonical graph
        Assert.Equal(2, graph.Entities.Count);
        Assert.Same(graph, editor100.Graph);
        Assert.Same(graph, editor500.Graph);
    }

    // =========================================================================
    // 6. HISTORICAL RECONSTRUCTION AFTER EDITS
    // =========================================================================

    [Fact]
    public void HistoricalReconstruction_EditAtHistoricalT_ReflectsAccurately()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var alpha = CreateEntity("Alpha");
        graph.CreateEntity(alpha, CreateTime(schema, 100));

        var gamma = CreateEntity("Gamma");
        graph.CreateEntity(gamma, CreateTime(schema, 500));

        // Author edits at historical coordinate 300
        var beta = CreateEntity("Beta");
        graph.CreateEntity(beta, CreateTime(schema, 300));

        var snap200 = graph.CreateSnapshot(CreateTime(schema, 200));
        Assert.True(snap200.ContainsEntity(alpha.Id));
        Assert.False(snap200.ContainsEntity(beta.Id));
        Assert.False(snap200.ContainsEntity(gamma.Id));

        var snap300 = graph.CreateSnapshot(CreateTime(schema, 300));
        Assert.True(snap300.ContainsEntity(alpha.Id));
        Assert.True(snap300.ContainsEntity(beta.Id));
        Assert.False(snap300.ContainsEntity(gamma.Id));

        var snap500 = graph.CreateSnapshot(CreateTime(schema, 500));
        Assert.True(snap500.ContainsEntity(alpha.Id));
        Assert.True(snap500.ContainsEntity(beta.Id));
        Assert.True(snap500.ContainsEntity(gamma.Id));
    }

    [Fact]
    public void HistoricalReconstruction_EditAtFutureT_ReflectsAccurately()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("E");
        graph.CreateEntity(e, CreateTime(schema, 100));

        var f = CreateEntity("F");
        graph.CreateEntity(f, CreateTime(schema, 800));

        Assert.False(graph.CreateSnapshot(CreateTime(schema, 500)).ContainsEntity(f.Id));
        Assert.True(graph.CreateSnapshot(CreateTime(schema, 800)).ContainsEntity(f.Id));
        Assert.True(graph.CreateSnapshot(CreateTime(schema, 900)).ContainsEntity(f.Id));
    }

    [Fact]
    public void HistoricalReconstruction_QueryOrderIndependence_AfterEdits()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var a = CreateEntity("A");
        var b = CreateEntity("B");
        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 300));

        var rel = CreateRelationship(a.Id, b.Id, "L");
        graph.CreateRelationship(rel, CreateTime(schema, 350));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        // Query sequence 1: 500, 100, 350
        var s500_seq1 = graph.CreateSnapshot(CreateTime(schema, 500));
        var s100_seq1 = graph.CreateSnapshot(CreateTime(schema, 100));
        var s350_seq1 = graph.CreateSnapshot(CreateTime(schema, 350));

        // Query sequence 2: 350, 500, 100
        var s350_seq2 = graph.CreateSnapshot(CreateTime(schema, 350));
        var s500_seq2 = graph.CreateSnapshot(CreateTime(schema, 500));
        var s100_seq2 = graph.CreateSnapshot(CreateTime(schema, 100));

        Assert.Equal(s100_seq1.EntityCount, s100_seq2.EntityCount);
        Assert.Equal(s350_seq1.RelationshipCount, s350_seq2.RelationshipCount);
        Assert.Equal(s500_seq1.RelationshipCount, s500_seq2.RelationshipCount);
    }

    // =========================================================================
    // 7. PERSISTENCE ROUND-TRIP
    // =========================================================================

    [Fact]
    public void Persistence_TemporalEdits_SurviveRoundTrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var hero = CreateEntity("Hero");
        var castle = CreateEntity("Castle");
        graph.CreateEntity(hero, CreateTime(schema, 100));
        graph.CreateEntity(castle, CreateTime(schema, 200));

        var rel = CreateRelationship(hero.Id, castle.Id, "Visits");
        graph.CreateRelationship(rel, CreateTime(schema, 300));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 450));

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        Assert.Equal(2, restored.Entities.Count);
        Assert.Single(restored.Relationships);
        Assert.True(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, 350)));
        Assert.False(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, 450)));
    }

    [Fact]
    public void Persistence_ReversedRetirement_SurvivesRoundTrip()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var hero = CreateEntity("Hero");

        graph.CreateEntity(hero, CreateTime(schema, 100));
        graph.RetireEntity(hero.Id, CreateTime(schema, 500));
        graph.RevertRetirement(hero.Id, CreateTime(schema, 500));

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        Assert.False(restored.IsEntityRetired(hero.Id));
        Assert.True(restored.EntityExistsAt(hero.Id, CreateTime(schema, 600)));
    }
}
