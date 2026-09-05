using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sorophy.Engine.Graph;

namespace Sorophy.Engine.StressTests;

public static class HighDegreeTopologyTests
{
    public static int Run(
        int seed = 12345,
        int operations = 10_000)
    {
        if (operations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "Operation count must be greater than zero.");
        }

        var scalePoints =
            BuildScalePoints(
                operations);

        PrintHeader(
            seed,
            operations,
            scalePoints);

        var generator =
            new WorkloadGenerator(
                seed);

        var results =
            new List<ScaleResult>();

        var allPassed =
            true;

        foreach (var scale in scalePoints)
        {
            Console.WriteLine();

            Console.WriteLine(
                "────────────────────────────────────────────────────────────");

            Console.WriteLine(
                $"HIGH-DEGREE SCALE POINT: {scale:N0} INCIDENT RELATIONSHIPS");

            Console.WriteLine(
                "────────────────────────────────────────────────────────────");

            var topology =
                generator.CreateTopology(
                    scale);

            var graph =
                BuildGraph(
                    topology);

            /*
             * =========================================================
             * STRUCTURAL COUNTS
             * =========================================================
             */

            var expectedRelationshipCount =
                topology.Relationships.Count;

            var entityCount =
                graph.Entities.Count;

            var relationshipCount =
                graph.Relationships.Count;

            var structuralCountsPassed =
                entityCount ==
                    topology.Entities.Count &&
                relationshipCount ==
                    expectedRelationshipCount;

            Console.WriteLine();

            Console.WriteLine(
                $"  Entities:                {entityCount,10:N0}");

            Console.WriteLine(
                $"  Relationships:           {relationshipCount,10:N0}");

            Console.WriteLine(
                $"  Expected relationships:  {expectedRelationshipCount,10:N0}");

            Console.WriteLine(
                $"  Structural counts:       {(structuralCountsPassed ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * OUTGOING QUERY
             * =========================================================
             */

            var outgoingExpected =
                topology.OutgoingRelationships.Count;

            var outgoingWarmup =
                graph.GetOutgoingRelationships(
                        topology.Hub.Id)
                    .Count();

            var outgoingCorrect =
                outgoingWarmup ==
                outgoingExpected;

            var outgoingMeasurement =
                MeasureQuery(
                    () =>
                        graph.GetOutgoingRelationships(
                                topology.Hub.Id)
                            .Count(),
                    expectedResult:
                        outgoingExpected);

            Console.WriteLine();

            Console.WriteLine(
                $"  Outgoing / Hub ({outgoingExpected:N0})");

            Console.WriteLine(
                $"    Result count:          {outgoingWarmup,10:N0}");

            Console.WriteLine(
                $"    Time:                  {outgoingMeasurement.Elapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Throughput:            {outgoingMeasurement.OperationsPerSecond,10:N0} queries/s");

            Console.WriteLine(
                $"    Correctness:           {(outgoingCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * INCOMING QUERY
             * =========================================================
             */

            var incomingExpected =
                topology.IncomingRelationships.Count;

            var incomingWarmup =
                graph.GetIncomingRelationships(
                        topology.Hub.Id)
                    .Count();

            var incomingCorrect =
                incomingWarmup ==
                incomingExpected;

            var incomingMeasurement =
                MeasureQuery(
                    () =>
                        graph.GetIncomingRelationships(
                                topology.Hub.Id)
                            .Count(),
                    expectedResult:
                        incomingExpected);

            Console.WriteLine();

            Console.WriteLine(
                $"  Incoming / Hub ({incomingExpected:N0})");

            Console.WriteLine(
                $"    Result count:          {incomingWarmup,10:N0}");

            Console.WriteLine(
                $"    Time:                  {incomingMeasurement.Elapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Throughput:            {incomingMeasurement.OperationsPerSecond,10:N0} queries/s");

            Console.WriteLine(
                $"    Correctness:           {(incomingCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * ALL INCIDENT RELATIONSHIPS
             * =========================================================
             */

            var incidentExpected =
                topology.ExpectedDistinctIncidentRelationships;

            var incidentWarmup =
                graph.GetRelationships(
                        topology.Hub.Id)
                    .Count();

            var incidentCorrect =
                incidentWarmup ==
                incidentExpected;

            var incidentMeasurement =
                MeasureQuery(
                    () =>
                        graph.GetRelationships(
                                topology.Hub.Id)
                            .Count(),
                    expectedResult:
                        incidentExpected);

            Console.WriteLine();

            Console.WriteLine(
                $"  All Relations / Hub ({incidentExpected:N0})");

            Console.WriteLine(
                $"    Result count:          {incidentWarmup,10:N0}");

            Console.WriteLine(
                $"    Time:                  {incidentMeasurement.Elapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Throughput:            {incidentMeasurement.OperationsPerSecond,10:N0} queries/s");

            Console.WriteLine(
                $"    Correctness:           {(incidentCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * NEIGHBOR QUERY
             * =========================================================
             *
             * Parallel relationships intentionally do NOT increase the
             * expected neighbor count.
             */

            var neighborExpected =
                topology.ExpectedDistinctNeighbors;

            var neighborWarmup =
                graph.GetNeighbors(
                        topology.Hub.Id)
                    .Count();

            var neighborCorrect =
                neighborWarmup ==
                neighborExpected;

            var neighborMeasurement =
                MeasureQuery(
                    () =>
                        graph.GetNeighbors(
                                topology.Hub.Id)
                            .Count(),
                    expectedResult:
                        neighborExpected);

            Console.WriteLine();

            Console.WriteLine(
                $"  Neighbors / Hub ({neighborExpected:N0})");

            Console.WriteLine(
                $"    Result count:          {neighborWarmup,10:N0}");

            Console.WriteLine(
                $"    Time:                  {neighborMeasurement.Elapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Throughput:            {neighborMeasurement.OperationsPerSecond,10:N0} queries/s");

            Console.WriteLine(
                $"    Correctness:           {(neighborCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * REPEATED QUERY STABILITY
             * =========================================================
             *
             * Run a small number of repeated high-degree queries. This
             * isn't intended as a microbenchmark; it checks that repeated
             * traversal of a massive adjacency list remains stable.
             */

            const int repeatedQueries =
                10;

            var repeatedStarted =
                Stopwatch.GetTimestamp();

            var repeatedIncidentCount =
                0;

            for (var index = 0;
                 index < repeatedQueries;
                 index++)
            {
                repeatedIncidentCount +=
                    graph.GetRelationships(
                            topology.Hub.Id)
                        .Count();
            }

            var repeatedElapsedTicks =
                Stopwatch.GetTimestamp() -
                repeatedStarted;

            var repeatedElapsed =
                TimeSpan.FromSeconds(
                    (double)repeatedElapsedTicks /
                    Stopwatch.Frequency);

            var repeatedCorrect =
                repeatedIncidentCount ==
                incidentExpected *
                repeatedQueries;

            Console.WriteLine();

            Console.WriteLine(
                $"  Repeated Incident Queries ({repeatedQueries}x)");

            Console.WriteLine(
                $"    Total elapsed:         {repeatedElapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Queries/sec:           {repeatedQueries / Math.Max(repeatedElapsed.TotalSeconds, double.Epsilon),10:N0}");

            Console.WriteLine(
                $"    Correctness:           {(repeatedCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * SELF-LINKS
             * =========================================================
             *
             * Self-links must appear once in incoming and once in
             * outgoing, but only once in combined GetRelationships().
             */

            var selfLinks =
                topology.SelfRelationships;

            var selfOutgoing =
                graph.GetOutgoingRelationships(
                        topology.Hub.Id)
                    .Count(
                        relationship =>
                            relationship.SourceId ==
                            topology.Hub.Id &&
                            relationship.TargetId ==
                            topology.Hub.Id);

            var selfIncoming =
                graph.GetIncomingRelationships(
                        topology.Hub.Id)
                    .Count(
                        relationship =>
                            relationship.SourceId ==
                            topology.Hub.Id &&
                            relationship.TargetId ==
                            topology.Hub.Id);

            var selfCombined =
                graph.GetRelationships(
                        topology.Hub.Id)
                    .Count(
                        relationship =>
                            relationship.SourceId ==
                            topology.Hub.Id &&
                            relationship.TargetId ==
                            topology.Hub.Id);

            var selfLinksCorrect =
                selfOutgoing ==
                    selfLinks.Count &&
                selfIncoming ==
                    selfLinks.Count &&
                selfCombined ==
                    selfLinks.Count;

            Console.WriteLine();

            Console.WriteLine(
                $"  Self Relationships ({selfLinks.Count:N0})");

            Console.WriteLine(
                $"    Outgoing occurrences:  {selfOutgoing,10:N0}");

            Console.WriteLine(
                $"    Incoming occurrences:  {selfIncoming,10:N0}");

            Console.WriteLine(
                $"    Combined occurrences:  {selfCombined,10:N0}");

            Console.WriteLine(
                $"    Correctness:           {(selfLinksCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * PARALLEL RELATIONSHIPS
             * =========================================================
             *
             * Several relationships may connect the hub with the same
             * neighbor. All relationships must be retained, but neighbor
             * queries must deduplicate the entity.
             */

            var parallelExpected =
                topology.ParallelRelationships.Count;

            var parallelStored =
                graph.Relationships.Values.Count(
                    relationship =>
                        relationship.SourceId ==
                        topology.Hub.Id &&
                        relationship.TargetId ==
                        topology.ParallelTarget.Id);

            var parallelNeighborOccurrences =
                graph.GetNeighbors(
                        topology.Hub.Id)
                    .Count(
                        entity =>
                            entity.Id ==
                            topology.ParallelTarget.Id);

            var parallelCorrect =
                parallelStored ==
                    parallelExpected &&
                parallelNeighborOccurrences ==
                    1;

            Console.WriteLine();

            Console.WriteLine(
                $"  Parallel Relationships ({parallelExpected:N0})");

            Console.WriteLine(
                $"    Stored relationships:  {parallelStored,10:N0}");

            Console.WriteLine(
                $"    Neighbor occurrences:  {parallelNeighborOccurrences,10:N0}");

            Console.WriteLine(
                $"    Correctness:           {(parallelCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * VALIDATION BEFORE REMOVAL
             * =========================================================
             */

            var validationBefore =
                graph.Validate();

            var validationBeforePassed =
                validationBefore.Count == 0;

            Console.WriteLine();

            Console.WriteLine(
                $"  Validation before removal: " +
                $"{(validationBeforePassed ? "PASS" : "FAIL")}");

            if (!validationBeforePassed)
            {
                PrintValidationErrors(
                    validationBefore);
            }

            /*
             * =========================================================
             * HIGH-DEGREE REMOVAL
             * =========================================================
             *
             * Remove the hub itself.
             *
             * This is an intentionally nasty test because RemoveEntity()
             * must discover and remove every connected relationship using
             * the adjacency index.
             */
            var removalStarted =
                Stopwatch.GetTimestamp();

            var removed =
                graph.RemoveEntity(
                    topology.Hub.Id);

            var removalElapsedTicks =
                Stopwatch.GetTimestamp() -
                removalStarted;

            var removalElapsed =
                TimeSpan.FromSeconds(
                    (double)removalElapsedTicks /
                    Stopwatch.Frequency);

            var relationshipsAfterRemoval =
                graph.Relationships.Count;

            var expectedRelationshipsAfterRemoval =
                topology.Relationships.Count -
                topology.IncidentRelationships.Count;

            var removalCorrect =
                removed &&
                relationshipsAfterRemoval ==
                    expectedRelationshipsAfterRemoval;

            Console.WriteLine();

            Console.WriteLine(
                $"  Hub Removal ({topology.IncidentRelationships.Count:N0} incident relationships)");

            Console.WriteLine(
                $"    Time:                  {removalElapsed.TotalMilliseconds,10:F3} ms");

            Console.WriteLine(
                $"    Relationships before:  {topology.Relationships.Count,10:N0}");

            Console.WriteLine(
                $"    Relationships after:   {relationshipsAfterRemoval,10:N0}");

            Console.WriteLine(
                $"    Expected after:        {expectedRelationshipsAfterRemoval,10:N0}");

            Console.WriteLine(
                $"    Result:                {(removalCorrect ? "PASS" : "FAIL")}");

            /*
             * =========================================================
             * VALIDATION AFTER REMOVAL
             * =========================================================
             */

            var validationAfter =
                graph.Validate();

            var validationAfterPassed =
                validationAfter.Count == 0;

            Console.WriteLine(
                $"    Final validation:      {(validationAfterPassed ? "PASS" : "FAIL")}");

            if (!validationAfterPassed)
            {
                PrintValidationErrors(
                    validationAfter);
            }

            var scalePassed =
                structuralCountsPassed &&
                outgoingCorrect &&
                incomingCorrect &&
                incidentCorrect &&
                neighborCorrect &&
                repeatedCorrect &&
                selfLinksCorrect &&
                parallelCorrect &&
                validationBeforePassed &&
                removalCorrect &&
                validationAfterPassed;

            allPassed &=
                scalePassed;

            Console.WriteLine();

            Console.WriteLine(
                $"  Scale-point result:      {(scalePassed ? "PASS" : "FAIL")}");

            results.Add(
                new ScaleResult(
                    scale,
                    topology.OutgoingRelationships.Count,
                    topology.IncomingRelationships.Count,
                    topology.ExpectedDistinctIncidentRelationships,
                    topology.ExpectedDistinctNeighbors,
                    outgoingMeasurement.Elapsed,
                    incomingMeasurement.Elapsed,
                    incidentMeasurement.Elapsed,
                    neighborMeasurement.Elapsed,
                    removalElapsed,
                    scalePassed));
        }

        PrintSummary(
            results);

        return allPassed
            ? 0
            : 1;
    }

    /*
     * =============================================================
     * GRAPH CONSTRUCTION
     * =============================================================
     */

    private static SorophyGraph BuildGraph(
        HighDegreeTopology topology)
    {
        var graph =
            new SorophyGraph();

        for (var index = 0;
             index < topology.Entities.Count;
             index++)
        {
            graph.AddEntity(
                topology.Entities[index]);
        }

        for (var index = 0;
             index < topology.Relationships.Count;
             index++)
        {
            graph.AddRelationship(
                topology.Relationships[index]);
        }

        return graph;
    }

    /*
     * =============================================================
     * QUERY MEASUREMENT
     * =============================================================
     */

    private static QueryMeasurement MeasureQuery(
        Func<int> query,
        int expectedResult)
    {
        /*
         * One warm-up execution.
         */
        var warmupResult =
            query();

        if (warmupResult !=
            expectedResult)
        {
            throw new InvalidOperationException(
                $"High-degree warm-up query returned {warmupResult}, " +
                $"expected {expectedResult}.");
        }

        const int repetitions =
            5;

        var started =
            Stopwatch.GetTimestamp();

        var result =
            0;

        for (var index = 0;
             index < repetitions;
             index++)
        {
            result =
                query();
        }

        var elapsedTicks =
            Stopwatch.GetTimestamp() -
            started;

        if (result !=
            expectedResult)
        {
            throw new InvalidOperationException(
                $"High-degree query returned {result}, " +
                $"expected {expectedResult}.");
        }

        var elapsed =
            TimeSpan.FromSeconds(
                Math.Max(
                    (double)elapsedTicks /
                    Stopwatch.Frequency /
                    repetitions,
                    double.Epsilon));

        return new QueryMeasurement(
            elapsed,
            1.0 /
                elapsed.TotalSeconds);
    }

    /*
     * =============================================================
     * SCALE POINTS
     * =============================================================
     */

    private static IReadOnlyList<int>
        BuildScalePoints(
            int operations)
    {
        var scales =
            new List<int>();

        AddScale(
            scales,
            1_000,
            operations);

        AddScale(
            scales,
            10_000,
            operations);

        AddScale(
            scales,
            50_000,
            operations);

        AddScale(
            scales,
            100_000,
            operations);

        if (operations > 100_000 &&
            !scales.Contains(
                operations))
        {
            scales.Add(
                operations);
        }

        return scales;
    }

    private static void AddScale(
        List<int> scales,
        int scale,
        int operations)
    {
        if (operations >= scale &&
            !scales.Contains(
                scale))
        {
            scales.Add(
                scale);
        }
    }

    /*
     * =============================================================
     * VALIDATION REPORTING
     * =============================================================
     */

    private static void PrintValidationErrors(
        IReadOnlyList<string> errors)
    {
        var count =
            Math.Min(
                errors.Count,
                8);

        for (var index = 0;
             index < count;
             index++)
        {
            Console.WriteLine(
                $"      {errors[index]}");
        }

        if (errors.Count > count)
        {
            Console.WriteLine(
                $"      ... and {errors.Count - count:N0} more errors.");
        }
    }

    /*
     * =============================================================
     * SUMMARY
     * =============================================================
     */

    private static void PrintSummary(
        IReadOnlyList<ScaleResult> results)
    {
        Console.WriteLine();

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine(
            "                   HIGH-DEGREE SUMMARY");

        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");

        Console.WriteLine();

        Console.WriteLine(
            "Scale | Outgoing | Incoming | Incident | Neighbors | Hub Remove | Result");

        Console.WriteLine(
            "------+----------+----------+----------+-----------+-------------+--------");

        for (var index = 0;
             index < results.Count;
             index++)
        {
            var result =
                results[index];

            Console.WriteLine(
                $"{result.Scale,5:N0} |" +
                $"{result.OutgoingCount,9:N0} |" +
                $"{result.IncomingCount,9:N0} |" +
                $"{result.IncidentCount,9:N0} |" +
                $"{result.NeighborCount,10:N0} |" +
                $"{result.RemovalElapsed.TotalMilliseconds,10:F2} ms | " +
                $"{(result.Passed ? "PASS" : "FAIL"),6}");
        }

        Console.WriteLine();

        Console.WriteLine(
            "Query timings:");

        for (var index = 0;
             index < results.Count;
             index++)
        {
            var result =
                results[index];

            Console.WriteLine(
                $"  {result.Scale,6:N0} incident relationships → " +
                $"Outgoing {result.OutgoingElapsed.TotalMilliseconds,8:F3} ms | " +
                $"Incoming {result.IncomingElapsed.TotalMilliseconds,8:F3} ms | " +
                $"All {result.IncidentElapsed.TotalMilliseconds,8:F3} ms | " +
                $"Neighbors {result.NeighborElapsed.TotalMilliseconds,8:F3} ms");
        }

        Console.WriteLine();

        Console.WriteLine(
            "Interpretation:");

        Console.WriteLine(
            "  • The hub intentionally has extremely high incident degree.");

        Console.WriteLine(
            "  • Parallel relationships are retained individually.");

        Console.WriteLine(
            "  • Neighbor queries must deduplicate parallel relationships.");

        Console.WriteLine(
            "  • Self-links must remain consistent across both directions.");

        Console.WriteLine(
            "  • Hub removal exercises large-scale adjacency unlinking.");

        Console.WriteLine(
            "  • Final validation checks the graph after high-degree destruction.");

        Console.WriteLine();
    }

    /*
     * =============================================================
     * HEADER
     * =============================================================
     */

    private static void PrintHeader(
        int seed,
        int operations,
        IReadOnlyList<int> scalePoints)
    {
        Console.WriteLine();

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                 SOROPHY ENGINE HIGH-DEGREE TEST                ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");

        Console.WriteLine(
            $"║ Seed:           {seed,-43}║");

        Console.WriteLine(
            $"║ Operations:     {operations,-43}║");

        Console.WriteLine(
            "║ Topology:       Extreme hub / high-degree graph           ║");

        Console.WriteLine(
            "║ Purpose:        Torture adjacency under huge degree      ║");

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");

        Console.WriteLine();

        Console.WriteLine(
            "Scale points:");

        for (var index = 0;
             index < scalePoints.Count;
             index++)
        {
            Console.WriteLine(
                $"  {scalePoints[index]:N0} incident relationships");
        }

        Console.WriteLine();
    }

    /*
     * =============================================================
     * DATA STRUCTURES
     * =============================================================
     */

    private sealed record QueryMeasurement(
        TimeSpan Elapsed,
        double OperationsPerSecond);

    private sealed record ScaleResult(
        int Scale,
        int OutgoingCount,
        int IncomingCount,
        int IncidentCount,
        int NeighborCount,
        TimeSpan OutgoingElapsed,
        TimeSpan IncomingElapsed,
        TimeSpan IncidentElapsed,
        TimeSpan NeighborElapsed,
        TimeSpan RemovalElapsed,
        bool Passed);

    private sealed class HighDegreeTopology
    {
        public required SorophyEntity Hub { get; init; }

        public required SorophyEntity ParallelTarget { get; init; }

        public required List<SorophyEntity> Entities { get; init; }

        public required List<SorophyRelationship> Relationships { get; init; }

        public required List<SorophyRelationship> OutgoingRelationships { get; init; }

        public required List<SorophyRelationship> IncomingRelationships { get; init; }

        public required List<SorophyRelationship> IncidentRelationships { get; init; }

        public required List<SorophyRelationship> SelfRelationships { get; init; }

        public required List<SorophyRelationship> ParallelRelationships { get; init; }

        public int ExpectedDistinctIncidentRelationships =>
            IncidentRelationships.Count;

        public int ExpectedDistinctNeighbors { get; init; }
    }

    /*
     * =============================================================
     * DETERMINISTIC WORKLOAD GENERATOR
     * =============================================================
     */

    private sealed class WorkloadGenerator
    {
        private readonly Random _random;

        public WorkloadGenerator(
            int seed)
        {
            _random =
                new Random(seed);
        }

        public HighDegreeTopology CreateTopology(
            int incidentRelationshipCount)
        {
            if (incidentRelationshipCount < 10)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incidentRelationshipCount),
                    "High-degree scale must be at least 10.");
            }

            var hub =
                new SorophyEntity
                {
                    Id =
                        CreateGuid(),

                    Name =
                        "HIGH-DEGREE-HUB",

                    Type =
                        "HighDegreeBenchmark"
                };

            var parallelTarget =
                new SorophyEntity
                {
                    Id =
                        CreateGuid(),

                    Name =
                        "PARALLEL-TARGET",

                    Type =
                        "HighDegreeBenchmark"
                };

            var entities =
                new List<SorophyEntity>
                {
                    hub,
                    parallelTarget
                };

            var relationships =
                new List<SorophyRelationship>(
                    incidentRelationshipCount);

            var outgoing =
                new List<SorophyRelationship>();

            var incoming =
                new List<SorophyRelationship>();

            var incident =
                new List<SorophyRelationship>();

            var selfRelationships =
                new List<SorophyRelationship>();

            var parallelRelationships =
                new List<SorophyRelationship>();

            /*
             * Reserve a small fixed block for self-links.
             */
            var selfCount =
                Math.Min(
                    10,
                    Math.Max(
                        1,
                        incidentRelationshipCount /
                        100));

            /*
             * Reserve a small fixed block for parallel relationships.
             */
            var parallelCount =
                Math.Min(
                    25,
                    Math.Max(
                        5,
                        incidentRelationshipCount /
                        100));

            var remaining =
                incidentRelationshipCount -
                selfCount -
                parallelCount;

            /*
             * Split the remaining incident degree roughly evenly:
             *
             * half outgoing
             * half incoming
             */
            var outgoingCount =
                remaining / 2;

            var incomingCount =
                remaining -
                outgoingCount;

            /*
             * =========================================================
             * OUTGOING RELATIONSHIPS
             * =========================================================
             */

            for (var index = 0;
                 index < outgoingCount;
                 index++)
            {
                var target =
                    new SorophyEntity
                    {
                        Id =
                            CreateGuid(),

                        Name =
                            $"OutgoingTarget-{index:N0}",

                        Type =
                            "HighDegreeBenchmark"
                    };

                entities.Add(
                    target);

                var relationship =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "high-degree-outgoing",

                        SourceId =
                            hub.Id,

                        TargetId =
                            target.Id
                    };

                relationships.Add(
                    relationship);

                outgoing.Add(
                    relationship);

                incident.Add(
                    relationship);
            }

            /*
             * =========================================================
             * INCOMING RELATIONSHIPS
             * =========================================================
             */

            for (var index = 0;
                 index < incomingCount;
                 index++)
            {
                var source =
                    new SorophyEntity
                    {
                        Id =
                            CreateGuid(),

                        Name =
                            $"IncomingSource-{index:N0}",

                        Type =
                            "HighDegreeBenchmark"
                    };

                entities.Add(
                    source);

                var relationship =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "high-degree-incoming",

                        SourceId =
                            source.Id,

                        TargetId =
                            hub.Id
                    };

                relationships.Add(
                    relationship);

                incoming.Add(
                    relationship);

                incident.Add(
                    relationship);
            }

            /*
             * =========================================================
             * SELF RELATIONSHIPS
             * =========================================================
             */

            for (var index = 0;
                 index < selfCount;
                 index++)
            {
                var relationship =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "high-degree-self",

                        SourceId =
                            hub.Id,

                        TargetId =
                            hub.Id
                    };

                relationships.Add(
                    relationship);

                outgoing.Add(
                    relationship);

                incoming.Add(
                    relationship);

                incident.Add(
                    relationship);

                selfRelationships.Add(
                    relationship);
            }

            /*
             * =========================================================
             * PARALLEL RELATIONSHIPS
             * =========================================================
             *
             * The same source/target pair gets many distinct relationship
             * IDs. This stresses both adjacency storage and neighbor
             * deduplication.
             */

            for (var index = 0;
                 index < parallelCount;
                 index++)
            {
                var relationship =
                    new SorophyRelationship
                    {
                        Id =
                            CreateGuid(),

                        Type =
                            "high-degree-parallel",

                        SourceId =
                            hub.Id,

                        TargetId =
                            parallelTarget.Id
                    };

                relationships.Add(
                    relationship);

                outgoing.Add(
                    relationship);

                incident.Add(
                    relationship);

                parallelRelationships.Add(
                    relationship);
            }

            /*
             * Ensure the parallel target itself remains in the graph.
             */
            if (!entities.Contains(
                    parallelTarget))
            {
                entities.Add(
                    parallelTarget);
            }

            /*
             * Distinct neighbor count:
             *
             * all generated source/target entities, except hub itself,
             * plus the parallel target exactly once.
             */
            var distinctNeighbors =
                new HashSet<Guid>();

            for (var index = 0;
                 index < outgoing.Count;
                 index++)
            {
                var relationship =
                    outgoing[index];

                if (relationship.SourceId ==
                    hub.Id &&
                    relationship.TargetId !=
                    hub.Id)
                {
                    distinctNeighbors.Add(
                        relationship.TargetId);
                }
            }

            for (var index = 0;
                 index < incoming.Count;
                 index++)
            {
                var relationship =
                    incoming[index];

                if (relationship.TargetId ==
                    hub.Id &&
                    relationship.SourceId !=
                    hub.Id)
                {
                    distinctNeighbors.Add(
                        relationship.SourceId);
                }
            }

            return new HighDegreeTopology
            {
                Hub =
                    hub,

                ParallelTarget =
                    parallelTarget,

                Entities =
                    entities,

                Relationships =
                    relationships,

                OutgoingRelationships =
                    outgoing,

                IncomingRelationships =
                    incoming,

                IncidentRelationships =
                    incident,

                SelfRelationships =
                    selfRelationships,

                ParallelRelationships =
                    parallelRelationships,

                ExpectedDistinctNeighbors =
                    distinctNeighbors.Count
            };
        }

        private Guid CreateGuid()
        {
            Span<byte> bytes =
                stackalloc byte[16];

            _random.NextBytes(
                bytes);

            return new Guid(
                bytes);
        }
    }
}