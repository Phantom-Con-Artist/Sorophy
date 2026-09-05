using Sorophy.Engine.Graph;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyGraphApiTests
{
    [Fact]
    public void ContainsEntity_ShouldReturnTrueForExistingEntity()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Avaria"
        };

        graph.AddEntity(entity);

        Assert.True(graph.ContainsEntity(entity.Id));
    }

    [Fact]
    public void ContainsEntity_ShouldReturnFalseForMissingEntity()
    {
        var graph = new SorophyGraph();

        Assert.False(graph.ContainsEntity(Guid.NewGuid()));
    }

    [Fact]
    public void ContainsRelationship_ShouldReturnTrueForExistingRelationship()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "A"
        };

        var target = new SorophyEntity
        {
            Name = "B"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship);

        Assert.True(
            graph.ContainsRelationship(relationship.Id));
    }

    [Fact]
    public void ContainsRelationship_ShouldReturnFalseForMissingRelationship()
    {
        var graph = new SorophyGraph();

        Assert.False(
            graph.ContainsRelationship(Guid.NewGuid()));
    }

    [Fact]
    public void GetNeighbors_ShouldReturnOutgoingNeighbors()
    {
        var graph = new SorophyGraph();

        var a = new SorophyEntity { Name = "A" };
        var b = new SorophyEntity { Name = "B" };
        var c = new SorophyEntity { Name = "C" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = c.Id
        });

        var neighbors = graph
            .GetNeighbors(a.Id)
            .ToList();

        Assert.Equal(2, neighbors.Count);
        Assert.Contains(b, neighbors);
        Assert.Contains(c, neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldReturnIncomingNeighbors()
    {
        var graph = new SorophyGraph();

        var a = new SorophyEntity { Name = "A" };
        var b = new SorophyEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "connects",
            SourceId = b.Id,
            TargetId = a.Id
        });

        var neighbors = graph
            .GetNeighbors(a.Id)
            .ToList();

        Assert.Single(neighbors);
        Assert.Contains(b, neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldCombineIncomingAndOutgoingNeighbors()
    {
        var graph = new SorophyGraph();

        var a = new SorophyEntity { Name = "A" };
        var b = new SorophyEntity { Name = "B" };
        var c = new SorophyEntity { Name = "C" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "connects",
            SourceId = c.Id,
            TargetId = a.Id
        });

        var neighbors = graph
            .GetNeighbors(a.Id)
            .ToList();

        Assert.Equal(2, neighbors.Count);
        Assert.Contains(b, neighbors);
        Assert.Contains(c, neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldReturnEachNeighborOnlyOnce()
    {
        var graph = new SorophyGraph();

        var a = new SorophyEntity { Name = "A" };
        var b = new SorophyEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "first",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "second",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "third",
            SourceId = b.Id,
            TargetId = a.Id
        });

        var neighbors = graph
            .GetNeighbors(a.Id)
            .ToList();

        Assert.Single(neighbors);
        Assert.Contains(b, neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldExcludeSelfRelationship()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Self"
        };

        graph.AddEntity(entity);

        graph.AddRelationship(new SorophyRelationship
        {
            Type = "references",
            SourceId = entity.Id,
            TargetId = entity.Id
        });

        var neighbors = graph
            .GetNeighbors(entity.Id)
            .ToList();

        Assert.Empty(neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldReturnEmptyForIsolatedEntity()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Isolated"
        };

        graph.AddEntity(entity);

        var neighbors = graph
            .GetNeighbors(entity.Id)
            .ToList();

        Assert.Empty(neighbors);
    }

    [Fact]
    public void GetNeighbors_ShouldReturnEmptyForUnknownEntity()
    {
        var graph = new SorophyGraph();

        var neighbors = graph
            .GetNeighbors(Guid.NewGuid())
            .ToList();

        Assert.Empty(neighbors);
    }
}