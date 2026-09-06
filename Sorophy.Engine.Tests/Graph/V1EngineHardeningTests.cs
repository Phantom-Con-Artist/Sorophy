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
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public sealed class V1EngineHardeningTests
{
    // =============================================================
    // 1. V1 ENTITY IDENTITY & OBSERVABLE FIELDS
    // =============================================================

    [Fact]
    public void Entity_CustomExplicitId_IsPreservedInGraph()
    {
        var graph = new SorophyGraph();
        var explicitId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var entity = new SorophyEntity
        {
            Id = explicitId,
            Name = "ExplicitNode"
        };

        graph.AddEntity(entity);

        Assert.True(graph.TryGetEntity(explicitId, out var retrieved));
        Assert.Same(entity, retrieved);
        Assert.Equal(explicitId, retrieved!.Id);
    }

    [Fact]
    public void Entity_MissingIdLookup_ReturnsFalseAndNull()
    {
        var graph = new SorophyGraph();
        var missingId = Guid.NewGuid();

        Assert.False(graph.TryGetEntity(missingId, out var retrieved));
        Assert.Null(retrieved);
        Assert.False(graph.ContainsEntity(missingId));
    }

    [Fact]
    public void Entity_ReAdditionAfterRemoval_MaintainsIdentityConsistency()
    {
        var graph = new SorophyGraph();
        var id = Guid.NewGuid();

        var original = new SorophyEntity { Id = id, Name = "Original" };
        graph.AddEntity(original);
        Assert.True(graph.RemoveEntity(id));
        Assert.False(graph.ContainsEntity(id));

        var readded = new SorophyEntity { Id = id, Name = "Re-added" };
        graph.AddEntity(readded);

        Assert.True(graph.ContainsEntity(id));
        Assert.True(graph.TryGetEntity(id, out var current));
        Assert.Equal("Re-added", current!.Name);
    }

    [Fact]
    public void Entity_UnicodeAndUnusualStrings_PreservedExactly()
    {
        var graph = new SorophyGraph();
        var unicodeName = "🌟 宇宙 / Космос / 宇宙 / فضاء / אวกาศ 🚀";
        var descriptionWithControl = "Line1\r\nLine2\t\"Quoted\"\0Backslash\\End";

        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = unicodeName,
            Type = "UnicodeType_世界",
            Description = descriptionWithControl
        };

        graph.AddEntity(entity);

        Assert.True(graph.TryGetEntity(entity.Id, out var result));
        Assert.Equal(unicodeName, result!.Name);
        Assert.Equal("UnicodeType_世界", result.Type);
        Assert.Equal(descriptionWithControl, result.Description);
    }

    [Fact]
    public void Entity_VeryLongStrings_PreservedWithoutTruncation()
    {
        var graph = new SorophyGraph();
        var longString = new string('X', 50_000);

        var entity = new SorophyEntity
        {
            Id = Guid.NewGuid(),
            Name = "LongEntity",
            Description = longString
        };

        entity.Properties["LongPayload"] = new SorophyProperty
        {
            Name = "LongPayload",
            Value = new SorophyValue(SorophyValueType.String, longString)
        };

        graph.AddEntity(entity);

        Assert.True(graph.TryGetEntity(entity.Id, out var result));
        Assert.Equal(50_000, result!.Description?.Length);
        Assert.Equal(longString, result.Description);
        Assert.Equal(longString, (string)result.Properties["LongPayload"].Value.Value!);
    }

    [Fact]
    public void Entity_TagsDuplicateHandling_ActsAsStrictSet()
    {
        var entity = new SorophyEntity();

        Assert.True(entity.Tags.Add("Important"));
        Assert.False(entity.Tags.Add("Important")); // Duplicate rejected by set semantics
        Assert.True(entity.Tags.Add("important")); // Ordinal difference preserved

        Assert.Equal(2, entity.Tags.Count);
        Assert.Contains("Important", entity.Tags);
        Assert.Contains("important", entity.Tags);
    }

    [Fact]
    public void Entity_EmbeddedDocuments_StoreMultipleContentTypes()
    {
        var entity = new SorophyEntity();

        var mdDoc = new SorophyEntityDocument("ReadMe.md", "# Title\nContent", "text/markdown");
        var jsonDoc = new SorophyEntityDocument("schema.json", "{\"key\":\"val\"}", "application/json");

        entity.Documents.Add(mdDoc.Name, mdDoc);
        entity.Documents.Add(jsonDoc.Name, jsonDoc);

        Assert.Equal(2, entity.Documents.Count);
        Assert.Equal("text/markdown", entity.Documents["ReadMe.md"].ContentType);
        Assert.Equal("application/json", entity.Documents["schema.json"].ContentType);
        Assert.Equal("# Title\nContent", entity.Documents["ReadMe.md"].Content);
    }

    [Fact]
    public void Entity_DeepNestedProperties_MaintainsStructuralIntegrity()
    {
        var entity = new SorophyEntity();

        var nestedDict = new Dictionary<string, object?>
        {
            ["alpha"] = 100L,
            ["beta"] = new List<object?> { "inner1", "inner2", 999L },
            ["gamma"] = new Dictionary<string, object?> { ["deepKey"] = "deepVal" }
        };

        entity.Properties["DeepObject"] = new SorophyProperty
        {
            Name = "DeepObject",
            Value = new SorophyValue(SorophyValueType.Object, nestedDict)
        };

        var val = (Dictionary<string, object?>)entity.Properties["DeepObject"].Value.Value!;
        var betaList = (List<object?>)val["beta"]!;
        var gammaDict = (Dictionary<string, object?>)val["gamma"]!;

        Assert.Equal(100L, val["alpha"]);
        Assert.Equal(3, betaList.Count);
        Assert.Equal("deepVal", gammaDict["deepKey"]);
    }

    // =============================================================
    // 2. V1 RELATIONSHIP HARDENING & INTEGRITY
    // =============================================================

    [Fact]
    public void Relationship_RejectInvalidSourceAndTarget()
    {
        var graph = new SorophyGraph();
        var validEntity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Node1" };
        graph.AddEntity(validEntity);

        // Missing source
        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(new SorophyRelationship
            {
                SourceId = Guid.NewGuid(),
                TargetId = validEntity.Id,
                Type = "Edge"
            }));

        // Missing target
        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(new SorophyRelationship
            {
                SourceId = validEntity.Id,
                TargetId = Guid.NewGuid(),
                Type = "Edge"
            }));
    }

    [Fact]
    public void Relationship_DuplicateId_ThrowsInvalidOperationException()
    {
        var graph = new SorophyGraph();
        var s = new SorophyEntity();
        var t = new SorophyEntity();
        graph.AddEntity(s);
        graph.AddEntity(t);

        var relId = Guid.NewGuid();
        var rel1 = new SorophyRelationship { Id = relId, SourceId = s.Id, TargetId = t.Id, Type = "TypeA" };
        var rel2 = new SorophyRelationship { Id = relId, SourceId = s.Id, TargetId = t.Id, Type = "TypeB" };

        graph.AddRelationship(rel1);
        Assert.Throws<InvalidOperationException>(() => graph.AddRelationship(rel2));
    }

    [Fact]
    public void Relationship_CascadingDeletion_CleansUpBothEndpoints()
    {
        var graph = new SorophyGraph();
        var a = new SorophyEntity { Name = "A" };
        var b = new SorophyEntity { Name = "B" };
        var c = new SorophyEntity { Name = "C" };
        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        var relAB = new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "AB" };
        var relBC = new SorophyRelationship { SourceId = b.Id, TargetId = c.Id, Type = "BC" };
        graph.AddRelationship(relAB);
        graph.AddRelationship(relBC);

        // Remove node B: both relAB and relBC must be removed
        Assert.True(graph.RemoveEntity(b.Id));

        Assert.False(graph.ContainsRelationship(relAB.Id));
        Assert.False(graph.ContainsRelationship(relBC.Id));
        Assert.Empty(graph.GetOutgoingRelationships(a.Id));
        Assert.Empty(graph.GetIncomingRelationships(c.Id));
        Assert.Empty(graph.Relationships);
    }

    // =============================================================
    // 3. V1 INDEXING, ADJACENCY & CHURN
    // =============================================================

    [Fact]
    public void Indexing_ChurnCycle_LeavesZeroGhostEntries()
    {
        var graph = new SorophyGraph();
        var a = new SorophyEntity { Id = Guid.NewGuid(), Name = "A" };
        var b = new SorophyEntity { Id = Guid.NewGuid(), Name = "B" };

        // Cycle 1: Add
        graph.AddEntity(a);
        graph.AddEntity(b);
        var rel = new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "Link" };
        graph.AddRelationship(rel);

        Assert.Single(graph.GetOutgoingRelationships(a.Id));
        Assert.Single(graph.GetIncomingRelationships(b.Id));

        // Cycle 2: Remove relationship
        graph.RemoveRelationship(rel.Id);
        Assert.Empty(graph.GetOutgoingRelationships(a.Id));
        Assert.Empty(graph.GetIncomingRelationships(b.Id));

        // Cycle 3: Re-add relationship with new ID
        var rel2 = new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "Link2" };
        graph.AddRelationship(rel2);
        Assert.Single(graph.GetOutgoingRelationships(a.Id));
        Assert.Single(graph.GetIncomingRelationships(b.Id));

        // Cycle 4: Remove entity a
        graph.RemoveEntity(a.Id);
        Assert.Empty(graph.GetIncomingRelationships(b.Id));
        Assert.Empty(graph.Relationships);

        // Cycle 5: Re-add a and check validation
        graph.AddEntity(a);
        Assert.Empty(graph.Validate());
    }

    [Fact]
    public void TagIndex_DynamicEntityMutation_ReflectsInTagLookups()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Id = Guid.NewGuid(), Name = "Tagged" };
        entity.Tags.Add("Active");
        entity.Tags.Add("Alpha");
        graph.AddEntity(entity);

        var activeEntities = graph.GetEntitiesByTag("Active").ToList();
        Assert.Single(activeEntities);
        Assert.Equal(entity.Id, activeEntities[0].Id);

        graph.RemoveEntity(entity.Id);
        Assert.Empty(graph.GetEntitiesByTag("Active"));
    }

    // =============================================================
    // 4. V1 TRAVERSAL & REACHABILITY
    // =============================================================

    [Fact]
    public void Traversal_LongLinearChain_ReachableAcross100Nodes()
    {
        var graph = new SorophyGraph();
        const int nodeCount = 100;
        var entities = new List<SorophyEntity>(nodeCount);

        for (int i = 0; i < nodeCount; i++)
        {
            var e = new SorophyEntity { Id = Guid.NewGuid(), Name = $"Node_{i}" };
            graph.AddEntity(e);
            entities.Add(e);
        }

        for (int i = 0; i < nodeCount - 1; i++)
        {
            graph.AddRelationship(new SorophyRelationship
            {
                SourceId = entities[i].Id,
                TargetId = entities[i + 1].Id,
                Type = "Next"
            });
        }

        Assert.True(graph.IsReachable(entities[0].Id, entities[^1].Id));
        Assert.False(graph.IsReachable(entities[^1].Id, entities[0].Id)); // Directed graph
    }

    [Fact]
    public void Traversal_CyclesAndBranching_HandlesWithoutInfiniteRecursion()
    {
        var graph = new SorophyGraph();
        var a = new SorophyEntity { Id = Guid.NewGuid(), Name = "A" };
        var b = new SorophyEntity { Id = Guid.NewGuid(), Name = "B" };
        var c = new SorophyEntity { Id = Guid.NewGuid(), Name = "C" };
        var d = new SorophyEntity { Id = Guid.NewGuid(), Name = "D" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);
        graph.AddEntity(d);

        // Cycle: A -> B -> C -> A
        graph.AddRelationship(new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "Next" });
        graph.AddRelationship(new SorophyRelationship { SourceId = b.Id, TargetId = c.Id, Type = "Next" });
        graph.AddRelationship(new SorophyRelationship { SourceId = c.Id, TargetId = a.Id, Type = "Next" });

        // Branch: B -> D
        graph.AddRelationship(new SorophyRelationship { SourceId = b.Id, TargetId = d.Id, Type = "Branch" });

        Assert.True(graph.IsReachable(a.Id, c.Id));
        Assert.True(graph.IsReachable(c.Id, b.Id));
        Assert.True(graph.IsReachable(a.Id, d.Id));
        Assert.True(graph.IsReachable(c.Id, d.Id));
        Assert.False(graph.IsReachable(d.Id, a.Id)); // D is a leaf
    }

    [Fact]
    public void Traversal_ReachabilityDropsImmediatelyAfterEdgeDeletion()
    {
        var graph = new SorophyGraph();
        var a = new SorophyEntity { Id = Guid.NewGuid(), Name = "A" };
        var b = new SorophyEntity { Id = Guid.NewGuid(), Name = "B" };
        graph.AddEntity(a);
        graph.AddEntity(b);

        var rel = new SorophyRelationship { SourceId = a.Id, TargetId = b.Id, Type = "Link" };
        graph.AddRelationship(rel);

        Assert.True(graph.IsReachable(a.Id, b.Id));

        graph.RemoveRelationship(rel.Id);

        Assert.False(graph.IsReachable(a.Id, b.Id));
    }
}

