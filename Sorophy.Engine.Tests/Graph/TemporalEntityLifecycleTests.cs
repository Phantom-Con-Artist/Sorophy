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
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public sealed class TemporalEntityLifecycleTests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(string timeline = "Kingdom Timeline")
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
    public void EntityCreation_LifecycleAndSnapshotProjections_CorrectAcrossTime()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "King Arthur",
            Type = "Character"
        };

        // Create entity at Year 100
        graph.CreateEntity(entity, CreateTime(schema, "100"));

        Assert.True(graph.ContainsEntity(entity.Id));
        Assert.True(graph.TryGetEntityHistory(entity.Id, out var history));
        Assert.NotNull(history);
        Assert.Single(history.Facts);
        Assert.Equal(SorophyEntityFactKind.Created, history.Facts[0].Kind);
        Assert.Equal(CreateTime(schema, "100"), history.CreatedAt);
        Assert.False(graph.IsEntityRetired(entity.Id));

        // Prior to creation: absent
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, "50")));
        Assert.Equal(SorophyEntityLifecycleStatus.Uncreated, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, "50")));
        var snap50 = graph.CreateSnapshot(CreateTime(schema, "50"));
        Assert.False(snap50.ContainsEntity(entity.Id));

        // At creation: present (inclusive)
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, "100")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, "100")));
        var snap100 = graph.CreateSnapshot(CreateTime(schema, "100"));
        Assert.True(snap100.ContainsEntity(entity.Id));

        // Future: present
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, "250")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, "250")));
        var snap250 = graph.CreateSnapshot(CreateTime(schema, "250"));
        Assert.True(snap250.ContainsEntity(entity.Id));
    }

    [Fact]
    public void EntityRetirement_LifecycleAndCanonicalPreservation_PreservesCanonicalData()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur", Type = "Character" };
        var sword = new SorophyEntity { Id = Guid.NewGuid(), Name = "Excalibur", Type = "Item" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.CreateEntity(sword, CreateTime(schema, "100"));

        var relationship = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = hero.Id,
            TargetId = sword.Id,
            Type = "Wields"
        };
        graph.AddRelationship(relationship);

        // Retire hero at Year 300
        var retired = graph.RetireEntity(hero.Id, CreateTime(schema, "300"), "Passed into Avalon");
        Assert.True(retired);
        Assert.True(graph.IsEntityRetired(hero.Id));

        // Invariant: Canonical storage is NOT mutated
        Assert.True(graph.ContainsEntity(hero.Id));
        Assert.True(graph.ContainsRelationship(relationship.Id));

        // Temporal queries reflect post-transition retirement
        Assert.True(graph.EntityExistsAt(hero.Id, CreateTime(schema, "299")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(hero.Id, CreateTime(schema, "299")));

        Assert.False(graph.EntityExistsAt(hero.Id, CreateTime(schema, "300")));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, graph.GetEntityLifecycleStatus(hero.Id, CreateTime(schema, "300")));

        Assert.False(graph.EntityExistsAt(hero.Id, CreateTime(schema, "400")));
        Assert.Equal(SorophyEntityLifecycleStatus.Retired, graph.GetEntityLifecycleStatus(hero.Id, CreateTime(schema, "400")));

        // Snapshot projections
        var snap200 = graph.CreateSnapshot(CreateTime(schema, "200"));
        Assert.True(snap200.ContainsEntity(hero.Id));
        Assert.True(snap200.ContainsRelationship(relationship.Id));

        // At Year 300: hero is retired, so relationship is automatically pruned from snapshot
        var snap300 = graph.CreateSnapshot(CreateTime(schema, "300"));
        Assert.False(snap300.ContainsEntity(hero.Id));
        Assert.True(snap300.ContainsEntity(sword.Id));
        Assert.False(snap300.ContainsRelationship(relationship.Id)); // Pruned by referential integrity!
    }

    [Fact]
    public void RetirementReversal_RemovesRetirementFactWithoutUnretiredTrace()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur", Type = "Character" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "300"));

        Assert.True(graph.IsEntityRetired(entity.Id));
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, "350")));

        // Revert retirement at 300
        var reverted = graph.RevertRetirement(entity.Id, CreateTime(schema, "300"));
        Assert.True(reverted);

        // Authoritative verification: NO Unretired fact exists!
        Assert.True(graph.TryGetEntityHistory(entity.Id, out var history));
        Assert.NotNull(history);
        Assert.Single(history.Facts);
        Assert.Equal(SorophyEntityFactKind.Created, history.Facts[0].Kind);
        Assert.Null(history.RetirementFact);
        Assert.Null(history.RetiredAt);
        Assert.False(graph.IsEntityRetired(entity.Id));

        // Entity is now active again across all T >= 100
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, "300")));
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, "350")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(entity.Id, CreateTime(schema, "350")));

        var snap350 = graph.CreateSnapshot(CreateTime(schema, "350"));
        Assert.True(snap350.ContainsEntity(entity.Id));
    }

    [Fact]
    public void SingleRetirementInvariant_MovingRetirementRequiresRevertThenRetire()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur" };

        graph.CreateEntity(entity, CreateTime(schema, "100"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "300"));

        // Attempting a second retirement directly is rejected
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireEntity(entity.Id, CreateTime(schema, "400")));
        Assert.Contains("already has an established retirement", ex.Message);

        // Move retirement by reverting then authoring new retirement point
        graph.RevertRetirement(entity.Id, CreateTime(schema, "300"));
        graph.RetireEntity(entity.Id, CreateTime(schema, "400"));

        Assert.True(graph.TryGetEntityHistory(entity.Id, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history.Facts.Count);
        Assert.Equal(CreateTime(schema, "400"), history.RetiredAt);

        // Active between 300 and 399
        Assert.True(graph.EntityExistsAt(entity.Id, CreateTime(schema, "350")));
        Assert.False(graph.EntityExistsAt(entity.Id, CreateTime(schema, "400")));
    }

    [Fact]
    public void PermanentDeletion_PurgesEntityAdjacencyAndHistory()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur" };
        var sword = new SorophyEntity { Id = Guid.NewGuid(), Name = "Excalibur" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.CreateEntity(sword, CreateTime(schema, "100"));
        graph.RetireEntity(hero.Id, CreateTime(schema, "300"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = hero.Id,
            TargetId = sword.Id,
            Type = "Wields"
        };
        graph.AddRelationship(rel);

        // Permanent deletion
        var deleted = graph.DeleteEntity(hero.Id);
        Assert.True(deleted);

        Assert.False(graph.ContainsEntity(hero.Id));
        Assert.False(graph.ContainsRelationship(rel.Id));
        Assert.False(graph.TryGetEntityHistory(hero.Id, out _));
        Assert.False(graph.EntityExistsAt(hero.Id, CreateTime(schema, "200")));
        Assert.Empty(graph.Validate());
    }

    [Fact]
    public void PersistenceRoundTrip_CreatedAndRetiredFacts_SerializeAndDeserializeCleanly()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur", Type = "Character" };
        hero.Tags.Add("Legend");

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.RetireEntity(hero.Id, CreateTime(schema, "300"), "Passed to Avalon");

        var json = LoreSerializer.Serialize(graph);
        Assert.Contains("entityHistories", json);
        Assert.Contains("Created", json);
        Assert.Contains("Retired", json);

        var restored = LoreSerializer.Deserialize(json);
        Assert.Empty(restored.Validate());

        Assert.True(restored.ContainsEntity(hero.Id));
        Assert.True(restored.TryGetEntityHistory(hero.Id, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history.Facts.Count);
        Assert.Equal(CreateTime(schema, "100"), history.CreatedAt);
        Assert.Equal(CreateTime(schema, "300"), history.RetiredAt);
        Assert.True(restored.IsEntityRetired(hero.Id));

        Assert.False(restored.EntityExistsAt(hero.Id, CreateTime(schema, "50")));
        Assert.True(restored.EntityExistsAt(hero.Id, CreateTime(schema, "200")));
        Assert.False(restored.EntityExistsAt(hero.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void PersistenceRoundTrip_RevertedRetirement_HasZeroTraceOfRetirement()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var hero = new SorophyEntity { Id = Guid.NewGuid(), Name = "Arthur", Type = "Character" };

        graph.CreateEntity(hero, CreateTime(schema, "100"));
        graph.RetireEntity(hero.Id, CreateTime(schema, "300"));
        graph.RevertRetirement(hero.Id, CreateTime(schema, "300"));

        var json = LoreSerializer.Serialize(graph);

        // Invariant: Authoritative world model contains zero trace of retirement
        Assert.DoesNotContain("Retired", json);
        Assert.DoesNotContain("Unretired", json);

        var restored = LoreSerializer.Deserialize(json);
        Assert.False(restored.IsEntityRetired(hero.Id));
        Assert.True(restored.EntityExistsAt(hero.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void LegacyL0Compatibility_EntitiesWithoutHistory_ExistAcrossAllCoordinates()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var legacyEntity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Timeless Mountain",
            Type = "Location"
        };

        // Restored directly without authoring a creation fact (legacy L-0 path)
        graph.RestoreEntity(legacyEntity);

        // Exists across all temporal coordinates
        Assert.True(graph.EntityExistsAt(legacyEntity.Id, CreateTime(schema, "1")));
        Assert.True(graph.EntityExistsAt(legacyEntity.Id, CreateTime(schema, "1000")));
        Assert.Equal(SorophyEntityLifecycleStatus.Active, graph.GetEntityLifecycleStatus(legacyEntity.Id, CreateTime(schema, "1")));
        Assert.False(graph.IsEntityRetired(legacyEntity.Id));

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        Assert.True(snap1.ContainsEntity(legacyEntity.Id));

        // Can subsequently be retired at Year 500
        graph.RetireEntity(legacyEntity.Id, CreateTime(schema, "500"));
        Assert.True(graph.EntityExistsAt(legacyEntity.Id, CreateTime(schema, "499")));
        Assert.False(graph.EntityExistsAt(legacyEntity.Id, CreateTime(schema, "500")));
    }
}

