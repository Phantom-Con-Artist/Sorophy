# Changelog

All notable changes to **Sorophy v2: Krono** and its
**Sorophy.Engine 2** are documented in this file.

---

# [2.0.0] — Krono

**Release status:** Stable

**Stability profile:** Stable S — Gold Standard

**Architectural generation:** Krono

**Implementation:** Sorophy.Engine 2

**Target framework:** .NET 10

**License:** GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)

> **Krono is the second-generation architecture of Sorophy,
> implemented by Sorophy Engine 2 as a Temporal Graph Evolution Core.**

## ✦ Krono Foundation

Krono transforms Sorophy from a static structural graph engine into a
temporal graph evolution core while preserving its graph foundation.

- Expanded entity semantics.
- Entity descriptions.
- Entity tags and derived tag indexing.
- Embedded structured entity documents.
- Event Entities represented by `SorophyEntity` with `Type == "Event"`.
- Authoritative event temporal coordinates through `OccurredAt`.
- Semantic temporal model through `SorophyTime`.
- Temporal schemas, units, positions, and precision.
- Strict temporal compatibility and comparison rules.
- Relationship validity through `ValidFrom` and `ValidTill`.
- Explicit relationship evolution operations.
- Explicit evolution executor.
- Immutable historical relationship facts.
- Append-only relationship histories.
- Event Entity provenance through `EventEntityId`.
- Permanent relationship identity retirement.
- Expanded graph validation and invariants.
- Canonical graph state separated from derived indexes and adjacency structures.

## ✦ Event Entities

Krono introduces Event Entities as first-class, passive temporal anchors.

- Event Entities are ordinary `SorophyEntity` instances classified as `Event`.
- Event Entities carry an authoritative `OccurredAt` temporal coordinate.
- Event Entities are declarative and passive.
- Event Entities do not execute themselves.
- No `event.Execute()` model is used.
- Application semantics determine which structural evolutions an Event causes.
- A single Event Entity may anchor multiple relationship evolutions.
- Event-anchored evolutions derive their temporal coordinate from `OccurredAt`.
- Multiple Events at the same temporal coordinate are temporally equivalent; Krono does not infer causal ordering between them.

## ✦ Relationship Evolution

Krono introduces explicit relationship state transitions:

- Relationship creation.
- Relationship type changes.
- Relationship property modification.
- Relationship validity changes.
- Relationship termination.

The five evolution operations are:

- `SorophyRelationshipCreation`
- `SorophyRelationshipPropertyModification`
- `SorophyRelationshipTypeChange`
- `SorophyRelationshipValidityChange`
- `SorophyRelationshipTermination`

Evolution descriptions remain separate from execution.

`SorophyRelationshipEvolutionExecutor` applies structural mutations and records the corresponding historical state.

Direct graph mutation does not fabricate temporal history.

## ✦ Historical State

Krono introduces historical relationship state through:

- `SorophyRelationshipFact`
- `SorophyRelationshipHistory`
- Graph-level history storage.
- Append-only historical facts.
- Historical capture during evolution.
- Immutable historical facts.
- Explicit Event Entity provenance.
- Permanent relationship identity retirement.

Historical state is relationship-centric.

History is not an audit log, edit log, undo/redo mechanism, or event-sourcing system.

The recording coordinate `At` remains distinct from semantic relationship validity through `ValidFrom` and `ValidTill`.

## ✦ Temporal Model

Krono introduces semantic time rather than assuming host-calendar timestamps.

The temporal system includes:

- `SorophyTime`
- `SorophyTimeSchema`
- `SorophyTimeUnit`
- `SorophyTimePositionDefinition`
- `SorophyTimePrecision`
- `SorophyTimeValidator`
- `SorophyTimeComparer`

Temporal schema and unit compatibility are enforced where required.

Numeric temporal positions use arbitrary-precision integer representation, while non-numeric positions are not treated as automatically orderable.

Krono does not guess temporal conversions between incompatible schemas or units.

## ✦ Point-in-Time Snapshots

Krono introduces point-in-time graph projection through:

- `SorophyGraph.CreateSnapshot(SorophyTime)`
- `SorophyGraph.CreateSnapshot(SorophyEntity)`
- `ISorophySnapshot`
- `ISorophySnapshotEntity`
- `ISorophySnapshotRelationship`

Snapshots reconstruct structural graph state at a temporal coordinate.

- Snapshot state uses post-transition / inclusive semantics.
- Event snapshots resolve through `Event.OccurredAt`.
- Historical relationship state is reconstructed from recorded facts.
- Baseline relationships without history are treated as pre-existing objects.
- Future-created relationships are excluded.
- Terminated relationships are excluded after their termination coordinate.
- Dangling relationships are pruned.
- Snapshot data is isolated through deep cloning.

## ✦ Temporal Query Domain

Krono introduces the read-only Temporal Query Domain (TQD).

`graph.TemporalQuery` provides:

- Point-in-time graph access.
- Event-anchored temporal access.
- Entity existence and lookup at a time.
- Relationship existence and lookup at a time.
- Inbound and outbound relationship queries.
- Historical fact queries over intervals.
- Modified relationship ID queries.
- Relationship history lookup.
- Facts associated with an Event Entity.
- Relationships evolved by an Event Entity.

TQD does not mutate the graph and does not infer causal precedence between co-temporal facts.

## ✦ Graph Diff

Krono introduces structural graph comparison through:

- `SorophyGraphDiff`
- `ISorophyGraphChangeSet`
- `SorophyEntityChange`
- `SorophyRelationshipChange`
- `GraphChangeKind`

Graph Diff compares two materialized snapshots and identifies:

- Entity additions.
- Entity removals.
- Entity modifications.
- Relationship additions.
- Relationship removals.
- Relationship modifications.

Graph Diff is a state comparison mechanism.

It does not infer history, causality, event meaning, patches, merges, or undo operations.

## ✦ Persistence — `.lore` v2

Krono upgrades the `.lore` persistence format to version 2.

`.lore` v2 persists:

- `formatVersion: 2`
- Entities.
- Relationships.
- Relationship validity.
- Relationship histories.
- Historical facts.
- Event Entity provenance.
- Retired relationship IDs.

Derived indexes and adjacency structures are reconstructed from canonical state rather than treated as serialized truth.

The serializer does not synthesize historical facts from direct mutations or from the absence of a relationship.

Krono maintains deterministic serialization and culture-invariant persistence behavior, with portability verified across Windows, Linux, and macOS.

## ✦ Validation & Integrity

Krono expands graph validation to cover:

- Entity identity and references.
- Relationship endpoints.
- Referential integrity.
- Relationship history integrity.
- Temporal schema compatibility.
- Event Entity provenance.
- Relationship identity retirement.
- Active/retired identity overlap.
- Serialization integrity.
- Derived index and adjacency consistency.

Corrupted canonical state is rejected explicitly rather than silently repaired.

## ✦ Cross-Platform Portability

Persistence portability is verified through the following chain:

```text
Windows
   ↓
Linux
   ↓
macOS
   ↓
Windows
```

The portability verification covers persisted `.lore` artifacts across operating systems, including encoding, line-ending, path, GUID, and culture-invariant serialization behavior.

## ✦ Verification

The Stable S release was hardened through the V1/V2 test and stress suites.

```text
769 / 769 unit test cases passed
0 failed
0 skipped

13 / 13 stress campaigns passed
58 / 58 stress checks passed
```

Stress and hardening coverage includes:

- Mutation chaos.
- Temporal scope.
- Deterministic replay.
- Mutation performance.
- Pool reuse.
- High-degree topology.
- Performance benchmarking.
- Relationship scaling.
- Memory benchmarking.
- Differential fuzzing.
- Serialization torture.
- Crash/recovery torture.
- Soak/endurance testing.
- Randomized integrity testing.
- Metamorphic testing.
- Cross-feature chaos.
- Persistence and retirement hardening.
- Public API regression coverage.

Deterministic stress/fuzzing seed families include:

```text
12345
42
1
987654321
```

## ✦ Endurance & Memory

Large-scale endurance testing established the expected memory characteristics of permanent relationship identity retirement.

Retired relationship IDs are intentionally retained so that an identity can never be reused. Therefore, retirement storage grows with the number of retired relationship identities.

Serialization of large graphs can additionally cause temporary allocation and Large Object Heap pressure.

The endurance test harness separates bounded active-state memory checks from retirement-scale endurance checks so that intentional O(N) retirement storage is not misclassified as a production memory leak.

No artificial production memory limit was introduced to satisfy the test harness.

## ✦ Release Packaging

The stable package is:

```text
Sorophy.Engine.2.0.0.nupkg
Sorophy.Engine.2.0.0.snupkg
```

The package targets:

```text
.NET 10
```

The release package includes the Sorophy package icon and stable package metadata.

---

# [1.0.0] — Sorophy V1

Sorophy V1 established the original structural graph foundation.

## ✦ V1 Foundation

- Entity graph.
- Directed relationships.
- Typed `SorophyValue` properties.
- Referential integrity.
- Indexed adjacency.
- Slab-based adjacency pool.
- Graph traversal.
- `.entity` persistence.
- `.lore` version 1 persistence.
- Core validation.
- Direct graph mutation.

V1 did not provide:

- First-class temporal coordinates.
- Relationship history.
- Temporal snapshots.
- Temporal queries.
- Event provenance.
- Relationship evolution operations.
- Permanent relationship identity retirement.

V2 Krono builds upon the structural graph foundation while introducing explicit temporal state and evolution semantics.