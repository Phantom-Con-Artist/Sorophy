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
using Sorophy.Engine.Diff;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Snapshot;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Diff;

public sealed class GraphDiffTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "Diff Timeline")
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

    private static SorophyGraph CreateGraphWithEntities(
        out Guid sourceId,
        out Guid targetId)
    {
        var graph = new SorophyGraph();
        sourceId = Guid.NewGuid();
        targetId = Guid.NewGuid();

        graph.AddEntity(new SorophyEntity { Id = sourceId, Name = "SourceNode", Type = "Node" });
        graph.AddEntity(new SorophyEntity { Id = targetId, Name = "TargetNode", Type = "Node" });

        return graph;
    }

    // =============================================================
    // 1. CONTRACT & ARGUMENT VALIDATION
    // =============================================================

    [Fact]
    public void Compare_NullBefore_ThrowsArgumentNullException()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var snapshot = graph.CreateSnapshot(CreateTime(schema, "1"));

        Assert.Throws<ArgumentNullException>(() =>
            SorophyGraphDiff.Compare(null!, snapshot));
    }

    [Fact]
    public void Compare_NullAfter_ThrowsArgumentNullException()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var snapshot = graph.CreateSnapshot(CreateTime(schema, "1"));

        Assert.Throws<ArgumentNullException>(() =>
            SorophyGraphDiff.Compare(snapshot, null!));
    }

    [Fact]
    public void Compare_SameSnapshotReference_ReturnsEmptyChangeSet()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var snapshot = graph.CreateSnapshot(CreateTime(schema, "1"));

        var diff = SorophyGraphDiff.Compare(snapshot, snapshot);

        Assert.NotNull(diff);
        Assert.False(diff.HasChanges);
        Assert.Equal(0, diff.TotalChanges);
        Assert.Empty(diff.EntityChanges);
        Assert.Empty(diff.RelationshipChanges);
        Assert.Empty(diff.AddedEntities);
        Assert.Empty(diff.RemovedEntities);
        Assert.Empty(diff.ModifiedEntities);
        Assert.Empty(diff.AddedRelationships);
        Assert.Empty(diff.RemovedRelationships);
        Assert.Empty(diff.ModifiedRelationships);
        Assert.Same(snapshot, diff.Before);
        Assert.Same(snapshot, diff.After);
    }

    [Fact]
    public void Diff_ExtensionMethod_MatchesCompare()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = snap1.Diff(snap2);

        Assert.NotNull(diff);
        Assert.False(diff.HasChanges);
        Assert.Same(snap1, diff.Before);
        Assert.Same(snap2, diff.After);
    }

    // =============================================================
    // 2. STATE-NOT-HISTORY SEMANTICS
    // =============================================================

    [Fact]
    public void StateNotHistory_ReversibleMutation_ReportsNoDifference()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();

        var entity = new SorophyEntity
        {
            Id = entityId,
            Name = "InitialName",
            Type = "Item"
        };
        entity.Properties["Level"] = new SorophyProperty
        {
            Name = "Level",
            Value = new SorophyValue(SorophyValueType.Integer, 10L)
        };
        graph.AddEntity(entity);

        var snapT1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        // Mutate entity to intermediate state
        entity.Name = "IntermediateName";
        entity.Properties["Level"].Value = new SorophyValue(SorophyValueType.Integer, 99L);

        // Revert entity back to original observable state
        entity.Name = "InitialName";
        entity.Properties["Level"].Value = new SorophyValue(SorophyValueType.Integer, 10L);

        var snapT2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snapT1, snapT2);

        Assert.False(diff.HasChanges);
        Assert.Equal(0, diff.TotalChanges);
    }

    [Fact]
    public void StateNotHistory_IntermediateRemovalAndRecreation_ReportsNoDifferenceWhenStateMatches()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entityId = Guid.NewGuid();

        var entity1 = new SorophyEntity
        {
            Id = entityId,
            Name = "IdentityNode",
            Type = "Node"
        };
        graph.AddEntity(entity1);

        var snapT1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        // Remove entity
        graph.RemoveEntity(entityId);

        // Recreate entity with same identity and state
        var entity2 = new SorophyEntity
        {
            Id = entityId,
            Name = "IdentityNode",
            Type = "Node"
        };
        graph.AddEntity(entity2);

        var snapT2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snapT1, snapT2);

        // Graph Diff compares endpoint snapshot state only and cannot detect intermediate removal/recreation
        Assert.False(diff.HasChanges);
        Assert.Equal(0, diff.TotalChanges);
    }

    // =============================================================
    // 3. ENTITY ADDITION & REMOVAL
    // =============================================================

    [Fact]
    public void EntityAdded_DetectsNewEntity()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.NewGuid(), Name = "E1" };
        graph.AddEntity(e1);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        var e2 = new SorophyEntity { Id = Guid.NewGuid(), Name = "E2" };
        graph.AddEntity(e2);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Equal(1, diff.TotalChanges);
        Assert.Single(diff.AddedEntities);
        Assert.Empty(diff.RemovedEntities);
        Assert.Empty(diff.ModifiedEntities);
        Assert.Equal(e2.Id, diff.AddedEntities[0].Id);

        var change = diff.EntityChanges.Single();
        Assert.Equal(GraphChangeKind.Added, change.Kind);
        Assert.Equal(e2.Id, change.EntityId);
        Assert.Null(change.Before);
        Assert.NotNull(change.After);
        Assert.Equal("E2", change.After.Name);
    }

    [Fact]
    public void EntityRemoved_DetectsDeletedEntity()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e1 = new SorophyEntity { Id = Guid.NewGuid(), Name = "E1" };
        var e2 = new SorophyEntity { Id = Guid.NewGuid(), Name = "E2" };
        graph.AddEntity(e1);
        graph.AddEntity(e2);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        graph.RemoveEntity(e2.Id);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Equal(1, diff.TotalChanges);
        Assert.Empty(diff.AddedEntities);
        Assert.Single(diff.RemovedEntities);
        Assert.Empty(diff.ModifiedEntities);
        Assert.Equal(e2.Id, diff.RemovedEntities[0].Id);

        var change = diff.EntityChanges.Single();
        Assert.Equal(GraphChangeKind.Removed, change.Kind);
        Assert.Equal(e2.Id, change.EntityId);
        Assert.NotNull(change.Before);
        Assert.Null(change.After);
        Assert.Equal("E2", change.Before.Name);
    }

    // =============================================================
    // 4. ENTITY MODIFICATIONS
    // =============================================================

    [Fact]
    public void EntityModified_NameChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Alice", Type = "Person" };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Name = "Alicia";
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        var mod = diff.ModifiedEntities[0];
        Assert.Equal(GraphChangeKind.Modified, mod.Kind);
        Assert.Equal(entity.Id, mod.EntityId);
        Assert.Equal("Alice", mod.Before?.Name);
        Assert.Equal("Alicia", mod.After?.Name);
    }

    [Fact]
    public void EntityModified_TypeChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Object1", Type = "Item" };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Type = "Artifact";
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Equal("Item", diff.ModifiedEntities[0].Before?.Type);
        Assert.Equal("Artifact", diff.ModifiedEntities[0].After?.Type);
    }

    [Fact]
    public void EntityModified_OccurredAtChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "Battle",
            Type = "Event",
            OccurredAt = CreateTime(schema, "10")
        };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.OccurredAt = CreateTime(schema, "20");
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Equal(CreateTime(schema, "10"), diff.ModifiedEntities[0].Before?.OccurredAt);
        Assert.Equal(CreateTime(schema, "20"), diff.ModifiedEntities[0].After?.OccurredAt);
    }

    [Fact]
    public void EntityModified_DescriptionChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Description = "Old description" };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Description = "New description";
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Equal("Old description", diff.ModifiedEntities[0].Before?.Description);
        Assert.Equal("New description", diff.ModifiedEntities[0].After?.Description);
    }

    [Fact]
    public void EntityModified_TagsChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid() };
        entity.Tags.Add("Tag1");
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Tags.Add("Tag2");
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Single(diff.ModifiedEntities[0].Before!.Tags);
        Assert.Equal(2, diff.ModifiedEntities[0].After!.Tags.Count);
    }

    [Fact]
    public void EntityModified_DocumentChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid() };
        entity.Documents["doc.md"] = new SorophyEntityDocument("doc.md", "Original Content");
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Documents["doc.md"] = new SorophyEntityDocument("doc.md", "Updated Content");
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Equal("Original Content", diff.ModifiedEntities[0].Before!.Documents["doc.md"].Content);
        Assert.Equal("Updated Content", diff.ModifiedEntities[0].After!.Documents["doc.md"].Content);
    }

    [Fact]
    public void EntityModified_PropertyValueChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid() };
        entity.Properties["Count"] = new SorophyProperty
        {
            Name = "Count",
            Value = new SorophyValue(SorophyValueType.Integer, 5L)
        };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));
        entity.Properties["Count"].Value = new SorophyValue(SorophyValueType.Integer, 10L);
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
        Assert.Equal(5L, diff.ModifiedEntities[0].Before!.Properties["Count"].Value.Value);
        Assert.Equal(10L, diff.ModifiedEntities[0].After!.Properties["Count"].Value.Value);
    }

    [Fact]
    public void EntityModified_NestedPropertyCollections_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid() };

        var list = new List<object?> { "item1", 100L };
        var dict = new Dictionary<string, object?> { ["k1"] = "v1" };

        entity.Properties["ListProp"] = new SorophyProperty
        {
            Name = "ListProp",
            Value = new SorophyValue(SorophyValueType.List, list)
        };
        entity.Properties["DictProp"] = new SorophyProperty
        {
            Name = "DictProp",
            Value = new SorophyValue(SorophyValueType.Object, dict)
        };
        graph.AddEntity(entity);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        // Mutate nested list
        var newList = new List<object?> { "item1", 200L };
        entity.Properties["ListProp"].Value = new SorophyValue(SorophyValueType.List, newList);
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedEntities);
    }

    // =============================================================
    // 5. RELATIONSHIP ADDITION, REMOVAL & MODIFICATION
    // =============================================================

    [Fact]
    public void RelationshipAdded_DetectsNewRelationship()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = sId,
            TargetId = tId,
            Type = "Connected"
        };
        graph.AddRelationship(rel);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.AddedRelationships);
        Assert.Empty(diff.RemovedRelationships);
        Assert.Empty(diff.ModifiedRelationships);
        Assert.Equal(rel.Id, diff.AddedRelationships[0].Id);

        var change = diff.RelationshipChanges.Single();
        Assert.Equal(GraphChangeKind.Added, change.Kind);
        Assert.Equal(rel.Id, change.RelationshipId);
        Assert.Null(change.Before);
        Assert.NotNull(change.After);
    }

    [Fact]
    public void RelationshipRemoved_DetectsDeletedRelationship()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var relId = Guid.NewGuid();
        var rel = new SorophyRelationship
        {
            Id = relId,
            SourceId = sId,
            TargetId = tId,
            Type = "Connected"
        };
        graph.AddRelationship(rel);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        graph.RemoveRelationship(relId);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Empty(diff.AddedRelationships);
        Assert.Single(diff.RemovedRelationships);
        Assert.Empty(diff.ModifiedRelationships);
        Assert.Equal(relId, diff.RemovedRelationships[0].Id);

        var change = diff.RelationshipChanges.Single();
        Assert.Equal(GraphChangeKind.Removed, change.Kind);
        Assert.Equal(relId, change.RelationshipId);
        Assert.NotNull(change.Before);
        Assert.Null(change.After);
    }

    [Fact]
    public void RelationshipModified_EvolutionTypeChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Alliance", CreateTime(schema, "10")));

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "10"));

        executor.Execute(graph, new SorophyRelationshipTypeChange(
            relId, "War", CreateTime(schema, "20")));

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "20"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedRelationships);
        Assert.Equal("Alliance", diff.ModifiedRelationships[0].Before?.Type);
        Assert.Equal("War", diff.ModifiedRelationships[0].After?.Type);
    }

    [Fact]
    public void RelationshipModified_EvolutionPropertyChange_ReportsModified()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);
        var relId = Guid.NewGuid();

        var executor = new SorophyRelationshipEvolutionExecutor();
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, sId, tId, "Trade", CreateTime(schema, "10")));

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "10"));

        var prop = new SorophyProperty
        {
            Name = "Tariff",
            Value = new SorophyValue(SorophyValueType.Decimal, 15.5m)
        };
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            relId,
            CreateTime(schema, "20"),
            new Dictionary<string, SorophyProperty> { ["Tariff"] = prop }));

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "20"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Single(diff.ModifiedRelationships);
        Assert.False(diff.ModifiedRelationships[0].Before!.Properties.ContainsKey("Tariff"));
        Assert.True(diff.ModifiedRelationships[0].After!.Properties.ContainsKey("Tariff"));
        Assert.Equal(15.5m, diff.ModifiedRelationships[0].After!.Properties["Tariff"].Value.Value);
    }

    // =============================================================
    // 6. COMPOSITE CHANGE SET & DETERMINISTIC ORDERING
    // =============================================================

    [Fact]
    public void DeterministicOrdering_SortsChangesByCanonicalGuid()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        // Create 3 entities with controlled GUIDs to ensure non-sorted insertion
        var idA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var idB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var idC = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Baseline: idB and idC exist
        graph.AddEntity(new SorophyEntity { Id = idB, Name = "B-Old" });
        graph.AddEntity(new SorophyEntity { Id = idC, Name = "C" });
        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        // Target: idA added, idB modified, idC removed
        graph.AddEntity(new SorophyEntity { Id = idA, Name = "A-New" });
        graph.Entities[idB].Name = "B-New";
        graph.RemoveEntity(idC);
        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Equal(3, diff.TotalChanges);

        // Verify EntityChanges order: idA (Added), idB (Modified), idC (Removed)
        Assert.Equal(3, diff.EntityChanges.Count);
        Assert.Equal(idA, diff.EntityChanges[0].EntityId);
        Assert.Equal(GraphChangeKind.Added, diff.EntityChanges[0].Kind);

        Assert.Equal(idB, diff.EntityChanges[1].EntityId);
        Assert.Equal(GraphChangeKind.Modified, diff.EntityChanges[1].Kind);

        Assert.Equal(idC, diff.EntityChanges[2].EntityId);
        Assert.Equal(GraphChangeKind.Removed, diff.EntityChanges[2].Kind);

        // Verify individual sorted collections
        Assert.Single(diff.AddedEntities);
        Assert.Equal(idA, diff.AddedEntities[0].Id);

        Assert.Single(diff.ModifiedEntities);
        Assert.Equal(idB, diff.ModifiedEntities[0].EntityId);

        Assert.Single(diff.RemovedEntities);
        Assert.Equal(idC, diff.RemovedEntities[0].Id);
    }

    [Fact]
    public void CompositeGraphDiff_HandlesMixedEntityAndRelationshipChanges()
    {
        var schema = CreateSchema();
        var graph = CreateGraphWithEntities(out var sId, out var tId);

        var r1 = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = sId, TargetId = tId, Type = "R1" };
        var r2 = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = sId, TargetId = tId, Type = "R2" };
        graph.AddRelationship(r1);
        graph.AddRelationship(r2);

        var snap1 = graph.CreateSnapshot(CreateTime(schema, "1"));

        // Changes:
        // Entity: sId modified, new entity added
        graph.Entities[sId].Name = "ModifiedSource";
        var newEntity = new SorophyEntity { Id = Guid.NewGuid(), Name = "ExtraNode" };
        graph.AddEntity(newEntity);

        // Relationship: r1 removed, new relationship r3 added, r2 modified
        graph.RemoveRelationship(r1.Id);
        r2.Type = "R2-Modified";
        var r3 = new SorophyRelationship { Id = Guid.NewGuid(), SourceId = sId, TargetId = newEntity.Id, Type = "R3" };
        graph.AddRelationship(r3);

        var snap2 = graph.CreateSnapshot(CreateTime(schema, "2"));

        var diff = SorophyGraphDiff.Compare(snap1, snap2);

        Assert.True(diff.HasChanges);
        Assert.Equal(2, diff.EntityChanges.Count);
        Assert.Single(diff.AddedEntities);
        Assert.Single(diff.ModifiedEntities);
        Assert.Empty(diff.RemovedEntities);

        Assert.Equal(3, diff.RelationshipChanges.Count);
        Assert.Single(diff.AddedRelationships);
        Assert.Single(diff.RemovedRelationships);
        Assert.Single(diff.ModifiedRelationships);

        Assert.Equal(5, diff.TotalChanges);
    }
}
