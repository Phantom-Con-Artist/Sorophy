using Sorophy.Engine.Graph;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyGraphTests
{
    [Fact]
    public void AddEntity_ShouldAddEntityToGraph()
    {
        var graph = new SorophyGraph();
        var entity = new SorophyEntity { Name = "Avaria" };

        graph.AddEntity(entity);

        Assert.True(graph.TryGetEntity(entity.Id, out var result));
        Assert.Same(entity, result);
    }

    [Fact]
    public void AddEntity_ShouldRejectDuplicateId()
    {
        var graph = new SorophyGraph();

        var entity1 = new SorophyEntity { Name = "Avaria" };
        var entity2 = new SorophyEntity
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        var target = new SorophyEntity { Name = "Velaris" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var target = new SorophyEntity { Name = "Velaris" };
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        graph.AddEntity(source);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var entity = new SorophyEntity { Name = "Self Referencing Entity" };
        graph.AddEntity(entity);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        var target = new SorophyEntity { Name = "Dravak" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship1 = new SorophyRelationship
        {
            Type = "borders",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var relationship2 = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity();
        var target = new SorophyEntity();

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        var target = new SorophyEntity { Name = "Velaris" };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var removed = graph.RemoveEntity(Guid.NewGuid());

        Assert.False(removed);
    }
}