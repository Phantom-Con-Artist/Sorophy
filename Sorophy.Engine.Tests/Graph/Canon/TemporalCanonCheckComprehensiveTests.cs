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
using System.Linq;
using Sorophy.Engine.Diff;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.Canon;

/// <summary>
/// Authoritative comprehensive test suite verifying Temporal Canon Check / Contradiction Validation
/// in Sorophy v2.0.0 "Krono".
/// </summary>
public sealed class TemporalCanonCheckComprehensiveTests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(string timeline = "CanonTimeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Year", 0, NumericPosition)
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, int position)
    {
        return new SorophyTime(schema, position.ToString(), "Year", SorophyTimePrecision.Exact);
    }

    private static SorophyEntity CreateEntity(string name = "Entity", string type = "Default")
    {
        return new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type
        };
    }

    private static SorophyValue ValInt(long v) => new(SorophyValueType.Integer, v);
    private static SorophyValue ValStr(string s) => new(SorophyValueType.String, s);

    /* =============================================================
     * 1. ENTITY LIFECYCLE CANON & BOUNDARIES
     * =============================================================
     */

    [Fact]
    public void Test01_EntityMutation_BeforeCreation_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        // Entity created at T=100
        graph.CreateEntity(entity, CreateTime(schema, 100));

        // Mutation at T=50 (< Created) must fail Canon Check
        var checkResult = graph.CanonCheck.CanSetEntityProperty(entity.Id, "power", ValInt(10), CreateTime(schema, 50));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("uncreated", checkResult.Message);

        // Mutating via editor must throw InvalidOperationException
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 50)).SetEntityProperty(entity.Id, "power", ValInt(10)));
        Assert.Contains("uncreated", ex.Message);
    }

    [Fact]
    public void Test02_EntityMutation_AtCreation_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));

        // Mutation exactly at T=100 (== Created) must succeed
        var checkResult = graph.CanonCheck.CanSetEntityProperty(entity.Id, "power", ValInt(10), CreateTime(schema, 100));
        Assert.True(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, checkResult.ViolationKind);

        graph.EditAt(CreateTime(schema, 100)).SetEntityProperty(entity.Id, "power", ValInt(10));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(10), graph.Entities[entity.Id].Properties["power"].Value));
    }

    [Fact]
    public void Test03_EntityMutation_WhileActive_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));

        // Mutation at T=250 (Created < T < Retired) must succeed
        var checkResult = graph.CanonCheck.CanSetEntityProperty(entity.Id, "power", ValInt(20), CreateTime(schema, 250));
        Assert.True(checkResult.IsValid);

        graph.EditAt(CreateTime(schema, 250)).SetEntityProperty(entity.Id, "power", ValInt(20));

        var snap = graph.TemporalQuery.At(CreateTime(schema, 300));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(20), snap.GetEntity(entity.Id)!.Properties["power"].Value));
    }

    [Fact]
    public void Test04_EntityMutation_AtRetirementBoundary_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        // Mutation exactly at T=300 (== Retired) must fail Canon Check
        var checkResult = graph.CanonCheck.CanSetEntityProperty(entity.Id, "power", ValInt(30), CreateTime(schema, 300));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("retired", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(entity.Id, "power", ValInt(30)));
        Assert.Contains("retired", ex.Message);
    }

    [Fact]
    public void Test05_EntityMutation_AfterRetirement_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        // Mutation at T=350 (> Retired) must fail Canon Check
        var checkResult = graph.CanonCheck.CanSetEntityProperty(entity.Id, "power", ValInt(40), CreateTime(schema, 350));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("retired", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).SetEntityProperty(entity.Id, "power", ValInt(40)));
        Assert.Contains("retired", ex.Message);
    }

    [Fact]
    public void Test06_EntityRetirement_ConflictingWithEstablishedFutureLifecycle_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));

        // Established retirement is at 500. Attempting retirement at 200 contradicts established lifecycle.
        var checkResult = graph.CanonCheck.CanRetireEntity(entity.Id, CreateTime(schema, 200));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.AlreadyRetired, checkResult.ViolationKind);
        Assert.Contains("already has an established retirement", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireEntity(entity.Id, CreateTime(schema, 200)));
        Assert.Contains("already has an established retirement", ex.Message);
    }

    [Fact]
    public void Test07_EntityRetirement_ValidWithNoContradiction_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));

        var checkResult = graph.CanonCheck.CanRetireEntity(entity.Id, CreateTime(schema, 300));
        Assert.True(checkResult.IsValid);

        var retired = graph.RetireEntity(entity.Id, CreateTime(schema, 300));
        Assert.True(retired);
        Assert.True(graph.IsEntityRetired(entity.Id));
    }

    [Fact]
    public void Test08_EntityRetirement_RevertingRetirement_FollowsLifecycleRules()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        // Reverting at matching coordinate 300 succeeds
        var checkValid = graph.CanonCheck.CanRevertEntityRetirement(entity.Id, CreateTime(schema, 300));
        Assert.True(checkValid.IsValid);

        // Reverting at mismatched coordinate 400 fails
        var checkWrongCoord = graph.CanonCheck.CanRevertEntityRetirement(entity.Id, CreateTime(schema, 400));
        Assert.False(checkWrongCoord.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RetirementNotFound, checkWrongCoord.ViolationKind);

        // Execute valid revert
        var reverted = graph.RevertRetirement(entity.Id, CreateTime(schema, 300));
        Assert.True(reverted);
        Assert.False(graph.IsEntityRetired(entity.Id));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, 350)));
    }

    /* =============================================================
     * 2. IMPORTANT DISTINCTION: PROPERTY HISTORY VS LIFECYCLE
     * =============================================================
     */

    [Fact]
    public void Test_ImportantDistinction_PropertyFactDoesNotBlockValidRetirement()
    {
        /*
         * CASE:
         * 100 Created
         * 300 PropertyChanged
         * (Not retired yet)
         *
         * Attempt retirement at 200.
         * Canon Check must NOT simply reject because a PropertyChanged fact exists at 300.
         * It evaluates actual lifecycle contradiction.
         */
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(entity.Id, "title", ValStr("Champion"));

        // Retiring at 200 is valid: entity has no established future retirement or incident relationships
        var checkResult = graph.CanonCheck.CanRetireEntity(entity.Id, CreateTime(schema, 200));
        Assert.True(checkResult.IsValid);

        var retired = graph.RetireEntity(entity.Id, CreateTime(schema, 200));
        Assert.True(retired);
        Assert.True(graph.IsEntityRetired(entity.Id));
    }

    [Fact]
    public void Test_ImportantDistinction_EstablishedFutureRetirementContradictsNewRetirement()
    {
        /*
         * CASE:
         * 100 Created
         * 500 Retired
         *
         * Attempt retirement at 200.
         * MUST be rejected because established future lifecycle contradicts it.
         */
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 500));

        var checkResult = graph.CanonCheck.CanRetireEntity(entity.Id, CreateTime(schema, 200));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.AlreadyRetired, checkResult.ViolationKind);
    }

    /* =============================================================
     * 3. RELATIONSHIP LIFECYCLE CANON & BOUNDARIES
     * =============================================================
     */

    [Fact]
    public void Test09_RelationshipMutation_BeforeCreation_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Mutation at T=75 (< Created) must fail Canon Check
        var checkResult = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "Ally", CreateTime(schema, 75));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("does not exist", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 75)).ChangeRelationshipType(rel.Id, "Ally"));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Test10_RelationshipMutation_AtCreation_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Mutation at T=100 (== Created) must succeed
        var checkResult = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "Ally", CreateTime(schema, 100));
        Assert.True(checkResult.IsValid);

        graph.EditAt(CreateTime(schema, 100)).ChangeRelationshipType(rel.Id, "Ally");
        Assert.Equal("Ally", graph.Relationships[rel.Id].Type);
    }

    [Fact]
    public void Test11_RelationshipMutation_WhileActive_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        // Mutation at T=250 (Created < T < Retired) must succeed
        var checkResult = graph.CanonCheck.CanSetRelationshipProperty(rel.Id, "bond", ValInt(99), CreateTime(schema, 250));
        Assert.True(checkResult.IsValid);

        graph.EditAt(CreateTime(schema, 250)).SetRelationshipProperty(rel.Id, "bond", ValInt(99));

        var snap = graph.TemporalQuery.At(CreateTime(schema, 300));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(99), snap.GetRelationship(rel.Id)!.Properties["bond"].Value));
    }

    [Fact]
    public void Test12_RelationshipMutation_AtRetirementBoundary_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));

        // Mutation at T=300 (== Retired) must fail Canon Check
        var checkResult = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "Enemy", CreateTime(schema, 300));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("does not exist", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 300)).ChangeRelationshipType(rel.Id, "Enemy"));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Test13_RelationshipMutation_AfterRetirement_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));

        // Mutation at T=350 (> Retired) must fail Canon Check
        var checkResult = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "Enemy", CreateTime(schema, 350));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, checkResult.ViolationKind);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).ChangeRelationshipType(rel.Id, "Enemy"));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Test14_RelationshipRetirement_ConflictingWithEstablishedFutureLifecycle_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Friend" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 500));

        // Established retirement is at 500. Retiring at 200 contradicts established lifecycle.
        var checkResult = graph.CanonCheck.CanRetireRelationship(rel.Id, CreateTime(schema, 200));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.AlreadyRetired, checkResult.ViolationKind);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireRelationship(rel.Id, CreateTime(schema, 200)));
        Assert.Contains("already has an established retirement", ex.Message);
    }

    /* =============================================================
     * 4. ENDPOINT INTEGRITY
     * =============================================================
     */

    [Fact]
    public void Test15_RelationshipCreation_WithUncreatedSource_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 200)); // Created at 200
        graph.CreateEntity(target, CreateTime(schema, 100));

        var checkResult = graph.CanonCheck.CanCreateRelationship(Guid.NewGuid(), source.Id, target.Id, CreateTime(schema, 100));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("source entity", checkResult.Message);
    }

    [Fact]
    public void Test16_RelationshipCreation_WithRetiredSource_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));
        graph.RetireEntity(source.Id, CreateTime(schema, 250)); // Retired at 250

        // Attempting to create relationship at 300 (> 250)
        var checkResult = graph.CanonCheck.CanCreateRelationship(Guid.NewGuid(), source.Id, target.Id, CreateTime(schema, 300));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("source entity", checkResult.Message);
    }

    [Fact]
    public void Test17_RelationshipCreation_WithUncreatedTarget_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 200)); // Created at 200

        var checkResult = graph.CanonCheck.CanCreateRelationship(Guid.NewGuid(), source.Id, target.Id, CreateTime(schema, 100));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("target entity", checkResult.Message);
    }

    [Fact]
    public void Test18_RelationshipCreation_WithRetiredTarget_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));
        graph.RetireEntity(target.Id, CreateTime(schema, 250)); // Retired at 250

        var checkResult = graph.CanonCheck.CanCreateRelationship(Guid.NewGuid(), source.Id, target.Id, CreateTime(schema, 300));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("target entity", checkResult.Message);
    }

    [Fact]
    public void Test19_RelationshipMutation_WithInactiveSource_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = source.Id, TargetId = target.Id, Type = "Link" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Retire source at 300
        graph.RetireEntity(source.Id, CreateTime(schema, 300));

        // Mutating relationship at 350 must fail because source is inactive
        var checkResult = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "NewLink", CreateTime(schema, 350));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("does not exist", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).ChangeRelationshipType(rel.Id, "NewLink"));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Test20_RelationshipMutation_WithInactiveTarget_Fails()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = source.Id, TargetId = target.Id, Type = "Link" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Retire target at 300
        graph.RetireEntity(target.Id, CreateTime(schema, 300));

        // Mutating relationship at 350 must fail because target is inactive
        var checkResult = graph.CanonCheck.CanSetRelationshipProperty(rel.Id, "prop", ValInt(1), CreateTime(schema, 350));
        Assert.False(checkResult.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, checkResult.ViolationKind);
        Assert.Contains("does not exist", checkResult.Message);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).SetRelationshipProperty(rel.Id, "prop", ValInt(1)));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Test21_HistoricalReconstruction_NeverReturnsDanglingRelationships()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("A");
        var target = CreateEntity("B");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = source.Id, TargetId = target.Id, Type = "Bond" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Retire source at 300
        graph.RetireEntity(source.Id, CreateTime(schema, 300));

        // Point-in-time snapshot at 200: rel is active
        var snap200 = graph.TemporalQuery.At(CreateTime(schema, 200));
        Assert.True(snap200.ContainsRelationship(rel.Id));

        // Point-in-time snapshot at 300: source is retired, rel must NOT be materialized!
        var snap300 = graph.TemporalQuery.At(CreateTime(schema, 300));
        Assert.False(snap300.ContainsRelationship(rel.Id));

        // Point-in-time snapshot at 400: rel must NOT be materialized!
        var snap400 = graph.TemporalQuery.At(CreateTime(schema, 400));
        Assert.False(snap400.ContainsRelationship(rel.Id));
    }

    [Fact]
    public void Test22_EndpointRetirement_DoesNotCreateSyntheticRelationshipRetirementHistory()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("A");
        var target = CreateEntity("B");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = source.Id, TargetId = target.Id, Type = "Bond" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Verify history has only the creation fact
        var historyBefore = graph.RelationshipHistories[rel.Id];
        Assert.Single(historyBefore.Facts);

        // Retire source at 300
        graph.RetireEntity(source.Id, CreateTime(schema, 300));

        // CRITICAL INVARIANT: Endpoint retirement must NOT author a synthetic retirement fact in relationship history!
        var historyAfter = graph.RelationshipHistories[rel.Id];
        Assert.Single(historyAfter.Facts);
        Assert.Equal(SorophyRelationshipFactKind.Created, historyAfter.Facts[0].Kind);
        Assert.False(graph.IsRelationshipRetired(rel.Id));
    }

    /* =============================================================
     * 5. PAST EDITING & CONTINUITY
     * =============================================================
     */

    [Fact]
    public void Test23_PastEdit_ValidEntityPropertyEdit_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Scholar");
        entity.Properties["title"] = new SorophyProperty { Name = "title", Value = ValStr("Apprentice") };

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.EditAt(CreateTime(schema, 500)).SetEntityProperty(entity.Id, "title", ValStr("Grandmaster"));

        // Authoring past edit at T=300 is valid
        var check = graph.CanonCheck.CanSetEntityProperty(entity.Id, "title", ValStr("Journeyman"), CreateTime(schema, 300));
        Assert.True(check.IsValid);

        graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(entity.Id, "title", ValStr("Journeyman"));

        // Effective snapshots:
        // T=200: Apprentice
        // T=400: Journeyman
        // T=600: Grandmaster
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Apprentice"), graph.TemporalQuery.At(CreateTime(schema, 200)).GetEntity(entity.Id)!.Properties["title"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Journeyman"), graph.TemporalQuery.At(CreateTime(schema, 400)).GetEntity(entity.Id)!.Properties["title"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("Grandmaster"), graph.TemporalQuery.At(CreateTime(schema, 600)).GetEntity(entity.Id)!.Properties["title"].Value));
    }

    [Fact]
    public void Test24_PastEdit_ValidRelationshipPropertyEdit_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Colleague" };
        rel.Properties["trust"] = new SorophyProperty { Name = "trust", Value = ValInt(10) };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        graph.EditAt(CreateTime(schema, 500)).SetRelationshipProperty(rel.Id, "trust", ValInt(100));

        // Author past edit at T=300
        var check = graph.CanonCheck.CanSetRelationshipProperty(rel.Id, "trust", ValInt(50), CreateTime(schema, 300));
        Assert.True(check.IsValid);

        graph.EditAt(CreateTime(schema, 300)).SetRelationshipProperty(rel.Id, "trust", ValInt(50));

        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(10), graph.TemporalQuery.At(CreateTime(schema, 200)).GetRelationship(rel.Id)!.Properties["trust"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(50), graph.TemporalQuery.At(CreateTime(schema, 400)).GetRelationship(rel.Id)!.Properties["trust"].Value));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValInt(100), graph.TemporalQuery.At(CreateTime(schema, 600)).GetRelationship(rel.Id)!.Properties["trust"].Value));
    }

    [Fact]
    public void Test25_PastEdit_ValidRelationshipTypeEdit_Succeeds()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 50));
        graph.CreateEntity(e2, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Ally" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        graph.EditAt(CreateTime(schema, 500)).ChangeRelationshipType(rel.Id, "Nemesis");

        // Author past edit at T=300
        var check = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "Rival", CreateTime(schema, 300));
        Assert.True(check.IsValid);

        graph.EditAt(CreateTime(schema, 300)).ChangeRelationshipType(rel.Id, "Rival");

        Assert.Equal("Ally", graph.TemporalQuery.At(CreateTime(schema, 200)).GetRelationship(rel.Id)!.Type);
        Assert.Equal("Rival", graph.TemporalQuery.At(CreateTime(schema, 400)).GetRelationship(rel.Id)!.Type);
        Assert.Equal("Nemesis", graph.TemporalQuery.At(CreateTime(schema, 600)).GetRelationship(rel.Id)!.Type);
    }

    [Fact]
    public void Test26_PastEdit_ImmediateFutureContinuityRemainsCorrect()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Hero");
        e.Properties["rank"] = new SorophyProperty { Name = "rank", Value = ValStr("D") };

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.EditAt(CreateTime(schema, 500)).SetEntityProperty(e.Id, "rank", ValStr("S"));

        // Insert at 300: D -> B
        graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(e.Id, "rank", ValStr("B"));

        var history = graph.EntityHistories[e.Id];
        var fact500 = history.Facts.Single(f => f.At.Equals(CreateTime(schema, 500)));

        // Fact at 500 must have its PreviousValue stitched to B!
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("B"), fact500.PreviousValue));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("S"), fact500.NewValue));
    }

    [Fact]
    public void Test27_PastEdit_DistantFutureFactsRemainUntouched()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Hero");
        e.Properties["rank"] = new SorophyProperty { Name = "rank", Value = ValStr("D") };

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.EditAt(CreateTime(schema, 500)).SetEntityProperty(e.Id, "rank", ValStr("B"));
        graph.EditAt(CreateTime(schema, 700)).SetEntityProperty(e.Id, "rank", ValStr("S"));

        var fact700Before = graph.EntityHistories[e.Id].Facts.Single(f => f.At.Equals(CreateTime(schema, 700)));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("B"), fact700Before.PreviousValue));

        // Insert past edit at 300: D -> C
        graph.EditAt(CreateTime(schema, 300)).SetEntityProperty(e.Id, "rank", ValStr("C"));

        // Immediate future at 500 stitched to C
        var fact500 = graph.EntityHistories[e.Id].Facts.Single(f => f.At.Equals(CreateTime(schema, 500)));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("C"), fact500.PreviousValue));

        // Distant future at 700 remained untouched (still PreviousValue == B)
        var fact700After = graph.EntityHistories[e.Id].Facts.Single(f => f.At.Equals(CreateTime(schema, 700)));
        Assert.True(SorophyStructuralEquality.ValueEquals(ValStr("B"), fact700After.PreviousValue));
    }

    /* =============================================================
     * 6. ATOMIC FAILURE (ABSOLUTELY ZERO STATE CHANGE ON REJECTION)
     * =============================================================
     */

    [Fact]
    public void Test28_To_34_AtomicFailure_RejectedOperationsLeaveStateAndReconstructionStrictlyUnchanged()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Bond" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        // Retire e1 at 300
        graph.RetireEntity(e1.Id, CreateTime(schema, 300));

        // Capture authoritative state before rejected operations
        var entityCountBefore = graph.Entities.Count;
        var relCountBefore = graph.Relationships.Count;
        var e1HistoryCountBefore = graph.EntityHistories[e1.Id].Facts.Count;
        var relHistoryCountBefore = graph.RelationshipHistories[rel.Id].Facts.Count;

        var snap150Before = graph.TemporalQuery.At(CreateTime(schema, 150));
        var snap350Before = graph.TemporalQuery.At(CreateTime(schema, 350));

        // 1. Rejected entity mutation (mutating e1 after retirement at 350)
        Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).SetEntityProperty(e1.Id, "illegal", ValInt(1)));

        // 2. Rejected relationship mutation (mutating rel at 350 with retired endpoint)
        Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).ChangeRelationshipType(rel.Id, "IllegalType"));

        // 3. Rejected relationship creation (endpoint retired)
        Assert.Throws<InvalidOperationException>(() =>
            graph.EditAt(CreateTime(schema, 350)).CreateRelationship(e1.Id, e2.Id, "DanglingRel"));

        // Verify Test 28: Canonical entity state unchanged
        Assert.Equal(entityCountBefore, graph.Entities.Count);
        Assert.False(graph.Entities[e1.Id].Properties.ContainsKey("illegal"));

        // Verify Test 29: Canonical relationship state unchanged
        Assert.Equal(relCountBefore, graph.Relationships.Count);
        Assert.Equal("Bond", graph.Relationships[rel.Id].Type);

        // Verify Test 30: Entity history unchanged
        Assert.Equal(e1HistoryCountBefore, graph.EntityHistories[e1.Id].Facts.Count);

        // Verify Test 31: Relationship history unchanged
        Assert.Equal(relHistoryCountBefore, graph.RelationshipHistories[rel.Id].Facts.Count);

        // Verify Test 32 & 33: No future facts or sequence counter modified
        Assert.True(graph.EntityHistories[e1.Id].Facts.All(f => f.PropertyName != "illegal"));

        // Verify Test 34: Reconstruction before and after rejected operation is strictly identical
        var snap150After = graph.TemporalQuery.At(CreateTime(schema, 150));
        var snap350After = graph.TemporalQuery.At(CreateTime(schema, 350));

        var diff150 = SorophyGraphDiff.Compare(snap150Before, snap150After);
        Assert.False(diff150.HasChanges);

        var diff350 = SorophyGraphDiff.Compare(snap350Before, snap350After);
        Assert.False(diff350.HasChanges);
    }

    /* =============================================================
     * 7. DETERMINISM
     * =============================================================
     */

    [Fact]
    public void Test35_Determinism_CanonCheckProducesDeterministicResults()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("DeterminismHero");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 300));

        for (int i = 0; i < 10; i++)
        {
            var resValid = graph.CanonCheck.CanSetEntityProperty(e.Id, "p", ValInt(1), CreateTime(schema, 200));
            Assert.True(resValid.IsValid);
            Assert.Equal(SorophyCanonViolationKind.None, resValid.ViolationKind);

            var resInvalid = graph.CanonCheck.CanSetEntityProperty(e.Id, "p", ValInt(1), CreateTime(schema, 350));
            Assert.False(resInvalid.IsValid);
            Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, resInvalid.ViolationKind);
            Assert.Contains("retired", resInvalid.Message);
        }
    }

    [Fact]
    public void Test36_Determinism_HistoricalReconstructionRemainsDeterministic()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Link" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        graph.EditAt(CreateTime(schema, 200)).SetEntityProperty(e1.Id, "score", ValInt(10));
        graph.EditAt(CreateTime(schema, 300)).ChangeRelationshipType(rel.Id, "StrongLink");

        // Repeated materializations across iterations produce identical snapshots
        for (int i = 0; i < 5; i++)
        {
            var snap1 = graph.TemporalQuery.At(CreateTime(schema, 250));
            var snap2 = graph.TemporalQuery.At(CreateTime(schema, 250));

            var diff = SorophyGraphDiff.Compare(snap1, snap2);
            Assert.False(diff.HasChanges);
        }
    }

    /* =============================================================
     * 8. PERSISTENCE ROUND-TRIP
     * =============================================================
     */

    [Fact]
    public void Test37_And_38_Persistence_CanonRelevantHistorySurvivesRoundTripAndProducesIdenticalResults()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("A");
        var e2 = CreateEntity("B");

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = e1.Id, TargetId = e2.Id, Type = "Link" };
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        graph.EditAt(CreateTime(schema, 200)).SetEntityProperty(e1.Id, "score", ValInt(10));
        graph.RetireEntity(e1.Id, CreateTime(schema, 400));

        // Roundtrip via LoreSerializer
        var json = LoreSerializer.Serialize(graph);
        var restoredGraph = LoreSerializer.Deserialize(json);

        // Verify Test 37: Canon-relevant history restored
        Assert.True(restoredGraph.TryGetEntityHistory(e1.Id, out var restoredHistory));
        Assert.NotNull(restoredHistory);
        Assert.True(restoredHistory!.IsRetired);
        Assert.Equal(CreateTime(schema, 400), restoredHistory.RetiredAt);

        // Verify Test 38: Restored graph produces identical canon check results
        var checkValidBefore = graph.CanonCheck.CanSetEntityProperty(e1.Id, "score", ValInt(20), CreateTime(schema, 250));
        var checkValidRestored = restoredGraph.CanonCheck.CanSetEntityProperty(e1.Id, "score", ValInt(20), CreateTime(schema, 250));
        Assert.Equal(checkValidBefore.IsValid, checkValidRestored.IsValid);
        Assert.Equal(checkValidBefore.ViolationKind, checkValidRestored.ViolationKind);

        var checkInvalidBefore = graph.CanonCheck.CanSetEntityProperty(e1.Id, "score", ValInt(20), CreateTime(schema, 450));
        var checkInvalidRestored = restoredGraph.CanonCheck.CanSetEntityProperty(e1.Id, "score", ValInt(20), CreateTime(schema, 450));
        Assert.Equal(checkInvalidBefore.IsValid, checkInvalidRestored.IsValid);
        Assert.Equal(checkInvalidBefore.ViolationKind, checkInvalidRestored.ViolationKind);

        var checkRelBefore = graph.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 450));
        var checkRelRestored = restoredGraph.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 450));
        Assert.Equal(checkRelBefore.IsValid, checkRelRestored.IsValid);
        Assert.Equal(checkRelBefore.ViolationKind, checkRelRestored.ViolationKind);
    }

    /* =============================================================
     * 9. BOUNDARY SEMANTICS EXPLICIT SUITE
     * =============================================================
     */

    [Fact]
    public void BoundarySemantics_ExplicitSuite_EntityAndRelationshipBoundaries()
    {
        /*
         * Tests all 5 boundaries:
         * 1. T < Created
         * 2. T == Created
         * 3. T < Retired
         * 4. T == Retired
         * 5. T > Retired
         */
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = CreateEntity("E1");
        var e2 = CreateEntity("E2");

        // Entities created at 100, retired at 300
        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));
        graph.RetireEntity(e1.Id, CreateTime(schema, 300));
        graph.RetireEntity(e2.Id, CreateTime(schema, 300));

        // 1. T < Created (T = 99)
        var e_before = graph.CanonCheck.CanSetEntityProperty(e1.Id, "p", ValInt(1), CreateTime(schema, 99));
        Assert.False(e_before.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, e_before.ViolationKind);
        Assert.Contains("uncreated", e_before.Message);

        // 2. T == Created (T = 100)
        var e_at_created = graph.CanonCheck.CanSetEntityProperty(e1.Id, "p", ValInt(1), CreateTime(schema, 100));
        Assert.True(e_at_created.IsValid);

        // 3. T < Retired (T = 299)
        var e_before_retire = graph.CanonCheck.CanSetEntityProperty(e1.Id, "p", ValInt(1), CreateTime(schema, 299));
        Assert.True(e_before_retire.IsValid);

        // 4. T == Retired (T = 300)
        var e_at_retire = graph.CanonCheck.CanSetEntityProperty(e1.Id, "p", ValInt(1), CreateTime(schema, 300));
        Assert.False(e_at_retire.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, e_at_retire.ViolationKind);
        Assert.Contains("retired", e_at_retire.Message);

        // 5. T > Retired (T = 301)
        var e_after_retire = graph.CanonCheck.CanSetEntityProperty(e1.Id, "p", ValInt(1), CreateTime(schema, 301));
        Assert.False(e_after_retire.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotActiveAtCoordinate, e_after_retire.ViolationKind);
        Assert.Contains("retired", e_after_retire.Message);

        // Now test relationship boundaries:
        var graph2 = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");
        graph2.CreateEntity(a, CreateTime(schema, 50));
        graph2.CreateEntity(b, CreateTime(schema, 50));

        var rel = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = a.Id, TargetId = b.Id, Type = "Link" };
        graph2.CreateRelationship(rel, CreateTime(schema, 100));
        graph2.RetireRelationship(rel.Id, CreateTime(schema, 300));

        // 1. T < Created (T = 99)
        var r_before = graph2.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 99));
        Assert.False(r_before.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, r_before.ViolationKind);

        // 2. T == Created (T = 100)
        var r_at_created = graph2.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 100));
        Assert.True(r_at_created.IsValid);

        // 3. T < Retired (T = 299)
        var r_before_retire = graph2.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 299));
        Assert.True(r_before_retire.IsValid);

        // 4. T == Retired (T = 300)
        var r_at_retire = graph2.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 300));
        Assert.False(r_at_retire.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, r_at_retire.ViolationKind);

        // 5. T > Retired (T = 301)
        var r_after_retire = graph2.CanonCheck.CanChangeRelationshipType(rel.Id, "NewType", CreateTime(schema, 301));
        Assert.False(r_after_retire.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotActiveAtCoordinate, r_after_retire.ViolationKind);
    }
}
