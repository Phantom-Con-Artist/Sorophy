using System;
using System.Collections.Generic;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Graph.Evolution;
using Sorophy.Engine.Graph.Evolution.Operations;
using Sorophy.Engine.Graph.History;
using Sorophy.Engine.Time;
using Sorophy.Engine.Types;
using Xunit;

namespace Sorophy.Engine.Tests.Graph.Evolution;

public sealed class SorophyRelationshipEvolutionExecutorTests
{
    // =============================================================
    // CREATION
    // =============================================================

    [Fact]
    public void Creation_CreatesRelationshipWithFullState()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "10");

        var relationshipId =
            Guid.NewGuid();

        var properties =
            new Dictionary<string, SorophyProperty>
            {
                ["Strength"] =
                    CreateIntegerProperty(
                        "Strength",
                        10),

                ["Status"] =
                    CreateStringProperty(
                        "Status",
                        "Active")
            };

        var evolution =
            new SorophyRelationshipCreation(
                relationshipId,
                sourceId,
                targetId,
                "Alliance",
                effectiveTime,
                properties,
                validFrom: effectiveTime);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var relationship));

        Assert.NotNull(
            relationship);

        Assert.Equal(
            relationshipId,
            relationship!.Id);

        Assert.Equal(
            sourceId,
            relationship.SourceId);

        Assert.Equal(
            targetId,
            relationship.TargetId);

        Assert.Equal(
            "Alliance",
            relationship.Type);

        Assert.Equal(
            2,
            relationship.Properties.Count);

        Assert.Equal(
            SorophyValueType.Integer,
            relationship.Properties["Strength"].Value.Type);

        Assert.Equal(
            10L,
            relationship.Properties["Strength"].Value.Value);

        Assert.Equal(
            SorophyValueType.String,
            relationship.Properties["Status"].Value.Type);

        Assert.Equal(
            "Active",
            relationship.Properties["Status"].Value.Value);

        Assert.Equal(
            effectiveTime,
            relationship.ValidFrom);

        Assert.Null(
            relationship.ValidTill);

        Assert.False(
            graph.IsRelationshipIdRetired(
                relationshipId));

        Assert.False(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out _));
    }

    [Fact]
    public void Creation_RejectsActiveDuplicateRelationshipId()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Existing"
            });

        var schema =
            CreateSchema();

        var evolution =
            new SorophyRelationshipCreation(
                relationshipId,
                sourceId,
                targetId,
                "Replacement",
                CreateTime(
                    schema,
                    "2"));

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<InvalidOperationException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    [Fact]
    public void Creation_RejectsRetiredRelationshipId()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Temporary"
            });

        Assert.True(
            graph.RemoveRelationship(
                relationshipId));

        Assert.True(
            graph.IsRelationshipIdRetired(
                relationshipId));

        var schema =
            CreateSchema();

        var evolution =
            new SorophyRelationshipCreation(
                relationshipId,
                sourceId,
                targetId,
                "Reused",
                CreateTime(
                    schema,
                    "3"));

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<InvalidOperationException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    // =============================================================
    // TYPE CHANGE
    // =============================================================

    [Fact]
    public void TypeChange_PreservesRelationshipIdAndRecordsHistoricalState()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance"
            };

        relationship.Properties["Rank"] =
            CreateIntegerProperty(
                "Rank",
                7);

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "20");

        var evolution =
            new SorophyRelationshipTypeChange(
                relationshipId,
                "War",
                effectiveTime);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var current));

        Assert.NotNull(
            current);

        Assert.Equal(
            relationshipId,
            current!.Id);

        Assert.Equal(
            "War",
            current.Type);

        Assert.Equal(
            7L,
            current.Properties["Rank"].Value.Value);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        var fact =
            history.Facts[0];

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
            "Alliance",
            fact.Type);

        Assert.Equal(
            effectiveTime,
            fact.At);

        Assert.Equal(
            effectiveTime,
            fact.ValidTill);

        Assert.Equal(
            7L,
            fact.Properties["Rank"].Value.Value);
    }

    // =============================================================
    // PROPERTY MODIFICATION
    // =============================================================

    [Fact]
    public void PropertyModification_RecordsCompletePreviousState()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Trade"
            };

        relationship.Properties["Gold"] =
            CreateIntegerProperty(
                "Gold",
                100);

        relationship.Properties["Status"] =
            CreateStringProperty(
                "Status",
                "Open");

        relationship.Properties["Route"] =
            CreateStringProperty(
                "Route",
                "North");

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "5");

        var propertiesToSet =
            new Dictionary<string, SorophyProperty>
            {
                ["Gold"] =
                    CreateIntegerProperty(
                        "Gold",
                        250),

                ["Status"] =
                    CreateStringProperty(
                        "Status",
                        "Closed")
            };

        var propertiesToRemove =
            new[]
            {
                "Route"
            };

        var evolution =
            new SorophyRelationshipPropertyModification(
                relationshipId,
                effectiveTime,
                propertiesToSet,
                propertiesToRemove);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var current));

        Assert.NotNull(
            current);

        Assert.Equal(
            250L,
            current!.Properties["Gold"].Value.Value);

        Assert.Equal(
            "Closed",
            current.Properties["Status"].Value.Value);

        Assert.False(
            current.Properties.ContainsKey(
                "Route"));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        var fact =
            history.Facts[0];

        Assert.Equal(
            effectiveTime,
            fact.At);

        Assert.Equal(
            "Trade",
            fact.Type);

        Assert.Equal(
            100L,
            fact.Properties["Gold"].Value.Value);

        Assert.Equal(
            "Open",
            fact.Properties["Status"].Value.Value);

        Assert.Equal(
            "North",
            fact.Properties["Route"].Value.Value);
    }

    [Fact]
    public void PropertyModification_DoesNotMutateHistoricalSnapshot()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Trade"
            };

        relationship.Properties["Value"] =
            CreateIntegerProperty(
                "Value",
                100);

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "10");

        var evolution =
            new SorophyRelationshipPropertyModification(
                relationshipId,
                effectiveTime,
                new Dictionary<string, SorophyProperty>
                {
                    ["Value"] =
                        CreateIntegerProperty(
                            "Value",
                            500)
                });

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        var fact =
            history!.Facts[0];

        Assert.Equal(
            100L,
            fact.Properties["Value"].Value.Value);

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var current));

        Assert.Equal(
            500L,
            current!.Properties["Value"].Value.Value);
    }

    // =============================================================
    // VALIDITY
    // =============================================================

    [Fact]
    public void ValidityChange_RecordsPreviousValidityAndAppliesNewValidity()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var schema =
            CreateSchema();

        var originalFrom =
            CreateTime(
                schema,
                "1");

        var originalTill =
            CreateTime(
                schema,
                "20");

        var newFrom =
            CreateTime(
                schema,
                "5");

        var newTill =
            CreateTime(
                schema,
                "30");

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Treaty",
                ValidFrom = originalFrom,
                ValidTill = originalTill
            };

        graph.AddRelationship(
            relationship);

        var effectiveTime =
            CreateTime(
                schema,
                "10");

        var evolution =
            new SorophyRelationshipValidityChange(
                relationshipId,
                effectiveTime,
                newFrom,
                newTill);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var current));

        Assert.NotNull(
            current);

        Assert.Equal(
            newFrom,
            current!.ValidFrom);

        Assert.Equal(
            newTill,
            current.ValidTill);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        var fact =
            history.Facts[0];

        Assert.Equal(
            effectiveTime,
            fact.At);

        Assert.Equal(
            originalFrom,
            fact.ValidFrom);

        Assert.Equal(
            effectiveTime,
            fact.ValidTill);
    }

    // =============================================================
    // TERMINATION
    // =============================================================

    [Fact]
    public void Termination_RecordsHistoricalStateAndRetiresRelationshipId()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance"
            };

        relationship.Properties["Reason"] =
            CreateStringProperty(
                "Reason",
                "Mutual defense");

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "50");

        var evolution =
            new SorophyRelationshipTermination(
                relationshipId,
                effectiveTime);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            evolution);

        Assert.False(
            graph.TryGetRelationship(
                relationshipId,
                out _));

        Assert.True(
            graph.IsRelationshipIdRetired(
                relationshipId));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        var fact =
            history.Facts[0];

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
            "Alliance",
            fact.Type);

        Assert.Equal(
            effectiveTime,
            fact.At);

        Assert.Equal(
            effectiveTime,
            fact.ValidTill);

        Assert.Equal(
            "Mutual defense",
            fact.Properties["Reason"].Value.Value);
    }

    [Fact]
    public void Termination_PreservesHistoryAfterRelationshipRemoval()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Temporary"
            });

        var schema =
            CreateSchema();

        var effectiveTime =
            CreateTime(
                schema,
                "9");

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            new SorophyRelationshipTermination(
                relationshipId,
                effectiveTime));

        Assert.False(
            graph.ContainsRelationship(
                relationshipId));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        Assert.Equal(
            effectiveTime,
            history.Facts[0].At);
    }

    // =============================================================
    // MULTI-EVOLUTION
    // =============================================================

    [Fact]
    public void MultipleEvolutions_AccumulateHistoryInOrder()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance"
            };

        relationship.Properties["Level"] =
            CreateIntegerProperty(
                "Level",
                1);

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var t1 =
            CreateTime(
                schema,
                "1");

        var t2 =
            CreateTime(
                schema,
                "2");

        var t3 =
            CreateTime(
                schema,
                "3");

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            new SorophyRelationshipTypeChange(
                relationshipId,
                "War",
                t1));

        executor.Execute(
            graph,
            new SorophyRelationshipPropertyModification(
                relationshipId,
                t2,
                new Dictionary<string, SorophyProperty>
                {
                    ["Level"] =
                        CreateIntegerProperty(
                            "Level",
                            2)
                }));

        executor.Execute(
            graph,
            new SorophyRelationshipTermination(
                relationshipId,
                t3));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Equal(
            3,
            history!.Count);

        Assert.Equal(
            t1,
            history.Facts[0].At);

        Assert.Equal(
            "Alliance",
            history.Facts[0].Type);

        Assert.Equal(
            1L,
            history.Facts[0]
                .Properties["Level"]
                .Value
                .Value);

        Assert.Equal(
            t2,
            history.Facts[1].At);

        Assert.Equal(
            "War",
            history.Facts[1].Type);

        Assert.Equal(
            1L,
            history.Facts[1]
                .Properties["Level"]
                .Value
                .Value);

        Assert.Equal(
            t3,
            history.Facts[2].At);

        Assert.Equal(
            "War",
            history.Facts[2].Type);

        Assert.Equal(
            2L,
            history.Facts[2]
                .Properties["Level"]
                .Value
                .Value);

        Assert.True(
            graph.IsRelationshipIdRetired(
                relationshipId));
    }

    [Fact]
    public void CreationThenEvolution_CreatesAndMutatesSameRelationship()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var schema =
            CreateSchema();

        var creationTime =
            CreateTime(
                schema,
                "1");

        var changeTime =
            CreateTime(
                schema,
                "2");

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            new SorophyRelationshipCreation(
                relationshipId,
                sourceId,
                targetId,
                "Alliance",
                creationTime,
                new Dictionary<string, SorophyProperty>
                {
                    ["Level"] =
                        CreateIntegerProperty(
                            "Level",
                            1)
                }));

        executor.Execute(
            graph,
            new SorophyRelationshipTypeChange(
                relationshipId,
                "Federation",
                changeTime));

        Assert.True(
            graph.TryGetRelationship(
                relationshipId,
                out var current));

        Assert.NotNull(
            current);

        Assert.Equal(
            "Federation",
            current!.Type);

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);

        Assert.Equal(
            "Alliance",
            history.Facts[0].Type);

        Assert.Equal(
            changeTime,
            history.Facts[0].At);
    }

    // =============================================================
    // FAILURE / VALIDATION
    // =============================================================

    [Fact]
    public void Evolution_NonexistentRelationshipIsRejected()
    {
        var graph =
            CreateGraph(
                out _,
                out _);

        var relationshipId =
            Guid.NewGuid();

        var schema =
            CreateSchema();

        var evolution =
            new SorophyRelationshipTypeChange(
                relationshipId,
                "War",
                CreateTime(
                    schema,
                    "1"));

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<InvalidOperationException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    [Fact]
    public void Evolution_WithMismatchedEffectiveTimeSchemaIsRejected()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationshipSchema =
            CreateSchema(
                "Test Timeline");

        var relationshipTime =
            CreateTime(
                relationshipSchema,
                "1");

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance",
                ValidFrom = relationshipTime
            });

        var incompatibleSchema =
            CreateSchema(
                "Different Timeline");

        var incompatibleTime =
            CreateTime(
                incompatibleSchema,
                "1");

        var evolution =
            new SorophyRelationshipTypeChange(
                relationshipId,
                "War",
                incompatibleTime);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<ArgumentException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    [Fact]
    public void Evolution_WithMismatchedValiditySchemaIsRejected()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var originalSchema =
            CreateSchema(
                "Original Timeline");

        var originalTime =
            CreateTime(
                originalSchema,
                "1");

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance",
                ValidFrom = originalTime
            });

        var incompatibleSchema =
            CreateSchema(
                "Different Timeline");

        var incompatibleTime =
            CreateTime(
                incompatibleSchema,
                "2");

        var evolution =
            new SorophyRelationshipValidityChange(
                relationshipId,
                originalTime,
                incompatibleTime,
                null);

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<ArgumentException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    [Fact]
    public void TerminationThenReuse_IsPermanentlyRejected()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        graph.AddRelationship(
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Old"
            });

        var schema =
            CreateSchema();

        var terminationTime =
            CreateTime(
                schema,
                "10");

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            new SorophyRelationshipTermination(
                relationshipId,
                terminationTime));

        Assert.Throws<InvalidOperationException>(
            () =>
                executor.Execute(
                    graph,
                    new SorophyRelationshipCreation(
                        relationshipId,
                        sourceId,
                        targetId,
                        "Reborn",
                        CreateTime(
                            schema,
                            "11"))));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Single(
            history!.Facts);
    }

    [Fact]
    public void UnsupportedEvolutionSubtype_IsRejected()
    {
        var graph =
            CreateGraph(
                out _,
                out _);

        var schema =
            CreateSchema();

        var evolution =
            new UnknownEvolution(
                Guid.NewGuid(),
                CreateTime(
                    schema,
                    "1"));

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        Assert.Throws<ArgumentException>(
            () =>
                executor.Execute(
                    graph,
                    evolution));
    }

    // =============================================================
    // SNAPSHOT ISOLATION
    // =============================================================

    [Fact]
    public void HistoryContainsDistinctSnapshotsForRepeatedMutation()
    {
        var graph =
            CreateGraph(
                out var sourceId,
                out var targetId);

        var relationshipId =
            Guid.NewGuid();

        var relationship =
            new SorophyRelationship
            {
                Id = relationshipId,
                SourceId = sourceId,
                TargetId = targetId,
                Type = "Alliance"
            };

        relationship.Properties["Power"] =
            CreateIntegerProperty(
                "Power",
                10);

        graph.AddRelationship(
            relationship);

        var schema =
            CreateSchema();

        var t1 =
            CreateTime(
                schema,
                "1");

        var t2 =
            CreateTime(
                schema,
                "2");

        var executor =
            new SorophyRelationshipEvolutionExecutor();

        executor.Execute(
            graph,
            new SorophyRelationshipPropertyModification(
                relationshipId,
                t1,
                new Dictionary<string, SorophyProperty>
                {
                    ["Power"] =
                        CreateIntegerProperty(
                            "Power",
                            20)
                }));

        executor.Execute(
            graph,
            new SorophyRelationshipPropertyModification(
                relationshipId,
                t2,
                new Dictionary<string, SorophyProperty>
                {
                    ["Power"] =
                        CreateIntegerProperty(
                            "Power",
                            30)
                }));

        Assert.True(
            graph.TryGetRelationshipHistory(
                relationshipId,
                out var history));

        Assert.NotNull(
            history);

        Assert.Equal(
            2,
            history!.Count);

        Assert.Equal(
            10L,
            history.Facts[0]
                .Properties["Power"]
                .Value
                .Value);

        Assert.Equal(
            20L,
            history.Facts[1]
                .Properties["Power"]
                .Value
                .Value);

        Assert.Equal(
            t1,
            history.Facts[0].At);

        Assert.Equal(
            t2,
            history.Facts[1].At);
    }

    // =============================================================
    // HELPERS
    // =============================================================

    private static SorophyGraph CreateGraph(
        out Guid sourceId,
        out Guid targetId)
    {
        var graph =
            new SorophyGraph();

        sourceId =
            Guid.NewGuid();

        targetId =
            Guid.NewGuid();

        graph.AddEntity(
            new SorophyEntity
            {
                Id = sourceId,
                Name = "Source",
                Type = "Person"
            });

        graph.AddEntity(
            new SorophyEntity
            {
                Id = targetId,
                Name = "Target",
                Type = "Person"
            });

        return graph;
    }

    private static SorophyTimeSchema CreateSchema(
        string timeline = "Test Timeline")
    {
        var numericPosition =
            new SorophyTimePositionDefinition(
                SorophyTimePositionKind.Numeric);

        return new SorophyTimeSchema(
            timeline,
            new[]
            {
                new SorophyTimeUnit(
                    "Year",
                    0,
                    numericPosition)
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

    private static SorophyProperty CreateIntegerProperty(
        string name,
        long value)
    {
        return new SorophyProperty
        {
            Name = name,
            Value =
                new SorophyValue(
                    SorophyValueType.Integer,
                    value)
        };
    }

    private static SorophyProperty CreateStringProperty(
        string name,
        string value)
    {
        return new SorophyProperty
        {
            Name = name,
            Value =
                new SorophyValue(
                    SorophyValueType.String,
                    value)
        };
    }

    private sealed class UnknownEvolution
        : SorophyRelationshipEvolution
    {
        public UnknownEvolution(
            Guid relationshipId,
            SorophyTime effectiveTime)
            : base(
                relationshipId,
                effectiveTime)
        {
        }
    }
}