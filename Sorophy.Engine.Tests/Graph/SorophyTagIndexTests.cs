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
using Sorophy.Engine.Graph;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyTagIndexTests
{
    [Fact]
    public void IndexEntity_ShouldIndexAllEntityTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");
        entity.Tags.Add("Root Document");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        Assert.True(
            index.ContainsEntity(entity.Id));

        Assert.True(
            index.ContainsTag("Kingdom"));

        Assert.True(
            index.ContainsTag("Important"));

        Assert.True(
            index.ContainsTag("Root Document"));

        Assert.Contains(
            entity.Id,
            index.GetEntityIds("Kingdom"));

        Assert.Contains(
            entity.Id,
            index.GetEntityIds("Important"));

        Assert.Contains(
            entity.Id,
            index.GetEntityIds("Root Document"));
    }

    [Fact]
    public void IndexEntity_ShouldNotDuplicateEntityMembership()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);
        index.IndexEntity(entity);

        var entities =
            index.GetEntityIds("Kingdom");

        Assert.Single(entities);

        Assert.Contains(
            entity.Id,
            entities);
    }

    [Fact]
    public void GetEntityIds_ShouldReturnEmptyForUnknownTag()
    {
        var index =
            new SorophyTagIndex();

        var result =
            index.GetEntityIds("Protagonist");

        Assert.Empty(result);
    }

    [Fact]
    public void ContainsTag_ShouldReturnFalseForUnknownTag()
    {
        var index =
            new SorophyTagIndex();

        Assert.False(
            index.ContainsTag("Protagonist"));
    }

    [Fact]
    public void HasTag_ShouldReturnTrueForIndexedMembership()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Protagonist");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        Assert.True(
            index.HasTag(
                entity.Id,
                "Protagonist"));
    }

    [Fact]
    public void HasTag_ShouldReturnFalseForMissingMembership()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Protagonist");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        Assert.False(
            index.HasTag(
                entity.Id,
                "Antagonist"));
    }

    [Fact]
    public void GetEntityIds_ShouldReturnAllEntitiesWithTag()
    {
        var protagonist =
            new SorophyEntity
            {
                Name = "Aran"
            };

        var secondProtagonist =
            new SorophyEntity
            {
                Name = "Mira"
            };

        var antagonist =
            new SorophyEntity
            {
                Name = "Veyr"
            };

        protagonist.Tags.Add("Protagonist");
        secondProtagonist.Tags.Add("Protagonist");
        antagonist.Tags.Add("Antagonist");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(protagonist);
        index.IndexEntity(secondProtagonist);
        index.IndexEntity(antagonist);

        var result =
            index.GetEntityIds("Protagonist");

        Assert.Equal(
            2,
            result.Count);

        Assert.Contains(
            protagonist.Id,
            result);

        Assert.Contains(
            secondProtagonist.Id,
            result);

        Assert.DoesNotContain(
            antagonist.Id,
            result);
    }

    [Fact]
    public void UpdateEntity_ShouldAddNewTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        entity.Tags.Add("Important");

        index.UpdateEntity(entity);

        Assert.True(
            index.HasTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            index.HasTag(
                entity.Id,
                "Important"));
    }

    [Fact]
    public void UpdateEntity_ShouldRemoveDeletedTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Protagonist");
        entity.Tags.Add("Important");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        entity.Tags.Remove("Protagonist");

        index.UpdateEntity(entity);

        Assert.False(
            index.HasTag(
                entity.Id,
                "Protagonist"));

        Assert.True(
            index.HasTag(
                entity.Id,
                "Important"));

        Assert.False(
            index.ContainsTag("Protagonist"));
    }

    [Fact]
    public void UpdateEntity_ShouldPreserveUnchangedTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Important");
        entity.Tags.Add("Character");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        entity.Tags.Remove("Character");
        entity.Tags.Add("Protagonist");

        index.UpdateEntity(entity);

        Assert.True(
            index.HasTag(
                entity.Id,
                "Important"));

        Assert.False(
            index.HasTag(
                entity.Id,
                "Character"));

        Assert.True(
            index.HasTag(
                entity.Id,
                "Protagonist"));
    }

    [Fact]
    public void UpdateEntity_ShouldWorkWhenEntityWasNotPreviouslyIndexed()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        var index =
            new SorophyTagIndex();

        index.UpdateEntity(entity);

        Assert.True(
            index.ContainsEntity(entity.Id));

        Assert.True(
            index.HasTag(
                entity.Id,
                "Kingdom"));

        Assert.True(
            index.HasTag(
                entity.Id,
                "Important"));
    }

    [Fact]
    public void UpdateEntity_ShouldRemoveEntityFromTagsWhenAllTagsAreRemoved()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        entity.Tags.Clear();

        index.UpdateEntity(entity);

        Assert.True(
            index.ContainsEntity(entity.Id));

        Assert.False(
            index.ContainsTag("Kingdom"));

        Assert.False(
            index.ContainsTag("Important"));

        Assert.Empty(
            index.GetEntityIds("Kingdom"));

        Assert.Empty(
            index.GetEntityIds("Important"));
    }

    [Fact]
    public void RemoveEntity_ShouldRemoveEntityFromEveryTag()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add("Kingdom");
        entity.Tags.Add("Important");
        entity.Tags.Add("Root Document");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        var removed =
            index.RemoveEntity(entity.Id);

        Assert.True(removed);

        Assert.False(
            index.ContainsEntity(entity.Id));

        Assert.False(
            index.ContainsTag("Kingdom"));

        Assert.False(
            index.ContainsTag("Important"));

        Assert.False(
            index.ContainsTag("Root Document"));

        Assert.Empty(
            index.GetEntityIds("Kingdom"));

        Assert.Empty(
            index.GetEntityIds("Important"));

        Assert.Empty(
            index.GetEntityIds("Root Document"));
    }

    [Fact]
    public void RemoveEntity_ShouldReturnFalseForUnknownEntity()
    {
        var index =
            new SorophyTagIndex();

        var removed =
            index.RemoveEntity(
                Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public void RemoveEntity_ShouldOnlyRemoveSpecifiedEntity()
    {
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

        var index =
            new SorophyTagIndex();

        index.IndexEntity(first);
        index.IndexEntity(second);

        index.RemoveEntity(first.Id);

        Assert.False(
            index.ContainsEntity(first.Id));

        Assert.True(
            index.ContainsEntity(second.Id));

        Assert.DoesNotContain(
            first.Id,
            index.GetEntityIds("Kingdom"));

        Assert.Contains(
            second.Id,
            index.GetEntityIds("Kingdom"));
    }

    [Fact]
    public void Rebuild_ShouldReplaceExistingIndex()
    {
        var oldEntity =
            new SorophyEntity
            {
                Name = "Old"
            };

        oldEntity.Tags.Add("Legacy");

        var newEntity =
            new SorophyEntity
            {
                Name = "New"
            };

        newEntity.Tags.Add("Current");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(oldEntity);

        index.Rebuild(
            new[]
            {
                newEntity
            });

        Assert.False(
            index.ContainsEntity(oldEntity.Id));

        Assert.True(
            index.ContainsEntity(newEntity.Id));

        Assert.False(
            index.ContainsTag("Legacy"));

        Assert.True(
            index.ContainsTag("Current"));

        Assert.Empty(
            index.GetEntityIds("Legacy"));

        Assert.Contains(
            newEntity.Id,
            index.GetEntityIds("Current"));
    }

    [Fact]
    public void Rebuild_ShouldHandleEntitiesWithoutTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Untyped"
            };

        var index =
            new SorophyTagIndex();

        index.Rebuild(
            new[]
            {
                entity
            });

        Assert.True(
            index.ContainsEntity(entity.Id));

        Assert.Equal(
            0,
            index.TagCount);

        Assert.Equal(
            1,
            index.EntityCount);

        Assert.Empty(
            index.GetTags());
    }

    [Fact]
    public void Clear_ShouldRemoveAllIndexState()
    {
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
        second.Tags.Add("City");

        var index =
            new SorophyTagIndex();

        index.IndexEntity(first);
        index.IndexEntity(second);

        index.Clear();

        Assert.Equal(
            0,
            index.TagCount);

        Assert.Equal(
            0,
            index.EntityCount);

        Assert.Empty(
            index.GetTags());

        Assert.False(
            index.ContainsEntity(first.Id));

        Assert.False(
            index.ContainsEntity(second.Id));

        Assert.False(
            index.ContainsTag("Kingdom"));

        Assert.False(
            index.ContainsTag("City"));
    }

    [Fact]
    public void GetTags_ShouldReturnDeterministicOrdinalOrder()
    {
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

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        var tags =
            index.GetTags();

        Assert.Equal(
            new[]
            {
                "Antagonist",
                "Character",
                "Important",
                "Protagonist",
                "Root Document"
            },
            tags);
    }

    [Fact]
    public void TagAndEntityCounts_ShouldTrackCurrentIndexState()
    {
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

        var index =
            new SorophyTagIndex();

        index.IndexEntity(first);
        index.IndexEntity(second);

        Assert.Equal(
            3,
            index.TagCount);

        Assert.Equal(
            2,
            index.EntityCount);

        index.RemoveEntity(first.Id);

        Assert.Equal(
            2,
            index.TagCount);

        Assert.Equal(
            1,
            index.EntityCount);

        Assert.True(
            index.ContainsTag("Important"));

        Assert.False(
            index.ContainsTag("Kingdom"));

        Assert.True(
            index.ContainsTag("City"));
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidIndex()
    {
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

        var index =
            new SorophyTagIndex();

        index.IndexEntity(first);
        index.IndexEntity(second);

        var errors =
            index.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void IndexEntity_ShouldRejectEmptyTag()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        entity.Tags.Add(" ");

        var index =
            new SorophyTagIndex();

        Assert.Throws<ArgumentException>(
            () =>
                index.IndexEntity(entity));
    }

    [Fact]
    public void GetEntityIds_ShouldRejectEmptyTag()
    {
        var index =
            new SorophyTagIndex();

        Assert.Throws<ArgumentException>(
            () =>
                index.GetEntityIds(" "));
    }

    [Fact]
    public void ContainsTag_ShouldRejectEmptyTag()
    {
        var index =
            new SorophyTagIndex();

        Assert.Throws<ArgumentException>(
            () =>
                index.ContainsTag(" "));
    }

    [Fact]
    public void HasTag_ShouldRejectEmptyTag()
    {
        var index =
            new SorophyTagIndex();

        Assert.Throws<ArgumentException>(
            () =>
                index.HasTag(
                    Guid.NewGuid(),
                    " "));
    }

    [Fact]
    public void IndexEntity_ShouldHandleEntityWithManyTags()
    {
        var entity =
            new SorophyEntity
            {
                Name = "Avaria"
            };

        for (var i = 0; i < 100; i++)
        {
            entity.Tags.Add(
                $"Tag-{i:D3}");
        }

        var index =
            new SorophyTagIndex();

        index.IndexEntity(entity);

        Assert.Equal(
            100,
            index.TagCount);

        Assert.Equal(
            1,
            index.EntityCount);

        for (var i = 0; i < 100; i++)
        {
            Assert.True(
                index.HasTag(
                    entity.Id,
                    $"Tag-{i:D3}"));
        }

        var errors =
            index.Validate();

        Assert.Empty(errors);
    }
}
