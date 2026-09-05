# Changelog

All notable changes to **Sorophy™** are documented in this file.

Sorophy™ uses semantic-style versioning.

Pre-release identifiers such as `-beta.N` and `-rc.N` identify development milestones and do not receive stable release grades. Stable releases may receive either **Grade A — Silver Standard** or **Grade S — Gold Standard** according to the verification requirements defined for that release.

---

# [1.0.0]

**Release status:** Stable  
**Release grade:** **Grade A — Silver Standard**  
**Release date:** September 2, 2026  
**Target framework:** .NET 10  
**Package ID:** `Sorophy.Engine`  
**License:** GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)

> **Grade A means that Sorophy™ is stable enough for the particular purposes represented by its documented capabilities and the workload classes verified for this release. It is not a claim of universal stability for every possible workload, platform, integration, or use case.**

## ✦ Release Summary

Sorophy™ `1.0.0` is the first stable release of the engine foundation.

The release establishes a verified core for structured information and graph workloads, including graph mutation, relationships, traversal, typed values, serialization, storage, validation, deterministic execution, allocation reuse, scaling, corruption handling, recovery, and long-duration endurance.

The release is designated **Grade A — Silver Standard** because the documented engine capabilities were exercised through both the complete unit-test suite and a dedicated stress-test arsenal, with no release-gate failure in the verified scope.

---

## ✦ Core Graph Model

- `SorophyGraph` serves as the authoritative graph container.
- `SorophyEntity`, `SorophyRelationship`, and `SorophyProperty` provide the core structured graph model.
- Graph objects use stable `Guid` identities.
- Entity and relationship collections expose controlled read access.
- Relationship insertion and removal maintain graph integrity.
- Removing an entity removes its incident relationships.
- Relationship endpoint validation prevents references to nonexistent entities.
- Graph validation checks the consistency of the graph's canonical and derived state.
- Internal adjacency indexing accelerates relationship-oriented operations while canonical entity and relationship stores remain authoritative.

The principal graph invariant is:

> **A failed graph mutation must not silently corrupt graph state.**

---

## ✦ Typed Value System

`SorophyValue` and `SorophyValueType` provide the engine's structured value system.

The supported `SorophyValueType` categories are:

```text
Null
Boolean
Integer
Decimal
String
Guid
DateTime
List
Object
```

The release establishes:

- `Int64` / `long` as the canonical integer representation.
- `Decimal` as the engine's decimal value category.
- CLR floating-point inputs such as `float` and `double` are handled through the engine's decimal-oriented representation rather than through a separate `SorophyValueType.Double` member.
- Recursive `List` and `Object` values.
- Validation of canonical value representations.
- Nested primitive and structured-value fidelity through serialization round trips.

---

## ✦ Graph Relationships and Traversal

Added and hardened:

- Relationship lookup.
- Outgoing relationship queries.
- Incoming relationship queries.
- All-relationship queries.
- Neighbor discovery.
- Reachability checks.
- Graph traversal.
- Self-relationship handling.
- Parallel-relationship handling.
- Relationship endpoint validation.
- Entity-removal relationship cleanup.

The implementation was exercised against sparse graphs, long chains, high-degree hubs, parallel relationships, self-links, and repeated add/remove churn.

---

## ✦ Adjacency Storage and Reuse

The adjacency implementation was hardened for repeated relationship creation and deletion.

The release includes dedicated pool-reuse verification at 1,000, 10,000, and 100,000 relationship scales. The measured re-add phase at the tested high scale allocated zero additional bytes after the initial adjacency-storage wave was removed, while final graph validation remained successful.

---

## ✦ Serialization and Persistence

`EntitySerializer` and `LoreSerializer` provide entity and complete-graph serialization for `.entity` and `.lore` documents.

The stable release preserves and verifies:

- Entity identity.
- Entity data.
- Relationship identity.
- Relationship metadata.
- Properties.
- Typed values.
- Nested lists and objects.
- Format-version information.
- Deterministic serialized output.
- Canonical round-trip reconstruction.
- Filesystem persistence.
- Rejection of malformed and unsupported documents.
- Rejection of invalid GUIDs, invalid endpoints, and invalid property representations.

Large-scale serialization verification reached **1,000,000 entities and 1,000,049 relationships** with a lore document of approximately **374.97 MB**.

---

## ✦ Storage and Recovery

`EntityStorage` and `LoreStorage` provide persistence operations for entity and graph documents while remaining separated from the graph and serializer layers.

The stable release verifies rejection of:

- Missing files.
- Empty documents.
- Truncated documents.
- Interrupted writes.
- Structurally corrupted documents.
- Invalid relationship endpoints.

Known-good persisted state was also verified for recovery after tested corruption scenarios.

---

## ✦ Public API Surface

The intended public API is centered on:

```text
SorophyGraph
SorophyEntity
SorophyRelationship
SorophyProperty
SorophyValue
SorophyValueType
EntitySerializer
LoreSerializer
EntityStorage
LoreStorage
```

Internal serialization codecs, conversion infrastructure, document models, and other implementation details are not intended to form part of the public consumer API.

---

# ✦ Verification and Release Qualification

`1.0.0` was subjected to a complete deterministic unit-test suite and a dedicated stress-test arsenal.

## Unit Test Verification

```text
251 total
251 passed
0 failed
0 skipped
```

The 251 unit tests act as the stable-core regression contract and cover:

- Graph mutation invariants.
- Entity insertion and removal.
- Relationship insertion and removal.
- Referential integrity.
- Entity-removal relationship cleanup.
- Duplicate and invalid operations.
- Self-relationships and parallel relationships.
- Traversal and reachability.
- Typed-value contracts.
- Nested lists and objects.
- Integer and decimal representation contracts.
- Floating-point CLR inputs.
- `Guid` and `DateTime` fidelity.
- Entity serialization.
- Lore serialization.
- Serialization round-trip fidelity.
- Malformed and invalid documents.
- Storage behavior.
- Serializer failure behavior.
- Public API surface constraints.
- Adversarial graph behavior.

---

## Stress-Test Arsenal

The complete release stress program consisted of **12 campaigns** and **49 release-gate verification checks**.

All campaigns were executed using the `full` profile with a configured workload of **1,000,000 operations** and an audit interval of **10,000**. Individual campaigns interpret that configured workload according to their test design; some execute one-million-operation workloads directly, while others distribute the workload across multiple graph sizes, independent seed families, or specialized benchmark stages.

| Campaign | Purpose | Release verification |
|---|---|---|
| **Mutation Chaos** | Exercise randomized graph mutation under sustained turbulence and detect invariant violations, failed-mutation corruption, relationship inconsistencies, and query failures. | **PASS.** 1,000,000-operation configuration; workload reached approximately 100,000 entities and tens of thousands of relationships while maintaining graph integrity. |
| **Deterministic Replay** | Verify deterministic journal generation, replay equivalence, independent graph agreement, and reference-model consistency. | **PASS.** Two independent 1,000,000-operation journals with repeated audits. |
| **Mutation Performance** | Measure isolated mutation latency and allocation behavior without contaminating timed regions with setup, reference-model execution, or full validation. | **PASS.** 1,000,000-operation configuration; entity and relationship mutation, edge cleanup, churn, allocation, and mixed mutation workloads all completed successfully. |
| **Pool Reuse** | Verify that released adjacency storage is reused after deletion instead of causing continuous allocation growth. | **PASS.** Tested at 1,000, 10,000, and 100,000 relationship scales; the measured re-add wave showed zero additional allocation at the tested high scale. |
| **High-Degree Topology** | Exercise pathological high-degree adjacency behavior, including self-links, parallel relationships, queries, traversal, large-scale removal, and final validation. | **PASS.** 100,000-edge hub with approximately 50,000 outgoing, 50,000 incoming, and 99,900 distinct neighbors. |
| **Performance Benchmark** | Establish empirical performance baselines across representative graph operations and graph sizes. | **PASS.** Full 1,000,000-operation configuration. Fastest recorded operation: approximately 21.2M ops/s for entity containment at 10,000 entities. Peak working set reached approximately 3.34 GB during the full benchmark workload. |
| **Relationship Scaling** | Measure relationship-query behavior across sparse graph sizes and expose unintended dependence on total graph size. | **PASS.** Tested 1,000, 10,000, 50,000, and 100,000 entities. |
| **Memory Benchmark** | Measure managed-memory and working-set behavior across entity, relationship/index, sparse-property, and churn workloads. | **PASS.** Tested through 100,000 entities; entity footprint stabilized near 271 B/entity and relationship/index footprint decreased toward approximately 418 B/relationship. |
| **Differential Fuzzing** | Compare Sorophy™ against an independent reference model under deterministic randomized mutations and topology queries. | **PASS.** Five independent 1,000,000-operation seed families, totaling **5,000,000 differential-fuzz operations**. |
| **Serialization Torture** | Stress serialization, deserialization, deterministic output, filesystem persistence, large graphs, and malformed-input handling. | **PASS.** Reached **1,000,000 entities / 1,000,049 relationships** and approximately **374.97 MB** serialized graph size. |
| **Crash / Recovery Torture** | Verify rejection of damaged persistence artifacts and restoration of known-good state under sustained corruption/recovery workloads. | **PASS.** **1,000,000 recovery operations**, including **549,088 injected fault conditions** and **49,923 disk-backed recovery cycles**. |
| **Soak / Endurance** | Detect cumulative state drift, memory retention, allocator degradation, persistence instability, validation failures, and throughput collapse. | **PASS.** **1,000,000 cycles** consisting of **700,476 mutation cycles** and **299,524 query-heavy cycles**, with **1,400,952 relationship additions**, **1,400,952 relationship removals**, **700,476 entity additions**, **700,476 entity removals**, **10,000 in-memory persistence round trips**, **1,000 disk persistence round trips**, and **100 full audits**. Canonical state and final validation remained correct; retained managed-memory delta was **+255.77 KB** and final throughput was approximately **56,923 cycles/s**. |

### Stress Verification Count by Campaign

```text
Mutation Chaos                  4 checks
Deterministic Replay            4 checks
Mutation Performance             7 checks
Pool Reuse                       1 check
High-Degree Topology             1 check
Performance Benchmark           10 checks
Relationship Scaling             8 checks
Memory Benchmark                10 checks
Differential Fuzzing             1 check
Serialization Torture            1 check
Crash / Recovery Torture         1 check
Soak / Endurance                 1 check
────────────────────────────────────
TOTAL                            49 checks
```

The 49 checks are **Test Arsenal verification checks**, not 49 additional unit tests. They complement the separate **251-test unit suite**.

---

## Aggregate Release-Gate Result

The complete stable-release verification program concluded with:

```text
Unit tests:                 251 / 251 passed
Stress campaigns:            12 / 12 passed
Stress checks:              49 / 49 passed
Failures:                    0
Skipped release gates:       0
Profile:                     full
Seed:                        12345
Configured operations:       1,000,000
Audit interval:              10,000
```

**Overall release verification status: PASS**

The strongest evidence is the cross-test agreement: the same core graph structures survived randomized mutation, deterministic replay, independent reference-model comparison, high-degree topology, allocator churn, large-scale serialization, corruption and recovery, memory-pressure testing, and one-million-cycle endurance testing.

---

## ✦ Release Grade

### Grade A — Silver Standard

**Definition:** A stable release that has passed every mandatory release-gate test for its declared engine capabilities and tested workload classes, with no unresolved correctness failure observed in those release-gate paths.

Grade A releases are recommended for the specific purposes, capabilities, and workload classes covered by their documented verification scope.

### Grade S — Gold Standard

**Definition:** A stable release that satisfies every Grade A requirement and has additionally passed verification for every documented supported capability, every declared supported workload class, every officially supported runtime environment, and the compatibility guarantees defined by the release contract.

Grade S is the project's highest stable release designation and represents the Gold Standard for general-purpose use within the complete documented and officially supported scope.

---

## ✦ 1.0.0 Grade A Assessment

**Recommendation: `1.0.0` qualifies as a Grade A — Silver Standard stable release and is suitable as the foundational core of The Saga within the tested and documented scope.**

The assessment is based on the combination of the **251/251 unit-test suite**, **12/12 stress campaigns**, and **49/49 stress verification checks**.

The release verification demonstrates stable behavior across the tested responsibilities of:

- Graph mutation and relationship integrity.
- Traversal and reachability.
- Deterministic execution.
- Differential correctness against an independent reference model.
- High-degree graph topology.
- Allocation reuse and mutation churn.
- Memory behavior at tested scale.
- Serialization and deserialization.
- Filesystem persistence.
- Corruption rejection and known-good recovery.
- Large graph serialization.
- Long-duration mutation, query, persistence, and validation endurance.

The designation remains bounded by the tested scope. It does not establish unrestricted thread safety, universal filesystem atomicity, compatibility with every operating system/runtime, hardware-independent performance guarantees, or behavior for workloads outside the documented release verification program.

---

## ✦ Historical Beta Release

# [1.0.0-beta.1]

**Release status:** Beta  
**Release date:** September 1, 2026  
**Target framework:** .NET 10  
**License:** GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)

> **Historical milestone:** This was the first public beta release of Sorophy™.

`1.0.0-beta.1` established the initial engine foundation, including the core graph model, typed values, relationship behavior, serialization boundaries, nested-value fidelity, storage, validation, and the initial public API surface.

The beta-era release was explicitly pre-release. Its API, serialization behavior, validation rules, and internal architecture were subject to change. The `1.0.0` stable release supersedes the beta readiness status.

---

## ✦ Versioning

Stable releases use semantic-style `MAJOR.MINOR.PATCH` versions:

```text
1.0.0
1.0.1
1.1.0
2.0.0
```

Pre-release versions use explicit identifiers:

```text
1.0.0-beta.1
1.0.0-rc.1
```

Pre-release versions do not receive stable release grades `A` or `S`.

A release grade belongs to the specific stable release being evaluated. Later stable releases must be evaluated independently because their verified scope may differ.

---

## ✦ License

Sorophy™ is released under the **GNU Affero General Public License v3.0 or later (`AGPL-3.0-or-later`)**.

See [`LICENSE`](LICENSE) for the complete license text. The repository's `LICENSE` file is the authoritative legal text.

---

## ✦ What's Next

Future releases may extend the engine through:

- Additional graph algorithms.
- Expanded storage capabilities.
- Additional serialization tooling.
- Further performance optimization.
- Broader interoperability.
- Stable-contract-compatible API improvements where applicable.
- Additional Myriad Ecosystem integrations.
- Expanded verification and broader runtime/environment coverage.

Each subsequent stable release is evaluated independently against its own implementation, compatibility commitments, and verification scope.

---

## ✦ Copyright

Copyright © 2026 **Subhradeep Sarkar**

Sorophy™ is distributed under the terms of the **GNU Affero General Public License v3.0 or later**.

---

<div align="center">
<strong>Sorophy™</strong>
<br/>
Structured information. Connected by design.
<br/><br/>
© 2026 <strong>Subhradeep Sarkar</strong>
</div>
