# V2 Testing and Verification

Krono keeps the project's adversarial testing philosophy, but the verification boundary changes with the release stage.

## Current Unit-Test Checkpoint

The current v2 development branch passes:

```text
469 / 469 tests
0 failed
0 skipped
```

These tests cover the implemented graph, entity, tag, temporal, history, and relationship-evolution behavior represented in the current v2 engine.

## What the Unit Suite Proves

The deterministic suite is intended to establish the behavioral contract of the engine at the feature level.

Relevant v2 areas include:

```text
Entity 2.0
Tag indexing
Temporal values
Relationship validity
Historical facts
Relationship histories
Relationship ID retirement
Evolution operations
Evolution execution
Graph invariants
Serialization behavior inherited from the existing suite
```

## Evolution Verification

The evolution test batch covers the complete operation family:

```text
Creation
Type Change
Property Modification
Validity Change
Termination
```

The tests also exercise sequences rather than only isolated operations.

Examples include:

```text
Create → Type Change
Type Change → Property Change → Termination
Repeated property changes
Termination → attempted identity reuse
```

## Historical Verification

History tests verify that:

- Facts belong to the correct relationship.
- Facts remain immutable.
- Histories preserve insertion order.
- Duplicate fact instances are rejected.
- Histories survive relationship removal.
- Retired relationship identities cannot be reused.

## Temporal Verification

Temporal tests verify schema consistency and explicit temporal semantics instead of assuming that every time-like value is interchangeable.

## Stress Testing

The repository contains a dedicated stress-test arsenal with campaigns covering areas such as:

```text
Mutation chaos
Deterministic replay
Mutation performance
Pool reuse
High-degree topology
Performance benchmarks
Relationship scaling
Memory behavior
Differential fuzzing
Serialization torture
Crash / recovery
Soak / endurance
```

That arsenal established the v1.0.0 release verification program.

## Why V2 Stress Is Deferred

The v2 engine now contains substantially different semantics: temporal state, history, and evolution.

Running the old release-gate program immediately would provide useful information, but it would not yet be the cleanest final v2 verification boundary.

The plan is:

```text
Finish V2 architecture
        ↓
Finish V2 documentation
        ↓
Finish V2 feature tests
        ↓
Run dedicated V2 stress program
        ↓
Evaluate beta / release readiness
```

This prevents a stress result from becoming stale immediately because another major subsystem is added afterward.

## Beta Status

`2.0.0-beta.1` is a development checkpoint, not a Grade A stable release.

The Grade A / Grade S release-grade system remains applicable to stable releases and should not be used to imply that the current beta has completed the v1-style release gate.

## Testing Philosophy

The project intentionally tests failure paths because stateful graph systems tend to fail in the places a demo never visits.

The guiding question is:

> **Can the engine stay internally consistent when users do inconvenient things?**
