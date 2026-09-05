using System;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyEntityTests
{
    [Fact]
    public void Entity_ShouldHaveUniqueIdByDefault()
    {
        var entity1 = new SorophyEntity();
        var entity2 = new SorophyEntity();

        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void Entity_ShouldAllowCustomProperties()
    {
        var entity = new SorophyEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["language"] = new SorophyProperty
        {
            Name = "language",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avarian")
        };

        Assert.Equal(2, entity.Properties.Count);
    }

    [Fact]
    public void Entity_ShouldAllowDifferentPropertyValueTypes()
    {
        var entity = new SorophyEntity();

        entity.Properties["name"] = new SorophyProperty
        {
            Name = "name",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Avaria")
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        entity.Properties["active"] = new SorophyProperty
        {
            Name = "active",
            Value = new SorophyValue(
                SorophyValueType.Boolean,
                true)
        };

        Assert.Equal(
            SorophyValueType.String,
            entity.Properties["name"].Value.Type);

        Assert.Equal(
            SorophyValueType.Integer,
            entity.Properties["population"].Value.Type);

        Assert.Equal(
            SorophyValueType.Boolean,
            entity.Properties["active"].Value.Type);
    }

    [Fact]
    public void Entity_ShouldAllowArbitraryPropertyNames()
    {
        var entity = new SorophyEntity();

        entity.Properties["completely_custom_field"] = new SorophyProperty
        {
            Name = "completely_custom_field",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Anything")
        };

        Assert.True(
            entity.Properties.ContainsKey("completely_custom_field"));
    }

    [Fact]
    public void Entity_ShouldAllowNoProperties()
    {
        var entity = new SorophyEntity();

        Assert.Empty(entity.Properties);
    }

    [Fact]
    public void Entity_IsEvent_ShouldBeTrueForEventType()
    {
        var entity = new SorophyEntity
        {
            Type = "Event"
        };

        Assert.True(entity.IsEvent);
    }

    [Fact]
    public void Entity_IsEvent_ShouldBeCaseInsensitive()
    {
        var lowerCase = new SorophyEntity
        {
            Type = "event"
        };

        var upperCase = new SorophyEntity
        {
            Type = "EVENT"
        };

        var mixedCase = new SorophyEntity
        {
            Type = "EvEnT"
        };

        Assert.True(lowerCase.IsEvent);
        Assert.True(upperCase.IsEvent);
        Assert.True(mixedCase.IsEvent);
    }

    [Fact]
    public void Entity_IsEvent_ShouldBeFalseForNonEventTypes()
    {
        var character = new SorophyEntity
        {
            Type = "Character"
        };

        var kingdom = new SorophyEntity
        {
            Type = "Kingdom"
        };

        var location = new SorophyEntity
        {
            Type = "Location"
        };

        Assert.False(character.IsEvent);
        Assert.False(kingdom.IsEvent);
        Assert.False(location.IsEvent);
    }

    [Fact]
    public void Entity_IsEvent_ShouldBeFalseWhenTypeIsNull()
    {
        var entity = new SorophyEntity();

        Assert.Null(entity.Type);
        Assert.False(entity.IsEvent);
    }

    [Fact]
    public void Entity_IsEvent_ShouldBeDerivedFromType()
    {
        var entity = new SorophyEntity
        {
            Type = "Character"
        };

        Assert.False(entity.IsEvent);

        entity.Type = "Event";

        Assert.True(entity.IsEvent);

        entity.Type = "Kingdom";

        Assert.False(entity.IsEvent);
    }
}