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
using Sorophy.Engine.Graph.Canon;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public sealed class TemporalRelationshipLifecycleTests
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

    private static (SorophyGraph graph, SorophyTimeSchema schema, SorophyEntity source, SorophyEntity target) CreateGraphWithEndpoints(
        string sourceCreated = "100",
        string targetCreated = "100")
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = new SorophyEntity { Id = Guid.NewGuid(), Name = "SourceEntity" };
        var target = new SorophyEntity { Id = Guid.NewGuid(), Name = "TargetEntity" };

        graph.CreateEntity(source, CreateTime(schema, sourceCreated));
        graph.CreateEntity(target, CreateTime(schema, targetCreated));

        return (graph, schema, source, target);
    }

    // =========================================================================
    // Group 1: Creation & Canonical Registration
    // =========================================================================

    [Fact]
    public void CreateRelationship_RegistersInCanonicalStoreAndRecordsCreationFact()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        Assert.True(graph.ContainsRelationship(rel.Id));
        Assert.True(graph.TryGetRelationshipHistory(rel.Id, out var history));
        Assert.NotNull(history);
        Assert.NotNull(history.CreationFact);
        Assert.Equal(SorophyRelationshipFactKind.Created, history.CreationFact.Kind);
        Assert.Equal(CreateTime(schema, "150"), history.CreatedAt);
        Assert.False(history.IsRetired);
    }

    [Fact]
    public void CreateRelationship_BuildsBidirectionalAdjacency()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        var outgoing = graph.GetOutgoingRelationships(source.Id).ToList();
        var incoming = graph.GetIncomingRelationships(target.Id).ToList();

        Assert.Single(outgoing);
        Assert.Equal(rel.Id, outgoing[0].Id);
        Assert.Single(incoming);
        Assert.Equal(rel.Id, incoming[0].Id);
    }

    [Fact]
    public void CreateRelationship_ThrowsWhenCanonCheckFails()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints(sourceCreated: "200");
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        // Source is not active at 100
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(rel, CreateTime(schema, "100")));

        Assert.Contains("source entity", ex.Message);
        Assert.False(graph.ContainsRelationship(rel.Id));
        Assert.False(graph.TryGetRelationshipHistory(rel.Id, out _));
    }

    [Fact]
    public void CreateRelationship_ThrowsWhenEndpointIsRetiredAtCreationCoordinate()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        graph.RetireEntity(target.Id, CreateTime(schema, "250"));

        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        // Target was retired at 250, cannot create relationship at 300
        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.CreateRelationship(rel, CreateTime(schema, "300")));

        Assert.Contains("target entity", ex.Message);
        Assert.False(graph.ContainsRelationship(rel.Id));
    }

    // =========================================================================
    // Group 2: Temporal Existence Queries (RelationshipExistsAt)
    // =========================================================================

    [Fact]
    public void RelationshipExistsAt_ReturnsFalseBeforeCreation()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));

        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "150")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "199")));
    }

    [Fact]
    public void RelationshipExistsAt_ReturnsTrueAtCreationAndActiveInterval()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));

        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "200")));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "250")));
    }

    [Fact]
    public void RelationshipExistsAt_ReturnsFalseAtAndAfterRetirement()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "299")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "300")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void RelationshipExistsAt_ReturnsFalseForNonExistentOrEmptyId()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        Assert.False(graph.RelationshipExistsAt(Guid.Empty, CreateTime(schema, "100")));
        Assert.False(graph.RelationshipExistsAt(Guid.NewGuid(), CreateTime(schema, "100")));
    }

    [Fact]
    public void RelationshipExistsAt_BaselineUnversionedRelationship_ExistsWhenEndpointsExist()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints(sourceCreated: "100", targetCreated: "100");
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "BaselineEdge"
        };

        // Added with AddRelationship (unversioned legacy insertion)
        graph.AddRelationship(rel);

        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "150")));
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "500")));
        // Prior to source creation at 100, endpoint does not exist so relationship does not exist
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "50")));
    }

    // =========================================================================
    // Group 3: Hard Endpoint Invariant
    // =========================================================================

    [Fact]
    public void HardEndpointInvariant_SourceRetirementDeactivatesRelationshipWithoutMutatingRelationshipHistory()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        // Retire source at 300
        graph.RetireEntity(source.Id, CreateTime(schema, "300"));

        // Relationship itself is not retired
        Assert.False(graph.IsRelationshipRetired(rel.Id));
        Assert.True(graph.TryGetRelationshipHistory(rel.Id, out var history));
        Assert.NotNull(history);
        Assert.False(history.IsRetired);

        // But RelationshipExistsAt returns false at and after 300 due to hard endpoint invariant
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "250")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "300")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void HardEndpointInvariant_TargetRetirementDeactivatesRelationship()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        // Retire target at 280
        graph.RetireEntity(target.Id, CreateTime(schema, "280"));

        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "279")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "280")));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "300")));
    }

    [Fact]
    public void GetRelationshipLifecycleStatus_ReflectsEndpointInactive()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireEntity(source.Id, CreateTime(schema, "300"));

        Assert.Equal(SorophyRelationshipLifecycleStatus.Uncreated, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "100")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Active, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "200")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.EndpointInactive, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "300")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.EndpointInactive, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "400")));
    }

    [Fact]
    public void GetRelationshipLifecycleStatus_ReflectsOwnRetirementWhenEndpointsActive()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "250"));

        Assert.Equal(SorophyRelationshipLifecycleStatus.Active, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "200")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Retired, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "250")));
        Assert.Equal(SorophyRelationshipLifecycleStatus.Retired, graph.GetRelationshipLifecycleStatus(rel.Id, CreateTime(schema, "300")));
    }

    [Fact]
    public void PermanentEntityDeletion_CascadesPermanentlyToIncidentRelationships()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        // Permanent delete of source entity
        Assert.True(graph.DeleteEntity(source.Id));

        Assert.False(graph.ContainsEntity(source.Id));
        Assert.False(graph.ContainsRelationship(rel.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "150")));
    }

    // =========================================================================
    // Group 4: Non-Destructive Retirement & Reversal
    // =========================================================================

    [Fact]
    public void RetireRelationship_RetainsRelationshipInCanonicalStoreAndAdjacency()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"), "Treaty expired");

        // Non-destructive: still in canonical store and index
        Assert.True(graph.ContainsRelationship(rel.Id));
        Assert.True(graph.IsRelationshipRetired(rel.Id));
        Assert.Single(graph.GetOutgoingRelationships(source.Id));
        Assert.Single(graph.GetIncomingRelationships(target.Id));

        Assert.True(graph.TryGetRelationshipHistory(rel.Id, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Equal(CreateTime(schema, "300"), history.RetiredAt);
        Assert.Equal("Treaty expired", history.RetirementFact?.Description);
    }

    [Fact]
    public void RevertRelationshipRetirement_RemovesRetiredFactAndRestoresActiveState()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));

        // Author reverses the retirement
        Assert.True(graph.RevertRelationshipRetirement(rel.Id, CreateTime(schema, "300")));

        Assert.False(graph.IsRelationshipRetired(rel.Id));
        Assert.True(graph.TryGetRelationshipHistory(rel.Id, out var history));
        Assert.NotNull(history);
        Assert.Null(history.RetirementFact);
        Assert.Null(history.RetiredAt);
        Assert.Single(history.Facts); // only Created fact remains

        // Relationship is active again at 350!
        Assert.True(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void RetireRelationship_RejectsSecondRetirementFact()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "300"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            graph.RetireRelationship(rel.Id, CreateTime(schema, "400")));

        Assert.Contains("already has an established retirement", ex.Message);
    }

    [Fact]
    public void DeleteRelationship_PermanentlyRemovesRelationship()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));

        Assert.True(graph.DeleteRelationship(rel.Id));
        Assert.False(graph.ContainsRelationship(rel.Id));
        Assert.Empty(graph.GetOutgoingRelationships(source.Id));
        Assert.Empty(graph.GetIncomingRelationships(target.Id));
        Assert.False(graph.RelationshipExistsAt(rel.Id, CreateTime(schema, "150")));
    }

    // =========================================================================
    // Group 5: Snapshot Projection
    // =========================================================================

    [Fact]
    public void Snapshot_ExcludesRelationshipPriorToCreation()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));

        var snapshot = graph.CreateSnapshot(CreateTime(schema, "150"));

        Assert.True(snapshot.ContainsEntity(source.Id));
        Assert.True(snapshot.ContainsEntity(target.Id));
        Assert.False(snapshot.ContainsRelationship(rel.Id));
        Assert.Empty(snapshot.GetOutboundRelationships(source.Id));
    }

    [Fact]
    public void Snapshot_IncludesRelationshipBetweenCreationAndRetirement()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };
        rel.Properties["Trust"] = new SorophyProperty { Name = "Trust", Value = new SorophyValue(SorophyValueType.Integer, 95L) };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "400"));

        var snapshot = graph.CreateSnapshot(CreateTime(schema, "300"));

        Assert.True(snapshot.ContainsRelationship(rel.Id));
        var snapRel = snapshot.GetRelationship(rel.Id);
        Assert.NotNull(snapRel);
        Assert.Equal("AllyOf", snapRel.Type);
        Assert.Equal(95L, (long)snapRel.Properties["Trust"].Value.Value!);

        var outgoing = snapshot.GetOutboundRelationships(source.Id).ToList();
        Assert.Single(outgoing);
        Assert.Equal(rel.Id, outgoing[0].Id);
    }

    [Fact]
    public void Snapshot_ExcludesRelationshipAtAndAfterRetirement()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "400"));

        var snapshot = graph.CreateSnapshot(CreateTime(schema, "400"));

        Assert.False(snapshot.ContainsRelationship(rel.Id));
        Assert.Empty(snapshot.GetOutboundRelationships(source.Id));
        Assert.Empty(snapshot.GetInboundRelationships(target.Id));
    }

    [Fact]
    public void Snapshot_ExcludesRelationshipWhenEndpointIsRetiredEvenIfRelationshipNotRetired()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "AllyOf"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "200"));
        // Retire target entity at 350
        graph.RetireEntity(target.Id, CreateTime(schema, "350"));

        var snapshot = graph.CreateSnapshot(CreateTime(schema, "370"));

        Assert.True(snapshot.ContainsEntity(source.Id));
        Assert.False(snapshot.ContainsEntity(target.Id)); // target is retired
        Assert.False(snapshot.ContainsRelationship(rel.Id)); // pruned: no dangling edges!
        Assert.Empty(snapshot.GetOutboundRelationships(source.Id));
    }

    // =========================================================================
    // Group 6: Serialization & Round-trip
    // =========================================================================

    [Fact]
    public void Serialization_RoundTripsRelationshipLifecycleFactsAccurately()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Pact"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "350"), "Pact dissolved");

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        Assert.True(restored.ContainsRelationship(rel.Id));
        Assert.True(restored.IsRelationshipRetired(rel.Id));
        Assert.True(restored.TryGetRelationshipHistory(rel.Id, out var history));
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);

        Assert.NotNull(history.CreationFact);
        Assert.Equal(SorophyRelationshipFactKind.Created, history.CreationFact.Kind);
        Assert.Equal(CreateTime(schema, "150"), history.CreatedAt);

        Assert.NotNull(history.RetirementFact);
        Assert.Equal(SorophyRelationshipFactKind.Retired, history.RetirementFact.Kind);
        Assert.Equal(CreateTime(schema, "350"), history.RetiredAt);
        Assert.Equal("Pact dissolved", history.RetirementFact.Description);

        // Check existence queries on restored graph
        Assert.False(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, "100")));
        Assert.True(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, "250")));
        Assert.False(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));
    }

    [Fact]
    public void Serialization_RevertedRetirementOmitsRetirementFactInJson()
    {
        var (graph, schema, source, target) = CreateGraphWithEndpoints();
        var rel = new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            TargetId = target.Id,
            Type = "Pact"
        };

        graph.CreateRelationship(rel, CreateTime(schema, "150"));
        graph.RetireRelationship(rel.Id, CreateTime(schema, "350"));
        graph.RevertRelationshipRetirement(rel.Id, CreateTime(schema, "350"));

        var json = LoreSerializer.Serialize(graph);

        Assert.DoesNotContain("\"Retired\"", json);

        var restored = LoreSerializer.Deserialize(json);
        Assert.True(restored.ContainsRelationship(rel.Id));
        Assert.False(restored.IsRelationshipRetired(rel.Id));
        Assert.True(restored.RelationshipExistsAt(rel.Id, CreateTime(schema, "350")));
    }
}
