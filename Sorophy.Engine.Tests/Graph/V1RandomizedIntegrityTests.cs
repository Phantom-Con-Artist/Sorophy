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

public sealed class V1RandomizedIntegrityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(12345)]
    [InlineData(987654321)]
    public void RandomizedGraphWorkload_PreservesAllInvariantsAcrossRequiredSeeds(int seed)
    {
        var rng = new Random(seed);
        var graph = new SorophyGraph();
        var entityIds = new List<Guid>();
        var relationshipIds = new List<Guid>();

        const int operations = 500;

        for (int op = 0; op < operations; op++)
        {
            int action = rng.Next(6);

            switch (action)
            {
                case 0: // Add Entity
                {
                    var id = Guid.NewGuid();
                    var entity = new SorophyEntity
                    {
                        Id = id,
                        Name = $"Entity_{seed}_{op}",
                        Type = op % 2 == 0 ? "Node" : "Item"
                    };
                    entity.Tags.Add($"tag_{rng.Next(10)}");
                    entity.Properties["score"] = new SorophyProperty
                    {
                        Name = "score",
                        Value = new SorophyValue(SorophyValueType.Integer, (long)rng.Next(1000))
                    };

                    graph.AddEntity(entity);
                    entityIds.Add(id);
                    break;
                }

                case 1: // Add Relationship (if at least 2 entities)
                {
                    if (entityIds.Count >= 2)
                    {
                        var sId = entityIds[rng.Next(entityIds.Count)];
                        var tId = entityIds[rng.Next(entityIds.Count)];

                        var relId = Guid.NewGuid();
                        var rel = new SorophyRelationship
                        {
                            Id = relId,
                            SourceId = sId,
                            TargetId = tId,
                            Type = $"Type_{rng.Next(5)}"
                        };
                        rel.Properties["weight"] = new SorophyProperty
                        {
                            Name = "weight",
                            Value = new SorophyValue(SorophyValueType.Integer, (long)rng.Next(100))
                        };

                        graph.AddRelationship(rel);
                        relationshipIds.Add(relId);
                    }
                    break;
                }

                case 2: // Mutate Entity Properties / Tags
                {
                    if (entityIds.Count > 0)
                    {
                        var id = entityIds[rng.Next(entityIds.Count)];
                        if (graph.TryGetEntity(id, out var entity) && entity is not null)
                        {
                            entity.Name = $"Mutated_{seed}_{op}";
                            entity.Tags.Add($"tag_dynamic_{rng.Next(5)}");
                            entity.Documents[$"doc_{op}.md"] =
                                new SorophyEntityDocument($"doc_{op}.md", $"Content at op {op}");
                            graph.UpdateEntityTags(id);
                        }
                    }
                    break;
                }

                case 3: // Remove Relationship
                {
                    if (relationshipIds.Count > 0)
                    {
                        int idx = rng.Next(relationshipIds.Count);
                        var relId = relationshipIds[idx];
                        relationshipIds.RemoveAt(idx);

                        bool removed = graph.RemoveRelationship(relId);
                        Assert.True(removed);
                        Assert.False(graph.ContainsRelationship(relId));
                    }
                    break;
                }

                case 4: // Remove Entity (cascading removal of connected edges)
                {
                    if (entityIds.Count > 0)
                    {
                        int idx = rng.Next(entityIds.Count);
                        var entId = entityIds[idx];
                        entityIds.RemoveAt(idx);

                        // Find edges that should be removed
                        var deadEdges = graph.Relationships.Values
                            .Where(r => r.SourceId == entId || r.TargetId == entId)
                            .Select(r => r.Id)
                            .ToHashSet();

                        bool removed = graph.RemoveEntity(entId);
                        Assert.True(removed);
                        Assert.False(graph.ContainsEntity(entId));

                        relationshipIds.RemoveAll(id => deadEdges.Contains(id));
                    }
                    break;
                }

                case 5: // Intermediate Validation & Adjacency Check
                {
                    var errors = graph.Validate();
                    Assert.Empty(errors);

                    // Verify active entity and relationship counts match collections
                    Assert.Equal(graph.Entities.Count, graph.Entities.Keys.Count());
                    Assert.Equal(graph.Relationships.Count, graph.Relationships.Keys.Count());

                    if (entityIds.Count > 0)
                    {
                        var checkId = entityIds[rng.Next(entityIds.Count)];
                        var outRels = graph.GetOutgoingRelationships(checkId);
                        foreach (var rel in outRels)
                        {
                            Assert.Equal(checkId, rel.SourceId);
                            Assert.True(graph.ContainsEntity(rel.TargetId));
                        }
                    }
                    break;
                }
            }
        }

        // Final thorough validation
        var finalErrors = graph.Validate();
        Assert.Empty(finalErrors);
    }
}
