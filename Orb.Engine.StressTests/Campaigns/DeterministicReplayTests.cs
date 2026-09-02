using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orb.Engine.Graph;
using Orb.Engine.Types;

namespace Orb.Engine.StressTests;

public static class DeterministicReplayTests
{
    public static int Run(
        int seed = 12345,
        int operations = 10_000,
        int auditInterval = 1_000)
    {
        if (operations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "Operation count cannot be negative.");
        }

        if (auditInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(auditInterval),
                "Audit interval must be greater than zero.");
        }

        PrintHeader(seed, operations, auditInterval);

        Console.WriteLine(
            "Generating deterministic operation journals...");

        var firstJournal =
            GenerateJournal(seed, operations);

        var secondJournal =
            GenerateJournal(seed, operations);

        Console.WriteLine(
            $"  Journal A: {firstJournal.Count:N0} operations");

        Console.WriteLine(
            $"  Journal B: {secondJournal.Count:N0} operations");

        Console.WriteLine();

        var journalsMatch =
            CompareJournals(
                firstJournal,
                secondJournal,
                out var mismatchIndex);

        if (journalsMatch)
        {
            Console.WriteLine(
                "  Journal determinism ................. PASS");
        }
        else
        {
            Console.WriteLine(
                "  Journal determinism ................. FAIL");

            Console.WriteLine();
            Console.WriteLine(
                $"First difference at operation {mismatchIndex + 1:N0}.");

            Console.WriteLine(
                $"A: {firstJournal[mismatchIndex]}");

            Console.WriteLine(
                $"B: {secondJournal[mismatchIndex]}");

            Console.WriteLine();
            Console.WriteLine("STATUS: FAIL");

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine(
            "Replaying identical journal against two independent graphs...");
        Console.WriteLine();

        var firstRun =
            new ReplayRun("A");

        var secondRun =
            new ReplayRun("B");

        for (var index = 0;
             index < firstJournal.Count;
             index++)
        {
            var operation =
                firstJournal[index];

            try
            {
                firstRun.Execute(operation);
                secondRun.Execute(operation);

                if (index % auditInterval == 0 ||
                    index == firstJournal.Count - 1)
                {
                    firstRun.Audit(index + 1);
                    secondRun.Audit(index + 1);

                    var firstFingerprint =
                        firstRun.GetFingerprint();

                    var secondFingerprint =
                        secondRun.GetFingerprint();

                    if (!string.Equals(
                            firstFingerprint,
                            secondFingerprint,
                            StringComparison.Ordinal))
                    {
                        return ReportFailure(
                            index,
                            operation,
                            "Two replay graphs diverged.",
                            firstRun,
                            secondRun);
                    }

                    Console.WriteLine(
                        $"  Audit {index + 1:N0}/{operations:N0} " +
                        $"→ PASS | " +
                        $"Entities: {firstRun.Graph.Entities.Count,5} | " +
                        $"Relationships: {firstRun.Graph.Relationships.Count,5}");
                }
            }
            catch (Exception exception)
            {
                return ReportFailure(
                    index,
                    operation,
                    exception.Message,
                    firstRun,
                    secondRun);
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                 DETERMINISTIC REPLAY RESULT");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine(
            $"Operations replayed: {firstJournal.Count:N0}");

        Console.WriteLine(
            $"Final entities:      {firstRun.Graph.Entities.Count:N0}");

        Console.WriteLine(
            $"Final relationships: {firstRun.Graph.Relationships.Count:N0}");

        Console.WriteLine();

        Console.WriteLine(
            "Journal determinism:      PASS");

        Console.WriteLine(
            "Reference model:          PASS");

        Console.WriteLine(
            "Replay A/B equivalence:   PASS");

        Console.WriteLine(
            "Graph invariants:         PASS");

        Console.WriteLine();

        Console.WriteLine(
            "                    100% PASS");

        Console.WriteLine(
            "STATUS: DETERMINISTIC REPLAY VERIFIED");

        Console.WriteLine();

        return 0;
    }

    private static List<StressOperation> GenerateJournal(
        int seed,
        int operationCount)
    {
        var random =
            new DeterministicRandom(seed);

        var reference =
            new ReferenceGraph();

        var journal =
            new List<StressOperation>(
                operationCount);

        for (var index = 0;
             index < operationCount;
             index++)
        {
            var operation =
                OperationGenerator.Generate(
                    random,
                    reference);

            journal.Add(operation);

            reference.ApplyForGeneration(
                operation);
        }

        return journal;
    }

    private static bool CompareJournals(
        IReadOnlyList<StressOperation> first,
        IReadOnlyList<StressOperation> second,
        out int mismatchIndex)
    {
        if (first.Count != second.Count)
        {
            mismatchIndex =
                Math.Min(
                    first.Count,
                    second.Count);

            return false;
        }

        for (var index = 0;
             index < first.Count;
             index++)
        {
            if (!first[index].Equals(second[index]))
            {
                mismatchIndex = index;
                return false;
            }
        }

        mismatchIndex = -1;

        return true;
    }

    private static int ReportFailure(
        int operationIndex,
        StressOperation operation,
        string reason,
        ReplayRun first,
        ReplayRun second)
    {
        Console.WriteLine();
        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine(
            "                 DETERMINISTIC REPLAY FAILURE");
        Console.WriteLine(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine();

        Console.WriteLine(
            $"Operation: {operationIndex + 1:N0}");

        Console.WriteLine(
            $"Reason:    {reason}");

        Console.WriteLine();

        Console.WriteLine(
            "Operation:");

        Console.WriteLine(
            $"  {operation}");

        Console.WriteLine();

        Console.WriteLine(
            "GRAPH A:");

        Console.WriteLine(
            $"  Entities:      {first.Graph.Entities.Count:N0}");

        Console.WriteLine(
            $"  Relationships: {first.Graph.Relationships.Count:N0}");

        Console.WriteLine(
            $"  Fingerprint:   {first.GetFingerprint()}");

        Console.WriteLine();

        Console.WriteLine(
            "GRAPH B:");

        Console.WriteLine(
            $"  Entities:      {second.Graph.Entities.Count:N0}");

        Console.WriteLine(
            $"  Relationships: {second.Graph.Relationships.Count:N0}");

        Console.WriteLine(
            $"  Fingerprint:   {second.GetFingerprint()}");

        Console.WriteLine();

        Console.WriteLine(
            "Last operations:");

        first.PrintJournal();

        Console.WriteLine();

        Console.WriteLine(
            "STATUS: FAIL");

        Console.WriteLine();

        return 1;
    }

    private static void PrintHeader(
        int seed,
        int operations,
        int auditInterval)
    {
        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                ORB ENGINE DETERMINISTIC REPLAY             ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine(
            $"║ Seed:           {seed,-41} ║");
        Console.WriteLine(
            $"║ Operations:     {operations,-41} ║");
        Console.WriteLine(
            $"║ Audit interval: {auditInterval,-41} ║");
        Console.WriteLine(
            "║ Mode:           EXACT REPLAY                               ║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    private sealed class ReplayRun
    {
        private readonly string _name;

        private readonly List<StressOperation>
            _journal = new();

        public OrbGraph Graph { get; }

        public ReferenceGraph Reference { get; }

        public ReplayRun(string name)
        {
            _name = name;

            Graph =
                new OrbGraph();

            Reference =
                new ReferenceGraph();
        }

        public void Execute(
            StressOperation operation)
        {
            _journal.Add(operation);

            switch (operation.Kind)
            {
                case OperationKind.AddEntity:
                    ExecuteAddEntity(operation);
                    break;

                case OperationKind.RemoveEntity:
                    ExecuteRemoveEntity(operation);
                    break;

                case OperationKind.AddRelationship:
                    ExecuteAddRelationship(operation);
                    break;

                case OperationKind.RemoveRelationship:
                    ExecuteRemoveRelationship(operation);
                    break;

                case OperationKind.DuplicateEntity:
                    ExecuteDuplicateEntity(operation);
                    break;

                case OperationKind.DuplicateRelationship:
                    ExecuteDuplicateRelationship(operation);
                    break;

                case OperationKind.InvalidRelationship:
                    ExecuteInvalidRelationship(operation);
                    break;

                case OperationKind.SelfRelationship:
                    ExecuteSelfRelationship(operation);
                    break;

                case OperationKind.Query:
                    ExecuteQuery(operation);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown operation kind: {operation.Kind}.");
            }
        }

        private void ExecuteAddEntity(
            StressOperation operation)
        {
            var shouldSucceed =
                !Reference.ContainsEntity(
                    operation.PrimaryId);

            var entity =
                new OrbEntity
                {
                    Id = operation.PrimaryId,
                    Name = operation.Name,
                    Type = operation.Type
                };

            entity.Properties["value"] =
                new OrbProperty
                {
                    Name = "value",
                    Value =
                        new OrbValue(
                            OrbValueType.Integer,
                            operation.IntegerValue)
                };

            Exception? graphException = null;

            try
            {
                Graph.AddEntity(entity);
            }
            catch (Exception exception)
            {
                graphException = exception;
            }

            if (graphException is not null)
            {
                if (shouldSucceed)
                {
                    throw new InvalidOperationException(
                        $"[{_name}] OrbGraph rejected a valid entity.",
                        graphException);
                }

                return;
            }

            if (!shouldSucceed)
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph accepted a duplicate entity.");
            }

            if (!Reference.AddEntity(
                    operation.PrimaryId,
                    operation.Name,
                    operation.Type,
                    operation.IntegerValue))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Reference model rejected an entity " +
                    "accepted by OrbGraph.");
            }
        }

        private void ExecuteRemoveEntity(
            StressOperation operation)
        {
            var shouldSucceed =
                Reference.ContainsEntity(
                    operation.PrimaryId);

            var actual =
                Graph.RemoveEntity(
                    operation.PrimaryId);

            if (actual != shouldSucceed)
            {
                throw new InvalidOperationException(
                    $"[{_name}] Entity removal mismatch. " +
                    $"Expected={shouldSucceed}, Actual={actual}.");
            }

            if (actual)
            {
                if (!Reference.RemoveEntity(
                        operation.PrimaryId))
                {
                    throw new InvalidOperationException(
                        $"[{_name}] Reference model failed to " +
                        "remove an entity accepted by OrbGraph.");
                }
            }
        }

        private void ExecuteAddRelationship(
            StressOperation operation)
        {
            var shouldSucceed =
                Reference.ContainsEntity(
                    operation.SourceId) &&
                Reference.ContainsEntity(
                    operation.TargetId) &&
                !Reference.ContainsRelationship(
                    operation.PrimaryId);

            Exception? graphException = null;

            try
            {
                Graph.AddRelationship(
                    new OrbRelationship
                    {
                        Id = operation.PrimaryId,
                        Type = operation.Type,
                        SourceId = operation.SourceId,
                        TargetId = operation.TargetId
                    });
            }
            catch (Exception exception)
            {
                graphException = exception;
            }

            if (graphException is not null)
            {
                if (shouldSucceed)
                {
                    throw new InvalidOperationException(
                        $"[{_name}] OrbGraph rejected a valid relationship.",
                        graphException);
                }

                return;
            }

            if (!shouldSucceed)
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph accepted an invalid relationship.");
            }

            if (!Reference.AddRelationship(
                    operation.PrimaryId,
                    operation.SourceId,
                    operation.TargetId,
                    operation.Type))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Reference model rejected a relationship " +
                    "accepted by OrbGraph.");
            }
        }

        private void ExecuteRemoveRelationship(
            StressOperation operation)
        {
            var shouldSucceed =
                Reference.ContainsRelationship(
                    operation.PrimaryId);

            var actual =
                Graph.RemoveRelationship(
                    operation.PrimaryId);

            if (actual != shouldSucceed)
            {
                throw new InvalidOperationException(
                    $"[{_name}] Relationship removal mismatch. " +
                    $"Expected={shouldSucceed}, Actual={actual}.");
            }

            if (actual)
            {
                if (!Reference.RemoveRelationship(
                        operation.PrimaryId))
                {
                    throw new InvalidOperationException(
                        $"[{_name}] Reference model failed to remove " +
                        "a relationship accepted by OrbGraph.");
                }
            }
        }

        private void ExecuteDuplicateEntity(
            StressOperation operation)
        {
            if (!Reference.ContainsEntity(
                    operation.PrimaryId))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Duplicate-entity test selected " +
                    "a nonexistent entity.");
            }

            var threw =
                false;

            try
            {
                Graph.AddEntity(
                    new OrbEntity
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
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph accepted duplicate entity ID " +
                    $"'{operation.PrimaryId:D}'.");
            }
        }

        private void ExecuteDuplicateRelationship(
            StressOperation operation)
        {
            if (!Reference.ContainsRelationship(
                    operation.PrimaryId))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Duplicate-relationship test selected " +
                    "a nonexistent relationship.");
            }

            var existing =
                Graph.Relationships[
                    operation.PrimaryId];

            var threw =
                false;

            try
            {
                Graph.AddRelationship(
                    new OrbRelationship
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
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph accepted duplicate " +
                    $"relationship ID '{operation.PrimaryId:D}'.");
            }
        }

        private void ExecuteInvalidRelationship(
            StressOperation operation)
        {
            var relationship =
                new OrbRelationship
                {
                    Id = operation.PrimaryId,
                    Type = operation.Type,
                    SourceId = operation.SourceId,
                    TargetId = operation.TargetId
                };

            var threw =
                false;

            try
            {
                Graph.AddRelationship(
                    relationship);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            if (!threw)
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph accepted intentionally invalid " +
                    "relationship.");
            }

            if (Reference.ContainsRelationship(
                    operation.PrimaryId))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Invalid relationship unexpectedly " +
                    "corresponds to an existing reference relationship.");
            }
        }

        private void ExecuteSelfRelationship(
            StressOperation operation)
        {
            if (!Reference.ContainsEntity(
                    operation.SourceId))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Self-relationship selected " +
                    "a nonexistent entity.");
            }

            if (Reference.ContainsRelationship(
                    operation.PrimaryId))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Self-relationship selected " +
                    "a duplicate relationship ID.");
            }

            Exception? graphException = null;

            try
            {
                Graph.AddRelationship(
                    new OrbRelationship
                    {
                        Id = operation.PrimaryId,
                        Type = "self",
                        SourceId = operation.SourceId,
                        TargetId = operation.SourceId
                    });
            }
            catch (Exception exception)
            {
                graphException = exception;
            }

            if (graphException is not null)
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph rejected a valid self relationship.",
                    graphException);
            }

            if (!Reference.AddRelationship(
                    operation.PrimaryId,
                    operation.SourceId,
                    operation.SourceId,
                    "self"))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Reference model rejected a valid " +
                    "self relationship.");
            }
        }

        private void ExecuteQuery(
            StressOperation operation)
        {
            var entityId =
                operation.SourceId;

            var actualOutgoing =
                Graph.GetOutgoingRelationships(
                        entityId)
                    .Select(x => x.Id)
                    .OrderBy(x => x)
                    .ToArray();

            var expectedOutgoing =
                Reference.GetOutgoing(
                        entityId)
                    .OrderBy(x => x)
                    .ToArray();

            if (!actualOutgoing.SequenceEqual(
                    expectedOutgoing))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Outgoing query mismatch. " +
                    $"Entity={entityId:D}; " +
                    $"Actual=[{FormatIds(actualOutgoing)}]; " +
                    $"Expected=[{FormatIds(expectedOutgoing)}].");
            }

            var actualIncoming =
                Graph.GetIncomingRelationships(
                        entityId)
                    .Select(x => x.Id)
                    .OrderBy(x => x)
                    .ToArray();

            var expectedIncoming =
                Reference.GetIncoming(
                        entityId)
                    .OrderBy(x => x)
                    .ToArray();

            if (!actualIncoming.SequenceEqual(
                    expectedIncoming))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Incoming query mismatch. " +
                    $"Entity={entityId:D}; " +
                    $"Actual=[{FormatIds(actualIncoming)}]; " +
                    $"Expected=[{FormatIds(expectedIncoming)}].");
            }
        }

        public void Audit(
            int operationNumber)
        {
            var errors =
                Graph.Validate();

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"[{_name}] OrbGraph.Validate() reported " +
                    $"{errors.Count} error(s)." +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        errors.Take(20)));
            }

            var graphFingerprint =
                GetFingerprint();

            var referenceFingerprint =
                Reference.GetFingerprint();

            if (!string.Equals(
                    graphFingerprint,
                    referenceFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[{_name}] Graph/reference divergence at " +
                    $"operation {operationNumber:N0}.");
            }

            if (Graph.Entities.Count !=
                Reference.EntityCount)
            {
                throw new InvalidOperationException(
                    $"[{_name}] Entity count mismatch at " +
                    $"operation {operationNumber:N0}. " +
                    $"Graph={Graph.Entities.Count}, " +
                    $"Reference={Reference.EntityCount}.");
            }

            if (Graph.Relationships.Count !=
                Reference.RelationshipCount)
            {
                throw new InvalidOperationException(
                    $"[{_name}] Relationship count mismatch at " +
                    $"operation {operationNumber:N0}. " +
                    $"Graph={Graph.Relationships.Count}, " +
                    $"Reference={Reference.RelationshipCount}.");
            }
        }

        public string GetFingerprint()
        {
            var entities =
                Graph.Entities
                    .OrderBy(x => x.Key)
                    .Select(
                        x =>
                        {
                            var entity =
                                x.Value;

                            var properties =
                                entity.Properties
                                    .OrderBy(
                                        p => p.Key,
                                        StringComparer.Ordinal)
                                    .Select(
                                        p =>
                                            $"{p.Key}=" +
                                            $"{p.Value.Value.Type}:" +
                                            FormatValue(
                                                p.Value.Value.Value));

                            return
                                $"{entity.Id:D}|{entity.Name}|{entity.Type}|" +
                                $"{string.Join(";", properties)}";
                        });

            var relationships =
                Graph.Relationships
                    .OrderBy(x => x.Key)
                    .Select(
                        x =>
                            $"{x.Key:D}:" +
                            $"{x.Value.SourceId:D}>" +
                            $"{x.Value.TargetId:D}:" +
                            $"{x.Value.Type}");

            return
                $"E[{string.Join(";", entities)}]" +
                $"R[{string.Join(";", relationships)}]";
        }

        public void PrintJournal()
        {
            var start =
                Math.Max(
                    0,
                    _journal.Count - 25);

            for (var index = start;
                 index < _journal.Count;
                 index++)
            {
                Console.WriteLine(
                    $"  #{index + 1:N0} " +
                    $"{_journal[index]}");
            }
        }
    }

    private static class OperationGenerator
    {
        public static StressOperation Generate(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var choice =
                random.Next(100);

            if (choice < 25)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            if (choice < 40)
            {
                return CreateRemoveEntity(
                    random,
                    reference);
            }

            if (choice < 58)
            {
                return CreateAddRelationship(
                    random,
                    reference);
            }

            if (choice < 68)
            {
                return CreateRemoveRelationship(
                    random,
                    reference);
            }

            if (choice < 74)
            {
                return CreateDuplicateEntity(
                    random,
                    reference);
            }

            if (choice < 80)
            {
                return CreateDuplicateRelationship(
                    random,
                    reference);
            }

            if (choice < 87)
            {
                return CreateInvalidRelationship(
                    random,
                    reference);
            }

            if (choice < 93)
            {
                return CreateSelfRelationship(
                    random,
                    reference);
            }

            return CreateQuery(
                random,
                reference);
        }

        private static StressOperation CreateAddEntity(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            var id =
                NextUniqueGuid(
                    random,
                    reference);

            return new StressOperation
            {
                Kind = OperationKind.AddEntity,
                PrimaryId = id,
                Name = $"Entity-{random.Next(1_000_000)}",
                Type = $"Type-{random.Next(16)}",
                IntegerValue =
                    random.NextLong(
                        -1_000_000,
                        1_000_001)
            };
        }

        private static StressOperation CreateRemoveEntity(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            return new StressOperation
            {
                Kind = OperationKind.RemoveEntity,
                PrimaryId =
                    reference.SelectEntity(
                        random)
            };
        }

        private static StressOperation CreateAddRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount < 2)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            var source =
                reference.SelectEntity(
                    random);

            var target =
                reference.SelectEntity(
                    random);

            return new StressOperation
            {
                Kind = OperationKind.AddRelationship,
                PrimaryId =
                    NextUniqueRelationshipGuid(
                        random,
                        reference),
                SourceId = source,
                TargetId = target,
                Type =
                    $"Relation-{random.Next(12)}"
            };
        }

        private static StressOperation CreateRemoveRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.RelationshipCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            return new StressOperation
            {
                Kind =
                    OperationKind.RemoveRelationship,
                PrimaryId =
                    reference.SelectRelationship(
                        random)
            };
        }

        private static StressOperation CreateDuplicateEntity(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            return new StressOperation
            {
                Kind =
                    OperationKind.DuplicateEntity,
                PrimaryId =
                    reference.SelectEntity(
                        random)
            };
        }

        private static StressOperation CreateDuplicateRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.RelationshipCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            return new StressOperation
            {
                Kind =
                    OperationKind.DuplicateRelationship,
                PrimaryId =
                    reference.SelectRelationship(
                        random)
            };
        }

        private static StressOperation CreateInvalidRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            Guid source;

            if (reference.EntityCount > 0)
            {
                source =
                    NextUniqueGuid(
                        random,
                        reference);
            }
            else
            {
                source =
                    random.NextGuid();
            }

            Guid target;

            if (reference.EntityCount > 0 &&
                random.Next(2) == 0)
            {
                target =
                    reference.SelectEntity(
                        random);
            }
            else
            {
                target =
                    reference.EntityCount > 0
                        ? NextUniqueGuid(
                            random,
                            reference)
                        : random.NextGuid();
            }

            return new StressOperation
            {
                Kind =
                    OperationKind.InvalidRelationship,
                PrimaryId =
                    NextUniqueRelationshipGuid(
                        random,
                        reference),
                SourceId = source,
                TargetId = target,
                Type = "invalid"
            };
        }

        private static StressOperation CreateSelfRelationship(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            var entityId =
                reference.SelectEntity(
                    random);

            return new StressOperation
            {
                Kind =
                    OperationKind.SelfRelationship,
                PrimaryId =
                    NextUniqueRelationshipGuid(
                        random,
                        reference),
                SourceId = entityId,
                TargetId = entityId,
                Type = "self"
            };
        }

        private static StressOperation CreateQuery(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            if (reference.EntityCount == 0)
            {
                return CreateAddEntity(
                    random,
                    reference);
            }

            return new StressOperation
            {
                Kind =
                    OperationKind.Query,
                SourceId =
                    reference.SelectEntity(
                        random)
            };
        }

        private static Guid NextUniqueGuid(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            Guid id;

            do
            {
                id =
                    random.NextGuid();
            }
            while (reference.ContainsEntity(id));

            return id;
        }

        private static Guid NextUniqueRelationshipGuid(
            DeterministicRandom random,
            ReferenceGraph reference)
        {
            Guid id;

            do
            {
                id =
                    random.NextGuid();
            }
            while (reference.ContainsRelationship(id));

            return id;
        }
    }

    private sealed class ReferenceGraph
    {
        private readonly Dictionary<Guid, ReferenceEntity>
            _entities = new();

        private readonly List<Guid>
            _entityOrder = new();

        private readonly Dictionary<Guid, int>
            _entityIndexes = new();

        private readonly Dictionary<Guid, ReferenceRelationship>
            _relationships = new();

        private readonly List<Guid>
            _relationshipOrder = new();

        private readonly Dictionary<Guid, int>
            _relationshipIndexes = new();

        public int EntityCount =>
            _entities.Count;

        public int RelationshipCount =>
            _relationships.Count;

        public bool ContainsEntity(
            Guid id)
        {
            return _entities.ContainsKey(id);
        }

        public bool ContainsRelationship(
            Guid id)
        {
            return _relationships.ContainsKey(id);
        }

        public bool AddEntity(
            Guid id,
            string name,
            string type,
            long integerValue)
        {
            if (_entities.ContainsKey(id))
            {
                return false;
            }

            _entities[id] =
                new ReferenceEntity(
                    id,
                    name,
                    type,
                    integerValue);

            _entityIndexes[id] =
                _entityOrder.Count;

            _entityOrder.Add(id);

            return true;
        }

        public bool RemoveEntity(
            Guid id)
        {
            if (!_entities.Remove(id))
            {
                return false;
            }

            RemoveEntityFromOrder(id);

            var relationshipsToRemove =
                _relationships.Values
                    .Where(
                        x =>
                            x.SourceId == id ||
                            x.TargetId == id)
                    .Select(
                        x => x.Id)
                    .ToArray();

            foreach (var relationshipId
                     in relationshipsToRemove)
            {
                RemoveRelationship(
                    relationshipId);
            }

            return true;
        }

        public bool AddRelationship(
            Guid id,
            Guid sourceId,
            Guid targetId,
            string type)
        {
            if (!ContainsEntity(sourceId) ||
                !ContainsEntity(targetId) ||
                ContainsRelationship(id))
            {
                return false;
            }

            _relationships[id] =
                new ReferenceRelationship(
                    id,
                    sourceId,
                    targetId,
                    type);

            _relationshipIndexes[id] =
                _relationshipOrder.Count;

            _relationshipOrder.Add(id);

            return true;
        }

        public bool RemoveRelationship(
            Guid id)
        {
            if (!_relationships.Remove(id))
            {
                return false;
            }

            RemoveRelationshipFromOrder(id);

            return true;
        }

        public Guid SelectEntity(
            DeterministicRandom random)
        {
            if (_entityOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot select an entity from an empty graph.");
            }

            return _entityOrder[
                random.Next(
                    _entityOrder.Count)];
        }

        public Guid SelectRelationship(
            DeterministicRandom random)
        {
            if (_relationshipOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot select a relationship from an empty graph.");
            }

            return _relationshipOrder[
                random.Next(
                    _relationshipOrder.Count)];
        }

        public IEnumerable<Guid> GetOutgoing(
            Guid entityId)
        {
            return _relationships.Values
                .Where(
                    x =>
                        x.SourceId == entityId)
                .Select(
                    x => x.Id);
        }

        public IEnumerable<Guid> GetIncoming(
            Guid entityId)
        {
            return _relationships.Values
                .Where(
                    x =>
                        x.TargetId == entityId)
                .Select(
                    x => x.Id);
        }

        public string GetFingerprint()
        {
            var entities =
                _entities.Values
                    .OrderBy(
                        x => x.Id)
                    .Select(
                        x =>
                            $"{x.Id:D}|" +
                            $"{x.Name}|" +
                            $"{x.Type}|" +
                            $"value=Integer:{x.IntegerValue}");

            var relationships =
                _relationships.Values
                    .OrderBy(
                        x => x.Id)
                    .Select(
                        x =>
                            $"{x.Id:D}:" +
                            $"{x.SourceId:D}>" +
                            $"{x.TargetId:D}:" +
                            $"{x.Type}");

            return
                $"E[{string.Join(";", entities)}]" +
                $"R[{string.Join(";", relationships)}]";
        }

        public void ApplyForGeneration(
            StressOperation operation)
        {
            switch (operation.Kind)
            {
                case OperationKind.AddEntity:
                    if (!AddEntity(
                            operation.PrimaryId,
                            operation.Name,
                            operation.Type,
                            operation.IntegerValue))
                    {
                        throw new InvalidOperationException(
                            "Generator attempted to add duplicate entity.");
                    }

                    break;

                case OperationKind.RemoveEntity:
                    RemoveEntity(
                        operation.PrimaryId);
                    break;

                case OperationKind.AddRelationship:
                    AddRelationship(
                        operation.PrimaryId,
                        operation.SourceId,
                        operation.TargetId,
                        operation.Type);
                    break;

                case OperationKind.RemoveRelationship:
                    RemoveRelationship(
                        operation.PrimaryId);
                    break;

                case OperationKind.SelfRelationship:
                    AddRelationship(
                        operation.PrimaryId,
                        operation.SourceId,
                        operation.TargetId,
                        "self");
                    break;

                case OperationKind.DuplicateEntity:
                case OperationKind.DuplicateRelationship:
                case OperationKind.InvalidRelationship:
                case OperationKind.Query:
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown operation kind: {operation.Kind}.");
            }
        }

        private void RemoveEntityFromOrder(
            Guid id)
        {
            var index =
                _entityIndexes[id];

            var lastIndex =
                _entityOrder.Count - 1;

            var lastId =
                _entityOrder[lastIndex];

            if (index != lastIndex)
            {
                _entityOrder[index] =
                    lastId;

                _entityIndexes[lastId] =
                    index;
            }

            _entityOrder.RemoveAt(
                lastIndex);

            _entityIndexes.Remove(id);
        }

        private void RemoveRelationshipFromOrder(
            Guid id)
        {
            var index =
                _relationshipIndexes[id];

            var lastIndex =
                _relationshipOrder.Count - 1;

            var lastId =
                _relationshipOrder[lastIndex];

            if (index != lastIndex)
            {
                _relationshipOrder[index] =
                    lastId;

                _relationshipIndexes[lastId] =
                    index;
            }

            _relationshipOrder.RemoveAt(
                lastIndex);

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

    private sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(
            int seed)
        {
            _state =
                unchecked(
                    (ulong)(uint)seed +
                    0x9E3779B97F4A7C15UL);

            if (_state == 0)
            {
                _state =
                    0x9E3779B97F4A7C15UL;
            }
        }

        private ulong NextUInt64()
        {
            _state +=
                0x9E3779B97F4A7C15UL;

            var z =
                _state;

            z =
                (z ^ (z >> 30)) *
                0xBF58476D1CE4E5B9UL;

            z =
                (z ^ (z >> 27)) *
                0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }

        public int Next(
            int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax));
            }

            return (int)(
                NextUInt64() %
                (ulong)exclusiveMax);
        }

        public long NextLong(
            long minimumInclusive,
            long maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExclusive));
            }

            var range =
                unchecked(
                    (ulong)(
                        maximumExclusive -
                        minimumInclusive));

            return unchecked(
                (long)(
                    NextUInt64() %
                    range)) +
                minimumInclusive;
        }

        public Guid NextGuid()
        {
            Span<byte> bytes =
                stackalloc byte[16];

            for (var index = 0;
                 index < bytes.Length;
                 index += 8)
            {
                var value =
                    NextUInt64();

                for (var offset = 0;
                     offset < 8;
                     offset++)
                {
                    bytes[index + offset] =
                        (byte)(
                            value >>
                            (offset * 8));
                }
            }

            return new Guid(bytes);
        }
    }

    private sealed record StressOperation
    {
        public OperationKind Kind { get; init; }

        public Guid PrimaryId { get; init; }

        public Guid SourceId { get; init; }

        public Guid TargetId { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public string Type { get; init; } =
            string.Empty;

        public long IntegerValue { get; init; }

        public override string ToString()
        {
            return
                $"{Kind} | " +
                $"Primary={PrimaryId:D} | " +
                $"Source={SourceId:D} | " +
                $"Target={TargetId:D} | " +
                $"Type={Type} | " +
                $"Name={Name} | " +
                $"Value={IntegerValue}";
        }
    }

    private enum OperationKind
    {
        AddEntity,
        RemoveEntity,
        AddRelationship,
        RemoveRelationship,
        DuplicateEntity,
        DuplicateRelationship,
        InvalidRelationship,
        SelfRelationship,
        Query
    }

    private static string FormatIds(
        IEnumerable<Guid> ids)
    {
        return string.Join(
            ",",
            ids.Select(
                x => x.ToString("D")));
    }

    private static string FormatValue(
        object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value switch
        {
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            _ =>
                value.ToString() ?? string.Empty
        };
    }
}