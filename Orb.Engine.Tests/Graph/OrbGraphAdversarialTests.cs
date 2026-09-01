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

    [Fact]
public void FailedDuplicateEntityInsertion_ShouldLeaveGraphUnchanged()
{
    var graph = new OrbGraph();

    var id = Guid.NewGuid();

    var first = new OrbEntity
    {
        Id = id,
        Name = "First"
    };

    var duplicate = new OrbEntity
    {
        Id = id,
        Name = "Duplicate"
    };

    graph.AddEntity(first);

    Assert.Throws<InvalidOperationException>(() =>
        graph.AddEntity(duplicate));

    Assert.Single(graph.Entities);
    Assert.Same(first, graph.Entities[id]);
    Assert.Equal("First", graph.Entities[id].Name);
    Assert.Empty(graph.Relationships);
    Assert.Empty(graph.Validate());
}

[Fact]
public void FailedRelationshipInsertion_ShouldLeaveGraphUnchanged()
{
    var graph = new OrbGraph();

    var source = new OrbEntity { Name = "Source" };
    var target = new OrbEntity { Name = "Target" };

    graph.AddEntity(source);
    graph.AddEntity(target);

    var valid = new OrbRelationship
    {
        Type = "valid",
        SourceId = source.Id,
        TargetId = target.Id
    };

    graph.AddRelationship(valid);

    var invalid = new OrbRelationship
    {
        Type = "invalid",
        SourceId = Guid.NewGuid(),
        TargetId = target.Id
    };

    Assert.Throws<InvalidOperationException>(() =>
        graph.AddRelationship(invalid));

    
    Assert.Equal(2, graph.Entities.Count);
    Assert.Single(graph.Relationships);
    Assert.Same(valid, graph.Relationships[valid.Id]);
    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingEntity_ShouldOnlyRemoveConnectedRelationships()
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

    var ab = new OrbRelationship
    {
        Type = "ab",
        SourceId = a.Id,
        TargetId = b.Id
    };

    var bc = new OrbRelationship
    {
        Type = "bc",
        SourceId = b.Id,
        TargetId = c.Id
    };

    var cd = new OrbRelationship
    {
        Type = "cd",
        SourceId = c.Id,
        TargetId = d.Id
    };

    graph.AddRelationship(ab);
    graph.AddRelationship(bc);
    graph.AddRelationship(cd);

    Assert.True(graph.RemoveEntity(b.Id));

    Assert.False(graph.ContainsEntity(b.Id));

    Assert.False(graph.ContainsRelationship(ab.Id));
    Assert.False(graph.ContainsRelationship(bc.Id));

    Assert.True(graph.ContainsEntity(a.Id));
    Assert.True(graph.ContainsEntity(c.Id));
    Assert.True(graph.ContainsEntity(d.Id));

    Assert.True(graph.ContainsRelationship(cd.Id));

    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingRelationship_ShouldNotRemoveEntities()
{
    var graph = new OrbGraph();

    var source = new OrbEntity { Name = "Source" };
    var target = new OrbEntity { Name = "Target" };

    graph.AddEntity(source);
    graph.AddEntity(target);

    var relationship = new OrbRelationship
    {
        Type = "connects",
        SourceId = source.Id,
        TargetId = target.Id
    };

    graph.AddRelationship(relationship);

    Assert.True(graph.RemoveRelationship(relationship.Id));

    Assert.True(graph.ContainsEntity(source.Id));
    Assert.True(graph.ContainsEntity(target.Id));

    Assert.False(graph.ContainsRelationship(relationship.Id));

    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingEntityTwice_ShouldNotChangeGraphAfterFirstRemoval()
{
    var graph = new OrbGraph();

    var entity = new OrbEntity
    {
        Name = "A"
    };

    graph.AddEntity(entity);

    Assert.True(graph.RemoveEntity(entity.Id));

    var entityCount = graph.Entities.Count;
    var relationshipCount = graph.Relationships.Count;

    Assert.False(graph.RemoveEntity(entity.Id));

    Assert.Equal(entityCount, graph.Entities.Count);
    Assert.Equal(relationshipCount, graph.Relationships.Count);
    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingRelationshipTwice_ShouldNotChangeGraphAfterFirstRemoval()
{
    var graph = new OrbGraph();

    var source = new OrbEntity { Name = "Source" };
    var target = new OrbEntity { Name = "Target" };

    graph.AddEntity(source);
    graph.AddEntity(target);

    var relationship = new OrbRelationship
    {
        Type = "connects",
        SourceId = source.Id,
        TargetId = target.Id
    };

    graph.AddRelationship(relationship);

    Assert.True(graph.RemoveRelationship(relationship.Id));

    var entityCount = graph.Entities.Count;
    var relationshipCount = graph.Relationships.Count;

    Assert.False(graph.RemoveRelationship(relationship.Id));

    Assert.Equal(entityCount, graph.Entities.Count);
    Assert.Equal(relationshipCount, graph.Relationships.Count);
    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingEntityWithSelfRelationship_ShouldRemoveSelfRelationship()
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

    Assert.True(graph.RemoveEntity(entity.Id));

    Assert.Empty(graph.Entities);
    Assert.Empty(graph.Relationships);
    Assert.Empty(graph.Validate());
}

[Fact]
public void RemovingEntity_ShouldRemoveAllConnectedRelationships_FromBothDirections()
{
    var graph = new OrbGraph();

    var center = new OrbEntity { Name = "Center" };
    var a = new OrbEntity { Name = "A" };
    var b = new OrbEntity { Name = "B" };
    var c = new OrbEntity { Name = "C" };

    graph.AddEntity(center);
    graph.AddEntity(a);
    graph.AddEntity(b);
    graph.AddEntity(c);

    var outgoingA = new OrbRelationship
    {
        Type = "outgoing",
        SourceId = center.Id,
        TargetId = a.Id
    };

    var outgoingB = new OrbRelationship
    {
        Type = "outgoing",
        SourceId = center.Id,
        TargetId = b.Id
    };

    var incomingC = new OrbRelationship
    {
        Type = "incoming",
        SourceId = c.Id,
        TargetId = center.Id
    };

    graph.AddRelationship(outgoingA);
    graph.AddRelationship(outgoingB);
    graph.AddRelationship(incomingC);

    Assert.True(graph.RemoveEntity(center.Id));

    Assert.Empty(graph.Relationships);

    Assert.True(graph.ContainsEntity(a.Id));
    Assert.True(graph.ContainsEntity(b.Id));
    Assert.True(graph.ContainsEntity(c.Id));

    Assert.Empty(graph.Validate());
}

[Fact]
public void AddRelationship_WithMissingSourceAndTarget_ShouldLeaveGraphUnchanged()
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

    var originalRelationshipCount = graph.Relationships.Count;

    var relationship = new OrbRelationship
    {
        Type = "invalid",
        SourceId = Guid.NewGuid(),
        TargetId = Guid.NewGuid()
    };

    Assert.Throws<InvalidOperationException>(() =>
        graph.AddRelationship(relationship));

    Assert.Equal(
        originalRelationshipCount,
        graph.Relationships.Count);

    Assert.Empty(graph.Validate());
}

[Fact]
public void AddNullEntity_ShouldLeaveGraphUnchanged()
{
    var graph = new OrbGraph();

    Assert.Throws<ArgumentNullException>(() =>
        graph.AddEntity(null!));

    Assert.Empty(graph.Entities);
    Assert.Empty(graph.Relationships);
    Assert.Empty(graph.Validate());
}

[Fact]
public void AddNullRelationship_ShouldLeaveGraphUnchanged()
{
    var graph = new OrbGraph();

    Assert.Throws<ArgumentNullException>(() =>
        graph.AddRelationship(null!));

    Assert.Empty(graph.Entities);
    Assert.Empty(graph.Relationships);
    Assert.Empty(graph.Validate());
}

[Fact]
public void FailedDuplicateRelationshipInsertion_ShouldPreserveOriginalRelationship()
{
    var graph = new OrbGraph();

    var source = new OrbEntity { Name = "Source" };
    var target = new OrbEntity { Name = "Target" };

    graph.AddEntity(source);
    graph.AddEntity(target);

    var id = Guid.NewGuid();

    var first = new OrbRelationship
    {
        Id = id,
        Type = "first",
        SourceId = source.Id,
        TargetId = target.Id
    };

    var duplicate = new OrbRelationship
    {
        Id = id,
        Type = "second",
        SourceId = target.Id,
        TargetId = source.Id
    };

    graph.AddRelationship(first);

    Assert.Throws<InvalidOperationException>(() =>
        graph.AddRelationship(duplicate));

    Assert.Single(graph.Relationships);
    Assert.Same(first, graph.Relationships[id]);
    Assert.Equal("first", graph.Relationships[id].Type);
    Assert.Equal(source.Id, graph.Relationships[id].SourceId);
    Assert.Equal(target.Id, graph.Relationships[id].TargetId);

    Assert.Empty(graph.Validate());
}

[Fact]
public void MutationSequence_ShouldPreserveGraphInvariants()
{
    var graph = new OrbGraph();

    var a = new OrbEntity { Name = "A" };
    var b = new OrbEntity { Name = "B" };
    var c = new OrbEntity { Name = "C" };

    graph.AddEntity(a);
    Assert.Empty(graph.Validate());

    graph.AddEntity(b);
    Assert.Empty(graph.Validate());

    var ab = new OrbRelationship
    {
        Type = "connects",
        SourceId = a.Id,
        TargetId = b.Id
    };

    graph.AddRelationship(ab);
    Assert.Empty(graph.Validate());

    graph.AddEntity(c);
    Assert.Empty(graph.Validate());

    var bc = new OrbRelationship
    {
        Type = "connects",
        SourceId = b.Id,
        TargetId = c.Id
    };

    graph.AddRelationship(bc);
    Assert.Empty(graph.Validate());

    Assert.True(graph.RemoveRelationship(ab.Id));
    Assert.Empty(graph.Validate());

    Assert.True(graph.RemoveEntity(b.Id));
    Assert.Empty(graph.Validate());

    Assert.True(graph.ContainsEntity(a.Id));
    Assert.True(graph.ContainsEntity(c.Id));

    Assert.Empty(graph.Relationships);
}
}