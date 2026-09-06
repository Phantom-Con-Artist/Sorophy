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
using Sorophy.Engine.Diff;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Diff;

public sealed class GraphDiffHardeningTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Diff Hardening Timeline")
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

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Tick", SorophyTimePrecision.Exact);
    }

    [Fact]
    public void Diff_EmptyVsEmpty_HasZeroChanges()
    {
        var schema = CreateSchema();
        var g1 = new SorophyGraph();
        var g2 = new SorophyGraph();

        var snap1 = g1.CreateSnapshot(CreateTime(schema, "1"));
        var snap2 = g2.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.False(diff.HasChanges);
        Assert.Equal(0, diff.TotalChanges);
    }

    [Fact]
    public void Diff_EmptyVsPopulated_AllElementsReportedAsAdded()
    {
        var schema = CreateSchema();
        var gEmpty = new SorophyGraph();
        var gPop = new SorophyGraph();

        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        gPop.AddEntity(e1);
        gPop.AddEntity(e2);
        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "Edge" };
        gPop.AddRelationship(rel);

        var snapEmpty = gEmpty.CreateSnapshot(CreateTime(schema, "1"));
        var snapPop = gPop.CreateSnapshot(CreateTime(schema, "1"));

        var diff = SorophyGraphDiff.Compare(snapEmpty, snapPop);

        Assert.True(diff.HasChanges);
        Assert.Equal(3, diff.TotalChanges);
        Assert.Equal(2, diff.AddedEntities.Count);
        Assert.Single(diff.AddedRelationships);
        Assert.Empty(diff.RemovedEntities);
        Assert.Empty(diff.RemovedRelationships);
        Assert.Empty(diff.ModifiedEntities);
        Assert.Empty(diff.ModifiedRelationships);
    }

    [Fact]
    public void Diff_PopulatedVsEmpty_AllElementsReportedAsRemoved()
    {
        var schema = CreateSchema();
        var gEmpty = new SorophyGraph();
        var gPop = new SorophyGraph();

        var e1 = new SorophyEntity { Name = "E1" };
        var e2 = new SorophyEntity { Name = "E2" };
        gPop.AddEntity(e1);
        gPop.AddEntity(e2);
        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "Edge" };
        gPop.AddRelationship(rel);

        var snapEmpty = gEmpty.CreateSnapshot(CreateTime(schema, "1"));
        var snapPop = gPop.CreateSnapshot(CreateTime(schema, "1"));

        var diff = SorophyGraphDiff.Compare(snapPop, snapEmpty);

        Assert.True(diff.HasChanges);
        Assert.Equal(3, diff.TotalChanges);
        Assert.Equal(2, diff.RemovedEntities.Count);
        Assert.Single(diff.RemovedRelationships);
        Assert.Empty(diff.AddedEntities);
        Assert.Empty(diff.AddedRelationships);
    }

    [Fact]
    public void Diff_DeepNestedValueEquivalence_DoesNotFalsePositiveOnClonedCollections()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Nested" };

        var complexDict = new Dictionary<string, object?>
        {
            ["list"] = new List<object?> { 10L, 20L, new Dictionary<string, object?> { ["leaf"] = "abc" } },
            ["str"] = "hello"
        };

        entity.Properties["Tree"] = new SorophyProperty
        {
            Name = "Tree",
            Value = new SorophyValue(SorophyValueType.Object, complexDict)
        };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        // Cloned references across snapshots must structurally match without false positive modification
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.ModifiedEntities);
    }

    [Fact]
    public void Diff_RelationshipValidityEvolution_ReportsModifiedWithBothStates()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s.Id, t.Id, "Edge", CreateTime(schema, "10"), validFrom: CreateTime(schema, "10")));

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "10"));

        executor.Execute(graph, new SorophyRelationshipValidityChange(
            relId, CreateTime(schema, "20"), newValidFrom: CreateTime(schema, "10"), newValidTill: CreateTime(schema, "50")));

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "20"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedRelationships);

        var mod = diff.ModifiedRelationships[0];
        Assert.Equal(relId, mod.RelationshipId);
        Assert.Null(mod.Before!.ValidTill);
        Assert.Equal(CreateTime(schema, "50"), mod.After!.ValidTill);
    }
}
