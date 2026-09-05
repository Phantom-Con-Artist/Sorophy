# Changelog

All notable changes to **sorophyv2: Krono** and its
**Sorophy.Engine 2** are documented in this file.

---

# [2.0.0-beta.1] — Krono

**Release status:** Beta

**Architectural generation:** Krono

**Implementation:** Sorophy.Engine 2

**Target framework:** .NET 10

**License:** GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)

> **Krono is the second-generation architecture of sorophyv2,
> implemented by Sorophy.Engine 2.**

## ✦ Krono Foundation

- Expanded entity semantics.
- Entity descriptions.
- Entity tags and tag indexing.
- Embedded structured entity documents.
- Semantic event classification through `SorophyEntity`.
- Semantic temporal model through `SorophyTime`.
- Temporal schemas, units, positions, and precision.
- Relationship validity through `ValidFrom` and `ValidTill`.
- Relationship evolution operations.
- Explicit evolution executor.
- Immutable historical relationship facts.
- Append-only relationship histories.
- Permanent relationship identity retirement.
- Expanded graph validation and invariants.

## ✦ Relationship Evolution

Krono introduces explicit relationship state transitions:

- Relationship creation.
- Relationship type changes.
- Relationship property modification.
- Relationship validity changes.
- Relationship termination.

Evolution descriptions remain separate from execution.

## ✦ Historical State

Krono introduces historical relationship state through:

- `SorophyRelationshipFact`
- `SorophyRelationshipHistory`
- Graph-level history storage.
- Historical capture before mutation.
- Immutable historical facts.
- Relationship identity retirement.

## ✦ Temporal Model

Krono introduces semantic time rather than assuming
host-calendar timestamps.

The temporal system includes:

- `SorophyTime`
- `SorophyTimeSchema`
- `SorophyTimeUnit`
- `SorophyTimePositionDefinition`
- `SorophyTimePrecision`

Temporal schema compatibility is enforced where required.

## ✦ Verification

Current Krono development checkpoint:

```text
469 / 469 tests passed
0 failed
0 skipped