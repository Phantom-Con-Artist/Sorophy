using System;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;
using Xunit;

namespace Sorophy.Engine.Tests.Graph;
using System;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

public sealed class SorophyRelationshipTests
{
    private static SorophyTimePositionDefinition NumericPosition =>
        new(SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(
        string timeline = "Era of Black Pig")
    {
        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    NumericPosition)
            });
    }

    private static SorophyTime CreateTime(
        SorophyTimeSchema schema,
        string position)
    {
        return new SorophyTime(
            schema,
            position,
            "Year",
            SorophyTimePrecision.Exact);
    }

    // -------------------------------------------------------------------------
    // Core relationship state
    // -------------------------------------------------------------------------

    [Fact]
    public void Relationship_ShouldHaveUniqueIdByDefault()
    {
        var relationship1 = new SorophyRelationship();
        var relationship2 = new SorophyRelationship();

        Assert.NotEqual(
            relationship1.Id,
            relationship2.Id);
    }

    [Fact]
    public void Relationship_ShouldStoreTypeSourceAndTarget()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            SourceId = sourceId,
            TargetId = targetId
        };

        Assert.Equal(
            "married_to",
            relationship.Type);

        Assert.Equal(
            sourceId,
            relationship.SourceId);

        Assert.Equal(
            targetId,
            relationship.TargetId);
    }

    [Fact]
    public void Relationship_ShouldAllowSelfReference()
    {
        var entityId = Guid.NewGuid();

        var relationship = new SorophyRelationship
        {
            Type = "references",
            SourceId = entityId,
            TargetId = entityId
        };

        Assert.Equal(
            entityId,
            relationship.SourceId);

        Assert.Equal(
            entityId,
            relationship.TargetId);
    }

    [Fact]
    public void Relationship_ShouldAllowParallelRelationshipSemantics()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var married = new SorophyRelationship
        {
            Type = "married_to",
            SourceId = sourceId,
            TargetId = targetId
        };

        var trusts = new SorophyRelationship
        {
            Type = "trusts",
            SourceId = sourceId,
            TargetId = targetId
        };

        Assert.NotEqual(
            married.Id,
            trusts.Id);

        Assert.Equal(
            sourceId,
            married.SourceId);

        Assert.Equal(
            sourceId,
            trusts.SourceId);

        Assert.Equal(
            targetId,
            married.TargetId);

        Assert.Equal(
            targetId,
            trusts.TargetId);
    }

    [Fact]
    public void Relationship_ShouldAllowCustomProperties()
    {
        var relationship = new SorophyRelationship
        {
            Type = "rules"
        };

        relationship.Properties["title"] = new SorophyProperty
        {
            Name = "title",
            Value = new SorophyValue(
                SorophyValueType.String,
                "Sovereign")
        };

        Assert.Single(relationship.Properties);

        Assert.Equal(
            "Sovereign",
            relationship.Properties["title"].Value.Value);
    }

    // -------------------------------------------------------------------------
    // Temporal validity
    // -------------------------------------------------------------------------

    [Fact]
    public void Relationship_ShouldAllowNoTemporalBounds()
    {
        var relationship = new SorophyRelationship
        {
            Type = "knows"
        };

        Assert.Null(relationship.ValidFrom);
        Assert.Null(relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowValidFromOnly()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "100");

        var relationship = new SorophyRelationship
        {
            Type = "rules",
            ValidFrom = validFrom
        };

        Assert.Same(
            validFrom,
            relationship.ValidFrom);

        Assert.Null(
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowValidTillOnly()
    {
        var schema = CreateSchema();

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            Type = "rules",
            ValidTill = validTill
        };

        Assert.Null(
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowBothTemporalBoundsFromSameSchema()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "100");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            ValidFrom = validFrom,
            ValidTill = validTill
        };

        Assert.Same(
            validFrom,
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowEqualTemporalBounds()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "110");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            ValidFrom = validFrom,
            ValidTill = validTill
        };

        Assert.Same(
            validFrom,
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldRejectDifferentTemporalSchemas()
    {
        var firstSchema =
            CreateSchema("Era of Black Pig");

        var secondSchema =
            CreateSchema("Era of Doom");

        var validFrom = CreateTime(
            firstSchema,
            "100");

        var validTill = CreateTime(
            secondSchema,
            "110");

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationship
            {
                Type = "married_to",
                ValidFrom = validFrom,
                ValidTill = validTill
            });
    }

    [Fact]
    public void Relationship_ShouldRejectDifferentSchemasWhenSettingValidTill()
    {
        var firstSchema =
            CreateSchema("Era of Black Pig");

        var secondSchema =
            CreateSchema("Era of Doom");

        var validFrom = CreateTime(
            firstSchema,
            "100");

        var validTill = CreateTime(
            secondSchema,
            "110");

        var relationship = new SorophyRelationship
        {
            Type = "married_to"
        };

        relationship.ValidFrom = validFrom;

        Assert.Throws<ArgumentException>(() =>
            relationship.ValidTill = validTill);

        Assert.Null(
            relationship.ValidTill);

        Assert.Same(
            validFrom,
            relationship.ValidFrom);
    }

    [Fact]
    public void Relationship_ShouldRejectDifferentSchemasWhenSettingValidFrom()
    {
        var firstSchema =
            CreateSchema("Era of Black Pig");

        var secondSchema =
            CreateSchema("Era of Doom");

        var validFrom = CreateTime(
            firstSchema,
            "100");

        var validTill = CreateTime(
            secondSchema,
            "110");

        var relationship = new SorophyRelationship
        {
            Type = "married_to"
        };

        relationship.ValidTill = validTill;

        Assert.Throws<ArgumentException>(() =>
            relationship.ValidFrom = validFrom);

        Assert.Null(
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowClearingValidFrom()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "100");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            ValidFrom = validFrom,
            ValidTill = validTill
        };

        relationship.ValidFrom = null;

        Assert.Null(
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowClearingValidTill()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "100");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            ValidFrom = validFrom,
            ValidTill = validTill
        };

        relationship.ValidTill = null;

        Assert.Same(
            validFrom,
            relationship.ValidFrom);

        Assert.Null(
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowReplacingTemporalBoundaryWithinSameSchema()
    {
        var schema = CreateSchema();

        var firstFrom = CreateTime(
            schema,
            "100");

        var secondFrom = CreateTime(
            schema,
            "105");

        var validTill = CreateTime(
            schema,
            "110");

        var relationship = new SorophyRelationship
        {
            ValidFrom = firstFrom,
            ValidTill = validTill
        };

        relationship.ValidFrom = secondFrom;

        Assert.Same(
            secondFrom,
            relationship.ValidFrom);

        Assert.Same(
            validTill,
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldAllowReplacingValidTillWithinSameSchema()
    {
        var schema = CreateSchema();

        var validFrom = CreateTime(
            schema,
            "100");

        var firstTill = CreateTime(
            schema,
            "110");

        var secondTill = CreateTime(
            schema,
            "120");

        var relationship = new SorophyRelationship
        {
            ValidFrom = validFrom,
            ValidTill = firstTill
        };

        relationship.ValidTill = secondTill;

        Assert.Same(
            validFrom,
            relationship.ValidFrom);

        Assert.Same(
            secondTill,
            relationship.ValidTill);
    }

    // -------------------------------------------------------------------------
    // Temporal + relationship semantics
    // -------------------------------------------------------------------------

    [Fact]
    public void Relationship_ShouldRepresentOpenEndedValidity()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "divorced_from",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "110")
        };

        Assert.Equal(
            "divorced_from",
            relationship.Type);

        Assert.NotEqual(
            Guid.Empty,
            relationship.SourceId);

        Assert.NotEqual(
            Guid.Empty,
            relationship.TargetId);

        Assert.Equal(
            "110",
            relationship.ValidFrom!.Position);

        Assert.Null(
            relationship.ValidTill);
    }

    [Fact]
    public void Relationship_ShouldRepresentClosedValidityInterval()
    {
        var schema = CreateSchema();

        var relationship = new SorophyRelationship
        {
            Type = "married_to",
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ValidFrom = CreateTime(
                schema,
                "100"),
            ValidTill = CreateTime(
                schema,
                "110")
        };

        Assert.Equal(
            "100",
            relationship.ValidFrom!.Position);

        Assert.Equal(
            "110",
            relationship.ValidTill!.Position);

        Assert.Same(
            relationship.ValidFrom.Schema,
            relationship.ValidTill.Schema);
    }
}