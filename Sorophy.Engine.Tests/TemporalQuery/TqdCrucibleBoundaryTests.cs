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
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.TemporalQuery;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.TemporalQuery;

/// <summary>
/// Domain C01 (Boundary Assault), C12 (Invalid Input Assault), C13 (Temporal Compatibility Assault).
/// </summary>
public sealed class TqdCrucibleBoundaryTests
{
    private readonly SorophyTimeSchema _schema = TqdCrucibleTestHelper.CreateNumericSchema();

    /*
     * =============================================================
     * C01: TEMPORAL BOUNDARY ASSAULT
     * =============================================================
     */

    [Fact]
    public void Boundary_ImmediatelyBeforeAtAndAfter_Creation()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Alliance", t100));

        var tqd = graph.TemporalQuery;
        var t99 = TqdCrucibleTestHelper.CreateTime(_schema, 99);
        var t101 = TqdCrucibleTestHelper.CreateTime(_schema, 101);

        // Before creation: absent
        Assert.False(tqd.RelationshipExistsAt(relId, t99));
        Assert.Null(tqd.GetRelationshipAt(relId, t99));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(s, t99));

        // At creation: present
        Assert.True(tqd.RelationshipExistsAt(relId, t100));
        var snapAt = tqd.GetRelationshipAt(relId, t100);
        Assert.NotNull(snapAt);
        Assert.Equal("Alliance", snapAt.Type);
        Assert.Single(tqd.GetOutboundRelationshipsAt(s, t100));

        // After creation: present
        Assert.True(tqd.RelationshipExistsAt(relId, t101));
        Assert.NotNull(tqd.GetRelationshipAt(relId, t101));
    }

    [Fact]
    public void Boundary_ImmediatelyBeforeAtAndAfter_Termination()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Bond", t100));
        executor.Execute(graph, new SorophyRelationshipTermination(relId, t200));

        var tqd = graph.TemporalQuery;
        var t199 = TqdCrucibleTestHelper.CreateTime(_schema, 199);
        var t201 = TqdCrucibleTestHelper.CreateTime(_schema, 201);

        // Immediately before termination: present
        Assert.True(tqd.RelationshipExistsAt(relId, t199));
        Assert.NotNull(tqd.GetRelationshipAt(relId, t199));
        Assert.Single(tqd.GetOutboundRelationshipsAt(s, t199));

        // Exactly at termination: absent (post-transition semantics)
        Assert.False(tqd.RelationshipExistsAt(relId, t200));
        Assert.Null(tqd.GetRelationshipAt(relId, t200));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(s, t200));

        // Immediately after termination: absent
        Assert.False(tqd.RelationshipExistsAt(relId, t201));
        Assert.Null(tqd.GetRelationshipAt(relId, t201));
    }

    [Fact]
    public void Boundary_ImmediatelyBeforeAtAndAfter_Modification()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var initialProps = new Dictionary<string, SorophyProperty>
        {
            ["Power"] = new SorophyProperty { Name = "Power", Value = new SorophyValue(SorophyValueType.Integer, 10L) }
        };

        var modProps = new Dictionary<string, SorophyProperty>
        {
            ["Power"] = new SorophyProperty { Name = "Power", Value = new SorophyValue(SorophyValueType.Integer, 99L) }
        };

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "PowerLink", t100, initialProps));
        executor.Execute(graph, new SorophyRelationshipPropertyModification(relId, t200, propertiesToSet: modProps));

        var tqd = graph.TemporalQuery;
        var t199 = TqdCrucibleTestHelper.CreateTime(_schema, 199);
        var t201 = TqdCrucibleTestHelper.CreateTime(_schema, 201);

        // Before modification: 10
        var snapBefore = tqd.GetRelationshipAt(relId, t199);
        Assert.NotNull(snapBefore);
        Assert.Equal(10L, snapBefore.Properties["Power"].Value.Value);

        // At modification: 99
        var snapAt = tqd.GetRelationshipAt(relId, t200);
        Assert.NotNull(snapAt);
        Assert.Equal(99L, snapAt.Properties["Power"].Value.Value);

        // After modification: 99
        var snapAfter = tqd.GetRelationshipAt(relId, t201);
        Assert.NotNull(snapAfter);
        Assert.Equal(99L, snapAfter.Properties["Power"].Value.Value);
    }

    [Fact]
    public void Boundary_ImmediatelyBeforeAtAndAfter_ValidityChange()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var v100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var v300 = TqdCrucibleTestHelper.CreateTime(_schema, 300);
        var v150 = TqdCrucibleTestHelper.CreateTime(_schema, 150);
        var v500 = TqdCrucibleTestHelper.CreateTime(_schema, 500);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Pact", t100, validFrom: v100, validTill: v300));
        executor.Execute(graph, new SorophyRelationshipValidityChange(relId, t200, v150, v500));

        var tqd = graph.TemporalQuery;
        var t199 = TqdCrucibleTestHelper.CreateTime(_schema, 199);
        var t201 = TqdCrucibleTestHelper.CreateTime(_schema, 201);

        // Before change (T=199): historical fact captured ValidTill = t200 (the effective time)
        var snapBefore = tqd.GetRelationshipAt(relId, t199);
        Assert.NotNull(snapBefore);
        Assert.Equal(v100, snapBefore.ValidFrom);
        Assert.Equal(t200, snapBefore.ValidTill);

        // At change (T=200): new validity interval applies
        var snapAt = tqd.GetRelationshipAt(relId, t200);
        Assert.NotNull(snapAt);
        Assert.Equal(v150, snapAt.ValidFrom);
        Assert.Equal(v500, snapAt.ValidTill);

        // After change (T=201): new validity interval applies
        var snapAfter = tqd.GetRelationshipAt(relId, t201);
        Assert.NotNull(snapAfter);
        Assert.Equal(v150, snapAfter.ValidFrom);
        Assert.Equal(v500, snapAfter.ValidTill);
    }

    [Fact]
    public void Boundary_ZeroWidthInterval_IncludesOnlyExactCoordinate()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);
        var t300 = TqdCrucibleTestHelper.CreateTime(_schema, 300);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step1", t100));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step2", t200));
        executor.Execute(graph, new SorophyRelationshipTypeChange(relId, "Step3", t300));

        var tqd = graph.TemporalQuery;

        // Interval [200, 200]
        var exactFacts = tqd.GetFactsInInterval(t200, t200);
        Assert.Single(exactFacts);
        Assert.Equal(t200, exactFacts[0].At);

        // Interval [250, 250] where no facts occurred
        var t250 = TqdCrucibleTestHelper.CreateTime(_schema, 250);
        var emptyFacts = tqd.GetFactsInInterval(t250, t250);
        Assert.Empty(emptyFacts);
    }

    [Fact]
    public void Boundary_TightBoundaryIntervals_InclusionsAndExclusions()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Step", t200));

        var tqd = graph.TemporalQuery;
        var t199 = TqdCrucibleTestHelper.CreateTime(_schema, 199);
        var t201 = TqdCrucibleTestHelper.CreateTime(_schema, 201);
        var t198 = TqdCrucibleTestHelper.CreateTime(_schema, 198);
        var t202 = TqdCrucibleTestHelper.CreateTime(_schema, 202);

        // [199, 200] includes 200
        Assert.Single(tqd.GetFactsInInterval(t199, t200));

        // [200, 201] includes 200
        Assert.Single(tqd.GetFactsInInterval(t200, t201));

        // [198, 199] strictly excludes 200
        Assert.Empty(tqd.GetFactsInInterval(t198, t199));

        // [201, 202] strictly excludes 200
        Assert.Empty(tqd.GetFactsInInterval(t201, t202));
    }

    [Fact]
    public void Boundary_ExtremeRanges_DistantTemporalCoordinates()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out var t);
        var relId = Guid.NewGuid();
        var t0 = TqdCrucibleTestHelper.CreateTime(_schema, 0);

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(relId, s, t, "Eternal", t0));

        var tqd = graph.TemporalQuery;
        var tFarPast = TqdCrucibleTestHelper.CreateTime(_schema, -999999999999999L);
        var tFarFuture = TqdCrucibleTestHelper.CreateTime(_schema, 999999999999999L);

        // Prior to creation at T=0
        Assert.False(tqd.RelationshipExistsAt(relId, tFarPast));

        // Far future after creation
        Assert.True(tqd.RelationshipExistsAt(relId, tFarFuture));

        // Giant interval encompassing all
        var facts = tqd.GetFactsInInterval(tFarPast, tFarFuture);
        Assert.Single(facts);
    }

    /*
     * =============================================================
     * C12: INVALID INPUT ASSAULT
     * =============================================================
     */

    [Fact]
    public void InvalidInput_NullTimeArguments_ThrowArgumentNullException()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out _);
        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var relId = Guid.NewGuid();

        Assert.Throws<ArgumentNullException>(() => tqd.At((SorophyTime)null!));
        Assert.Throws<ArgumentNullException>(() => tqd.EntityExistsAt(s, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetEntityAt(s, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.RelationshipExistsAt(relId, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetRelationshipAt(relId, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetOutboundRelationshipsAt(s, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetInboundRelationshipsAt(s, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetFactsInInterval(null!, t100));
        Assert.Throws<ArgumentNullException>(() => tqd.GetFactsInInterval(t100, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetModifiedRelationshipIds(null!, t100));
        Assert.Throws<ArgumentNullException>(() => tqd.GetModifiedRelationshipIds(t100, null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetRelationshipFactsInInterval(relId, null!, t100));
        Assert.Throws<ArgumentNullException>(() => tqd.GetRelationshipFactsInInterval(relId, t100, null!));
    }

    [Fact]
    public void InvalidInput_NullEntityArguments_ThrowArgumentNullException()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;

        Assert.Throws<ArgumentNullException>(() => tqd.At((SorophyEntity)null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetFactsByEvent((SorophyEntity)null!));
        Assert.Throws<ArgumentNullException>(() => tqd.GetRelationshipsEvolvedByEvent((SorophyEntity)null!));
    }

    [Fact]
    public void InvalidInput_InvertedIntervals_ThrowArgumentException()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var t200 = TqdCrucibleTestHelper.CreateTime(_schema, 200);
        var relId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => tqd.GetFactsInInterval(t200, t100));
        Assert.Throws<ArgumentException>(() => tqd.GetModifiedRelationshipIds(t200, t100));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipFactsInInterval(relId, t200, t100));
    }

    [Fact]
    public void InvalidInput_EmptyGuid_RejectionOrEmpty()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);

        // History queries throw ArgumentException for empty Guid
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipHistory(Guid.Empty));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipFactsInInterval(Guid.Empty, t100, t100));
        Assert.Throws<ArgumentException>(() => tqd.GetFactsByEvent(Guid.Empty));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipsEvolvedByEvent(Guid.Empty));

        // Point-in-time entity/relationship lookups return false/null/empty for empty Guid
        Assert.False(tqd.EntityExistsAt(Guid.Empty, t100));
        Assert.Null(tqd.GetEntityAt(Guid.Empty, t100));
        Assert.False(tqd.RelationshipExistsAt(Guid.Empty, t100));
        Assert.Null(tqd.GetRelationshipAt(Guid.Empty, t100));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(Guid.Empty, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(Guid.Empty, t100));
    }

    [Fact]
    public void InvalidInput_NonEventEntity_ThrowsArgumentException()
    {
        var graph = TqdCrucibleTestHelper.CreateGraphWithEndpoints(out var s, out _);
        var tqd = graph.TemporalQuery;

        // Pass SorophyEntity of non-Event type
        var characterEntity = graph.Entities[s];
        Assert.Throws<ArgumentException>(() => tqd.At(characterEntity));
        Assert.Throws<ArgumentException>(() => tqd.GetFactsByEvent(characterEntity));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipsEvolvedByEvent(characterEntity));

        // Pass Guid of existing entity with non-Event type
        Assert.Throws<ArgumentException>(() => tqd.GetFactsByEvent(s));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipsEvolvedByEvent(s));
    }

    [Fact]
    public void InvalidInput_EventWithoutOccurredAt_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;

        var timelessEvent = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Timeless",
            Type = "Event",
            OccurredAt = null
        };
        graph.AddEntity(timelessEvent);

        Assert.Throws<ArgumentException>(() => tqd.At(timelessEvent));
    }

    [Fact]
    public void InvalidInput_UnknownIds_ReturnNullFalseOrEmpty()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;
        var t100 = TqdCrucibleTestHelper.CreateTime(_schema, 100);
        var randomId = Guid.NewGuid();

        Assert.False(tqd.EntityExistsAt(randomId, t100));
        Assert.Null(tqd.GetEntityAt(randomId, t100));
        Assert.False(tqd.RelationshipExistsAt(randomId, t100));
        Assert.Null(tqd.GetRelationshipAt(randomId, t100));
        Assert.Empty(tqd.GetOutboundRelationshipsAt(randomId, t100));
        Assert.Empty(tqd.GetInboundRelationshipsAt(randomId, t100));

        Assert.Null(tqd.GetRelationshipHistory(randomId));
        Assert.Empty(tqd.GetRelationshipFactsInInterval(randomId, t100, t100));
        Assert.Empty(tqd.GetFactsByEvent(randomId));
        Assert.Empty(tqd.GetRelationshipsEvolvedByEvent(randomId));
    }

    /*
     * =============================================================
     * C13: TEMPORAL COMPATIBILITY ASSAULT
     * =============================================================
     */

    [Fact]
    public void Compatibility_CrossTimelineCoordinates_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;

        var schema1 = TqdCrucibleTestHelper.CreateNumericSchema("Timeline 1");
        var schema2 = TqdCrucibleTestHelper.CreateNumericSchema("Timeline 2");

        var t1 = TqdCrucibleTestHelper.CreateTime(schema1, 100);
        var t2 = TqdCrucibleTestHelper.CreateTime(schema2, 200);

        Assert.Throws<ArgumentException>(() => tqd.GetFactsInInterval(t1, t2));
        Assert.Throws<ArgumentException>(() => tqd.GetModifiedRelationshipIds(t1, t2));
        Assert.Throws<ArgumentException>(() => tqd.GetRelationshipFactsInInterval(Guid.NewGuid(), t1, t2));
    }

    [Fact]
    public void Compatibility_CrossUnitCoordinatesInSameSchema_ThrowsArgumentException()
    {
        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;

        var schema = new SorophyTimeSchema(
            "MultiUnitTimeline",
            [
                new SorophyTimeUnit("Tick", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric)),
                new SorophyTimeUnit("Second", 1, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric))
            ]);

        var tTick = new SorophyTime(schema, "100", "Tick", SorophyTimePrecision.Exact);
        var tSecond = new SorophyTime(schema, "200", "Second", SorophyTimePrecision.Exact);

        Assert.Throws<ArgumentException>(() => tqd.GetFactsInInterval(tTick, tSecond));
        Assert.Throws<ArgumentException>(() => tqd.GetModifiedRelationshipIds(tTick, tSecond));
    }

    [Fact]
    public void Compatibility_NonNumericPositionDefinition_ThrowsInvalidOperationException()
    {
        var stringSchema = new SorophyTimeSchema(
            "StringTimeline",
            [
                new SorophyTimeUnit("Phase", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Named))
            ]);

        var tPhaseA = new SorophyTime(stringSchema, "Alpha", "Phase", SorophyTimePrecision.Exact);
        var tPhaseB = new SorophyTime(stringSchema, "Beta", "Phase", SorophyTimePrecision.Exact);

        var graph = new SorophyGraph();
        var tqd = graph.TemporalQuery;

        Assert.Throws<InvalidOperationException>(() => tqd.GetFactsInInterval(tPhaseA, tPhaseB));
    }
}
