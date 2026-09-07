# Sorophy™ v2.0.0 "Krono" — Graph Diff

## Overview

**Graph Diff** is Sorophy Krono's deterministic, read-only structural comparison capability.

It compares two materialized `ISorophySnapshot` instances and produces an immutable `ISorophyGraphChangeSet`.

```text
Snapshot A × Snapshot B
          │
          ▼
 SorophyGraphDiff.Compare
          │
          ▼
   ISorophyGraphChangeSet
```

Graph Diff compares **state, not history**.

---

## Architectural Role

Graph Diff deliberately consumes the public snapshot abstraction rather than reaching into `SorophyGraph` internals.

```text
Canonical Graph + History
          │
          ▼
       Snapshot
       /      \
      A        B
       \      /
        ▼    ▼
      Graph Diff
          │
          ▼
      Change Set
```

This makes the subsystem independently composable and prevents it from becoming coupled to:

- canonical graph internals;
- adjacency storage;
- mutation executors;
- relationship history tables;
- persistence internals.

---

## Governing Question

Graph Diff answers:

> **What is structurally different between these two materialized states?**

It does not answer:

> Why did it change?

That second question belongs to temporal history, provenance, and application semantics.

---

## Identity Matching

Graph Diff uses strict canonical identity.

### Entities

Entity identity is:

```text
Entity.Id : Guid
```

### Relationships

Relationship identity is:

```text
Relationship.Id : Guid
```

No fuzzy matching is performed.

Graph Diff does not match elements by:

- name;
- properties;
- endpoints alone;
- semantic similarity;
- inferred meaning.

Identity is explicit and deterministic.

---

## Change Categories

The v2 foundation uses three coarse structural categories:

```text
Added
Removed
Modified
```

### Added

An element exists in the target snapshot but not in the baseline snapshot.

### Removed

An element exists in the baseline snapshot but not in the target snapshot.

### Modified

An element exists in both snapshots under the same identity but has different observable structural state.

The model currently does not expand a modification into field-level deltas.

---

## Entity Changes

Entity changes are represented through immutable entity change descriptors.

The subsystem can identify:

```text
Entity Added
Entity Removed
Entity Modified
```

Matching is based exclusively on `Entity.Id`.

A modification means the observable snapshot entity state differs according to Krono's structural equality rules.

---

## Relationship Changes

Relationship changes are represented through immutable relationship change descriptors.

The subsystem can identify:

```text
Relationship Added
Relationship Removed
Relationship Modified
```

Matching is based exclusively on `Relationship.Id`.

Observable relationship state is compared structurally rather than by object reference.

---

## Structural Equality

Snapshot materialization intentionally deep-clones mutable values.

Therefore, reference equality is insufficient for Graph Diff.

Graph Diff uses a dedicated structural equality mechanism capable of recursively comparing the observable snapshot state, including relevant:

- scalar values;
- nested collections;
- dictionaries;
- tags;
- documents;
- temporal coordinates;
- relationship properties.

The comparison is structural, not reference-based.

---

## State-Not-History Boundary

This is one of the most important Graph Diff rules.

```text
History:
A → B → C → B

Snapshot at A
     vs
Snapshot at final B
```

If the two snapshots contain identical observable structural state, Graph Diff returns no change even if many historical operations occurred between them.

Therefore:

```text
Same State
   ≠
Same History

Graph Diff cares about the first.
TQD / History cares about the second.
```

---

## Intermediate Deletion and Recreation

Suppose a relationship with identity `R` is:

```text
T1: exists
T2: removed
T3: recreated with identity R
```

If the compared snapshots both contain relationship `R` with identical observable state, Graph Diff does not report the intermediate removal and recreation.

That is intentional.

Graph Diff sees only:

```text
Snapshot A
    ↓
endpoint state

Snapshot B
    ↓
endpoint state
```

Intermediate transition analysis belongs to TQD and relationship history.

---

## Deterministic Ordering

All change-set collections are presented in deterministic canonical identity order.

For entity and relationship collections, ordering uses ascending `Guid.CompareTo`.

```text
Added Entities       → Guid ascending
Removed Entities     → Guid ascending
Modified Entities    → Guid ascending

Added Relationships  → Guid ascending
Removed Relationships→ Guid ascending
Modified Relationships→ Guid ascending
```

This ordering is a presentation convention.

It carries **no temporal or causal meaning**.

A smaller GUID does not mean an element happened earlier.

---

## Immutability

The returned change set is immutable from the caller's perspective.

```text
Snapshot A
Snapshot B
    │
    ▼
 Graph Diff
    │
    ▼
Immutable Change Set
```

Graph Diff does not mutate either input snapshot.

It also does not mutate the canonical graph.

---

## Public API

The primary API is:

```csharp
public static ISorophyGraphChangeSet Compare(
    ISorophySnapshot before,
    ISorophySnapshot after);
```

A convenience extension is also available conceptually as:

```csharp
before.Diff(after)
```

The public model is centered around:

```text
GraphChangeKind
SorophyEntityChange
SorophyRelationshipChange
ISorophyGraphChangeSet
SorophyGraphChangeSet
SorophyStructuralEquality
SorophyGraphDiff
```

The subsystem resides in:

```text
Sorophy.Engine/Diff/
```

---

## Production Footprint

The v2 Graph Diff implementation was deliberately isolated.

Exactly seven production files were introduced under the dedicated `Sorophy.Engine/Diff/` namespace.

No existing production files were modified by the Graph Diff implementation.

This was an intentional architectural constraint:

> Add the capability as a consumer of existing snapshot contracts rather than redesigning the engine underneath it.

---

## TQD / Graph Diff Boundary

| Responsibility | TQD | Graph Diff |
|---|---|---|
| Governing question | What happened / what was state at T? | What differs between two states? |
| Primary input | Graph, history, temporal coordinates, Events | Two `ISorophySnapshot` instances |
| History aware | Yes | No |
| Event provenance aware | Yes | No |
| Intermediate transitions | Queryable | Invisible |
| Output | Snapshots, facts, IDs, histories | Immutable change set |
| Causality | Not inferred | Not inferred |
| Structural comparison | Limited to query state | Primary responsibility |

Graph Diff does not replace, duplicate, or wrap TQD.

---

## What Graph Diff Does Not Do

The v2 foundation intentionally excludes:

- field-level delta expansion;
- property-level change descriptions;
- causal inference;
- Event semantic interpretation;
- history reconstruction;
- intermediate transition detection;
- patches;
- merges;
- undo;
- multi-snapshot diff chains;
- query language;
- semantic matching;
- fuzzy identity matching.

These boundaries keep the v2 implementation a **foundation**, not a general-purpose change-management system.

Future capabilities may be considered only when their design principles can be integrated cleanly with the existing Krono architecture.

---

## Verification

The Graph Diff milestone was verified with:

- **22 new Graph Diff unit tests**;
- **678/678 total unit tests passed** at the implementation milestone;
- **0 failed**;
- **0 skipped**;
- Release build passed with **0 errors**;
- **13/13 stress campaigns passed**;
- **58/58 stress checks passed**.

The implementation preserved the seven-file production footprint and modified zero existing production files.

Later full Krono hardening incorporated Graph Diff into broader cross-feature, metamorphic, determinism, corruption, and regression verification.

---

## Design Principle

Graph Diff is deliberately a state comparison tool, not a temporal historian.

> **Snapshots tell you what the graph was. TQD tells you what was recorded across time. Graph Diff tells you how two states differ.**

That separation is what lets the three capabilities coexist without collapsing Krono's architecture into a single overloaded subsystem.
