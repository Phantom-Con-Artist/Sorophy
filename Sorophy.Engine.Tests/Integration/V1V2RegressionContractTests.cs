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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Integration;

public sealed class V1V2RegressionContractTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Regression Timeline")
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

    [Fact]
    public void V1Contract_PureGraphWithoutTime_FunctionsCompletely()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "V1_Node_1" };
        var e2 = new SorophyEntity { Name = "V1_Node_2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "DirectEdge" };
        graph.AddRelationship(rel);

        Assert.True(graph.ContainsEntity(e1.Id));
        Assert.True(graph.ContainsEntity(e2.Id));
        Assert.True(graph.ContainsRelationship(rel.Id));
        Assert.True(graph.IsReachable(e1.Id, e2.Id));
        Assert.Empty(graph.RelationshipHistories);
        Assert.Empty(graph.Validate());
    }

    [Fact]
    public void V1Contract_BaselineObjectsWithoutHistory_AppearInSnapshots()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "Node1" };
        var e2 = new SorophyEntity { Name = "Node2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "BaselineEdge" };
        graph.AddRelationship(rel);

        // Even though no temporal evolution was applied, baseline objects project into any snapshot
        var snapshot = graph.CreateSnapshot(new SorophyTime(schema, "100", "Tick", SorophyTimePrecision.Exact));

        Assert.Equal(2, snapshot.EntityCount);
        Assert.Equal(1, snapshot.RelationshipCount);
        Assert.True(snapshot.ContainsEntity(e1.Id));
        Assert.True(snapshot.ContainsRelationship(rel.Id));
    }

    [Fact]
    public void V1Contract_PureV1GraphPersistence_RoundTripsAccurately()
    {
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Name = "EntityA", Type = "Category1" };
        var e2 = new SorophyEntity { Name = "EntityB", Type = "Category2" };
        e1.Tags.Add("ClassicTag");
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var rel = new SorophyRelationship { SourceId = e1.Id, TargetId = e2.Id, Type = "Connected" };
        graph.AddRelationship(rel);

        var json = LoreSerializer.Serialize(graph);
        var loaded = LoreSerializer.Deserialize(json);

        Assert.Empty(loaded.Validate());
        Assert.Equal(2, loaded.Entities.Count);
        Assert.Single(loaded.Relationships);
        Assert.Empty(loaded.RelationshipHistories);
        Assert.True(loaded.IsReachable(e1.Id, e2.Id));
    }
}

