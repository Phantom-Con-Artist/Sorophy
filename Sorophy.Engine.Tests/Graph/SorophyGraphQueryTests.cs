using Sorophy.Engine.Graph;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyGraphQueryTests
{
    [Fact]
    public void GetOutgoingRelationships_ShouldReturnOutgoingRelationships()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        var target1 = new SorophyEntity { Name = "Valor" };
        var target2 = new SorophyEntity { Name = "Dravak" };

        graph.AddEntity(source);
        graph.AddEntity(target1);
        graph.AddEntity(target2);

        var relationship1 = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target1.Id
        };

        var relationship2 = new SorophyRelationship
        {
            Type = "borders",
            SourceId = source.Id,
            TargetId = target2.Id
        };

        graph.AddRelationship(relationship1);
        graph.AddRelationship(relationship2);

        var result = graph
            .GetOutgoingRelationships(source.Id)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(relationship1, result);
        Assert.Contains(relationship2, result);
    }

    [Fact]
    public void GetIncomingRelationships_ShouldReturnIncomingRelationships()
    {
        var graph = new SorophyGraph();

        var source1 = new SorophyEntity { Name = "Avaria" };
        var source2 = new SorophyEntity { Name = "Dravak" };
        var target = new SorophyEntity { Name = "Valor" };

        graph.AddEntity(source1);
        graph.AddEntity(source2);
        graph.AddEntity(target);

        var relationship1 = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source1.Id,
            TargetId = target.Id
        };

        var relationship2 = new SorophyRelationship
        {
            Type = "trades_with",
            SourceId = source2.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(relationship1);
        graph.AddRelationship(relationship2);

        var result = graph
            .GetIncomingRelationships(target.Id)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(relationship1, result);
        Assert.Contains(relationship2, result);
    }

    [Fact]
    public void GetRelationships_ShouldReturnIncomingAndOutgoingRelationships()
    {
        var graph = new SorophyGraph();

        var source = new SorophyEntity { Name = "Avaria" };
        var target = new SorophyEntity { Name = "Valor" };
        var other = new SorophyEntity { Name = "Dravak" };

        graph.AddEntity(source);
        graph.AddEntity(target);
        graph.AddEntity(other);

        var outgoing = new SorophyRelationship
        {
            Type = "capital_of",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var incoming = new SorophyRelationship
        {
            Type = "allied_with",
            SourceId = other.Id,
            TargetId = source.Id
        };

        graph.AddRelationship(outgoing);
        graph.AddRelationship(incoming);

        var result = graph
            .GetRelationships(source.Id)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(outgoing, result);
        Assert.Contains(incoming, result);
    }

    [Fact]
    public void SelfLink_ShouldAppearInBothIncomingAndOutgoingRelationships()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Self Referencing Entity"
        };

        graph.AddEntity(entity);

        var relationship = new SorophyRelationship
        {
            Type = "references_itself",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        var outgoing = graph
            .GetOutgoingRelationships(entity.Id)
            .ToList();

        var incoming = graph
            .GetIncomingRelationships(entity.Id)
            .ToList();

        Assert.Single(outgoing);
        Assert.Single(incoming);

        Assert.Same(relationship, outgoing[0]);
        Assert.Same(relationship, incoming[0]);
    }

    [Fact]
    public void GetRelationships_ShouldReturnEmptyForEntityWithNoRelationships()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "Isolated Entity"
        };

        graph.AddEntity(entity);

        Assert.Empty(
            graph.GetOutgoingRelationships(entity.Id));

        Assert.Empty(
            graph.GetIncomingRelationships(entity.Id));

        Assert.Empty(
            graph.GetRelationships(entity.Id));
    }
}