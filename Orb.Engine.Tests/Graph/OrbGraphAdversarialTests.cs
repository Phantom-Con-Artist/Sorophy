using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Graph;

public class OrbGraphAdversarialTests
{
    [Fact]
    public void AddEntity_ShouldRejectDuplicateId()
    {
        var graph = new OrbGraph();

        var id = Guid.NewGuid();

        var first = new OrbEntity
        {
            Id = id,
            Name = "First"
        };

        var second = new OrbEntity
        {
            Id = id,
            Name = "Second"
        };

        graph.AddEntity(first);

        Assert.Throws<InvalidOperationException>(() =>
            graph.AddEntity(second));
    }

    [Fact]
    public void AddRelationship_ShouldRejectMissingSource()
    {
        var graph = new OrbGraph();

        var target = new OrbEntity
        {
            Name = "Target"
        };

        graph.AddEntity(target);

        var relationship = new OrbRelationship
        {
            Type = "connects_to",
            SourceId = Guid.NewGuid(),
            TargetId = target.Id
        };

        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(relationship));
    }

    [Fact]
    public void AddRelationship_ShouldRejectMissingTarget()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "Source"
        };

        graph.AddEntity(source);

        var relationship = new OrbRelationship
        {
            Type = "connects_to",
            SourceId = source.Id,
            TargetId = Guid.NewGuid()
        };

        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(relationship));
    }

    [Fact]
    public void AddRelationship_ShouldRejectDuplicateId()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "Source"
        };

        var target = new OrbEntity
        {
            Name = "Target"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationshipId = Guid.NewGuid();

        var first = new OrbRelationship
        {
            Id = relationshipId,
            Type = "connects_to",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var second = new OrbRelationship
        {
            Id = relationshipId,
            Type = "different_type",
            SourceId = target.Id,
            TargetId = source.Id
        };

        graph.AddRelationship(first);

        Assert.Throws<InvalidOperationException>(() =>
            graph.AddRelationship(second));
    }

    [Fact]
    public void SelfRelationship_ShouldBeAllowed()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Self"
        };

        graph.AddEntity(entity);

        var relationship = new OrbRelationship
        {
            Type = "references",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        Assert.Single(graph.Relationships);
        Assert.Equal(
            entity.Id,
            relationship.SourceId);
        Assert.Equal(
            entity.Id,
            relationship.TargetId);
    }

    [Fact]
    public void MultipleIdenticalRelationships_ShouldBeAllowed()
    {
        var graph = new OrbGraph();

        var source = new OrbEntity
        {
            Name = "Source"
        };

        var target = new OrbEntity
        {
            Name = "Target"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var first = new OrbRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        var second = new OrbRelationship
        {
            Type = "knows",
            SourceId = source.Id,
            TargetId = target.Id
        };

        graph.AddRelationship(first);
        graph.AddRelationship(second);

        Assert.Equal(2, graph.Relationships.Count);
    }

    [Fact]
    public void ReverseRelationships_ShouldBeIndependent()
    {
        var graph = new OrbGraph();

        var first = new OrbEntity
        {
            Name = "A"
        };

        var second = new OrbEntity
        {
            Name = "B"
        };

        graph.AddEntity(first);
        graph.AddEntity(second);

        var forward = new OrbRelationship
        {
            Type = "knows",
            SourceId = first.Id,
            TargetId = second.Id
        };

        var reverse = new OrbRelationship
        {
            Type = "knows",
            SourceId = second.Id,
            TargetId = first.Id
        };

        graph.AddRelationship(forward);
        graph.AddRelationship(reverse);

        Assert.Equal(2, graph.Relationships.Count);

        Assert.Single(
            graph.GetOutgoingRelationships(first.Id));

        Assert.Single(
            graph.GetOutgoingRelationships(second.Id));
    }

    [Fact]
    public void IsolatedEntity_ShouldHaveNoRelationships()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Isolated"
        };

        graph.AddEntity(entity);

        Assert.Empty(
            graph.GetOutgoingRelationships(entity.Id));

        Assert.Empty(
            graph.GetIncomingRelationships(entity.Id));

        Assert.Empty(
            graph.GetRelationships(entity.Id));
    }

    [Fact]
    public void RemovingEntity_ShouldRemoveAllConnectedRelationships()
    {
        var graph = new OrbGraph();

        var first = new OrbEntity
        {
            Name = "First"
        };

        var second = new OrbEntity
        {
            Name = "Second"
        };

        var third = new OrbEntity
        {
            Name = "Third"
        };

        graph.AddEntity(first);
        graph.AddEntity(second);
        graph.AddEntity(third);

        var outgoing = new OrbRelationship
        {
            Type = "connects",
            SourceId = first.Id,
            TargetId = second.Id
        };

        var incoming = new OrbRelationship
        {
            Type = "connects",
            SourceId = third.Id,
            TargetId = first.Id
        };

        var unrelated = new OrbRelationship
        {
            Type = "connects",
            SourceId = second.Id,
            TargetId = third.Id
        };

        graph.AddRelationship(outgoing);
        graph.AddRelationship(incoming);
        graph.AddRelationship(unrelated);

        graph.RemoveEntity(first.Id);

        Assert.Empty(
            graph.Relationships.Values.Where(
                relationship =>
                    relationship.SourceId == first.Id ||
                    relationship.TargetId == first.Id));

        Assert.Single(graph.Relationships);
        Assert.Contains(
            unrelated,
            graph.Relationships.Values);
    }

    [Fact]
    public void RemovingNonexistentEntity_ShouldReturnFalse()
    {
        var graph = new OrbGraph();

        var result =
            graph.RemoveEntity(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void RemovingNonexistentRelationship_ShouldReturnFalse()
    {
        var graph = new OrbGraph();

        var result =
            graph.RemoveRelationship(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void EntityPropertyName_ShouldNotConflictWithEntityName()
    {
        var entity = new OrbEntity
        {
            Name = "Avaria"
        };

        entity.Properties["name"] = new OrbProperty
        {
            Name = "name",
            Value = new OrbValue(
                OrbValueType.String,
                "Different Value")
        };

        Assert.Equal("Avaria", entity.Name);

        Assert.Equal(
            "Different Value",
            entity.Properties["name"].Value.Value);
    }

    [Fact]
    public void SelfRelationship_ShouldBeReturnedAsIncomingAndOutgoing()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "Self"
        };

        graph.AddEntity(entity);

        var relationship = new OrbRelationship
        {
            Type = "references",
            SourceId = entity.Id,
            TargetId = entity.Id
        };

        graph.AddRelationship(relationship);

        Assert.Single(
            graph.GetOutgoingRelationships(entity.Id));

        Assert.Single(
            graph.GetIncomingRelationships(entity.Id));

        Assert.Single(
            graph.GetRelationships(entity.Id));
    }
}