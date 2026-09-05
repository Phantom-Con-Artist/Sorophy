/*
 * Sorophy Engine — a structured knowledge and graph engine
 * Copyright (C) 2026  Subhradeep Sarkar
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with the Sorophyis Project. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.History;

public sealed class SorophyRelationshipFactTests
{
    /*
     * =============================================================
     * HELPERS
     * =============================================================
     */

    private static SorophyTimePositionDefinition NumericPosition =>
        new(
            SorophyTimePositionKind.Numeric);

    private static SorophyTimeSchema CreateSchema(
        string timeline = "Test Timeline")
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
        string position = "100")
    {
        return new SorophyTime(
            schema,
            position,
            "Year",
            SorophyTimePrecision.Exact);
    }

    private static SorophyProperty CreateProperty(
        string name,
        string value)
    {
        return new SorophyProperty
        {
            Name =
                name,

            Value =
                new SorophyValue(
                    SorophyValueType.String,
                    value)
        };
    }

    /*
     * =============================================================
     * CONSTRUCTOR
     * =============================================================
     */

    [Fact]
    public void Constructor_ShouldStoreAllProperties()
    {
        var schema =
            CreateSchema();

        var at =
            CreateTime(
                schema,
                "150");

        var validFrom =
            CreateTime(
                schema,
                "100");

        var validTill =
            CreateTime(
                schema,
                "200");

        var relationshipId =
            Guid.NewGuid();

        var sourceId =
            Guid.NewGuid();

        var targetId =
            Guid.NewGuid();

        var property =
            CreateProperty(
                "status",
                "active");

        var properties =
            new Dictionary<string, SorophyProperty>
            {
                ["status"] =
                    property
            };

        var fact =
            new SorophyRelationshipFact(
                at,
                relationshipId,
                sourceId,
                targetId,
                "member_of",
                properties,
                validFrom,
                validTill);

        Assert.Same(
            at,
            fact.At);

        Assert.Equal(
            relationshipId,
            fact.RelationshipId);

        Assert.Equal(
            sourceId,
            fact.SourceId);

        Assert.Equal(
            targetId,
            fact.TargetId);

        Assert.Equal(
            "member_of",
            fact.Type);

        Assert.Same(
            property,
            fact.Properties["status"]);

        Assert.Same(
            validFrom,
            fact.ValidFrom);

        Assert.Same(
            validTill,
            fact.ValidTill);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingValidityBounds()
    {
        var schema =
            CreateSchema();

        var at =
            CreateTime(
                schema,
                "150");

        var fact =
            new SorophyRelationshipFact(
                at,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "knows");

        Assert.Same(
            at,
            fact.At);

        Assert.Null(
            fact.ValidFrom);

        Assert.Null(
            fact.ValidTill);

        Assert.Empty(
            fact.Properties);
    }

    /*
     * =============================================================
     * ID VALIDATION
     * =============================================================
     */

    [Fact]
    public void Constructor_ShouldRejectEmptyRelationshipId()
    {
        var schema =
            CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "knows"));
    }

    [Fact]
    public void Constructor_ShouldRejectEmptySourceId()
    {
        var schema =
            CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                "knows"));
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyTargetId()
    {
        var schema =
            CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                "knows"));
    }

    /*
     * =============================================================
     * TYPE VALIDATION
     * =============================================================
     */

    [Fact]
    public void Constructor_ShouldRejectBlankType()
    {
        var schema =
            CreateSchema();

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                ""));

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "   "));
    }

    /*
     * =============================================================
     * TEMPORAL VALIDATION
     * =============================================================
     */

    [Fact]
    public void Constructor_ShouldAcceptMatchingValiditySchema()
    {
        var schema =
            CreateSchema();

        var validFrom =
            CreateTime(
                schema,
                "100");

        var validTill =
            CreateTime(
                schema,
                "200");

        var fact =
            new SorophyRelationshipFact(
                CreateTime(
                    schema,
                    "150"),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "member_of",
                null,
                validFrom,
                validTill);

        Assert.Same(
            validFrom,
            fact.ValidFrom);

        Assert.Same(
            validTill,
            fact.ValidTill);
    }

    [Fact]
    public void Constructor_ShouldRejectMismatchedValiditySchemas()
    {
        var firstSchema =
            CreateSchema(
                "Timeline A");

        var secondSchema =
            CreateSchema(
                "Timeline B");

        var validFrom =
            CreateTime(
                firstSchema,
                "100");

        var validTill =
            CreateTime(
                secondSchema,
                "200");

        Assert.Throws<ArgumentException>(() =>
            new SorophyRelationshipFact(
                CreateTime(
                    firstSchema,
                    "150"),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "member_of",
                null,
                validFrom,
                validTill));
    }

    /*
     * =============================================================
     * PROPERTY COPYING
     * =============================================================
     */

    [Fact]
    public void Constructor_ShouldCopyPropertyDictionary()
    {
        var schema =
            CreateSchema();

        var property =
            CreateProperty(
                "status",
                "active");

        var properties =
            new Dictionary<string, SorophyProperty>
            {
                ["status"] =
                    property
            };

        var fact =
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "knows",
                properties);

        properties.Clear();

        Assert.Single(
            fact.Properties);

        Assert.True(
            fact.Properties.ContainsKey(
                "status"));

        Assert.Same(
            property,
            fact.Properties["status"]);
    }

    [Fact]
    public void Constructor_ShouldAllowEmptyProperties()
    {
        var schema =
            CreateSchema();

        var fact =
            new SorophyRelationshipFact(
                CreateTime(schema),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "knows",
                new Dictionary<string, SorophyProperty>());

        Assert.Empty(
            fact.Properties);
    }

    /*
     * =============================================================
     * IMMUTABILITY
     * =============================================================
     */

    [Fact]
    public void Properties_ShouldNotExposePublicSetters()
    {
        var type =
            typeof(SorophyRelationshipFact);

        var atProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.At));

        var relationshipIdProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.RelationshipId));

        var sourceIdProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.SourceId));

        var targetIdProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.TargetId));

        var typeProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.Type));

        var propertiesProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.Properties));

        var validFromProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.ValidFrom));

        var validTillProperty =
            type.GetProperty(
                nameof(
                    SorophyRelationshipFact.ValidTill));

        Assert.NotNull(
            atProperty);

        Assert.NotNull(
            relationshipIdProperty);

        Assert.NotNull(
            sourceIdProperty);

        Assert.NotNull(
            targetIdProperty);

        Assert.NotNull(
            typeProperty);

        Assert.NotNull(
            propertiesProperty);

        Assert.NotNull(
            validFromProperty);

        Assert.NotNull(
            validTillProperty);

        Assert.Null(
            atProperty!.SetMethod);

        Assert.Null(
            relationshipIdProperty!.SetMethod);

        Assert.Null(
            sourceIdProperty!.SetMethod);

        Assert.Null(
            targetIdProperty!.SetMethod);

        Assert.Null(
            typeProperty!.SetMethod);

        Assert.Null(
            propertiesProperty!.SetMethod);

        Assert.Null(
            validFromProperty!.SetMethod);

        Assert.Null(
            validTillProperty!.SetMethod);
    }
}