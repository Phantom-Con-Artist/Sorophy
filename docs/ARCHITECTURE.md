<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Architecture</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines the architectural boundaries of **Sorophy v2: Krono** as implemented by **Sorophy.Engine 2.0.0**.

Krono extends the v1 graph engine into a **Temporal Graph Evolution Core** without turning the engine into a monolith. The architecture remains intentionally layered: Sorophy provides structural information primitives and temporal mechanics, while applications provide domain semantics and user experience.

## Architectural Philosophy

Krono follows a strict separation of responsibility:

```text
Philosophy
    ↓
Architecture
    ↓
Sorophy.Engine
    ↓
Ecosystem / Applications
    ↓
User Semantics
```

Sorophy understands **structure, state, relationships, time, and structural evolution**.

Applications understand **what those structures mean**.

This distinction is fundamental. The engine must provide temporal graph mechanics without attempting to become a domain-specific workflow engine, event-sourcing framework, semantic knowledge system, or application runtime.

## High-Level Model

```text
┌──────────────────────────────────────────────────────────────┐
│                    Applications / Ecosystem                  │
│              Orbpad · Editors · Domain Tools                 │
│                                                              │
│     Application semantics determine what an Event means     │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                        Sorophy.Engine                        │
│                                                              │
│ Entity · Graph · Relationship · Typed Values · Tags          │
│ Time · Event Entities · Evolution · Executor · History      │
│ Snapshot · Temporal Query Domain · Graph Diff · Validation   │
└───────────────┬──────────────────┬───────────────────────────┘
                │                  │
                ▼                  ▼
        Serialization          Storage / Files
                │                  │
                └────────┬─────────┘
                         ▼
                 .entity / .lore
```

## Core Architectural Layers

Krono can be understood as several cooperating boundaries rather than one monolithic subsystem.

```text
Entity Model
     │
     ▼
Canonical Graph State
     │
     ├───────────────┐
     ▼               ▼
Evolution       Direct Graph Operations
     │
     ▼
Evolution Executor
     │
     ├───────────────┐
     ▼               ▼
Current Graph    Historical Fact Store
     │               │
     └───────┬───────┘
             ▼
       Temporal Projection
             │
       ┌─────┼─────┐
       ▼     ▼     ▼
   Snapshot  TQD  Graph Diff
             │
             ▼
       Read-only Analysis
```

Each layer has a distinct responsibility.

## Canonical State vs Derived State

Krono maintains an explicit distinction between authoritative graph state and derived supporting structures.

```text
Canonical
├── Entities
├── Relationships
├── Relationship Histories
└── Retired Relationship Identities

Derived / Supporting
├── Tag Index
├── Relationship Indexes
└── Adjacency Storage
```

Canonical state is authoritative.

Indexes, adjacency structures, and other accelerators exist to make access efficient and deterministic. They must remain consistent with canonical state but must never become an alternative source of truth.

Temporal projections such as snapshots are also **derived views**, not replacement graph state.

## SorophyGraph

`SorophyGraph` remains the canonical owner of active graph state.

It owns and coordinates:

- entities;
- active relationships;
- relationship identity retirement;
- graph indexes;
- adjacency structures;
- referential integrity;
- validation;
- temporal projection entry points;
- persistence-facing graph state.

The implementation uses partial classes for maintainability, but conceptually the public graph remains one `SorophyGraph`.

The graph does not delegate its authority to indexes, snapshots, queries, or application-specific objects.

## Entity Model Boundary

Entities remain independently constructible and structurally generic.

A Krono entity contains structural information such as:

```text
ID
Name
Type
Description
Tags
Documents
Properties
OccurredAt?   ← Event semantics
```

Graph membership does not redefine entity identity and does not require application-specific graph subclasses.

### Entity Lifecycle

Krono deliberately keeps the core entity lifecycle smaller than the relationship evolution system:

```text
Creation
    ↓
Property Mutation
    ↓
Termination
```

Directly added entities represent baseline graph state unless temporal lifecycle semantics are explicitly introduced through the supported model.

The engine does not fabricate historical timestamps for direct mutations.

## Event Entity Boundary

An Event is a normal `SorophyEntity` whose type identifies it as an Event.

```text
SorophyEntity
    │
    └── Type = Event
          │
          └── OccurredAt = temporal coordinate
```

Event Entities are **first-class passive temporal anchors**.

They are:

- declarative;
- structurally represented as entities;
- temporal through `OccurredAt`;
- usable as provenance for evolutions;
- not executable objects.

### Events Do Not Execute

Krono intentionally has no `event.Execute()` lifecycle.

The correct architectural pipeline is:

```text
Event Entity
     │
     │ EventEntityId
     ▼
Evolution Operation
     │
     ▼
SorophyRelationshipEvolutionExecutor
     │
     ├── mutate canonical graph
     └── record historical fact
```

The application determines what structural consequences an Event has.

A single Event may anchor multiple Evolutions.

This allows the engine to understand temporal structure without embedding application semantics into the Event itself.

### Event Entities Are Not Relationship Endpoints

Normal relationships remain strictly:

```text
Entity → Relationship → Entity
```

An Event Entity is not a special relationship endpoint.

Its role is temporal anchoring and provenance, not graph topology.

## Temporal Boundary

`SorophyTime` is a semantic temporal model rather than a generic CLR timestamp.

Temporal values are represented through explicit schemas, units, positions, precision, and numeric representation.

Temporal comparison is deliberately strict.

Operations fail explicitly when the participating values have incompatible:

- schemas;
- units;
- numeric representations;
- temporal semantics.

Krono does not guess conversions or invent temporal meaning.

## Event-Anchored Temporal Semantics

For an Evolution anchored to an Event:

```text
Event.OccurredAt
       │
       ▼
Evolution temporal coordinate
       │
       ▼
Historical Fact.At
```

`Event.OccurredAt` is authoritative.

An Event-anchored Evolution does not introduce a conflicting independent `At` coordinate.

Events occurring at the same temporal coordinate are temporally equivalent. Krono does not infer deterministic causal ordering between them.

## Relationship Model

Normal relationships remain structural graph edges:

```text
Source Entity
     │
     ▼
Relationship
     │
     ▼
Target Entity
```

A relationship contains current structural state, including its identity, type, properties, and validity interval.

Temporal validity and historical recording are separate concepts.

```text
ValidFrom / ValidTill
    = semantic validity interval

At
    = temporal coordinate of a recorded historical fact
```

`At` does not automatically equal `ValidFrom`.

## Relationship Evolution Boundary

Relationship evolution is deliberately separated from ordinary relationship mutation.

The architecture is:

```text
SorophyRelationshipEvolution
          │
          ▼
SorophyRelationshipEvolutionExecutor
          │
          ▼
SorophyGraph
```

Krono defines five structural relationship evolution operations:

```text
Creation
Property Modification
Type Change
Validity Change
Termination
```

Each operation may optionally carry `EventEntityId`.

The executor is responsible for applying the structural transition and recording the corresponding historical fact where required.

### Creation

A Creation Evolution establishes a relationship as a temporal transition and preserves the initial historical fact.

Direct `graph.AddRelationship(...)` remains a baseline structural operation and does not fabricate temporal history.

### Property Modification

Changes relationship properties while preserving the relationship identity and recording the resulting historical state.

### Type Change

Changes the relationship type while preserving identity and recording the structural transition.

### Validity Change

Changes `ValidFrom` and/or `ValidTill` and records the temporal state transition.

### Termination

Terminates the relationship through an explicit evolution.

Termination records the historical state and retires the relationship identity.

Ordinary relationship deletion does **not** automatically synthesize a termination history.

## Relationship Identity Retirement

Relationship identity is never silently reusable after retirement.

```text
Active Relationship ID
        │
        ▼
Termination Evolution
        │
        ├── historical fact
        └── retired identity
```

Krono persists retired relationship identities and exposes them for inspection.

This prevents a previously meaningful relationship identity from being accidentally reused for a different structural relationship.

Ordinary deletion and temporal termination remain distinct operations.

## Historical State

Relationship history is relationship-centric and append-only.

```text
Active Relationship
    = current structural truth

Historical Fact
    = recorded temporal truth

Relationship History
    = append-only sequence of facts
```

A historical fact records the structural state relevant to the transition, including:

```text
At
RelationshipId
SourceId
TargetId
Type
Properties
ValidFrom
ValidTill
EventEntityId
```

History is not an edit log.

Krono does not claim to reconstruct every direct mutation ever performed. History exists only where the temporal evolution model explicitly records it.

## Event Provenance

`EventEntityId` provides persistent provenance for an Evolution.

The executor validates that a supplied provenance ID:

1. exists;
2. identifies an entity;
3. identifies an Event Entity.

Invalid, dangling, or non-Event provenance is rejected.

Provenance therefore answers:

> Which Event Entity anchored this structural Evolution?

It is **not** an audit log and does not transform Krono into an event-sourcing architecture.

## Temporal Snapshots

Krono provides a read-only temporal projection layer over canonical graph state and recorded history.

```text
SorophyGraph
     +
Relationship History
     +
SorophyTime
     │
     ▼
SorophyGraphSnapshot
```

A snapshot represents the graph as reconstructed at a requested temporal coordinate.

Snapshots are:

- read-only;
- isolated from the canonical graph;
- derived from canonical state plus historical facts;
- temporally reconstructed;
- post-transition / inclusive at the requested coordinate.

An Event-based snapshot resolves through the Event's `OccurredAt`.

```text
CreateSnapshot(time)
CreateSnapshot(event)
```

Events at the same temporal coordinate therefore produce equivalent temporal snapshots.

Snapshots do not become an alternate source of truth.

## Temporal Query Domain

The **Temporal Query Domain (TQD)** is a read-only query layer over the temporal model.

```text
Canonical Graph + History
            │
            ▼
      Temporal Query
            │
       ┌────┼────┐
       ▼    ▼    ▼
    Point  Interval  History
    Queries Queries   Queries
```

The query domain supports temporal inspection such as:

- point-in-time entity and relationship state;
- outbound and inbound relationships;
- relationship history;
- historical facts within temporal intervals;
- modified relationship IDs;
- facts associated with Event Entities;
- relationships evolved by Event Entities.

Point-in-time queries delegate to snapshot semantics.

TQD does not mutate the graph, infer causality, or create a new semantic query language.

## Graph Diff

Graph Diff compares two graph snapshots as structural states.

```text
Snapshot A
    │
    ├──────────┐
    │          │
    ▼          ▼
          SorophyGraphDiff
    ▲          │
    │          ▼
Snapshot B    Change Set
```

The diff model identifies:

- added entities;
- removed entities;
- modified entities;
- added relationships;
- removed relationships;
- modified relationships.

Graph Diff is intentionally structural.

It does not infer:

- causality;
- Event semantics;
- application meaning;
- multi-hop explanations;
- patches;
- merges;
- undo operations.

Deterministic ordering is provided for stable structural results.

## Temporal Projection Boundary

Snapshot, TQD, and Graph Diff form a read-only projection/analysis layer.

```text
Canonical State
      +
Historical State
      +
Temporal Model
      │
      ▼
Projection Layer
      │
      ├── Snapshot
      ├── Temporal Query Domain
      └── Graph Diff
```

These systems inspect the graph without becoming owners of graph state.

## Persistence Boundary

`.lore` v2 persists the temporal graph model explicitly.

The persisted model includes:

```text
formatVersion = 2
entities
relationships
relationshipHistories
retiredRelationshipIds
```

Historical facts preserve Event provenance through `eventEntityId` where present.

Serialization reflects existing state. It does not manufacture history from the absence of a record, from direct deletion, or from other unsupported assumptions.

Deserialization validates the reconstructed state rather than silently repairing corrupted data.

## Validation Boundary

Krono treats validation as an explicit architectural responsibility.

Validation covers:

- entity and relationship IDs;
- referential integrity;
- relationship endpoints;
- historical fact integrity;
- temporal schema compatibility;
- relationship retirement;
- active/retired identity overlap;
- Event provenance;
- Event Entity type;
- persistence round-trip integrity.

Corrupted or inconsistent state is rejected rather than silently repaired.

## Cross-Platform Boundary

Sorophy.Engine targets **.NET 10** and is designed as a portable engine rather than a platform-specific application component.

The persistence model is exercised across Windows, Linux, and macOS to verify that `.lore` state can be exported, ingested, re-serialized, and consumed across platforms.

Platform-specific application behavior belongs above the engine boundary.

## Canonical vs Projection Architecture

The complete temporal architecture can be summarized as:

```text
                 APPLICATION
                      │
              Application Semantics
                      │
                      ▼
                Event Entity
                OccurredAt
                      │
                      │ EventEntityId
                      ▼
              Evolution Operation
                      │
                      ▼
           Evolution Executor
                      │
              ┌───────┴────────┐
              ▼                ▼
       Canonical Graph    Historical Facts
              │                │
              └───────┬────────┘
                      ▼
               Temporal Model
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
       Snapshot      TQD       Graph Diff
          │           │           │
          └───────────┴───────────┘
                      ▼
               Read-only Views
```

This boundary is central to Krono: **the graph owns state, Evolutions change state, History records temporal facts, and projections inspect state.**

## What Krono Is Not

Krono is deliberately not:

- an event-sourcing framework;
- an audit-log framework;
- a workflow engine;
- a domain-driven semantics engine;
- an automatic causal inference system;
- an NLP query engine;
- an application UI;
- a graph visualization framework;
- an undo/redo system;
- an application-specific runtime.

These concerns may exist in the wider Saga ecosystem, but they do not belong inside the Sorophy structural core unless explicitly introduced as future architectural layers.

## Design Rules

The most important architectural rules for Krono are:

1. **State ownership remains explicit.**
2. **Canonical graph state is authoritative.**
3. **Derived indexes and projections are never sources of truth.**
4. **Events are passive temporal anchors.**
5. **Events do not execute themselves.**
6. **Application semantics determine which Evolutions an Event causes.**
7. **Evolutions perform structural graph transitions.**
8. **Event-anchored Evolutions derive their temporal coordinate from `Event.OccurredAt`.**
9. **Normal Relationships remain Entity → Relationship → Entity.**
10. **Historical state is explicit and append-only where recorded.**
11. **Direct mutations do not fabricate temporal history.**
12. **Relationship identity retirement prevents reuse of terminated identities.**
13. **Temporal comparison is strict and never based on guessed conversions.**
14. **Snapshots, TQD, and Diff are read-only projections.**
15. **Serialization persists explicit state; it does not invent history.**
16. **Validation rejects corrupted state rather than silently repairing it.**
17. **Application behavior stays above the Sorophy.Engine boundary.**

> **Keep state ownership, structural mutation, temporal semantics, historical state, persistence, and application experience separate enough that one subsystem can evolve without contaminating the others.**

That separation is what allows Krono to remain a reusable temporal graph core rather than becoming a monolithic application engine.
