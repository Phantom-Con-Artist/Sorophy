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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.Canon;

public sealed class SorophyRelationshipCanonCheckTests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(string timeline = "Test Timeline")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit("Year", 0, NumericPosition)
            });
    }

    private static SorophyTime CreateTime(SorophyTimeSchema schema, string position)
    {
        return new SorophyTime(schema, position, "Year", SorophyTimePrecision.Exact);
    }

    [Fact]
    public void CanCreateRelationship_Valid_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "100"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_DuplicateId_ReturnsRelationshipAlreadyExists()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));

        var duplicate = new SorophyRelationship
        {
            Id = rel.Id,
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Duplicate"
        };

        var result = graph.CanCreateRelationship(duplicate, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipAlreadyExists, result.ViolationKind);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_EmptyId_ReturnsRelationshipNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var result = graph.CanCreateRelationship(Guid.Empty, source.Id, target.Id, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanCreateRelationship_MissingSourceEntity_ReturnsEntityNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            TargetId = target.Id,
            Type = "Connected"
        };

        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotFound, result.ViolationKind);
        Assert.Contains("Source entity", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_MissingTargetEntity_ReturnsEntityNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        graph.CreateEntity(source, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = Guid.NewGuid(),
            Type = "Connected"
        };

        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotFound, result.ViolationKind);
        Assert.Contains("Target entity", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_SourceNotActiveAtTime_ReturnsEndpointNotActiveAtCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "200")); // Created at 200
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        // Attempting to create relationship at 100 when source doesn't exist until 200
        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, result.ViolationKind);
        Assert.Contains("source entity", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_TargetNotActiveAtTime_ReturnsEndpointNotActiveAtCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "200")); // Created at 200

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        // Attempting to create relationship at 100 when target doesn't exist until 200
        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, result.ViolationKind);
        Assert.Contains("target entity", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateRelationship_SourceRetiredAtTime_ReturnsEndpointNotActiveAtCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));
        graph.RetireEntity(source.Id, CreateTime(schema, "300"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        // Attempting to create relationship at 350 when source was retired at 300
        var result = graph.CanCreateRelationship(rel, CreateTime(schema, "350"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EndpointNotActiveAtCoordinate, result.ViolationKind);
    }

    [Fact]
    public void CanRetireRelationship_ValidRetirement_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));

        var result = graph.CanRetireRelationship(rel.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
    }

    [Fact]
    public void CanRetireRelationship_NonExistentRelationship_ReturnsRelationshipNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanRetireRelationship(Guid.NewGuid(), CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanRetireRelationship_EmptyId_ReturnsRelationshipNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanRetireRelationship(Guid.Empty, CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanRetireRelationship_PrecedesCreation_ReturnsPrecedesCreation()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));

        var result = graph.CanRetireRelationship(rel.Id, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.PrecedesCreation, result.ViolationKind);
        Assert.Contains("cannot precede creation", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireRelationship_IncompatibleTimeline_ReturnsIncompatibleTimeline()
    {
        var schema1 = CreateSchema("Timeline1");
        var schema2 = CreateSchema("Timeline2");
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema1, "100"));
        graph.CreateEntity(target, CreateTime(schema1, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema1, "100"));

        var result = graph.CanRetireRelationship(rel.Id, CreateTime(schema2, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.IncompatibleTimeline, result.ViolationKind);
        Assert.Contains("not comparable", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireRelationship_AlreadyRetired_ReturnsAlreadyRetired()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        var result = graph.CanRetireRelationship(rel.Id, CreateTime(schema, "400"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.AlreadyRetired, result.ViolationKind);
        Assert.Contains("already has an established retirement", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireRelationship_ContradictsEstablishedLaterState_ReturnsConflict()
    {
        /*
         * MANDATORY CANON CHECK CASE:
         * Relationship R is created at 100.
         * The world is established at 500 (e.g., via another entity created at 500).
         * At 500, R is established as active.
         * Author attempts to retire R at 300.
         * Canon Check rejects with ContradictsEstablishedLaterState because R was established active at 500.
         */
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var person = new SorophyEntity { Id = Guid.NewGuid(), Name = "Alice" };
        var group = new SorophyEntity { Id = Guid.NewGuid(), Name = "Guild" };
        var milestone = new SorophyEntity { Id = Guid.NewGuid(), Name = "EventAt500" };

        graph.CreateEntity(person, CreateTime(schema, "100"));
        graph.CreateEntity(group, CreateTime(schema, "100"));
        graph.CreateEntity(milestone, CreateTime(schema, "500")); // establishes T_established coordinate 500

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = person.Id,
            TargetId = group.Id,
            Type = "MemberOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));

        // At coordinate 500, rel is currently active.
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "500")));

        // Attempting to retire rel at 300 must be rejected
        var result = graph.CanRetireRelationship(rel.Id, CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.ContradictsEstablishedLaterState, result.ViolationKind);
        Assert.Contains("established as active at later coordinate", result.ErrorMessage);
    }

    [Fact]
    public void CanRevertRelationshipRetirement_ValidReversal_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        var result = graph.CanRevertRelationshipRetirement(rel.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
    }

    [Fact]
    public void CanRevertRelationshipRetirement_NotRetired_ReturnsRetirementNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));

        var result = graph.CanRevertRelationshipRetirement(rel.Id, CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RetirementNotFound, result.ViolationKind);
        Assert.Contains("no established retirement fact", result.ErrorMessage);
    }

    [Fact]
    public void CanRevertRelationshipRetirement_WrongCoordinate_ReturnsRetirementNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "Source" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "Target" };

        graph.CreateEntity(source, CreateTime(schema, "100"));
        graph.CreateEntity(target, CreateTime(schema, "100"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Connected"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "100"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        var result = graph.CanRevertRelationshipRetirement(rel.Id, CreateTime(schema, "400"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RetirementNotFound, result.ViolationKind);
        Assert.Contains("does not have a retirement fact at coordinate", result.ErrorMessage);
    }

    [Fact]
    public void CanRevertRelationshipRetirement_NonExistentRelationship_ReturnsRelationshipNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanRevertRelationshipRetirement(Guid.NewGuid(), CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RelationshipNotFound, result.ViolationKind);
    }
}
