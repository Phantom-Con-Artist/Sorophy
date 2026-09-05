<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Lore Model</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines the **Krono Lore Model**, the connected-information semantics implemented by Sorophy.Engine 2.


Krono Lore Model describes the connected information model represented by `.lore` concepts and `SorophyGraph` relationships.

The v1 model treated lore primarily as a connected body of entities and relationships. Krono keeps that foundation but gives relationships a richer semantic lifecycle.

## The Core Idea

```text
.entity
    = one independently identifiable thing

.lore
    = a connected body of things and relationships
```

An `SorophyGraph` is the in-memory representation of that connected structure.

## Relationships Are First-Class Data

A relationship is not merely a sentence hidden inside an entity description.

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

This allows the relationship itself to evolve without requiring the application to reconstruct history from prose.

## Krono Lore Model and Time

A relationship can have explicit temporal validity:

```text
A ──[rules]──> B

ValidFrom = T1
ValidTill = T2
```

This makes a temporal statement part of the relationship model rather than an application convention.

## Krono Lore Model and Evolution

Relationships can change through explicit evolution operations.

```text
Relationship
    │
    ├── Type Change
    ├── Property Modification
    ├── Validity Change
    └── Termination
```

A relationship can also be created through an explicit creation operation.

The executor performs the mutation. The evolution object itself remains a description of the requested transition.

## Krono Lore Model and History

When an existing relationship changes, the prior state can be recorded as an immutable historical fact before the mutation is applied.

```text
Current Relationship
        │
        ▼
Historical Fact
        │
        ▼
Historical Relationship
```

This prevents the graph from treating mutation as erasure.

## Relationship Identity

Relationship identity is deliberately stronger in Krono.

Once a relationship identity is retired, it cannot simply be resurrected as a new active relationship with the same identifier.

Conceptually:

```text
Created → Active → Evolved → Terminated → Retired
```

The history of the retired relationship remains available independently of its active presence in the graph.

## Lore and Events

An event remains an entity with `Type = "Event"`.

It may be associated with evolution operations by an application or higher-level event system, but the graph does not silently execute operations merely because an entity is an event.

## Lore Model Boundary

Krono Lore Model is still a general information model.

It should not encode worldbuilding-specific rules such as kingdoms, characters, magic systems, scientific units, or game mechanics.

Those are domain vocabularies built on top of the model.

## Long-Term Direction

The intended next step is to make temporal lore queryable through projections and snapshots:

```text
Lore / Graph + History
          ↓
Temporal Projection
          ↓
Snapshot at T
          ↓
Queries / Diff / Analysis
```

That work belongs to the Krono roadmap and should build on the already-established relationship and history model.
