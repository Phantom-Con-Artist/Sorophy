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
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Sorophy.Engine.Graph;
using Sorophy.Engine.HistoricalFacts;
using Sorophy.Engine.Serialization;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.HistoricalFacts;

/// <summary>
/// Authoritative test suite verifying Emergent Historical Facts (EHG) read model
/// in Sorophy v2.0.0 "Krono".
/// </summary>
public sealed class EmergentHistoricalFactsTests
{
    private static SorophyTimeSchema CreateSchema(string timeline = "KronoTimeline")
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

    private static SorophyTime CreateTime(SorophyTimeSchema schema, int position)
    {
        return new SorophyTime(schema, position.ToString(), "Tick", SorophyTimePrecision.Exact);
    }

    private static SorophyEntity CreateEntity(string name = "Node", string type = "Item")
    {
        return new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Description = $"Entity {name}"
        };
    }

    private static SorophyRelationship CreateRelationship(
        Guid sourceId,
        Guid targetId,
        string type = "Connects")
    {
        return new SorophyRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            Type = type
        };
    }

    // 1. Empty graph returns no facts
    [Fact]
    public void EmptyGraph_ReturnsNoHistoricalFacts()
    {
        var graph = new SorophyGraph();

        Assert.Empty(graph.GetHistoricalFacts());
        Assert.Empty(graph.GetEntityHistoricalFacts(Guid.NewGuid()));
        Assert.Empty(graph.GetRelationshipHistoricalFacts(Guid.NewGuid()));
        Assert.Empty(graph.TemporalQuery.GetHistoricalFacts());
    }

    // 2. Entity creation produces one Created fact
    [Fact]
    public void EntityCreation_ProducesOneCreatedFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));

        var facts = graph.GetEntityHistoricalFacts(entity.Id);
        Assert.Single(facts);

        var fact = facts[0];
        Assert.Equal(CreateTime(schema, 100), fact.Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, fact.Kind);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, fact.TargetKind);
        Assert.Equal(entity.Id, fact.EntityId);
        Assert.Null(fact.RelationshipId);
        Assert.Null(fact.SourceEntityId);
        Assert.Null(fact.TargetEntityId);
    }

    // 3. Entity-specific query returns only that entity's facts
    [Fact]
    public void EntitySpecificQuery_ReturnsOnlyThatEntityFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 200));

        var factsA = graph.GetEntityHistoricalFacts(a.Id);
        Assert.Single(factsA);
        Assert.Equal(a.Id, factsA[0].EntityId);

        var factsB = graph.GetEntityHistoricalFacts(b.Id);
        Assert.Single(factsB);
        Assert.Equal(b.Id, factsB[0].EntityId);
    }

    // 4. Entity retirement produces one Retired fact
    [Fact]
    public void EntityRetirement_ProducesOneRetiredFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));

        var facts = graph.GetEntityHistoricalFacts(entity.Id);
        Assert.Equal(2, facts.Count);

        Assert.Equal(CreateTime(schema, 100), facts[0].Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, facts[0].Kind);

        Assert.Equal(CreateTime(schema, 300), facts[1].Time);
        Assert.Equal(SorophyHistoricalFactKind.Retired, facts[1].Kind);
        Assert.Equal(entity.Id, facts[1].EntityId);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, facts[1].TargetKind);
    }

    // 5. Reversal removes the retirement fact
    [Fact]
    public void Reversal_RemovesRetirementFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.RetireEntity(entity.Id, CreateTime(schema, 300));
        graph.RevertRetirement(entity.Id, CreateTime(schema, 300));

        var facts = graph.GetEntityHistoricalFacts(entity.Id);
        Assert.Single(facts);
        Assert.Equal(SorophyHistoricalFactKind.Created, facts[0].Kind);
        Assert.Equal(CreateTime(schema, 100), facts[0].Time);
    }

    // 6. Relationship creation produces one Created fact
    // 7. Relationship fact contains relationship identity and endpoint info
    [Fact]
    public void RelationshipCreation_ProducesOneCreatedFactWithEndpoints()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Ally");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        var facts = graph.GetRelationshipHistoricalFacts(rel.Id);
        Assert.Single(facts);

        var fact = facts[0];
        Assert.Equal(CreateTime(schema, 200), fact.Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, fact.Kind);
        Assert.Equal(SorophyHistoricalFactTarget.Relationship, fact.TargetKind);
        Assert.Equal(rel.Id, fact.RelationshipId);
        Assert.Equal(source.Id, fact.SourceEntityId);
        Assert.Equal(target.Id, fact.TargetEntityId);
        Assert.Equal("Ally", fact.RelationshipType);
        Assert.Null(fact.EntityId);
    }

    // 8. Relationship retirement produces one Retired fact
    [Fact]
    public void RelationshipRetirement_ProducesOneRetiredFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Ally");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 400));

        var facts = graph.GetRelationshipHistoricalFacts(rel.Id);
        Assert.Equal(2, facts.Count);

        Assert.Equal(CreateTime(schema, 200), facts[0].Time);
        Assert.Equal(SorophyHistoricalFactKind.Created, facts[0].Kind);

        Assert.Equal(CreateTime(schema, 400), facts[1].Time);
        Assert.Equal(SorophyHistoricalFactKind.Retired, facts[1].Kind);
        Assert.Equal(rel.Id, facts[1].RelationshipId);
    }

    // 9. Endpoint retirement does NOT synthesize relationship retirement
    [Fact]
    public void EndpointRetirement_DoesNotSynthesizeRelationshipRetirementFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");

        graph.CreateEntity(source, CreateTime(schema, 100));
        graph.CreateEntity(target, CreateTime(schema, 100));

        var rel = CreateRelationship(source.Id, target.Id, "Bond");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        // In Krono, retiring an endpoint requires retiring incident relationship first if established at/after retirement,
        // or retiring endpoint after relationship. But suppose target retires at 400 while relationship was retired at 300:
        // What if source entity retires at 500 when relationship had no explicit retirement?
        // Wait, Canon Check prevents entity retirement if incident relationship exists at later coordinate.
        // But if entity target is retired at 400 after relationship is retired at 350,
        // the relationship ONLY has the explicit retirement fact at 350, NOT at 400!
        graph.RetireRelationship(rel.Id, CreateTime(schema, 350));
        graph.RetireEntity(target.Id, CreateTime(schema, 400));

        var relFacts = graph.GetRelationshipHistoricalFacts(rel.Id);
        Assert.Equal(2, relFacts.Count);
        Assert.Equal(CreateTime(schema, 200), relFacts[0].Time);
        Assert.Equal(CreateTime(schema, 350), relFacts[1].Time);

        // There is NO synthetic fact at 400 in relationship facts
        Assert.DoesNotContain(relFacts, f => f.Time.Equals(CreateTime(schema, 400)));
    }

    // 10. Permanent deletion does NOT fabricate a historical fact
    [Fact]
    public void PermanentDeletion_DoesNotFabricateHistoricalFact()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var entity = CreateEntity("Hero");

        graph.CreateEntity(entity, CreateTime(schema, 100));
        graph.DeleteEntity(entity.Id);

        // Entity is permanently destroyed, its history is purged.
        var facts = graph.GetEntityHistoricalFacts(entity.Id);
        Assert.Empty(facts);
    }

    // 11. Multiple facts are ordered chronologically
    [Fact]
    public void MultipleFacts_AreOrderedChronologically()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 50));
        graph.RetireEntity(b.Id, CreateTime(schema, 250));

        var allFacts = graph.GetHistoricalFacts();
        Assert.Equal(3, allFacts.Count);

        Assert.Equal(CreateTime(schema, 50), allFacts[0].Time);
        Assert.Equal(CreateTime(schema, 100), allFacts[1].Time);
        Assert.Equal(CreateTime(schema, 250), allFacts[2].Time);
    }

    // 12. Same-coordinate facts have deterministic ordering
    [Fact]
    public void SameCoordinateFacts_HaveDeterministicOrdering()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var e1 = CreateEntity("E1");
        var e2 = CreateEntity("E2");

        // Force IDs to have distinct sort order
        if (e1.Id.CompareTo(e2.Id) > 0)
        {
            (e1, e2) = (e2, e1);
        }

        graph.CreateEntity(e1, CreateTime(schema, 100));
        graph.CreateEntity(e2, CreateTime(schema, 100));

        var rel = CreateRelationship(e1.Id, e2.Id, "Link");
        graph.CreateRelationship(rel, CreateTime(schema, 100));

        var facts = graph.GetHistoricalFacts();
        Assert.Equal(3, facts.Count);

        // Ordering rule:
        // 1. Time (all 100)
        // 2. TargetKind: Entity before Relationship
        // 3. Entity Guid comparison: e1 before e2
        // 4. Then Relationship
        Assert.Equal(e1.Id, facts[0].EntityId);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, facts[0].TargetKind);

        Assert.Equal(e2.Id, facts[1].EntityId);
        Assert.Equal(SorophyHistoricalFactTarget.Entity, facts[1].TargetKind);

        Assert.Equal(rel.Id, facts[2].RelationshipId);
        Assert.Equal(SorophyHistoricalFactTarget.Relationship, facts[2].TargetKind);
    }

    // 13. Repeated EHG queries produce equivalent results
    [Fact]
    public void RepeatedQueries_ProduceEquivalentResults()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 200));

        var query1 = graph.GetHistoricalFacts();
        var query2 = graph.GetHistoricalFacts();

        Assert.Equal(query1.Count, query2.Count);
        for (int i = 0; i < query1.Count; i++)
        {
            Assert.Equal(query1[i], query2[i]);
        }
    }

    // 14. EHG does not mutate canonical entity state
    // 15. EHG does not mutate canonical relationship state
    // 16. EHG does not mutate entity histories
    // 17. EHG does not mutate relationship histories
    [Fact]
    public void EHG_DoesNotMutateCanonicalOrHistoricalState()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));

        int entCount = graph.Entities.Count;
        int relCount = graph.Relationships.Count;
        int factCount = graph.EntityHistories[e.Id].Facts.Count;

        for (int i = 0; i < 5; i++)
        {
            _ = graph.GetHistoricalFacts();
            _ = graph.GetEntityHistoricalFacts(e.Id);
            _ = graph.TemporalQuery.GetHistoricalFacts();
        }

        Assert.Equal(entCount, graph.Entities.Count);
        Assert.Equal(relCount, graph.Relationships.Count);
        Assert.Equal(factCount, graph.EntityHistories[e.Id].Facts.Count);
    }

    // 18. EHG does not alter Snapshot behavior
    // 19. EHG does not alter TQD behavior
    [Fact]
    public void EHG_DoesNotAlterSnapshotOrTQDBehavior()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");
        graph.CreateEntity(e, CreateTime(schema, 100));

        var snapBefore = graph.CreateSnapshot(CreateTime(schema, 100));
        bool tqdBefore = graph.TemporalQuery.EntityExistsAt(e.Id, CreateTime(schema, 100));

        _ = graph.GetHistoricalFacts();

        var snapAfter = graph.CreateSnapshot(CreateTime(schema, 100));
        bool tqdAfter = graph.TemporalQuery.EntityExistsAt(e.Id, CreateTime(schema, 100));

        Assert.Equal(snapBefore.EntityCount, snapAfter.EntityCount);
        Assert.Equal(tqdBefore, tqdAfter);
    }

    // 20. Persistence round-trip produces identical EHG output
    [Fact]
    public void PersistenceRoundTrip_ProducesIdenticalEHGOutput()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();

        var a = CreateEntity("A");
        var b = CreateEntity("B");
        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 150));

        var rel = CreateRelationship(a.Id, b.Id, "Ally");
        graph.CreateRelationship(rel, CreateTime(schema, 200));
        graph.RetireRelationship(rel.Id, CreateTime(schema, 300));

        var beforeFacts = graph.GetHistoricalFacts();

        var json = LoreSerializer.Serialize(graph);
        var restored = LoreSerializer.Deserialize(json);

        var afterFacts = restored.GetHistoricalFacts();

        Assert.Equal(beforeFacts.Count, afterFacts.Count);
        for (int i = 0; i < beforeFacts.Count; i++)
        {
            Assert.Equal(beforeFacts[i], afterFacts[i]);
        }
    }

    // 21. Range lower boundary is inclusive
    // 22. Range upper boundary is inclusive
    [Fact]
    public void RangeBoundaries_AreInclusive()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 300));

        // Exact range [100, 300] contains both
        var factsExact = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 100), CreateTime(schema, 300));
        Assert.Equal(2, factsExact.Count);

        // Range [100, 200] contains only Created at 100
        var factsLower = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 100), CreateTime(schema, 200));
        Assert.Single(factsLower);
        Assert.Equal(CreateTime(schema, 100), factsLower[0].Time);

        // Range [200, 300] contains only Retired at 300
        var factsUpper = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 200), CreateTime(schema, 300));
        Assert.Single(factsUpper);
        Assert.Equal(CreateTime(schema, 300), factsUpper[0].Time);
    }

    // 23. Single-coordinate range works correctly
    [Fact]
    public void SingleCoordinateRange_ReturnsOnlyFactsAtExactCoordinate()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 300));

        var facts100 = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 100), CreateTime(schema, 100));
        Assert.Single(facts100);
        Assert.Equal(CreateTime(schema, 100), facts100[0].Time);

        var facts200 = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 200), CreateTime(schema, 200));
        Assert.Empty(facts200);
    }

    // 24. Inverted range returns empty
    [Fact]
    public void InvertedRange_ReturnsEmpty()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));

        var facts = graph.GetEntityHistoricalFacts(e.Id, CreateTime(schema, 300), CreateTime(schema, 100));
        Assert.Empty(facts);
    }

    // 25. Open-ended ranges work correctly
    [Fact]
    public void OpenEndedRanges_WorkCorrectly()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 300));

        // From 200 (unbounded above)
        var factsFrom = graph.GetEntityHistoricalFacts(e.Id, from: CreateTime(schema, 200));
        Assert.Single(factsFrom);
        Assert.Equal(CreateTime(schema, 300), factsFrom[0].Time);

        // To 200 (unbounded below)
        var factsTo = graph.GetEntityHistoricalFacts(e.Id, to: CreateTime(schema, 200));
        Assert.Single(factsTo);
        Assert.Equal(CreateTime(schema, 100), factsTo[0].Time);
    }

    // 26. No synthetic reversal facts appear
    [Fact]
    public void Reversal_ProducesNoSyntheticFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 300));
        graph.RevertRetirement(e.Id, CreateTime(schema, 300));

        var facts = graph.GetEntityHistoricalFacts(e.Id);
        Assert.Single(facts);
        Assert.Equal(SorophyHistoricalFactKind.Created, facts[0].Kind);

        // Ensure kind is only Created, no Unretired
        Assert.DoesNotContain(facts, f => f.Kind.ToString().Contains("Unretire", StringComparison.OrdinalIgnoreCase));
    }

    // 27. No synthetic endpoint-consequence facts appear
    [Fact]
    public void NoSyntheticEndpointConsequenceFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var a = CreateEntity("A");
        var b = CreateEntity("B");

        graph.CreateEntity(a, CreateTime(schema, 100));
        graph.CreateEntity(b, CreateTime(schema, 100));

        var rel = CreateRelationship(a.Id, b.Id, "Edge");
        graph.CreateRelationship(rel, CreateTime(schema, 200));

        // Retiring relationship at 250 then retiring a at 300
        graph.RetireRelationship(rel.Id, CreateTime(schema, 250));
        graph.RetireEntity(a.Id, CreateTime(schema, 300));

        var relFacts = graph.GetRelationshipHistoricalFacts(rel.Id);
        Assert.Equal(2, relFacts.Count);
        Assert.DoesNotContain(relFacts, f => f.Time.Equals(CreateTime(schema, 300)));
    }

    // 28. Fact kinds are machine-level classifications, not presentation strings
    // 29. No Skaldee-specific terminology exists in the EHG model
    [Fact]
    public void FactKinds_AreMachineClassifications_WithoutSkaldeeTerminology()
    {
        var enumNames = Enum.GetNames<SorophyHistoricalFactKind>();

        // Ensure machine-level classifications
        Assert.Equal(4, enumNames.Length);
        Assert.Contains("Created", enumNames);
        Assert.Contains("Retired", enumNames);
        Assert.Contains("PropertyChanged", enumNames);
        Assert.Contains("RelationshipChanged", enumNames);

        // Verify forbidden presentation labels do not exist in the engine enum
        Assert.DoesNotContain("Ended", enumNames);
        Assert.DoesNotContain("Died", enumNames);
        Assert.DoesNotContain("Destroyed", enumNames);
        Assert.DoesNotContain("Changed", enumNames);
        Assert.DoesNotContain("Founded", enumNames);
        Assert.DoesNotContain("Born", enumNames);
        Assert.DoesNotContain("Transformed", enumNames);
    }

    // 30. If no authoritative mutation facts currently exist, EHG does NOT expose Mutated facts
    [Fact]
    public void NoAuthoritativeMutationFacts_EHGDoesNotExposeMutatedFacts()
    {
        var schema = CreateSchema();
        var graph = new SorophyGraph();
        var e = CreateEntity("Node");

        graph.CreateEntity(e, CreateTime(schema, 100));
        graph.RetireEntity(e.Id, CreateTime(schema, 200));

        var facts = graph.GetEntityHistoricalFacts(e.Id);
        Assert.DoesNotContain(facts, f => f.Kind.ToString().Equals("Mutated", StringComparison.OrdinalIgnoreCase));
    }
}
