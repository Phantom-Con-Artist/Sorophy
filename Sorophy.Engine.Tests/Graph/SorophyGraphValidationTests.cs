using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.Tests.Graph;

public class SorophyGraphValidationTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrorsForEmptyGraph()
    {
        var graph = new SorophyGraph();

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidEntity()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Source"
        };

        var target = new SorophyEntity
        {
            Name = "Target"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        graph.AddRelationship(new SorophyRelationship
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

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldRemainValidAfterRemovingEntity()
    {
        var graph = new SorophyGraph();

        var a = new SorophyEntity
        {
            Name = "A"
        };

        var b = new SorophyEntity
        {
            Name = "B"
        };

        graph.AddEntity(a);
        graph.AddEntity(b);

        graph.AddRelationship(new SorophyRelationship
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
        var graph = new SorophyGraph();

        var a = new SorophyEntity
        {
            Name = "A"
        };

        var b = new SorophyEntity
        {
            Name = "B"
        };

        graph.AddEntity(a);
        graph.AddEntity(b);

        var relationship = new SorophyRelationship
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
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "A"
        };

        graph.AddEntity(entity);

        Assert.False(
            graph.Entities is Dictionary<Guid, SorophyEntity>);
    }

    [Fact]
    public void Relationships_ShouldNotExposeMutableDictionary()
    {
        var graph = new SorophyGraph();

        Assert.False(
            graph.Relationships is Dictionary<Guid, SorophyRelationship>);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrorsForValidEntityProperty()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "population",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                2400000L)
        };

        graph.AddEntity(entity);

        var errors = graph.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldDetectEntityPropertyNameMismatch()
    {
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = "banana",
            Value = new SorophyValue(
                SorophyValueType.Integer,
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
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "A"
        };

        entity.Properties["population"] = new SorophyProperty
        {
            Name = " ",
            Value = new SorophyValue(
                SorophyValueType.Integer,
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
        var graph = new SorophyGraph();

        var source = new SorophyEntity
        {
            Name = "Source"
        };

        var target = new SorophyEntity
        {
            Name = "Target"
        };

        graph.AddEntity(source);
        graph.AddEntity(target);

        var relationship = new SorophyRelationship
        {
            Type = "connects",
            SourceId = source.Id,
            TargetId = target.Id
        };

        relationship.Properties["strength"] = new SorophyProperty
        {
            Name = "wrong_name",
            Value = new SorophyValue(
                SorophyValueType.Decimal,
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
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
        {
            Name = "A"
        };

        entity.Properties[""] = new SorophyProperty
        {
            Name = "valid_name",
            Value = new SorophyValue(
                SorophyValueType.String,
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
        var graph = new SorophyGraph();

        var entity = new SorophyEntity
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
