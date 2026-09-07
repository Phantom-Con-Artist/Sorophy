# V2 Testing and Verification

Krono keeps the project's adversarial testing philosophy, but the verification boundary now reflects the completed v2 architecture rather than the earlier beta checkpoint.

## Current Stable Verification

The current Sorophy v2.0.0 "Krono" stable release passes:

```text
769 / 769 unit tests
0 failed
0 skipped

13 / 13 stress campaigns
58 / 58 stress checks
```

The release is verified as a **Stable S** release under the project's **Gold Standard** stability profile.

The verification scope covers the complete implemented Krono architecture, including graph structure, Entity 2.0, temporal semantics, Event Entities, relationship evolution, historical state, provenance, snapshots, TQD, Graph Diff, persistence, validation, determinism, and hardening.

## What the Unit Suite Proves

The deterministic suite establishes the behavioral contract of the implemented engine at the feature and cross-feature levels.

Relevant v2 areas include:

```text
Entity 2.0
Tag indexing
Temporal values and schemas
Relationship validity
Historical facts
Relationship histories
Relationship ID retirement
Event Entity provenance
Relationship evolution operations
Evolution execution
Graph invariants
Graph Snapshot
Temporal Query Domain (TQD)
Graph Diff
.lore v2 persistence
Serialization validation
Snapshot / TQD / Diff isolation
Metamorphic properties
Cross-feature regression behavior
Corruption and invariant handling
```

The suite is intentionally broader than isolated feature tests. It verifies interactions between temporal state, history, evolution, projections, persistence, and graph invariants.

## Evolution Verification

The evolution test batch covers the complete operation family:

```text
Creation
Type Change
Property Modification
Validity Change
Termination
```

The tests exercise sequences rather than only isolated operations.

Examples include:

```text
Create → Type Change
Type Change → Property Change → Termination
Repeated property changes
Termination → attempted identity reuse
Event-anchored Evolution
Event provenance across historical facts
```

Creation Evolutions also preserve an initial historical fact. Direct graph insertion remains distinct from temporal Evolution execution and does not fabricate historical creation records.

## Historical Verification

History tests verify that:

- Facts belong to the correct relationship.
- Facts remain immutable.
- Histories preserve insertion order.
- Duplicate fact instances are rejected.
- Histories survive relationship removal.
- Retired relationship identities cannot be reused.
- Event provenance is preserved through `EventEntityId`.
- Historical state remains consistent with the corresponding relationship evolution.

## Temporal Verification

Temporal tests verify strict temporal semantics rather than assuming that every time-like value is interchangeable.

Coverage includes:

- temporal schemas;
- units and position definitions;
- precision;
- numeric temporal coordinates;
- strict compatibility checks;
- `At` versus `ValidFrom` / `ValidTill`;
- Event `OccurredAt`;
- point-in-time reconstruction;
- same-time Event equivalence;
- temporal boundary behavior;
- invalid temporal input.

Krono does not guess conversions between incompatible temporal schemas or representations.

## Graph Snapshot Verification

Snapshot tests verify point-in-time reconstruction and projection isolation.

Coverage includes:

- entity and relationship membership at a coordinate;
- future creation exclusion;
- termination boundaries;
- baseline structures without temporal history;
- Event-based snapshot creation;
- `OccurredAt` anchoring;
- relationship reconstruction from historical facts;
- dangling-edge pruning;
- deterministic materialization;
- deep isolation from subsequent live-graph mutation.

Snapshots are read-only projections and do not become an alternate source of canonical graph state.

## Temporal Query Domain Verification

TQD tests verify the read-only temporal query boundary.

Coverage includes:

- point-in-time entity and relationship queries;
- outbound and inbound temporal queries;
- interval/history queries;
- modified relationship identification;
- relationship history retrieval;
- Event provenance queries;
- relationships evolved by an Event;
- strict temporal compatibility;
- unknown-ID behavior;
- deterministic result ordering;
- read-only result isolation;
- repeatability and metamorphic properties.

TQD does not infer causality, execute Event Entities, or fabricate history.

## Graph Diff Verification

Graph Diff tests verify deterministic structural comparison between two materialized snapshots.

Coverage includes:

```text
Entity Added
Entity Removed
Entity Modified

Relationship Added
Relationship Removed
Relationship Modified

Stable Guid identity matching
Structural equality
Immutable change sets
Deterministic ordering
Snapshot isolation
State-not-history semantics
Intermediate transition invisibility
```

Graph Diff deliberately compares endpoint states rather than reconstructing the history between them.

## Persistence Verification

`.lore` v2 verification covers:

```text
Canonical entities
Canonical relationships
Relationship histories
Retired relationship identities
EventEntityId provenance
Temporal state
Typed values
Validation during deserialization
Deterministic serialization
Round-trip integrity
Cross-platform persistence
```

Deserialization restores recorded state; it does not execute Evolutions or Events.

The persistence layer rejects invalid or corrupted state rather than silently repairing it.

## Metamorphic Verification

Krono's hardening suite includes metamorphic properties designed to test relationships between operations and results rather than only fixed examples.

These tests cover properties such as:

- repeated deterministic execution;
- snapshot consistency;
- temporal query consistency;
- Diff symmetry/identity expectations where applicable;
- persistence round-trip preservation;
- isolation;
- cross-feature invariants.

The purpose is to catch implementation errors that ordinary example-based tests can miss.

## Adversarial and Cross-Feature Verification

The hardening suite combines independent Krono capabilities in deliberately inconvenient sequences.

Coverage includes:

```text
Temporal + Evolution
History + Retirement
Event Provenance + Evolution
Snapshot + TQD
Snapshot + Graph Diff
Persistence + History
Persistence + Retirement
Temporal Reconstruction + Persistence
Graph Mutation + Projection Isolation
Invalid Input + Invariant Validation
```

The goal is not merely to prove that each subsystem works independently, but that their boundaries remain coherent when used together.

## Stress Testing

The stress-test arsenal now forms part of the stable verification record.

The completed arsenal includes campaigns covering areas such as:

```text
Mutation chaos
Krono temporal scope
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

The final stable verification record reports:

```text
13 / 13 campaigns passed
58 / 58 checks passed
```

The campaigns use deterministic seeds and repeated execution where appropriate to test reproducibility and cross-run consistency.

## Deterministic Replay

A dedicated V1 → V2 deterministic replay comparison also verified Krono against the V1 reference workload.

The recorded one-million-operation campaign produced:

```text
V1 final graph: 99,194 entities / 39,090 relationships
V2 final graph: 99,194 entities / 39,090 relationships

Final-state difference:
0 entities
0 relationships

V1 replay: 1928.879 s
V2 replay: 1972.371 s
Observed V2 overhead: ~2.25%
```

All four replay verification checks passed.

The correct interpretation is that Krono demonstrated deterministic correctness and exact final-state equivalence with the V1 workload, while showing a small observed replay-performance overhead in that specific campaign. Broader performance conclusions require dedicated performance/scaling campaigns.

## Memory and Endurance Verification

The endurance program includes multi-scale soak and memory-retention verification.

The testing boundary distinguishes expected permanent retention from unexpected memory retention, particularly for retired relationship identities whose storage is intentionally persistent.

The final bounded-churn recovery verification recorded:

```text
Baseline Managed Heap: 33.62 MB
Final Managed Heap:    34.09 MB
Unexpected Retained Delta: 0 B
```

The test therefore passed its unexpected-retention guardrail.

The soak program also exercises large mutation counts, persistence rounds, relationship retirement, entity lifecycle churn, and repeated validation.

## Cross-Platform Verification

Krono's stable verification includes sequential cross-platform CI and persistence portability checks across:

```text
Windows → Linux → macOS → Windows
```

The purpose is to verify that deterministic behavior and persisted structural state do not depend on a single operating-system environment.

## Testing Philosophy

The project intentionally tests failure paths because stateful graph systems tend to fail in the places a demo never visits.

The guiding question remains:

> **Can the engine stay internally consistent when users do inconvenient things?**

For Krono, that question expands to:

> **Can graph structure, temporal state, evolution, history, provenance, projections, persistence, and derived analysis remain mutually consistent under inconvenient combinations of operations?**

That is the standard the v2 verification program is intended to enforce.

## Release Boundary

The earlier `469 / 469` checkpoint represented an intermediate v2 development state.

It is no longer the stable verification boundary.

Krono's completed stable architecture includes the later Snapshot, TQD, Graph Diff, hardening, persistence, endurance, and cross-platform verification work. The release-grade verification record therefore uses the final stable suite rather than the earlier beta checkpoint.
