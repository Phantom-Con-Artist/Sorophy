using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Graph;

public class OrbEntityTests
{
    [Fact]
    public void Entity_ShouldHaveUniqueIdByDefault()
    {
        var entity1 = new OrbEntity();
        var entity2 = new OrbEntity();

        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void Entity_ShouldAllowCustomProperties()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria",
            Type = "Kingdom"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        entity.Properties["language"] = new OrbProperty
        {
            Name = "language",
            Value = new OrbValue(
                OrbValueType.String,
                "Avarian")
        };

        Assert.Equal(2, entity.Properties.Count);
    }

    [Fact]
    public void Entity_ShouldAllowDifferentPropertyValueTypes()
    {
        var entity = new OrbEntity();

        entity.Properties["name"] = new OrbProperty
        {
            Name = "name",
            Value = new OrbValue(
                OrbValueType.String,
                "Avaria")
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        entity.Properties["active"] = new OrbProperty
        {
            Name = "active",
            Value = new OrbValue(
                OrbValueType.Boolean,
                true)
        };

        Assert.Equal(
            OrbValueType.String,
            entity.Properties["name"].Value.Type);

        Assert.Equal(
            OrbValueType.Integer,
            entity.Properties["population"].Value.Type);

        Assert.Equal(
            OrbValueType.Boolean,
            entity.Properties["active"].Value.Type);
    }

    [Fact]
    public void Entity_ShouldAllowArbitraryPropertyNames()
    {
        var entity = new OrbEntity();

        entity.Properties["completely_custom_field"] = new OrbProperty
        {
            Name = "completely_custom_field",
            Value = new OrbValue(
                OrbValueType.String,
                "Anything")
        };

        Assert.True(
            entity.Properties.ContainsKey("completely_custom_field"));
    }

    [Fact]
    public void Entity_ShouldAllowNoProperties()
    {
        var entity = new OrbEntity();

        Assert.Empty(entity.Properties);
    }
}