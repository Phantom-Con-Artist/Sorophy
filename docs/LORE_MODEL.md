<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Lore Model</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines the **Krono Lore Model**, the connected-information semantics implemented by **Sorophy.Engine 2.0.0**.

## The Core Idea

The Lore Model describes the connected information represented by `.lore` concepts and `SorophyGraph`.

The v1 model established lore as a connected body of entities and relationships. Krono preserves that foundation while giving relationships an explicit temporal and evolutionary lifecycle.

```text
.entity
    = one independently identifiable thing

.lore
    = a connected body of things and relationships
```

An `SorophyGraph` is the in-memory representation of that connected structure.

## Relationships Are First-Class Data

A relationship is not merely prose hidden inside an entity description.

It has its own identity and state:

```text
SorophyRelationship
├── Id
├── SourceId
├── TargetId
├── Type
├── Properties
├── ValidFrom
└── ValidTill
```

Relationship identity and state allow the relationship itself to evolve without requiring applications to reconstruct structural history from descriptive text.

## Lore and Time

A relationship can carry explicit temporal validity:

```text
A ──[rules]──> B

ValidFrom = T1
ValidTill = T2
```

Temporal validity is part of the relationship model rather than merely an application convention.

Krono also distinguishes semantic validity from historical recording:

```text
ValidFrom / ValidTill
    = when the relationship is semantically valid

At
    = when a historical fact is recorded
```

These coordinates must not be conflated.

## Lore and Events

An Event remains a normal entity:

```text
SorophyEntity
    ├── Type = "Event"
    └── OccurredAt
```

The Event is a passive temporal anchor.

It does not execute graph mutations.

Applications may use an Event Entity to anchor Evolutions:

```text
Event Entity
      │
      │ EventEntityId
      ▼
Evolution
      │
      ▼
Evolution Executor
      │
      ▼
Graph + History
```

Event Entities are not special relationship endpoints. Normal relationships remain:

```text
Entity → Relationship → Entity
```

## Lore and Evolution

Relationships can change through five explicit Evolution operations:

```text
Creation
Type Change
Property Modification
Validity Change
Termination
```

The Evolution object describes the requested structural transition.

The executor performs the mutation.

This gives the Lore Model a clean separation between:

```text
Temporal Event
      ≠
Structural Evolution
      ≠
Graph Execution
```

## Lore and History

When the Evolution model records a relationship transition, the historical relationship state is preserved as an immutable fact.

```text
Current Relationship
        │
        ▼
Historical Fact
        │
        ▼
Evolution / Mutation
        │
        ▼
New Current State
```

History remains available even after a relationship is terminated and removed from active graph state.

Relationship identity retirement prevents a terminated identity from being reused as an unrelated active relationship.

## `.lore` v2

Krono's `.lore` v2 representation explicitly persists the temporal graph model.

The top-level structure includes:

```text
formatVersion
entities
relationships
relationshipHistories
retiredRelationshipIds
```

Historical facts may preserve Event provenance through:

```text
eventEntityId
```

The persistence model stores explicit existing state. It does not fabricate history from missing records or ordinary deletion.

Deserialization validates the reconstructed state and rejects invalid or inconsistent data rather than silently repairing it.

## Temporal Projection Layer

Krono now provides read-only temporal projections over Lore state.

```text
Lore / Graph
     +
History
     +
SorophyTime
     │
     ▼
Temporal Projection Layer
     │
     ├── Snapshot
     ├── Temporal Query Domain
     └── Graph Diff
```

### Snapshot

A `SorophyGraphSnapshot` represents the graph reconstructed at a requested temporal coordinate.

Snapshots are read-only and isolated from canonical graph state.

### Temporal Query Domain

The Temporal Query Domain provides read-only temporal inspection of point-in-time state, intervals, historical facts, and Event provenance.

### Graph Diff

Graph Diff compares structural graph states, typically through snapshots, and reports added, removed, and modified entities and relationships.

These projection systems inspect structure. They do not infer application semantics or causality.

## Lore Model Boundary

Krono Lore Model remains a general connected-information model.

It does not encode domain-specific rules such as:

- kingdoms;
- characters;
- magic systems;
- scientific units;
- game mechanics;
- application workflows.

Those are domain vocabularies built on top of Sorophy.

## Canonical vs Derived Lore State

The canonical Lore state remains the graph itself:

```text
Canonical
├── Entities
├── Relationships
├── Relationship Histories
└── Retired Relationship Identities
```

Supporting indexes and temporal projections are derived:

```text
Derived
├── Indexes
├── Adjacency
├── Snapshots
├── Temporal Query Results
└── Graph Diffs
```

Derived structures must not become alternative sources of truth.

## Design Direction

Krono's Lore Model can therefore be summarized as:

```text
Connected Entities
       │
       ▼
Structural Relationships
       │
       ├── Temporal Validity
       ├── Evolution
       └── Historical State
                │
                ▼
        Temporal Projections
        ├── Snapshot
        ├── TQD
        └── Graph Diff
```

The result is a connected-information model that understands **structure with time** while leaving application semantics where they belong: above the Sorophy structural core.
