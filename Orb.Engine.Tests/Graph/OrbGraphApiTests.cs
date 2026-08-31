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

        Assert.True(graph.ContainsEntity(entity.Id));
    }

    [Fact]
    public void ContainsEntity_ShouldReturnFalseForMissingEntity()
    {
        var graph = new OrbGraph();

        Assert.False(graph.ContainsEntity(Guid.NewGuid()));
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
            graph.ContainsRelationship(relationship.Id));
    }

    [Fact]
    public void ContainsRelationship_ShouldReturnFalseForMissingRelationship()
    {
        var graph = new OrbGraph();

        Assert.False(
            graph.ContainsRelationship(Guid.NewGuid()));
    }

    [Fact]
    public void GetNeighbors_ShouldReturnOutgoingNeighbors()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };
        var c = new OrbEntity { Name = "C" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
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
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
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
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };
        var c = new OrbEntity { Name = "C" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
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
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "first",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "second",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
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
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Self"
        };

        graph.AddEntity(entity);

        graph.AddRelationship(new OrbRelationship
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
        var graph = new OrbGraph();

        var entity = new OrbEntity
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
        var graph = new OrbGraph();

        var neighbors = graph
            .GetNeighbors(Guid.NewGuid())
            .ToList();

        Assert.Empty(neighbors);
    }
}