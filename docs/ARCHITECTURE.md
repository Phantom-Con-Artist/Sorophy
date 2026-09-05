<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Architecture</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines the architectural boundaries of **sorophyv2: Krono** as implemented by Sorophy.Engine 2.


Krono builds on the v1 graph engine without turning the engine into a monolith.

The architecture remains intentionally layered.

## High-Level Model

```text
┌─────────────────────────────────────────────┐
│                 Applications                 │
│       Orbpad · Editors · Domain Tools       │
└──────────────────────┬──────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────┐
│                  Sorophy.Engine                 │
│                                             │
│  Entity · Graph · Relationship · Properties │
│  Typed Values · Tags · Time · Evolution     │
│  History · Validation                       │
└───────────────┬───────────────┬─────────────┘
                │               │
                ▼               ▼
        Serialization         Storage
                │               │
                └───────┬───────┘
                        ▼
                .entity / .lore
```

## Canonical State vs Derived State

Krono maintains an important distinction between authoritative state and derived structures.

Examples:

```text
Canonical
├── Entities
├── Relationships
└── Relationship Histories

Derived / supporting
├── Tag index
├── Relationship indexes
└── Adjacency storage
```

Derived structures exist to make access fast and deterministic. They must agree with canonical state but should not become the source of truth.

## SorophyGraph

`SorophyGraph` remains the canonical owner of active graph state.

It owns the lifecycle of entities and relationships and coordinates the supporting graph structures.

The partial-class decomposition exists for maintainability; the public conceptual object is still one `SorophyGraph`.

## Relationship Evolution Boundary

Evolution is not folded directly into every relationship mutation method.

Instead:

```text
SorophyRelationshipEvolution
        │
        ▼
SorophyRelationshipEvolutionExecutor
        │
        ▼
SorophyGraph
```

This keeps declarative transition descriptions separate from graph mutation mechanics.

## History Boundary

History is separate from active relationship storage.

```text
Active relationship
    = current truth

Historical fact
    = recorded previous truth

Relationship history
    = append-only sequence of facts
```

An active relationship does not require historical storage until a historical fact is recorded.

## Temporal Boundary

`SorophyTime` is a semantic model rather than a generic CLR timestamp.

The graph and historical systems consume temporal values through the defined temporal contracts instead of inventing ad-hoc string timestamps.

## Entity Boundary

Entities remain independently constructible.

Graph membership does not redefine the entity's identity or require application code to create graph-specific entity subclasses.

## Event Boundary

Events are entity semantics, not a separate inheritance tree.

```text
SorophyEntity
Type = Event
```

Execution remains explicit through evolution objects and the executor.

## File and Application Boundary

Applications should not need to implement a private graph engine, relationship lifecycle, typed-value system, and temporal model simply because they want to manage connected information.

The intended dependency direction is:

```text
Application
    ↓
Sorophy.Engine
    ↓
Structured Information
```

not:

```text
Sorophy.Engine
    ↓
Orbpad-specific behavior
```

## Future Snapshot Layer

The architecture intentionally leaves room for a read-only temporal projection layer:

```text
SorophyGraph + History + Time
            ↓
      SorophyGraphSnapshot
            ↓
      Query / Diff / Analysis
```

This is planned work, not a current public API contract.

## Design Rule

The most important architectural rule for Krono is still:

> **Keep state ownership, mutation, temporal semantics, history, serialization, and application experience separate enough that one subsystem can evolve without contaminating the others.**
