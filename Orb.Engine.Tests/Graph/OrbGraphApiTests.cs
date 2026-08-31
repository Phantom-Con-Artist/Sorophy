using Orb.Engine.Graph;

namespace Orb.Engine.Tests.Graph;

public class OrbGraphApiTests
{
    [Fact]
    public void ContainsEntity_ShouldReturnTrueForExistingEntity()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        graph.AddEntity(entity);

        Assert.True(
            graph.ContainsEntity(entity.Id));
    }

    [Fact]
    public void ContainsEntity_ShouldReturnFalseForMissingEntity()
    {
        var graph = new OrbGraph();

        Assert.False(
            graph.ContainsEntity(Guid.NewGuid()));
    }

    [Fact]
    public void ContainsRelationship_ShouldReturnTrueForExistingRelationship()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "A"
        };

        var target = new OrbEntity
        {
            Name = "B"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        Assert.True(
            graph.ContainsRelationship(
                relationship.Id));
    }

    [Fact]
    public void ContainsRelationship_ShouldReturnFalseForMissingRelationship()
    {
        var graph = new OrbGraph();

        Assert.False(
            graph.ContainsRelationship(
                Guid.NewGuid()));
    }
}