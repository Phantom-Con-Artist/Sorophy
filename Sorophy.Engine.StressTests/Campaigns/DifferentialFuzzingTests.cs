using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sorophy.Engine.Graph;
using Sorophy.Engine.Types;

namespace Sorophy.Engine.StressTests;

public static class DifferentialFuzzingTests
{
    private const int RecentOperationCapacity = 32;

    public static int Run(
        int seed = 12345,
        int operations = 10_000,
        int auditInterval = 1_000,
        string profile = "quick")
    {
        if (operations <= 0)
            throw new ArgumentOutOfRangeException(nameof(operations));
        if (auditInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(auditInterval));

        var seedCount = profile.ToLowerInvariant() switch
        {
            "quick" => 2,
            "full" => 5,
            "torture" => 10,
            _ => 2
        };

        PrintHeader(seed, operations, auditInterval, profile, seedCount);

        var overall = Stopwatch.StartNew();
        var passedSeeds = 0;

        for (var index = 0; index < seedCount; index++)
        {
            var derivedSeed = DeriveSeed(seed, index);

            Console.WriteLine();
            Console.WriteLine(
                $"  ┌─ Fuzz Run {index + 1}/{seedCount} | Seed {derivedSeed}");

            if (!RunSeed(
                    derivedSeed,
                    operations,
                    auditInterval,
                    index + 1,
                    seedCount))
            {
                Console.WriteLine("  └─ FAIL");
                overall.Stop();
                PrintSummary(
                    passedSeeds,
                    seedCount,
                    operations,
                    overall.Elapsed,
                    false);
                return 1;
            }

            passedSeeds++;
            Console.WriteLine("  └─ PASS");
        }

        overall.Stop();
        PrintSummary(
            passedSeeds,
            seedCount,
            operations,
            overall.Elapsed,
            true);

        return 0;
    }

    private static bool RunSeed(
        int seed,
        int operations,
        int auditInterval,
        int runIndex,
        int runCount)
    {
        var random = new DeterministicRandom(seed);
        var auditRandom = new DeterministicRandom(
            unchecked(seed ^ (int)0xA5A5A5A5));
        var graph = new SorophyGraph();
        var reference = new ReferenceGraph();
        var recent = new RecentOperations(RecentOperationCapacity);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var operationNumber = 1;
                 operationNumber <= operations;
                 operationNumber++)
            {
                var operation =
                    OperationGenerator.Generate(
                        random,
                        reference);

                recent.Add(
                    operationNumber,
                    operation);

                Execute(
                    graph,
                    reference,
                    operation,
                    operationNumber);

                if (operationNumber % auditInterval == 0 ||
                    operationNumber == operations)
                {
                    Audit(
                        graph,
                        reference,
                        auditRandom,
                        operationNumber);
                }

                if (operationNumber % 10_000 == 0)
                {
                    var rate =
                        operationNumber /
                        Math.Max(
                            stopwatch.Elapsed.TotalSeconds,
                            0.001);

                    Console.WriteLine(
                        $"      [{operationNumber,10:N0}/{operations:N0}] " +
                        $"Entities={graph.Entities.Count,7:N0} " +
                        $"Relationships={graph.Relationships.Count,7:N0} " +
                        $"Rate={rate,10:N0} ops/s");
                }
            }

            Console.WriteLine(
                $"      Final: Entities={graph.Entities.Count:N0}, " +
                $"Relationships={graph.Relationships.Count:N0}");

            return true;
        }
        catch (Exception exception)
        {
            PrintFailure(
                seed,
                operations,
                runIndex,
                runCount,
                recent,
                graph,
                reference,
                exception);

            return false;
        }
    }

    private static void Execute(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        switch (operation.Kind)
        {
            case FuzzOperationKind.AddEntity:
                ExecuteAddEntity(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.RemoveEntity:
                ExecuteRemoveEntity(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.AddRelationship:
                ExecuteAddRelationship(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.RemoveRelationship:
                ExecuteRemoveRelationship(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.DuplicateEntity:
                ExecuteDuplicateEntity(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.DuplicateRelationship:
                ExecuteDuplicateRelationship(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.InvalidRelationship:
                ExecuteInvalidRelationship(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.SelfRelationship:
                ExecuteSelfRelationship(graph, reference, operation, operationNumber);
                return;
            case FuzzOperationKind.QueryOutgoing:
                CompareOutgoing(graph, reference, operation.PrimaryId, operationNumber, operation);
                return;
            case FuzzOperationKind.QueryIncoming:
                CompareIncoming(graph, reference, operation.PrimaryId, operationNumber, operation);
                return;
            case FuzzOperationKind.QueryIncident:
                CompareIncident(graph, reference, operation.PrimaryId, operationNumber, operation);
                return;
            case FuzzOperationKind.QueryNeighbors:
                CompareNeighbors(graph, reference, operation.PrimaryId, operationNumber, operation);
                return;
            case FuzzOperationKind.Reachability:
                CompareReachability(
                    graph,
                    reference,
                    operation.SourceId,
                    operation.TargetId,
                    operationNumber,
                    operation);
                return;
            case FuzzOperationKind.Traverse:
                CompareTraverse(graph, reference, operation.PrimaryId, operationNumber, operation);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unknown fuzz operation '{operation.Kind}'.");
        }
    }

    private static void ExecuteAddEntity(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        if (reference.ContainsEntity(operation.PrimaryId))
            throw Failure(operationNumber, "Generator produced duplicate entity ID.");

        var entity = new SorophyEntity
        {
            Id = operation.PrimaryId,
            Name = operation.Name,
            Type = operation.Type
        };

        entity.Properties["value"] = new SorophyProperty
        {
            Name = "value",
            Value = new SorophyValue(
                SorophyValueType.Integer,
                operation.IntegerValue)
        };

        var before = graph.Entities.Count;

        try
        {
            graph.AddEntity(entity);
        }
        catch (Exception exception)
        {
            throw Failure(
                operationNumber,
                "SorophyGraph rejected a valid entity.",
                exception);
        }

        if (graph.Entities.Count != before + 1)
            throw Failure(operationNumber, "Entity count did not increase by one.");

        if (!reference.AddEntity(
                operation.PrimaryId,
                operation.Name,
                operation.Type,
                operation.IntegerValue))
        {
            throw Failure(operationNumber, "Reference model rejected a valid entity.");
        }
    }

    private static void ExecuteRemoveEntity(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        var expected =
            reference.ContainsEntity(operation.PrimaryId);
        var actual =
            graph.RemoveEntity(operation.PrimaryId);

        if (actual != expected)
        {
            throw Failure(
                operationNumber,
                $"Entity removal mismatch. Expected={expected}, Actual={actual}.");
        }

        if (actual &&
            !reference.RemoveEntity(operation.PrimaryId))
        {
            throw Failure(operationNumber, "Reference entity removal failed.");
        }
    }

    private static void ExecuteAddRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        if (!reference.ContainsEntity(operation.SourceId) ||
            !reference.ContainsEntity(operation.TargetId) ||
            reference.ContainsRelationship(operation.PrimaryId))
        {
            throw Failure(
                operationNumber,
                "Generator produced invalid AddRelationship workload.");
        }

        try
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id = operation.PrimaryId,
                    Type = operation.Type,
                    SourceId = operation.SourceId,
                    TargetId = operation.TargetId
                });
        }
        catch (Exception exception)
        {
            throw Failure(
                operationNumber,
                "SorophyGraph rejected a valid relationship.",
                exception);
        }

        if (!reference.AddRelationship(
                operation.PrimaryId,
                operation.SourceId,
                operation.TargetId,
                operation.Type))
        {
            throw Failure(
                operationNumber,
                "Reference model rejected a relationship accepted by SorophyGraph.");
        }
    }

    private static void ExecuteRemoveRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        var expected =
            reference.ContainsRelationship(operation.PrimaryId);
        var actual =
            graph.RemoveRelationship(operation.PrimaryId);

        if (actual != expected)
        {
            throw Failure(
                operationNumber,
                $"Relationship removal mismatch. Expected={expected}, Actual={actual}.");
        }

        if (actual &&
            !reference.RemoveRelationship(operation.PrimaryId))
        {
            throw Failure(operationNumber, "Reference relationship removal failed.");
        }
    }

    private static void ExecuteDuplicateEntity(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        if (!reference.ContainsEntity(operation.PrimaryId))
            throw Failure(operationNumber, "Duplicate entity target does not exist.");

        var before = graph.Entities.Count;
        var threw = false;

        try
        {
            graph.AddEntity(
                new SorophyEntity
                {
                    Id = operation.PrimaryId,
                    Name = "DUPLICATE",
                    Type = "Duplicate"
                });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        if (!threw)
            throw Failure(operationNumber, "SorophyGraph accepted duplicate entity ID.");

        if (graph.Entities.Count != before)
            throw Failure(operationNumber, "Rejected duplicate entity mutated graph state.");
    }

    private static void ExecuteDuplicateRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        if (!reference.TryGetRelationship(
                operation.PrimaryId,
                out var existing))
        {
            throw Failure(
                operationNumber,
                "Duplicate relationship target does not exist.");
        }

        var before = graph.Relationships.Count;
        var threw = false;

        try
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id = existing.Id,
                    Type = existing.Type,
                    SourceId = existing.SourceId,
                    TargetId = existing.TargetId
                });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        if (!threw)
            throw Failure(operationNumber, "SorophyGraph accepted duplicate relationship ID.");

        if (graph.Relationships.Count != before)
        {
            throw Failure(
                operationNumber,
                "Rejected duplicate relationship mutated graph state.");
        }
    }

    private static void ExecuteInvalidRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        var beforeEntities = graph.Entities.Count;
        var beforeRelationships = graph.Relationships.Count;
        var threw = false;

        try
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id = operation.PrimaryId,
                    Type = operation.Type,
                    SourceId = operation.SourceId,
                    TargetId = operation.TargetId
                });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        if (!threw)
        {
            throw Failure(
                operationNumber,
                "SorophyGraph accepted intentionally invalid relationship.");
        }

        if (graph.Entities.Count != beforeEntities ||
            graph.Relationships.Count != beforeRelationships)
        {
            throw Failure(
                operationNumber,
                "Rejected invalid relationship mutated graph state.");
        }

        if (reference.ContainsRelationship(operation.PrimaryId))
        {
            throw Failure(
                operationNumber,
                "Invalid relationship ID already exists in reference model.");
        }
    }

    private static void ExecuteSelfRelationship(
        SorophyGraph graph,
        ReferenceGraph reference,
        FuzzOperation operation,
        int operationNumber)
    {
        if (!reference.ContainsEntity(operation.SourceId))
            throw Failure(operationNumber, "Self relationship target entity does not exist.");

        if (reference.ContainsRelationship(operation.PrimaryId))
            throw Failure(operationNumber, "Self relationship generated duplicate ID.");

        try
        {
            graph.AddRelationship(
                new SorophyRelationship
                {
                    Id = operation.PrimaryId,
                    Type = "self",
                    SourceId = operation.SourceId,
                    TargetId = operation.SourceId
                });
        }
        catch (Exception exception)
        {
            throw Failure(
                operationNumber,
                "SorophyGraph rejected a valid self relationship.",
                exception);
        }

        if (!reference.AddRelationship(
                operation.PrimaryId,
                operation.SourceId,
                operation.SourceId,
                "self"))
        {
            throw Failure(
                operationNumber,
                "Reference model rejected a valid self relationship.");
        }
    }

    private static void CompareOutgoing(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid entityId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.GetOutgoingRelationships(entityId)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var expected = reference.GetOutgoing(entityId)
            .OrderBy(x => x)
            .ToArray();

        EnsureEqual(
            actual,
            expected,
            operationNumber,
            "Outgoing query",
            operation);
    }

    private static void CompareIncoming(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid entityId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.GetIncomingRelationships(entityId)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var expected = reference.GetIncoming(entityId)
            .OrderBy(x => x)
            .ToArray();

        EnsureEqual(
            actual,
            expected,
            operationNumber,
            "Incoming query",
            operation);
    }

    private static void CompareIncident(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid entityId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.GetRelationships(entityId)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var expected = reference.GetIncident(entityId)
            .OrderBy(x => x)
            .ToArray();

        EnsureEqual(
            actual,
            expected,
            operationNumber,
            "Incident query",
            operation);
    }

    private static void CompareNeighbors(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid entityId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.GetNeighbors(entityId)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var expected = reference.GetNeighbors(entityId)
            .OrderBy(x => x)
            .ToArray();

        EnsureEqual(
            actual,
            expected,
            operationNumber,
            "Neighbor query",
            operation);
    }

    private static void CompareReachability(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid sourceId,
        Guid targetId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.IsReachable(sourceId, targetId);
        var expected = reference.IsReachable(sourceId, targetId);

        if (actual != expected)
        {
            throw Failure(
                operationNumber,
                $"Reachability mismatch. Operation={operation}; Expected={expected}; Actual={actual}.");
        }
    }

    private static void CompareTraverse(
        SorophyGraph graph,
        ReferenceGraph reference,
        Guid sourceId,
        int operationNumber,
        FuzzOperation operation)
    {
        var actual = graph.Traverse(sourceId)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var expected = reference.Traverse(sourceId)
            .OrderBy(x => x)
            .ToArray();

        EnsureEqual(
            actual,
            expected,
            operationNumber,
            "Traversal query",
            operation);
    }

    private static void EnsureEqual(
        IReadOnlyList<Guid> actual,
        IReadOnlyList<Guid> expected,
        int operationNumber,
        string description,
        FuzzOperation operation)
    {
        if (actual.Count != expected.Count)
        {
            throw Failure(
                operationNumber,
                $"{description} mismatch. Operation={operation}; " +
                $"ExpectedCount={expected.Count}; ActualCount={actual.Count}; " +
                $"Expected=[{FormatIds(expected)}]; Actual=[{FormatIds(actual)}].");
        }

        for (var index = 0; index < actual.Count; index++)
        {
            if (actual[index] != expected[index])
            {
                throw Failure(
                    operationNumber,
                    $"{description} mismatch at index {index}. Operation={operation}; " +
                    $"Expected={expected[index]:D}; Actual={actual[index]:D}.");
            }
        }
    }

    private static void Audit(
        SorophyGraph graph,
        ReferenceGraph reference,
        DeterministicRandom auditRandom,
        int operationNumber)
    {
        var graphErrors = graph.Validate();
        if (graphErrors.Count > 0)
        {
            throw Failure(
                operationNumber,
                "SorophyGraph.Validate() reported " +
                $"{graphErrors.Count} error(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, graphErrors.Take(20)));
        }

        var referenceErrors = reference.Validate();
        if (referenceErrors.Count > 0)
        {
            throw Failure(
                operationNumber,
                "Reference model validation reported " +
                $"{referenceErrors.Count} error(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, referenceErrors.Take(20)));
        }

        CompareCanonicalState(
            graph,
            reference,
            operationNumber);

        var samples = Math.Min(4, reference.EntityCount);

        for (var index = 0; index < samples; index++)
        {
            var entityId = reference.SelectEntity(auditRandom);

            CompareOutgoing(
                graph,
                reference,
                entityId,
                operationNumber,
                new FuzzOperation
                {
                    Kind = FuzzOperationKind.QueryOutgoing,
                    PrimaryId = entityId
                });

            CompareIncoming(
                graph,
                reference,
                entityId,
                operationNumber,
                new FuzzOperation
                {
                    Kind = FuzzOperationKind.QueryIncoming,
                    PrimaryId = entityId
                });

            CompareIncident(
                graph,
                reference,
                entityId,
                operationNumber,
                new FuzzOperation
                {
                    Kind = FuzzOperationKind.QueryIncident,
                    PrimaryId = entityId
                });

            CompareNeighbors(
                graph,
                reference,
                entityId,
                operationNumber,
                new FuzzOperation
                {
                    Kind = FuzzOperationKind.QueryNeighbors,
                    PrimaryId = entityId
                });

            var targetId = reference.SelectEntity(auditRandom);

            CompareReachability(
                graph,
                reference,
                entityId,
                targetId,
                operationNumber,
                new FuzzOperation
                {
                    Kind = FuzzOperationKind.Reachability,
                    SourceId = entityId,
                    TargetId = targetId
                });
        }
    }

    private static void CompareCanonicalState(
        SorophyGraph graph,
        ReferenceGraph reference,
        int operationNumber)
    {
        if (graph.Entities.Count != reference.EntityCount)
        {
            throw Failure(
                operationNumber,
                $"Entity count mismatch. Expected={reference.EntityCount}; Actual={graph.Entities.Count}.");
        }

        if (graph.Relationships.Count != reference.RelationshipCount)
        {
            throw Failure(
                operationNumber,
                $"Relationship count mismatch. Expected={reference.RelationshipCount}; Actual={graph.Relationships.Count}.");
        }

        foreach (var entityId in reference.EntityIds)
        {
            if (!graph.Entities.TryGetValue(entityId, out var actual))
                throw Failure(operationNumber, $"Graph is missing entity '{entityId:D}'.");

            if (!reference.TryGetEntity(entityId, out var expected))
                throw Failure(operationNumber, $"Reference lookup failed for entity '{entityId:D}'.");

            if (!string.Equals(actual.Name, expected.Name, StringComparison.Ordinal) ||
                !string.Equals(actual.Type, expected.Type, StringComparison.Ordinal))
            {
                throw Failure(
                    operationNumber,
                    $"Entity payload mismatch for '{entityId:D}'.");
            }

            if (!actual.Properties.TryGetValue("value", out var property) ||
                property is null ||
                property.Value.Type != SorophyValueType.Integer ||
                property.Value.Value is not long value ||
                value != expected.IntegerValue)
            {
                throw Failure(
                    operationNumber,
                    $"Entity property mismatch for '{entityId:D}'.");
            }
        }

        foreach (var relationshipId in reference.RelationshipIds)
        {
            if (!graph.Relationships.TryGetValue(relationshipId, out var actual))
                throw Failure(operationNumber, $"Graph is missing relationship '{relationshipId:D}'.");

            if (!reference.TryGetRelationship(relationshipId, out var expected))
                throw Failure(operationNumber, $"Reference lookup failed for relationship '{relationshipId:D}'.");

            if (actual.SourceId != expected.SourceId ||
                actual.TargetId != expected.TargetId ||
                !string.Equals(actual.Type, expected.Type, StringComparison.Ordinal))
            {
                throw Failure(
                    operationNumber,
                    $"Relationship payload mismatch for '{relationshipId:D}'.");
            }
        }

        foreach (var entityId in graph.Entities.Keys)
        {
            if (!reference.ContainsEntity(entityId))
                throw Failure(operationNumber, $"Graph contains unexpected entity '{entityId:D}'.");
        }

        foreach (var relationshipId in graph.Relationships.Keys)
        {
            if (!reference.ContainsRelationship(relationshipId))
            {
                throw Failure(
                    operationNumber,
                    $"Graph contains unexpected relationship '{relationshipId:D}'.");
            }
        }
    }

    private static InvalidOperationException Failure(
        int operationNumber,
        string message,
        Exception? inner = null)
    {
        return new InvalidOperationException(
            $"Operation {operationNumber:N0}: {message}",
            inner);
    }

    private static int DeriveSeed(int baseSeed, int index)
    {
        unchecked
        {
            var value =
                (uint)baseSeed +
                ((uint)index * 0x9E3779B9u);

            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;

            return (int)value;
        }
    }

    private static void PrintHeader(
        int seed,
        int operations,
        int auditInterval,
        string profile,
        int seedCount)
    {
        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                SOROPHY ENGINE DIFFERENTIAL FUZZ               ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ Base seed:      {seed,-43}║");
        Console.WriteLine($"║ Operations:     {operations,-43}║");
        Console.WriteLine($"║ Audit interval: {auditInterval,-43}║");
        Console.WriteLine($"║ Profile:        {profile,-43}║");
        Console.WriteLine($"║ Seed families:  {seedCount,-43}║");
        Console.WriteLine(
            "║ Model:          SorophyGraph vs independent reference model  ║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine(
            "Randomized mutations, negative cases, topology queries, and");
        Console.WriteLine(
            "traversal are compared against a deliberately simple model.");
    }

    private static void PrintSummary(
        int passedSeeds,
        int totalSeeds,
        int operations,
        TimeSpan elapsed,
        bool success)
    {
        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                 DIFFERENTIAL FUZZ RESULT");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine(
            $"Seed families passed: {passedSeeds}/{totalSeeds}");
        Console.WriteLine(
            $"Operations per seed: {operations:N0}");
        Console.WriteLine(
            $"Operations executed: {checked(passedSeeds * operations):N0}");
        Console.WriteLine(
            $"Total elapsed:       {elapsed}");
        Console.WriteLine();

        if (success)
        {
            Console.WriteLine("                    100% PASS");
            Console.WriteLine("STATUS: DIFFERENTIAL FUZZING VERIFIED");
        }
        else
        {
            Console.WriteLine("                    FAILURE");
            Console.WriteLine("STATUS: DIFFERENTIAL FUZZING FAILED");
        }

        Console.WriteLine();
    }

    private static void PrintFailure(
        int seed,
        int operations,
        int runIndex,
        int runCount,
        RecentOperations recent,
        SorophyGraph graph,
        ReferenceGraph reference,
        Exception exception)
    {
        Console.WriteLine();
        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine(
            "                DIFFERENTIAL FUZZ FAILURE");
        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine();
        Console.WriteLine($"Run:                {runIndex}/{runCount}");
        Console.WriteLine($"Seed:               {seed}");
        Console.WriteLine($"Operations target:  {operations:N0}");
        Console.WriteLine();
        Console.WriteLine("GRAPH:");
        Console.WriteLine($"  Entities:         {graph.Entities.Count:N0}");
        Console.WriteLine($"  Relationships:    {graph.Relationships.Count:N0}");
        Console.WriteLine();
        Console.WriteLine("REFERENCE:");
        Console.WriteLine($"  Entities:         {reference.EntityCount:N0}");
        Console.WriteLine($"  Relationships:    {reference.RelationshipCount:N0}");
        Console.WriteLine();
        Console.WriteLine("FAILURE:");
        Console.WriteLine($"  {exception.Message}");
        Console.WriteLine();
        Console.WriteLine("LAST OPERATIONS:");

        foreach (var entry in recent.Items)
        {
            Console.WriteLine(
                $"  #{entry.Number:N0} {entry.Operation}");
        }

        Console.WriteLine();
        Console.WriteLine("STACK:");
        Console.WriteLine(exception);
        Console.WriteLine();
    }

    private static string FormatIds(
        IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0)
            return string.Empty;

        const int limit = 12;
        var builder = new StringBuilder();
        var count = Math.Min(ids.Count, limit);

        for (var index = 0; index < count; index++)
        {
            if (index > 0)
                builder.Append(',');

            builder.Append(ids[index].ToString("D"));
        }

        if (ids.Count > limit)
            builder.Append(",...");

        return builder.ToString();
    }

    private enum FuzzOperationKind
    {
        AddEntity,
        RemoveEntity,
        AddRelationship,
        RemoveRelationship,
        DuplicateEntity,
        DuplicateRelationship,
        InvalidRelationship,
        SelfRelationship,
        QueryOutgoing,
        QueryIncoming,
        QueryIncident,
        QueryNeighbors,
        Reachability,
        Traverse
    }

    private sealed record FuzzOperation
    {
        public FuzzOperationKind Kind { get; init; }
        public Guid PrimaryId { get; init; }
        public Guid SourceId { get; init; }
        public Guid TargetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public long IntegerValue { get; init; }

        public override string ToString() => Kind switch
        {
            FuzzOperationKind.AddEntity =>
                $"ADD_ENTITY {PrimaryId:D} '{Name}' '{Type}' value={IntegerValue}",
            FuzzOperationKind.RemoveEntity =>
                $"REMOVE_ENTITY {PrimaryId:D}",
            FuzzOperationKind.AddRelationship =>
                $"ADD_RELATIONSHIP {PrimaryId:D} {SourceId:D}->{TargetId:D} type='{Type}'",
            FuzzOperationKind.RemoveRelationship =>
                $"REMOVE_RELATIONSHIP {PrimaryId:D}",
            FuzzOperationKind.DuplicateEntity =>
                $"DUPLICATE_ENTITY {PrimaryId:D}",
            FuzzOperationKind.DuplicateRelationship =>
                $"DUPLICATE_RELATIONSHIP {PrimaryId:D}",
            FuzzOperationKind.InvalidRelationship =>
                $"INVALID_RELATIONSHIP {PrimaryId:D} {SourceId:D}->{TargetId:D}",
            FuzzOperationKind.SelfRelationship =>
                $"SELF_RELATIONSHIP {PrimaryId:D} {SourceId:D}",
            FuzzOperationKind.QueryOutgoing =>
                $"QUERY_OUTGOING {PrimaryId:D}",
            FuzzOperationKind.QueryIncoming =>
                $"QUERY_INCOMING {PrimaryId:D}",
            FuzzOperationKind.QueryIncident =>
                $"QUERY_INCIDENT {PrimaryId:D}",
            FuzzOperationKind.QueryNeighbors =>
                $"QUERY_NEIGHBORS {PrimaryId:D}",
            FuzzOperationKind.Reachability =>
                $"REACHABILITY {SourceId:D}->{TargetId:D}",
            FuzzOperationKind.Traverse =>
                $"TRAVERSE {PrimaryId:D}",
            _ => Kind.ToString()
        };
    }

    private static class OperationGenerator
    {
        public static FuzzOperation Generate(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount == 0)
                return CreateAddEntity(random, reference);

            var choice = random.Next(100);

            if (choice < 20)
                return CreateAddEntity(random, reference);
            if (choice < 32)
                return CreateRemoveEntity(random, reference);
            if (choice < 48)
                return CreateAddRelationship(random, reference);
            if (choice < 58)
                return CreateRemoveRelationship(random, reference);
            if (choice < 63)
                return CreateDuplicateEntity(random, reference);
            if (choice < 68)
                return CreateDuplicateRelationship(random, reference);
            if (choice < 76)
                return CreateInvalidRelationship(random, reference);
            if (choice < 82)
                return CreateSelfRelationship(random, reference);
            if (choice < 86)
                return CreateQueryOutgoing(random, reference);
            if (choice < 90)
                return CreateQueryIncoming(random, reference);
            if (choice < 93)
                return CreateQueryIncident(random, reference);
            if (choice < 96)
                return CreateQueryNeighbors(random, reference);
            if (choice < 98)
                return CreateReachability(random, reference);

            return CreateTraverse(random, reference);
        }

        private static FuzzOperation CreateAddEntity(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var id = UniqueEntityId(random, reference);

            return new FuzzOperation
            {
                Kind = FuzzOperationKind.AddEntity,
                PrimaryId = id,
                Name = $"FuzzEntity-{random.Next(1_000_000):N0}",
                Type = $"Type-{random.Next(16)}",
                IntegerValue = random.NextLong(-1_000_000, 1_000_001)
            };
        }

        private static FuzzOperation CreateRemoveEntity(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            return new FuzzOperation
            {
                Kind = FuzzOperationKind.RemoveEntity,
                PrimaryId = random.Next(100) < 85
                    ? reference.SelectEntity(random)
                    : random.NextGuid()
            };
        }

        private static FuzzOperation CreateAddRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            Guid source;
            Guid target;
            var existing = reference.SelectRelationshipOrNull(random);

            if (existing is not null && random.Next(100) < 40)
            {
                source = existing.SourceId;
                target = random.Next(2) == 0
                    ? existing.TargetId
                    : existing.SourceId;
            }
            else
            {
                source = reference.SelectEntity(random);
                target = reference.SelectEntity(random);
            }

            return new FuzzOperation
            {
                Kind = FuzzOperationKind.AddRelationship,
                PrimaryId = UniqueRelationshipId(random, reference),
                SourceId = source,
                TargetId = target,
                Type = $"Relation-{random.Next(12)}"
            };
        }

        private static FuzzOperation CreateRemoveRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            return new FuzzOperation
            {
                Kind = FuzzOperationKind.RemoveRelationship,
                PrimaryId = reference.RelationshipCount > 0 && random.Next(100) < 85
                    ? reference.SelectRelationship(random)
                    : random.NextGuid()
            };
        }

        private static FuzzOperation CreateDuplicateEntity(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            new()
            {
                Kind = FuzzOperationKind.DuplicateEntity,
                PrimaryId = reference.SelectEntity(random)
            };

        private static FuzzOperation CreateDuplicateRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.RelationshipCount == 0)
                return CreateAddEntity(random, reference);

            return new FuzzOperation
            {
                Kind = FuzzOperationKind.DuplicateRelationship,
                PrimaryId = reference.SelectRelationship(random)
            };
        }

        private static FuzzOperation CreateInvalidRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var invalid = random.NextGuid();

            while (reference.ContainsEntity(invalid))
                invalid = random.NextGuid();

            var invalidSource = random.Next(2) == 0;

            return new FuzzOperation
            {
                Kind = FuzzOperationKind.InvalidRelationship,
                PrimaryId = UniqueRelationshipId(random, reference),
                SourceId = invalidSource
                    ? invalid
                    : reference.SelectEntity(random),
                TargetId = invalidSource
                    ? reference.SelectEntity(random)
                    : invalid,
                Type = "invalid"
            };
        }

        private static FuzzOperation CreateSelfRelationship(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            new()
            {
                Kind = FuzzOperationKind.SelfRelationship,
                PrimaryId = UniqueRelationshipId(random, reference),
                SourceId = reference.SelectEntity(random),
                Type = "self"
            };

        private static FuzzOperation CreateQueryOutgoing(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            Query(FuzzOperationKind.QueryOutgoing, random, reference);

        private static FuzzOperation CreateQueryIncoming(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            Query(FuzzOperationKind.QueryIncoming, random, reference);

        private static FuzzOperation CreateQueryIncident(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            Query(FuzzOperationKind.QueryIncident, random, reference);

        private static FuzzOperation CreateQueryNeighbors(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            Query(FuzzOperationKind.QueryNeighbors, random, reference);

        private static FuzzOperation Query(
            FuzzOperationKind kind,
            DeterministicRandom random,
            ReferenceGraph reference) =>
            new()
            {
                Kind = kind,
                PrimaryId = random.Next(100) < 90
                    ? reference.SelectEntity(random)
                    : random.NextGuid()
            };

        private static FuzzOperation CreateReachability(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            new()
            {
                Kind = FuzzOperationKind.Reachability,
                SourceId = random.Next(100) < 90
                    ? reference.SelectEntity(random)
                    : random.NextGuid(),
                TargetId = random.Next(100) < 90
                    ? reference.SelectEntity(random)
                    : random.NextGuid()
            };

        private static FuzzOperation CreateTraverse(
            DeterministicRandom random,
            ReferenceGraph reference) =>
            new()
            {
                Kind = FuzzOperationKind.Traverse,
                PrimaryId = random.Next(100) < 90
                    ? reference.SelectEntity(random)
                    : random.NextGuid()
            };

        private static Guid UniqueEntityId(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var id = random.NextGuid();
            while (reference.ContainsEntity(id))
                id = random.NextGuid();
            return id;
        }

        private static Guid UniqueRelationshipId(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var id = random.NextGuid();
            while (reference.ContainsRelationship(id))
                id = random.NextGuid();
            return id;
        }
    }

    private sealed class ReferenceGraph
    {
        private readonly Dictionary<Guid, ReferenceEntity> _entities = new();
        private readonly List<Guid> _entityOrder = new();
        private readonly Dictionary<Guid, int> _entityIndexes = new();

        private readonly Dictionary<Guid, ReferenceRelationship> _relationships = new();
        private readonly List<Guid> _relationshipOrder = new();
        private readonly Dictionary<Guid, int> _relationshipIndexes = new();

        private readonly Dictionary<Guid, HashSet<Guid>> _outgoing = new();
        private readonly Dictionary<Guid, HashSet<Guid>> _incoming = new();

        public int EntityCount => _entities.Count;
        public int RelationshipCount => _relationships.Count;
        public IEnumerable<Guid> EntityIds => _entities.Keys;
        public IEnumerable<Guid> RelationshipIds => _relationships.Keys;

        public bool ContainsEntity(Guid id) => _entities.ContainsKey(id);
        public bool ContainsRelationship(Guid id) => _relationships.ContainsKey(id);

        public bool TryGetEntity(
            Guid id,
            out ReferenceEntity entity) =>
            _entities.TryGetValue(id, out entity!);

        public bool TryGetRelationship(
            Guid id,
            out ReferenceRelationship relationship) =>
            _relationships.TryGetValue(id, out relationship!);

        public bool AddEntity(
            Guid id,
            string name,
            string type,
            long integerValue)
        {
            if (!_entities.TryAdd(
                    id,
                    new ReferenceEntity(id, name, type, integerValue)))
            {
                return false;
            }

            _entityIndexes[id] = _entityOrder.Count;
            _entityOrder.Add(id);
            _outgoing[id] = new HashSet<Guid>();
            _incoming[id] = new HashSet<Guid>();
            return true;
        }

        public bool RemoveEntity(Guid id)
        {
            if (!_entities.Remove(id))
                return false;

            var incident = new HashSet<Guid>();

            incident.UnionWith(_outgoing[id]);
            incident.UnionWith(_incoming[id]);

            foreach (var relationshipId in incident)
                RemoveRelationship(relationshipId);

            _outgoing.Remove(id);
            _incoming.Remove(id);
            RemoveEntityFromOrder(id);
            return true;
        }

        public bool AddRelationship(
            Guid id,
            Guid source,
            Guid target,
            string type)
        {
            if (!_entities.ContainsKey(source) ||
                !_entities.ContainsKey(target) ||
                !_relationships.TryAdd(
                    id,
                    new ReferenceRelationship(id, source, target, type)))
            {
                return false;
            }

            _relationshipIndexes[id] = _relationshipOrder.Count;
            _relationshipOrder.Add(id);
            _outgoing[source].Add(id);
            _incoming[target].Add(id);
            return true;
        }

        public bool RemoveRelationship(Guid id)
        {
            if (!_relationships.Remove(id, out var relationship))
                return false;

            _outgoing[relationship.SourceId].Remove(id);
            _incoming[relationship.TargetId].Remove(id);
            RemoveRelationshipFromOrder(id);
            return true;
        }

        public Guid SelectEntity(DeterministicRandom random)
        {
            if (_entityOrder.Count == 0)
                throw new InvalidOperationException("Reference graph has no entities.");
            return _entityOrder[random.Next(_entityOrder.Count)];
        }

        public Guid SelectRelationship(DeterministicRandom random)
        {
            if (_relationshipOrder.Count == 0)
                throw new InvalidOperationException("Reference graph has no relationships.");
            return _relationshipOrder[random.Next(_relationshipOrder.Count)];
        }

        public ReferenceRelationship? SelectRelationshipOrNull(
            DeterministicRandom random)
        {
            if (_relationshipOrder.Count == 0)
                return null;
            return _relationships[
                _relationshipOrder[random.Next(_relationshipOrder.Count)]];
        }

        public IEnumerable<Guid> GetOutgoing(Guid entityId) =>
            _outgoing.TryGetValue(entityId, out var values)
                ? values
                : Array.Empty<Guid>();

        public IEnumerable<Guid> GetIncoming(Guid entityId) =>
            _incoming.TryGetValue(entityId, out var values)
                ? values
                : Array.Empty<Guid>();

        public IEnumerable<Guid> GetIncident(Guid entityId)
        {
            var result = new HashSet<Guid>();

            if (_outgoing.TryGetValue(entityId, out var outgoing))
                result.UnionWith(outgoing);

            if (_incoming.TryGetValue(entityId, out var incoming))
                result.UnionWith(incoming);

            return result;
        }

        public IEnumerable<Guid> GetNeighbors(Guid entityId)
        {
            var result = new HashSet<Guid>();

            foreach (var relationshipId in GetIncident(entityId))
            {
                if (!_relationships.TryGetValue(
                        relationshipId,
                        out var relationship))
                {
                    continue;
                }

                if (relationship.SourceId == entityId &&
                    relationship.TargetId != entityId)
                {
                    result.Add(relationship.TargetId);
                }
                else if (relationship.TargetId == entityId &&
                         relationship.SourceId != entityId)
                {
                    result.Add(relationship.SourceId);
                }
            }

            return result;
        }

        public bool IsReachable(
            Guid sourceId,
            Guid targetId)
        {
            if (!ContainsEntity(sourceId) ||
                !ContainsEntity(targetId))
            {
                return false;
            }

            if (sourceId == targetId)
                return true;

            var visited = new HashSet<Guid> { sourceId };
            var queue = new Queue<Guid>();
            queue.Enqueue(sourceId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var relationshipId in GetOutgoing(current))
                {
                    if (!_relationships.TryGetValue(
                            relationshipId,
                            out var relationship))
                    {
                        continue;
                    }

                    var next = relationship.TargetId;

                    if (next == targetId)
                        return true;

                    if (visited.Add(next))
                        queue.Enqueue(next);
                }
            }

            return false;
        }

        public IEnumerable<Guid> Traverse(Guid sourceId)
        {
            if (!ContainsEntity(sourceId))
                return Array.Empty<Guid>();

            var result = new List<Guid>();
            var visited = new HashSet<Guid> { sourceId };
            var queue = new Queue<Guid>();
            queue.Enqueue(sourceId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var relationshipId in GetOutgoing(current))
                {
                    if (!_relationships.TryGetValue(
                            relationshipId,
                            out var relationship))
                    {
                        continue;
                    }

                    var target = relationship.TargetId;

                    if (!visited.Add(target))
                        continue;

                    result.Add(target);
                    queue.Enqueue(target);
                }
            }

            return result;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (_entityOrder.Count != _entities.Count ||
                _entityIndexes.Count != _entities.Count)
            {
                errors.Add("Reference entity storage counts disagree.");
            }

            for (var index = 0; index < _entityOrder.Count; index++)
            {
                var id = _entityOrder[index];

                if (!_entities.ContainsKey(id))
                    errors.Add($"Reference entity order contains unknown '{id:D}'.");

                if (!_entityIndexes.TryGetValue(id, out var storedIndex) ||
                    storedIndex != index)
                {
                    errors.Add($"Reference entity index mismatch for '{id:D}'.");
                }
            }

            if (_relationshipOrder.Count != _relationships.Count ||
                _relationshipIndexes.Count != _relationships.Count)
            {
                errors.Add("Reference relationship storage counts disagree.");
            }

            for (var index = 0; index < _relationshipOrder.Count; index++)
            {
                var id = _relationshipOrder[index];

                if (!_relationships.ContainsKey(id))
                    errors.Add($"Reference relationship order contains unknown '{id:D}'.");

                if (!_relationshipIndexes.TryGetValue(id, out var storedIndex) ||
                    storedIndex != index)
                {
                    errors.Add($"Reference relationship index mismatch for '{id:D}'.");
                }
            }

            foreach (var pair in _relationships)
            {
                var relationship = pair.Value;

                if (!_entities.ContainsKey(relationship.SourceId))
                    errors.Add($"Reference relationship '{relationship.Id:D}' has missing source.");

                if (!_entities.ContainsKey(relationship.TargetId))
                    errors.Add($"Reference relationship '{relationship.Id:D}' has missing target.");

                if (!_outgoing.TryGetValue(
                        relationship.SourceId,
                        out var outgoing) ||
                    !outgoing.Contains(relationship.Id))
                {
                    errors.Add($"Reference relationship '{relationship.Id:D}' missing from outgoing storage.");
                }

                if (!_incoming.TryGetValue(
                        relationship.TargetId,
                        out var incoming) ||
                    !incoming.Contains(relationship.Id))
                {
                    errors.Add($"Reference relationship '{relationship.Id:D}' missing from incoming storage.");
                }
            }

            foreach (var pair in _outgoing)
            {
                foreach (var id in pair.Value)
                {
                    if (!_relationships.TryGetValue(id, out var relationship))
                    {
                        errors.Add(
                            $"Reference outgoing storage for '{pair.Key:D}' contains stale '{id:D}'.");
                        continue;
                    }

                    if (relationship.SourceId != pair.Key)
                    {
                        errors.Add(
                            $"Reference outgoing storage for '{pair.Key:D}' contains '{id:D}' with wrong source.");
                    }
                }
            }

            foreach (var pair in _incoming)
            {
                foreach (var id in pair.Value)
                {
                    if (!_relationships.TryGetValue(id, out var relationship))
                    {
                        errors.Add(
                            $"Reference incoming storage for '{pair.Key:D}' contains stale '{id:D}'.");
                        continue;
                    }

                    if (relationship.TargetId != pair.Key)
                    {
                        errors.Add(
                            $"Reference incoming storage for '{pair.Key:D}' contains '{id:D}' with wrong target.");
                    }
                }
            }

            return errors;
        }

        private void RemoveEntityFromOrder(Guid id)
        {
            var index = _entityIndexes[id];
            var last = _entityOrder.Count - 1;
            var lastId = _entityOrder[last];

            if (index != last)
            {
                _entityOrder[index] = lastId;
                _entityIndexes[lastId] = index;
            }

            _entityOrder.RemoveAt(last);
            _entityIndexes.Remove(id);
        }

        private void RemoveRelationshipFromOrder(Guid id)
        {
            var index = _relationshipIndexes[id];
            var last = _relationshipOrder.Count - 1;
            var lastId = _relationshipOrder[last];

            if (index != last)
            {
                _relationshipOrder[index] = lastId;
                _relationshipIndexes[lastId] = index;
            }

            _relationshipOrder.RemoveAt(last);
            _relationshipIndexes.Remove(id);
        }
    }

    private sealed record ReferenceEntity(
        Guid Id,
        string Name,
        string Type,
        long IntegerValue);

    private sealed record ReferenceRelationship(
        Guid Id,
        Guid SourceId,
        Guid TargetId,
        string Type);

    private sealed class RecentOperations
    {
        private readonly int _capacity;
        private readonly Queue<JournalEntry> _entries = new();

        public RecentOperations(int capacity) => _capacity = capacity;
        public IEnumerable<JournalEntry> Items => _entries;

        public void Add(
            int number,
            FuzzOperation operation)
        {
            if (_entries.Count == _capacity)
                _entries.Dequeue();

            _entries.Enqueue(
                new JournalEntry(number, operation));
        }
    }

    private sealed record JournalEntry(
        int Number,
        FuzzOperation Operation);

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(int seed)
        {
            _state =
                unchecked(
                    (ulong)(uint)seed +
                    0x9E3779B97F4A7C15UL);

            if (_state == 0)
                _state = 0x9E3779B97F4A7C15UL;
        }

        private ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;

            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));

            return (int)(NextUInt64() % (ulong)exclusiveMax);
        }

        public long NextLong(
            long minimumInclusive,
            long maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));

            var range =
                unchecked(
                    (ulong)(maximumExclusive - minimumInclusive));

            return unchecked(
                (long)(NextUInt64() % range)) +
                minimumInclusive;
        }

        public Guid NextGuid()
        {
            Span<byte> bytes = stackalloc byte[16];

            for (var index = 0; index < bytes.Length; index += 8)
            {
                var value = NextUInt64();

                for (var offset = 0; offset < 8; offset++)
                    bytes[index + offset] =
                        (byte)(value >> (offset * 8));
            }

            return new Guid(bytes);
        }
    }
}
