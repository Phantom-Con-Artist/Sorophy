<div align="center">

<img src="../assets/sorophy-v2-cover.png"
     alt="sorophyv2 · Krono"
     width="900"/>

<p><strong>SOROPHYV2 · KRONO</strong></p>

<h1>Temporal Model</h1>

<p>
Krono Temporal Graph Architecture
</p>

</div>

This document defines the **Krono Temporal Model**, the semantic time system implemented by Sorophy.Engine 2.


Krono treats time as semantic information.

The engine does not assume that every meaningful timeline is the wall-clock calendar of the host operating system. A domain can define its own temporal schema and units.

## Core Types

The temporal model includes concepts such as:

```text
SorophyTime
SorophyTimeSchema
SorophyTimeUnit
SorophyTimePositionDefinition
SorophyTimePrecision
```

An `SorophyTime` belongs to a schema and describes a position using the units defined by that schema.

## Why a Schema Exists

Two values that look similar are not necessarily points on the same timeline.

For example:

```text
Timeline A: Age / Year
Timeline B: Cycle / Phase / Tick
```

The engine therefore carries temporal schema identity with the value.

When a relationship has `ValidFrom` and `ValidTill`, the values must belong to a compatible temporal schema.

## Precision

Temporal values can carry semantic precision rather than pretending every point is an infinitely exact timestamp.

This allows applications to represent information whose temporal precision is less than exact without turning that uncertainty into an arbitrary string convention.

## Relationship Validity

`SorophyRelationship` supports:

```text
ValidFrom : SorophyTime?
ValidTill : SorophyTime?
```

These fields describe the semantic interval during which the relationship state is considered valid.

They are separate from the historical fact's `At` coordinate.

## Three Temporal Concepts

Krono now distinguishes three ideas that must not be collapsed into one field:

```text
At
    When a historical fact is recorded in the temporal history.

ValidFrom
    When the represented relationship state becomes valid.

ValidTill
    When that relationship state ceases to be valid.
```

Example:

```text
Relationship state:
    ValidFrom = Year 100
    ValidTill = Year 150

Historical fact:
    At = Year 150
```

The fact says that at Year 150 the engine is recording the state that existed immediately before the transition ending it.

## Schema Consistency

Temporal values that participate in one temporal invariant must use compatible schemas.

The engine explicitly validates schema consistency for relationship validity and historical facts.

This protects against a particularly nasty class of bugs where values are numerically comparable but semantically unrelated.

## What the Temporal Model Does Not Do

The temporal model does not yet attempt to solve every question about time.

In particular, Krono does not automatically impose arbitrary ordering semantics over every custom temporal representation merely because values can be represented numerically.

Domain-specific chronological reasoning can be added where the schema supports it.

## Future Temporal Work

The immediate future is not a second time system. It is the **query layer over the existing one**:

```text
CreateSnapshot(T)
GetRelationshipAt(T)
GetHistoryAround(T)
Diff(T1, T2)
```

Those capabilities will turn the temporal model from a storage primitive into a queryable temporal graph.
