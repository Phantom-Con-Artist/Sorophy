using Orb.Engine.Graph;

namespace Orb.Engine.Tests.Graph;

public class OrbGraphTraversalTests
{
    [Fact]
    public void IsReachable_ShouldReturnTrueForDirectRelationship()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        Assert.True(graph.IsReachable(a.Id, b.Id));
    }

    [Fact]
    public void IsReachable_ShouldReturnTrueForMultiHopPath()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };
        var c = new OrbEntity { Name = "C" };
        var d = new OrbEntity { Name = "D" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);
        graph.AddEntity(d);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = b.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = c.Id,
            TargetId = d.Id
        });

        Assert.True(graph.IsReachable(a.Id, d.Id));
    }

    [Fact]
    public void IsReachable_ShouldRespectRelationshipDirection()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "points_to",
            SourceId = a.Id,
            TargetId = b.Id
        });

        Assert.True(graph.IsReachable(a.Id, b.Id));
        Assert.False(graph.IsReachable(b.Id, a.Id));
    }

    [Fact]
    public void IsReachable_ShouldReturnFalseWhenNoPathExists()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        Assert.False(graph.IsReachable(a.Id, b.Id));
    }

    [Fact]
    public void IsReachable_ShouldHandleCycles()
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
            Type = "next",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = b.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = c.Id,
            TargetId = a.Id
        });

        Assert.True(graph.IsReachable(a.Id, c.Id));
        Assert.True(graph.IsReachable(c.Id, b.Id));
    }

    [Fact]
    public void IsReachable_ShouldReturnTrueForSameEntity()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        Assert.True(
            graph.IsReachable(
                entity.Id,
                entity.Id));
    }

    [Fact]
    public void IsReachable_ShouldReturnFalseForUnknownSource()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        Assert.False(
            graph.IsReachable(
                Guid.NewGuid(),
                entity.Id));
    }

    [Fact]
    public void IsReachable_ShouldReturnFalseForUnknownTarget()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        Assert.False(
            graph.IsReachable(
                entity.Id,
                Guid.NewGuid()));
    }

    [Fact]
    public void IsReachable_ShouldReturnFalseForIsolatedEntity()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        Assert.False(
            graph.IsReachable(
                a.Id,
                b.Id));
    }

    [Fact]
    public void IsReachable_ShouldNotGetStuckInCycle()
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
            Type = "cycle",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "cycle",
            SourceId = b.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "cycle",
            SourceId = c.Id,
            TargetId = a.Id
        });

        var result = graph.IsReachable(
            a.Id,
            Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void Traverse_ShouldReturnDirectNeighbor()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        var result = graph
            .Traverse(a.Id)
            .ToList();

        Assert.Single(result);
        Assert.Equal(b.Id, result[0].Id);
    }

    [Fact]
    public void Traverse_ShouldReturnAllReachableEntities()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };
        var c = new OrbEntity { Name = "C" };
        var d = new OrbEntity { Name = "D" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);
        graph.AddEntity(d);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = b.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = c.Id,
            TargetId = d.Id
        });

        var result = graph
            .Traverse(a.Id)
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(b, result);
        Assert.Contains(c, result);
        Assert.Contains(d, result);
    }

    [Fact]
    public void Traverse_ShouldRespectDirection()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "points_to",
            SourceId = a.Id,
            TargetId = b.Id
        });

        var fromA = graph
            .Traverse(a.Id)
            .ToList();

        var fromB = graph
            .Traverse(b.Id)
            .ToList();

        Assert.Single(fromA);
        Assert.Equal(b.Id, fromA[0].Id);
        Assert.Empty(fromB);
    }

    [Fact]
    public void Traverse_ShouldExcludeSourceEntity()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        var result = graph
            .Traverse(entity.Id)
            .ToList();

        Assert.DoesNotContain(
            entity,
            result);
    }

    [Fact]
    public void Traverse_ShouldHandleCycles()
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
            Type = "cycle",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "cycle",
            SourceId = b.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "cycle",
            SourceId = c.Id,
            TargetId = a.Id
        });

        var result = graph
            .Traverse(a.Id)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(b, result);
        Assert.Contains(c, result);
    }

    [Fact]
    public void Traverse_ShouldReturnEachEntityOnlyOnce()
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
            Type = "first",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "second",
            SourceId = a.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "third",
            SourceId = b.Id,
            TargetId = c.Id
        });

        var result = graph
            .Traverse(a.Id)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Single(
            result.Where(entity => entity.Id == c.Id));
    }

    [Fact]
    public void Traverse_ShouldIgnoreSelfRelationship()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "references",
            SourceId = entity.Id,
            TargetId = entity.Id
        });

        var result = graph
            .Traverse(entity.Id)
            .ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Traverse_ShouldReturnEmptyForIsolatedEntity()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        var result = graph
            .Traverse(entity.Id)
            .ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Traverse_ShouldReturnEmptyForUnknownSource()
    {
        var graph = new OrbGraph();

        var result = graph
            .Traverse(Guid.NewGuid())
            .ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Traverse_ShouldFollowBreadthFirstOrder()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity { Name = "A" };
        var b = new OrbEntity { Name = "B" };
        var c = new OrbEntity { Name = "C" };
        var d = new OrbEntity { Name = "D" };

        graph.AddEntity(a);
        graph.AddEntity(b);
        graph.AddEntity(c);
        graph.AddEntity(d);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = a.Id,
            TargetId = c.Id
        });

        graph.AddRelationship(new OrbRelationship
        {
            Type = "next",
            SourceId = b.Id,
            TargetId = d.Id
        });

        var result = graph
            .Traverse(a.Id)
            .ToList();

        Assert.Equal(3, result.Count);

        Assert.Equal(b.Id, result[0].Id);
        Assert.Equal(c.Id, result[1].Id);
        Assert.Equal(d.Id, result[2].Id);
    }
}