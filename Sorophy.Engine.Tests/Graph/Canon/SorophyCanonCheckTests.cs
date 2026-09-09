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

public sealed class SorophyCanonCheckTests
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
    public void CanCreateEntity_Valid_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        var result = graph.CanCreateEntity(entity, CreateTime(schema, "100"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void CanCreateEntity_DuplicateId_ReturnsEntityAlreadyExists()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));

        var duplicate = new SorophyEntity { Id = entity.Id, Name = "DuplicateHero" };
        var result = graph.CanCreateEntity(duplicate, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityAlreadyExists, result.ViolationKind);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public void CanCreateEntity_EmptyGuid_ReturnsEntityNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanCreateEntity(Guid.Empty, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanRetireEntity_ValidRetirement_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));

        var result = graph.CanRetireEntity(entity.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
    }

    [Fact]
    public void CanRetireEntity_NonExistentEntity_ReturnsEntityNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanRetireEntity(Guid.NewGuid(), CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanRetireEntity_PrecedesCreation_ReturnsPrecedesCreation()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "200"));

        var result = graph.CanRetireEntity(entity.Id, CreateTime(schema, "100"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.PrecedesCreation, result.ViolationKind);
        Assert.Contains("cannot precede creation", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireEntity_IncompatibleTimeline_ReturnsIncompatibleTimeline()
    {
        var schema1 = CreateSchema("Timeline1");
        var schema2 = CreateSchema("Timeline2");
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema1, "100"));

        var result = graph.CanRetireEntity(entity.Id, CreateTime(schema2, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.IncompatibleTimeline, result.ViolationKind);
        Assert.Contains("not comparable", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireEntity_AlreadyRetired_ReturnsAlreadyRetired()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "300"));

        var result = graph.CanRetireEntity(entity.Id, CreateTime(schema, "400"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.AlreadyRetired, result.ViolationKind);
        Assert.Contains("already has an established retirement", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireEntity_IncidentRelationshipExistsInFuture_ReturnsContradictsIncidentRelationships()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };
        var sword = new SorophyEntity { Id = Guid.NewGuid(), Name = "Excalibur" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.CreateEntity(sword, CreateTime(schema, "100"));

        // Record a relationship fact at year 400 with active existence
        var relId = Guid.NewGuid();
        var relFact = new SorophyRelationshipFact(
            CreateTime(schema, "400"),
            relId,
            hero.Id,
            sword.Id,
            "Wields",
            new Dictionary<string, SorophyProperty>(),
            CreateTime(schema, "400"),
            null); // ValidTill null = active existence

        graph.RecordRelationshipFact(relFact);

        // Attempting to retire hero at 300 should be rejected because hero is required at 400
        var result = graph.CanRetireEntity(hero.Id, CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.ContradictsIncidentRelationships, result.ViolationKind);
        Assert.Contains("established existence at", result.ErrorMessage);
    }

    [Fact]
    public void CanRetireEntity_UnrelatedRelationshipInFuture_DoesNotConflict()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };
        var sword = new SorophyEntity { Id = Guid.NewGuid(), Name = "Excalibur" };
        var mage = new SorophyEntity { Id = Guid.NewGuid(), Name = "Merlin" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.CreateEntity(sword, CreateTime(schema, "100"));
        graph.CreateEntity(mage, CreateTime(schema, "100"));

        // Record relationship fact between mage and sword at 400 (hero is NOT involved)
        var relId = Guid.NewGuid();
        var relFact = new SorophyRelationshipFact(
            CreateTime(schema, "400"),
            relId,
            mage.Id,
            sword.Id,
            "Enchants",
            new Dictionary<string, SorophyProperty>(),
            CreateTime(schema, "400"),
            null);

        graph.RecordRelationshipFact(relFact);

        // Retiring hero at 300 should succeed
        var result = graph.CanRetireEntity(hero.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CanRetireEntity_IncidentRelationshipTerminatedPriorToRetirement_DoesNotConflict()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };
        var sword = new SorophyEntity { Id = Guid.NewGuid(), Name = "Excalibur" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.CreateEntity(sword, CreateTime(schema, "100"));

        // Relationship fact occurred at 200 (prior to 300)
        var relId = Guid.NewGuid();
        var relFact = new SorophyRelationshipFact(
            CreateTime(schema, "200"),
            relId,
            hero.Id,
            sword.Id,
            "Wields",
            new Dictionary<string, SorophyProperty>(),
            CreateTime(schema, "200"),
            null);

        graph.RecordRelationshipFact(relFact);

        // Retiring hero at 300 should succeed because the fact at 200 precedes retirement at 300
        var result = graph.CanRetireEntity(hero.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CanRevertRetirement_ValidReversal_ReturnsSuccess()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "300"));

        var result = graph.CanRevertRetirement(entity.Id, CreateTime(schema, "300"));

        Assert.True(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.None, result.ViolationKind);
    }

    [Fact]
    public void CanRevertRetirement_EntityNotFound_ReturnsEntityNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var result = graph.CanRevertRetirement(Guid.NewGuid(), CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.EntityNotFound, result.ViolationKind);
    }

    [Fact]
    public void CanRevertRetirement_NoRetirementFact_ReturnsRetirementNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));

        var result = graph.CanRevertRetirement(entity.Id, CreateTime(schema, "300"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RetirementNotFound, result.ViolationKind);
        Assert.Contains("no established retirement fact", result.ErrorMessage);
    }

    [Fact]
    public void CanRevertRetirement_MismatchedCoordinate_ReturnsRetirementNotFound()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Hero" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "300"));

        var result = graph.CanRevertRetirement(entity.Id, CreateTime(schema, "400"));

        Assert.False(result.IsValid);
        Assert.Equal(SorophyCanonViolationKind.RetirementNotFound, result.ViolationKind);
        Assert.Contains("does not have a retirement fact at coordinate", result.ErrorMessage);
    }
}

