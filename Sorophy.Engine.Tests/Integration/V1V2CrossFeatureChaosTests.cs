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
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Integration;

public sealed class V1V2CrossFeatureChaosTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "CrossFeature Timeline")
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

    // =============================================================
    // 1. END-TO-END V1 + V2 CHAOS PIPELINE ACROSS 4 SEEDS
    // =============================================================

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(12345)]
    [InlineData(987654321)]
    public void CrossFeaturePipeline_CompleteLifecycleAcrossRequiredSeeds(int seed)
    {
        var rng = new Random(seed);
        var schema = CreateSchema($"Timeline_Seed_{seed}");
        var graph = new SorophyGraph();
        var executor = new SorophyRelationshipEvolutionExecutor();

        // 1. Baseline Entities
        var a = new SorophyEntity { Name = $"A_{seed}" };
        var b = new SorophyEntity { Name = $"B_{seed}" };
        var c = new SorophyEntity { Name = $"C_{seed}" };
        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        // 2. Baseline Relationship
        var baseRel = new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "Connects" };
        graph.AddRelationship(baseRel);

        // 3. Event E1 at T=100
        var t100 = CreateTime(schema, "100");
        var e1 = new SorophyEntity { Name = "Event_E1", Type = "Event", OccurredAt = t100 };
        graph.AddEntity(e1);

        // 4. Evolutions from E1
        var evolRelId = Guid.NewGuid();
        executor.Execute(graph, new SorophyRelationshipCreation(
            evolRelId, b.Id, c.Id, "EvolvedEdge", t100, eventEntityId: e1.Id));

        // 5. Event E2 at T=200
        var t200 = CreateTime(schema, "200");
        var e2 = new SorophyEntity { Name = "Event_E2", Type = "Event", OccurredAt = t200 };
        graph.AddEntity(e2);

        // 6. Evolutions from E2: Property modification & Type change
        var prop = new SorophyProperty
        {
            Name = "DynamicMetric",
            Value = new SorophyValue(SorophyValueType.Integer, (long)rng.Next(10000))
        };
        executor.Execute(graph, new SorophyRelationshipPropertyModification(
            evolRelId, t200, new Dictionary<string, SorophyProperty> { ["DynamicMetric"] = prop }, eventEntityId: e2.Id));

        executor.Execute(graph, new SorophyRelationshipTypeChange(
            evolRelId, "UpgradedEdge", t200, eventEntityId: e2.Id));

        // 7. Event E3 at T=300: Termination
        var t300 = CreateTime(schema, "300");
        var e3 = new SorophyEntity { Name = "Event_E3", Type = "Event", OccurredAt = t300 };
        graph.AddEntity(e3);

        executor.Execute(graph, new SorophyRelationshipTermination(
            evolRelId, t300, eventEntityId: e3.Id));

        // 8. Snapshots at T=50, T=150, T=250, T=350
        var snap50 = graph.CreateSnapshot(CreateTime(schema, "50"));
        var snap150 = graph.CreateSnapshot(CreateTime(schema, "150"));
        var snap250 = graph.CreateSnapshot(CreateTime(schema, "250"));
        var snap350 = graph.CreateSnapshot(CreateTime(schema, "350"));

        Assert.False(snap50.ContainsRelationship(evolRelId));
        Assert.True(snap150.ContainsRelationship(evolRelId));
        Assert.True(snap250.ContainsRelationship(evolRelId));
        Assert.False(snap350.ContainsRelationship(evolRelId)); // Terminated at 300

        // 9. TQD Queries
        var factsE1 = graph.TemporalQuery.GetFactsByEvent(e1.Id);
        Assert.Single(factsE1);
        Assert.Equal("EvolvedEdge", factsE1[0].Type);

        var factsE2 = graph.TemporalQuery.GetFactsByEvent(e2.Id);
        Assert.Equal(2, factsE2.Count);

        var factsE3 = graph.TemporalQuery.GetFactsByEvent(e3.Id);
        Assert.Single(factsE3);

        // 10. Persistence Serialization & Deserialization
        var json = LoreSerializer.Serialize(graph);
        var loaded = LoreSerializer.Deserialize(json);

        Assert.Empty(loaded.Validate());
        Assert.True(loaded.IsRelationshipIdRetired(evolRelId));

        // Restore in-memory OccurredAt on loaded entities (OccurredAt is an in-memory runtime property)
        loaded.Entities[e1.Id].OccurredAt = t100;
        loaded.Entities[e2.Id].OccurredAt = t200;
        loaded.Entities[e3.Id].OccurredAt = t300;

        // 11. Compare snapshots across persistence
        var loadedSnap150 = loaded.CreateSnapshot(CreateTime(schema, "150"));
        var crossPersistenceDiff = SorophyGraphDiff.Compare(snap150, loadedSnap150);

        Assert.False(crossPersistenceDiff.HasChanges);
        Assert.Equal(0, crossPersistenceDiff.TotalChanges);

        // 12. Compare T150 vs T250 on loaded graph
        var loadedSnap250 = loaded.CreateSnapshot(CreateTime(schema, "250"));
        var temporalDiff = SorophyGraphDiff.Compare(loadedSnap150, loadedSnap250);

        Assert.True(temporalDiff.HasChanges);
        Assert.Single(temporalDiff.ModifiedRelationships);
        Assert.Equal("EvolvedEdge", temporalDiff.ModifiedRelationships[0].Before!.Type);
        Assert.Equal("UpgradedEdge", temporalDiff.ModifiedRelationships[0].After!.Type);
    }

    // =============================================================
    // 2. CORRUPTED REFERENCES & INVALID ARGUMENT RESILIENCE
    // =============================================================

    [Fact]
    public void Assault_InvalidArgumentsAndCorruptedReferences_RejectedConsistently()
    {
        var schema = CreateSchema("ChaosValidation");
        var graph = new SorophyGraph();
        var s = new SorophyEntity { Name = "S" };
        var t = new SorophyEntity { Name = "T" };
        graph.AddEntity(s);
        graph.AddEntity(t);

        var executor = new SorophyRelationshipEvolutionExecutor();
        var relId = Guid.NewGuid();

        // 1. Creation with missing endpoints throws
        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(graph, new SorophyRelationshipCreation(
                relId, Guid.NewGuid(), t.Id, "Edge", CreateTime(schema, "10"))));

        // 2. Evolution on non-existent relationship throws
        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(graph, new SorophyRelationshipTypeChange(
                Guid.NewGuid(), "NewType", CreateTime(schema, "20"))));

        // 3. Provenance pointing to non-existent entity throws InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            executor.Execute(graph, new SorophyRelationshipCreation(
                relId, s.Id, t.Id, "Edge", CreateTime(schema, "10"), eventEntityId: Guid.NewGuid())));

        // 4. Incompatible schemas in validity change throws ArgumentException
        var otherSchema = new SorophyTimeSchema("OtherTimeline", new[] { new SorophyTimeUnit("Tick", 0, new SorophyTimePositionDefinition(SorophyTimePositionKind.Numeric)) });
        var invalidTime = new SorophyTime(otherSchema, "10", "Tick", SorophyTimePrecision.Exact);

        // Valid creation
        executor.Execute(graph, new SorophyRelationshipCreation(
            relId, s.Id, t.Id, "Edge", CreateTime(schema, "10")));

        // Validity change with incompatible temporal schema throws ArgumentException
        Assert.Throws<ArgumentException>(() =>
            executor.Execute(graph, new SorophyRelationshipValidityChange(
                relId, CreateTime(schema, "20"), newValidFrom: invalidTime)));
    }
}
