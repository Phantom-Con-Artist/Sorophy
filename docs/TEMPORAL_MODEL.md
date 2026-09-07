<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="Sorophy v2 · Krono"
     width="900"/>

<p><strong>SOROPHY V2 · KRONO</strong></p>

<h1>Temporal Model</h1>

<p>
Krono Temporal Graph Evolution Core
</p>

</div>

This document defines the **Krono Temporal Model**, the semantic time system implemented by **Sorophy.Engine 2.0.0**.

## Core Principle

Krono treats time as semantic information.

The engine does not assume that every meaningful timeline is the wall-clock calendar of the host operating system. A domain can define its own temporal schema and units.

Time is part of the information model rather than merely a formatting convention.

## Core Types

The temporal model includes:

```text
SorophyTime
SorophyTimeSchema
SorophyTimeUnit
SorophyTimePositionDefinition
SorophyTimePrecision
```

An `SorophyTime` belongs to a temporal schema and describes a position using the units defined by that schema.

## Why a Schema Exists

Two values that look numerically similar are not necessarily points on the same timeline.

For example:

```text
Timeline A: Age / Year
Timeline B: Cycle / Phase / Tick
```

The engine therefore carries temporal schema identity with the value.

Values participating in one temporal invariant must use compatible temporal semantics.

## Strict Temporal Comparison

Krono does not guess how unrelated temporal values should be compared.

Compatibility is validated across the temporal schema, unit, and supported numeric representation.

When values are incompatible, the operation fails explicitly rather than silently converting or inventing a relationship between the timelines.

This is important for custom temporal systems where a numeric position alone has no universal meaning.

## Precision

Temporal values can carry semantic precision rather than pretending every point is an infinitely exact timestamp.

This allows applications to represent information whose temporal precision is less than exact without reducing that uncertainty to an arbitrary string convention.

## Relationship Validity

`SorophyRelationship` supports:

```text
ValidFrom : SorophyTime?
ValidTill : SorophyTime?
```

These fields describe the semantic interval during which the relationship state is considered valid.

They are separate from the historical fact coordinate `At`.

## Three Temporal Concepts

Krono explicitly distinguishes three related concepts:

```text
At
    = temporal coordinate of a recorded historical fact

ValidFrom
    = when the represented relationship state becomes valid

ValidTill
    = when that relationship state ceases to be valid
```

These values must not be collapsed into a single timestamp.

For example:

```text
Relationship state:
    ValidFrom = Year 100
    ValidTill = Year 150

Historical fact:
    At = Year 150
```

The historical coordinate records when the fact belongs in temporal history; the validity interval describes the semantic lifetime of the represented relationship state.

## Event Temporal Anchoring

Event Entities provide first-class temporal anchors.

An Event is a `SorophyEntity` classified as:

```text
Type = "Event"
```

and carries:

```text
OccurredAt
```

`Event.OccurredAt` is the authoritative temporal coordinate of the Event.

For an Event-anchored Evolution:

```text
Event.OccurredAt
       │
       ▼
Evolution temporal coordinate
       │
       ▼
Historical Fact.At
```

The Evolution does not introduce a conflicting independent `At` coordinate.

One Event may anchor multiple Evolutions.

Events at the same temporal coordinate are temporally equivalent. Krono does not infer a deterministic temporal or causal ordering between them.

## Temporal State and Snapshots

The temporal model is consumed by the read-only projection layer.

```text
Graph State
    +
Historical Facts
    +
SorophyTime
        │
        ▼
SorophyGraphSnapshot
```

A snapshot represents the graph as reconstructed at the requested temporal coordinate.

Snapshot semantics are inclusive/post-transition at the requested coordinate.

An Event-based snapshot resolves through `Event.OccurredAt`.

## Temporal Queries

The Temporal Query Domain (TQD) builds read-only queries on top of the same temporal model.

It supports inspection of:

- point-in-time state;
- temporal intervals;
- relationship history;
- historical facts;
- Event provenance;
- relationships evolved by Event Entities.

Point-in-time queries use snapshot semantics rather than introducing a separate temporal interpretation.

## Temporal Boundaries

The temporal model does not attempt to solve every possible question about chronology.

In particular, Krono does not automatically impose arbitrary ordering semantics over custom temporal representations merely because values can be represented numerically.

Domain-specific chronological reasoning belongs to higher layers when the relevant temporal schema provides enough meaning to support it.

## Design Principle

The temporal system exists to make time **structured information**:

```text
Temporal Schema
      │
      ▼
  SorophyTime
      │
      ├── Relationship Validity
      ├── Event Occurrence
      └── Historical Coordinates
              │
              ▼
       Temporal Projections
```

The goal is not to force every domain into one universal clock.

The goal is to provide explicit temporal structure that the rest of Krono can use consistently.
