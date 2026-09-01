# Changelog

All notable changes to **Orbis Engine** are documented in this file.

The project follows a versioned release model, with pre-release versions used to identify changes while the API and underlying formats continue to mature.

---

# [1.0.0-beta.1]

**Release status:** Beta  
**Release date:** September 1, 2026  
**Target framework:** .NET 10  
**License:** GNU General Public License v3.0

> **This is the first public beta release of Orbis Engine.**

Orbis Engine `1.0.0-beta.1` establishes the initial V1 engine foundation for the broader Orbis Project.

This release focuses primarily on establishing a reliable core model, deterministic graph behavior, structured serialization, data fidelity, and a controlled public API.

---

## ✦ Added

### Core Graph Model

- Introduced `OrbGraph` as the central graph container.
- Added entity management through `OrbEntity`.
- Added relationship management through `OrbRelationship`.
- Added structured properties through `OrbProperty`.
- Added unique `Guid`-based identity for graph objects.
- Added graph validation and invariant enforcement.
- Added read-only views over graph entity and relationship collections.

### Typed Value System

- Introduced `OrbValue`.
- Introduced `OrbValueType`.
- Added explicit support for:
  - `Null`
  - `Boolean`
  - `Integer`
  - `Decimal`
  - `Double`
  - `String`
  - `Guid`
  - `DateTime`
  - `List`
  - `Object`
- Added validation of canonical Orb value representations.
- Established the canonical `long` representation for integer values.
- Established the canonical `decimal` representation for decimal values.

---

## ✦ Graph Relationships

- Added relationship insertion and removal.
- Added validation ensuring relationship endpoints reference existing entities.
- Added protection against invalid relationship insertion.
- Added relationship lookup operations.
- Added outgoing and incoming relationship queries.
- Added neighbor discovery.
- Added reachability checks.
- Added graph traversal operations.

### Entity Removal

Removing an entity now maintains graph integrity by removing relationships connected to the deleted entity.

For example:

```text
A ─────→ B
│
└──────→ C
```

Removing `A` results in:

```text
B

C
```

with the associated relationships removed.

---

## ✦ Graph Mutation Safety

The engine was hardened against failed graph mutations.

Failed relationship insertion does not leave behind partially mutated graph state.

This establishes an important V1 invariant:

> **A failed graph mutation must not silently corrupt the graph.**

Adversarial tests were added to verify this behavior.

---

## ✦ Entity Serialization

Introduced `EntitySerializer` for serializing and deserializing individual entities.

The serializer supports:

```text
OrbEntity
    ↓
EntitySerializer
    ↓
.entity representation
```

and:

```text
.entity representation
    ↓
EntitySerializer
    ↓
OrbEntity
```

Serialization includes:

- Entity identity
- Entity name
- Entity type
- Properties
- Typed values
- Nested structured values
- Format versioning

---

## ✦ Lore Serialization

Introduced `LoreSerializer` for serializing and deserializing complete graphs.

The `.lore` representation contains:

```text
Lore
├── Entities
└── Relationships
```

The serializer preserves:

- Entity identity
- Entity data
- Properties
- Relationships
- Relationship identity
- Relationship metadata
- Typed values
- Nested structured values
- Format versioning

---

## ✦ Document Boundaries

Established explicit serialization document models for `.entity` and `.lore`.

Serialization document types are treated as implementation details rather than part of the intended public engine API.

The public API therefore focuses on Orbis concepts such as:

```text
OrbGraph
OrbEntity
OrbRelationship
OrbProperty
OrbValue
OrbValueType
```

rather than exposing serialization plumbing to consumers.

---

## ✦ Nested Value Serialization

Added recursive handling of nested `List` and `Object` values.

Nested values can contain other structured values, including:

```text
Object
 ├── String
 ├── Integer
 ├── Guid
 ├── DateTime
 └── List
      ├── Integer
      ├── Object
      └── Boolean
```

The recursive codec ensures nested values are interpreted according to their encoded type information.

---

## ✦ Nested Type Fidelity

Added preservation of nested CLR types during serialization round trips.

Specifically hardened:

- `Guid`
- `DateTime`
- Integer primitive types
- Floating-point values
- Decimal values
- Nested lists
- Nested objects

This prevents nested values from silently degrading into incompatible CLR representations after deserialization.

For example:

```text
DateTime
   ↓
Serialize
   ↓
JSON
   ↓
Deserialize
   ↓
DateTime
```

rather than:

```text
DateTime
   ↓
Serialize
   ↓
JSON
   ↓
Deserialize
   ↓
String
```

---

## ✦ Nested Primitive CLR Type Fidelity

The beta establishes preservation of nested primitive CLR subtypes where they are represented by the Orb value system.

This addresses cases such as:

```text
int
short
long
uint
float
double
decimal
```

being placed inside nested lists or objects.

The goal is to prevent a round trip from unexpectedly transforming the original primitive representation into an unrelated CLR type.

---

## ✦ Serialization Failure Handling

Serialization failures caused by unsupported or invalid nested values are normalized through the serializer's public exception contract.

Invalid nested values such as:

```text
double.NaN
double.PositiveInfinity
double.NegativeInfinity
```

are rejected.

Unsupported nested CLR objects are also rejected.

The public serializer surface exposes `InvalidOperationException` for these serialization failures rather than leaking framework-specific exceptions from the underlying JSON implementation.

---

## ✦ Storage

Added storage functionality for entity and lore documents.

The storage layer provides operations for:

```text
EntityStorage
├── Save
└── Load

LoreStorage
├── Save
└── Load
```

Storage remains separated from the graph and serialization layers.

---

## ✦ Public API Surface

Introduced explicit public API surface testing.

The V1 public API is intentionally limited to the engine concepts required by consumers.

Implementation details such as:

```text
OrbValueCodec
serialization document models
internal conversion infrastructure
```

are not intended to form part of the public API.

An API surface regression test suite was added to prevent accidental exposure of implementation types.

---

## ✦ Release Metadata

The project has been prepared as a .NET package with:

- Package ID: `Orb.Engine`
- Version: `1.0.0-beta.1`
- Target framework: `.NET 10`
- Package author: `Subhradeep Sarkar`
- License: `GPL-3.0-only`
- Repository metadata
- Package README metadata
- XML documentation generation
- Symbol package generation

---

# ✦ V1 Audit

Before the beta release, the engine underwent a dedicated V1 audit covering its fundamental behavioral contracts.

```text
V1 AUDIT

1. Integer Contract              🟢
2. List/Object Contract          🟢
3. Property Identity              🟢
4. Entity Serialization           🟢
5. Lore Serialization             🟢
6. Document Boundary              🟢
7. Graph Mutation Invariants      🟢
8. Traversal Semantics            🟢
9. Round-trip Fidelity            🟢
10. Public API Surface            🟢
11. Package/Release Readiness     🟡
```

The engine's functional and API audit was backed by an expanding automated test suite.

### Beta Test Status

```text
251 tests
251 passed
0 failed
0 skipped
```

The test suite includes normal, edge-case, adversarial, serialization, traversal, fidelity, and API-surface tests.

---

# ✦ Beta Stability

`1.0.0-beta.1` should be considered an **early public beta**.

The underlying architecture has been deliberately tested, but the project has not yet reached a stable API commitment.

During the beta period:

- Public APIs may change.
- Method signatures may change.
- Serialization formats may change.
- Validation rules may become stricter.
- Internal architecture may change.
- Additional graph capabilities may be introduced.
- Breaking changes may occur between beta releases.

Applications integrating Orb.Engine should therefore pin their package version.

---

# ✦ Known Limitations

The following areas remain intentionally open for future development:

- Long-term API stabilization.
- Expanded developer documentation.
- Additional storage backends and capabilities.
- Performance benchmarking and optimization.
- Expanded graph algorithms.
- Broader ecosystem tooling.
- Application-level integrations.
- Orbpad integration.
- Future Orbis document and ecosystem concepts.

These limitations do not necessarily indicate defects in `1.0.0-beta.1`; they represent areas outside the current beta foundation.

---

# ✦ What's Next

Future releases are expected to focus on:

1. Continued API stabilization.
2. Expanded documentation.
3. Performance profiling.
4. Additional graph capabilities.
5. Improved tooling.
6. Ecosystem integration.
7. Feedback from real-world consumers.
8. Preparation for a stable V1 release.

The exact roadmap may evolve as the engine is used by applications and developers.

---

# ✦ Versioning

Orbis Engine follows semantic-style versioning with pre-release identifiers.

Examples:

```text
1.0.0-beta.1
1.0.0-beta.2
1.0.0-rc.1
1.0.0
```

Pre-release versions may contain breaking changes even when their major/minor components remain unchanged.

---

# ✦ Copyright

Copyright © 2026 **Subhradeep Sarkar**

Orbis Engine is distributed under the terms of the GNU General Public License v3.0.

See `LICENSE` for the complete license text.

---

<div align="center">

<strong>Orbis Engine</strong>

<br/>

Structured information. Connected by design.

<br/><br/>

© 2026 <strong>Subhradeep Sarkar</strong>

</div>