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
using System.Linq;
using Sorophy.Engine.Graph;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyGraphTagIndexTests
{
    [Fact]
    public void AddEntity_ShouldAutomaticallyIndexTags()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        Assert.True(
            graph.ContainsTag("Kingdom"));

        Assert.True(
            graph.ContainsTag("Important"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Important"));
    }

    [Fact]
    public void AddEntity_ShouldIndexAnEntityWithoutTags()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Untyped"
            };

        graph.AddEntity(entity);

        Assert.True(
            graph.ContainsEntity(entity.Id));

        Assert.False(
            graph.ContainsTag("Character"));

        Assert.Empty(
            graph.GetTags());
    }

    [Fact]
    public void AddEntity_ShouldRejectDuplicateEntityIdWithoutCorruptingTagIndex()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Id = Guid.NewGuid(),
                Name = "First"
            };

        first.Tags.Add("Important");

        var duplicate =
            new SorophyEntity
            {
                Id = first.Id,
                Name = "Duplicate"
            };

        duplicate.Tags.Add("ShouldNotAppear");

        graph.AddEntity(first);

        Assert.Throws<InvalidOperationException>(
            () =>
                graph.AddEntity(duplicate));

        Assert.True(
            graph.HasEntityTag(
                first.Id,
                "Important"));

        Assert.False(
            graph.ContainsTag("ShouldNotAppear"));

        Assert.Single(
            graph.GetEntityIdsByTag("Important"));
    }

    [Fact]
    public void GetEntityIdsByTag_ShouldReturnIndexedEntityIds()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Aran"
            };

        var second =
            new SorophyEntity
            {
                Name = "Mira"
            };

        var other =
            new SorophyEntity
            {
                Name = "Veyr"
            };

        first.Tags.Add("Protagonist");
        second.Tags.Add("Protagonist");
        other.Tags.Add("Antagonist");

        graph.AddEntity(first);
        graph.AddEntity(second);
        graph.AddEntity(other);

        var result =
            graph.GetEntityIdsByTag(
                "Protagonist");

        Assert.Equal(
            2,
            result.Count);

        Assert.Contains(
            first.Id,
            result);

        Assert.Contains(
            second.Id,
            result);

        Assert.DoesNotContain(
            other.Id,
            result);
    }

    [Fact]
    public void GetEntitiesByTag_ShouldReturnIndexedEntities()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Aran"
            };

        var second =
            new SorophyEntity
            {
                Name = "Mira"
            };

        first.Tags.Add("Character");
        second.Tags.Add("Character");

        graph.AddEntity(first);
        graph.AddEntity(second);

        var result =
            graph.GetEntitiesByTag(
                    "Character")
                .ToList();

        Assert.Equal(
            2,
            result.Count);

        Assert.Contains(
            first,
            result);

        Assert.Contains(
            second,
            result);
    }

    [Fact]
    public void RemoveEntity_ShouldRemoveEntityFromTagIndex()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        var removed =
            graph.RemoveEntity(
                entity.Id);

        Assert.True(removed);

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Important"));

        Assert.False(
            graph.ContainsTag("Kingdom"));

        Assert.False(
            graph.ContainsTag("Important"));
    }

    [Fact]
    public void RemoveEntity_ShouldNotAffectOtherEntitiesSharingTags()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        var second =
            new SorophyEntity
            {
                Name = "Valor"
            };

        first.Tags.Add("Kingdom");
        second.Tags.Add("Kingdom");

        graph.AddEntity(first);
        graph.AddEntity(second);

        graph.RemoveEntity(
            first.Id);

        var result =
            graph.GetEntityIdsByTag(
                "Kingdom");

        Assert.Single(result);

        Assert.DoesNotContain(
            first.Id,
            result);

        Assert.Contains(
            second.Id,
            result);

        Assert.True(
            graph.ContainsTag("Kingdom"));
    }

    [Fact]
    public void RemoveEntity_ShouldReturnFalseAndLeaveTagIndexUntouched()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");

        graph.AddEntity(entity);

        var unknownId =
            Guid.NewGuid();

        Assert.False(
            graph.RemoveEntity(
                unknownId));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            graph.ContainsTag("Kingdom"));
    }

    [Fact]
    public void UpdateEntityTags_ShouldAddNewTags()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");

        graph.AddEntity(entity);

        entity.Tags.Add("Important");

        graph.UpdateEntityTags(
            entity.Id);

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Important"));

        Assert.Contains(
            entity.Id,
            graph.GetEntityIdsByTag(
                "Important"));
    }

    [Fact]
    public void UpdateEntityTags_ShouldRemoveDeletedTags()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Protagonist");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        entity.Tags.Remove(
            "Protagonist");

        graph.UpdateEntityTags(
            entity.Id);

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Protagonist"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Important"));

        Assert.False(
            graph.ContainsTag(
                "Protagonist"));
    }

    [Fact]
    public void UpdateEntityTags_ShouldReplaceTagMembershipCorrectly()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        entity.Tags.Remove("Kingdom");
        entity.Tags.Remove("Important");
        entity.Tags.Add("Character");
        entity.Tags.Add("Protagonist");

        graph.UpdateEntityTags(
            entity.Id);

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Important"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Character"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Protagonist"));

        Assert.False(
            graph.ContainsTag(
                "Kingdom"));

        Assert.False(
            graph.ContainsTag(
                "Important"));

        Assert.True(
            graph.ContainsTag(
                "Character"));

        Assert.True(
            graph.ContainsTag(
                "Protagonist"));
    }

    [Fact]
    public void UpdateEntityTags_ShouldHandleRemovingAllTags()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        entity.Tags.Clear();

        graph.UpdateEntityTags(
            entity.Id);

        Assert.False(
            graph.ContainsTag(
                "Kingdom"));

        Assert.False(
            graph.ContainsTag(
                "Important"));

        Assert.Empty(
            graph.GetTags());

        Assert.Empty(
            graph.GetEntityIdsByTag(
                "Kingdom"));

        Assert.Empty(
            graph.GetEntityIdsByTag(
                "Important"));
    }

    [Fact]
    public void UpdateEntityTags_ShouldThrowForUnknownEntity()
    {
        var graph =
            new SorophyGraph();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    graph.UpdateEntityTags(
                        Guid.NewGuid()));

        Assert.Contains(
            "does not exist",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RebuildTagIndex_ShouldRecoverFromDirectTagMutation()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");

        graph.AddEntity(entity);

        /*
         * Simulate a direct Entity mutation that bypasses the graph's
         * synchronization method.
         */
        entity.Tags.Remove(
            "Kingdom");

        entity.Tags.Add(
            "Character");

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Character"));

        graph.RebuildTagIndex();

        Assert.False(
            graph.HasEntityTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            graph.HasEntityTag(
                entity.Id,
                "Character"));
    }

    [Fact]
    public void RebuildTagIndex_ShouldReflectAllCanonicalEntities()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Aran"
            };

        var second =
            new SorophyEntity
            {
                Name = "Mira"
            };

        var third =
            new SorophyEntity
            {
                Name = "Veyr"
            };

        first.Tags.Add("Character");
        second.Tags.Add("Character");
        third.Tags.Add("Location");

        graph.AddEntity(first);
        graph.AddEntity(second);
        graph.AddEntity(third);

        /*
         * Change tags directly to simulate a bulk mutation.
         */
        first.Tags.Clear();
        first.Tags.Add("Location");

        second.Tags.Clear();
        second.Tags.Add("Antagonist");

        third.Tags.Clear();
        third.Tags.Add("Important");

        graph.RebuildTagIndex();

        Assert.Equal(
            1,
            graph.GetEntityIdsByTag(
                "Location").Count);

        Assert.Contains(
            first.Id,
            graph.GetEntityIdsByTag(
                "Location"));

        Assert.Equal(
            1,
            graph.GetEntityIdsByTag(
                "Antagonist").Count);

        Assert.Contains(
            second.Id,
            graph.GetEntityIdsByTag(
                "Antagonist"));

        Assert.Equal(
            1,
            graph.GetEntityIdsByTag(
                "Important").Count);

        Assert.Contains(
            third.Id,
            graph.GetEntityIdsByTag(
                "Important"));

        Assert.False(
            graph.ContainsTag(
                "Character"));
    }

    [Fact]
    public void GetTags_ShouldReturnDeterministicOrder()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Root Document");
        entity.Tags.Add("Protagonist");
        entity.Tags.Add("Antagonist");
        entity.Tags.Add("Character");
        entity.Tags.Add("Important");

        graph.AddEntity(entity);

        Assert.Equal(
            new[]
            {
                "Antagonist",
                "Character",
                "Important",
                "Protagonist",
                "Root Document"
            },
            graph.GetTags());
    }

    [Fact]
    public void GetEntitiesByTag_ShouldNotReturnEntitiesWithoutThatTag()
    {
        var graph =
            new SorophyGraph();

        var protagonist =
            new SorophyEntity
            {
                Name = "Aran"
            };

        var antagonist =
            new SorophyEntity
            {
                Name = "Veyr"
            };

        protagonist.Tags.Add(
            "Protagonist");

        antagonist.Tags.Add(
            "Antagonist");

        graph.AddEntity(
            protagonist);

        graph.AddEntity(
            antagonist);

        var result =
            graph.GetEntitiesByTag(
                    "Protagonist")
                .ToList();

        Assert.Single(result);

        Assert.Equal(
            protagonist.Id,
            result[0].Id);
    }

    [Fact]
    public void HasEntityTag_ShouldReturnFalseForUnknownEntity()
    {
        var graph =
            new SorophyGraph();

        Assert.False(
            graph.HasEntityTag(
                Guid.NewGuid(),
                "Protagonist"));
    }

    [Fact]
    public void GetEntityIdsByTag_ShouldReturnEmptyForUnknownTag()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add(
            "Kingdom");

        graph.AddEntity(
            entity);

        Assert.Empty(
            graph.GetEntityIdsByTag(
                "Protagonist"));
    }

    [Fact]
    public void Validate_ShouldPassForSynchronizedTagIndex()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        var second =
            new SorophyEntity
            {
                Name = "Valor"
            };

        first.Tags.Add("Kingdom");
        first.Tags.Add("Important");

        second.Tags.Add("City");
        second.Tags.Add("Important");

        graph.AddEntity(first);
        graph.AddEntity(second);

        var errors =
            graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldDetectStaleTagIndexAfterDirectMutation()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add(
            "Kingdom");

        graph.AddEntity(
            entity);

        /*
         * Deliberately bypass the synchronization API.
         */
        entity.Tags.Remove(
            "Kingdom");

        entity.Tags.Add(
            "Character");

        var errors =
            graph.Validate();

        Assert.NotEmpty(errors);

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "tag",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RebuildTagIndex_ShouldRestoreValidateIntegrity()
    {
        var graph =
            new SorophyGraph();

        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add(
            "Kingdom");

        graph.AddEntity(
            entity);

        entity.Tags.Clear();
        entity.Tags.Add(
            "Character");

        Assert.NotEmpty(
            graph.Validate());

        graph.RebuildTagIndex();

        Assert.Empty(
            graph.Validate());
    }

    [Fact]
    public void RemoveEntity_ShouldPreserveGraphValidation()
    {
        var graph =
            new SorophyGraph();

        var first =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        var second =
            new SorophyEntity
            {
                Name = "Valor"
            };

        first.Tags.Add(
            "Kingdom");

        second.Tags.Add(
            "City");

        graph.AddEntity(first);
        graph.AddEntity(second);

        graph.RemoveEntity(
            first.Id);

        var errors =
            graph.Validate();

        Assert.Empty(errors);

        Assert.False(
            graph.ContainsEntity(
                first.Id));

        Assert.True(
            graph.ContainsEntity(
                second.Id));

        Assert.False(
            graph.ContainsTag(
                "Kingdom"));

        Assert.True(
            graph.ContainsTag(
                "City"));
    }
}