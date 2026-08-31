using Orb.Engine.Graph;

namespace Orb.Engine.Tests.Graph;

public class OrbGraphTests
{
    [Fact]
    public void AddEntity_ShouldAddEntityToGraph()
    {
        var graph = new OrbGraph();
        var entity = new OrbEntity { Name = "Avaria" };

        graph.AddEntity(entity);

        Assert.True(graph.TryGetEntity(entity.Id, out var result));
        Assert.Same(entity, result);
    }

    [Fact]
    public void AddEntity_ShouldRejectDuplicateId()
    {
        var graph = new OrbGraph();

        var entity1 = new OrbEntity { Name = "Avaria" };
        var entity2 = new OrbEntity
        {
            Id = entity1.Id,
            Name = "Another Avaria"
        };

        graph.AddEntity(entity1);

        Assert.Throws<InvalidOperationException>(
            () => graph.AddEntity(entity2));
    }

    [Fact]
    public void AddRelationship_ShouldAddValidRelationship()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity { Name = "Avaria" };
        var target = new OrbEntity { Name = "Velaris" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        Assert.True(
            graph.TryGetRelationship(relationship.Id, out var result));

        Assert.Same(relationship, result);
    }

    [Fact]
    public void AddRelationship_ShouldRejectMissingSource()
    {
        var graph = new OrbGraph();

        var target = new OrbEntity { Name = "Velaris" };
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "capital_of",
            SourceId = Guid.NewGuid(),
            TargetId = target.Id
        };

        Assert.Throws<InvalidOperationException>(
            () => graph.AddRelationship(relationship));
    }

    [Fact]
    public void AddRelationship_ShouldRejectMissingTarget()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity { Name = "Avaria" };
        graph.AddEntity(source);

        var relationship = new OrbRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = Guid.NewGuid()
        };

        Assert.Throws<InvalidOperationException>(
            () => graph.AddRelationship(relationship));
    }

    [Fact]
    public void AddRelationship_ShouldAllowSelfLink()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity { Name = "Self Referencing Entity" };
        graph.AddEntity(entity);

        var relationship = new OrbRelationship
        {
            Type = "references_itself",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        Assert.True(
            graph.TryGetRelationship(relationship.Id, out var result));

        Assert.Same(relationship, result);
    }

    [Fact]
    public void AddRelationship_ShouldAllowMultipleRelationshipsBetweenSameEntities()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity { Name = "Avaria" };
        var target = new OrbEntity { Name = "Dravak" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship1 = new OrbRelationship
        {
            Type = "borders",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var relationship2 = new OrbRelationship
        {
            Type = "trades_with",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship1);
        graph.AddRelationship(relationship2);

        Assert.Equal(2, graph.Relationships.Count);
    }

    [Fact]
    public void RemoveRelationship_ShouldRemoveRelationship()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity();
        var target = new OrbEntity();

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "connects",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        var removed = graph.RemoveRelationship(relationship.Id);

        Assert.True(removed);
        Assert.False(
            graph.TryGetRelationship(relationship.Id, out _));
    }

    [Fact]
    public void RemoveEntity_ShouldRemoveConnectedRelationships()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity { Name = "Avaria" };
        var target = new OrbEntity { Name = "Velaris" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        var removed = graph.RemoveEntity(target.Id);

        Assert.True(removed);

        Assert.False(
            graph.TryGetEntity(target.Id, out _));

        Assert.False(
            graph.TryGetRelationship(relationship.Id, out _));
    }

    [Fact]
    public void RemoveEntity_ShouldReturnFalseForUnknownEntity()
    {
        var graph = new OrbGraph();

        var removed = graph.RemoveEntity(Guid.NewGuid());

        Assert.False(removed);
    }
}