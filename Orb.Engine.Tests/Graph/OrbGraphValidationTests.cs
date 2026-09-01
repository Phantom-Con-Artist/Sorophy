using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.Tests.Graph;

public class OrbGraphValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrorsForEmptyGraph()
    {
        var graph = new OrbGraph();

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidEntity()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidRelationship()
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

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = source.Id,
            TargetId = target.Id
        });

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldAllowSelfRelationship()
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

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldRemainValidAfterRemovingEntity()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity
        {
            Name = "A"
        };

        var b = new OrbEntity
        {
            Name = "B"
        };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        });

        graph.RemoveEntity(a.Id);

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldRemainValidAfterRemovingRelationship()
    {
        var graph = new OrbGraph();

        var a = new OrbEntity
        {
            Name = "A"
        };

        var b = new OrbEntity
        {
            Name = "B"
        };

        graph.AddEntity(a);
        graph.AddEntity(b);

        var relationship = new OrbRelationship
        {
            Type = "connects",
            SourceId = a.Id,
            TargetId = b.Id
        };

        graph.AddRelationship(relationship);

        graph.RemoveRelationship(relationship.Id);

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Entities_ShouldNotExposeMutableDictionary()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        Assert.False(
            graph.Entities is Dictionary<Guid, OrbEntity>);
    }

    [Fact]
    public void Relationships_ShouldNotExposeMutableDictionary()
    {
        var graph = new OrbGraph();

        Assert.False(
            graph.Relationships is Dictionary<Guid, OrbRelationship>);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidEntityProperty()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "population",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldDetectEntityPropertyNameMismatch()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = "banana",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "property dictionary key 'population'",
                    StringComparison.Ordinal) &&
                error.Contains(
                    "property name 'banana'",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectEntityEmptyPropertyName()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new OrbProperty
        {
            Name = " ",
            Value = new OrbValue(
                OrbValueType.Integer,
                2400000L)
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "has an empty name",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectRelationshipPropertyNameMismatch()
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

        var relationship = new OrbRelationship
        {
            Type = "connects",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["strength"] = new OrbProperty
        {
            Name = "wrong_name",
            Value = new OrbValue(
                OrbValueType.Decimal,
                42.5m)
        };

        graph.AddRelationship(relationship);

        var errors = graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "property dictionary key 'strength'",
                    StringComparison.Ordinal) &&
                error.Contains(
                    "property name 'wrong_name'",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectEmptyPropertyDictionaryKey()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        entity.Properties[""] = new OrbProperty
        {
            Name = "valid_name",
            Value = new OrbValue(
                OrbValueType.String,
                "value")
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "empty dictionary key",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldDetectNullEntityProperty()
    {
        var graph = new OrbGraph();

        var entity = new OrbEntity
        {
            Name = "A"
        };

        entity.Properties["broken"] = null!;

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Contains(
            errors,
            error =>
                error.Contains(
                    "property 'broken' is null",
                    StringComparison.Ordinal));
    }
}
