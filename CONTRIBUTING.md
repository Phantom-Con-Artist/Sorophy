# Contributing to Sorophy™

Thank you for contributing to **Sorophy™**, the structured information and graph core at the heart of The Saga.

Sorophy™ is maintained with a strong emphasis on explicit invariants, predictable behavior, serialization fidelity, a controlled public API, and adversarial testing. Contributions should preserve those properties rather than merely adding functionality.

## 1. Scope

This repository contains Sorophy™ and its automated verification projects.

The engine currently centers on:

- `SorophyGraph`
- `SorophyEntity`
- `SorophyRelationship`
- `SorophyProperty`
- `SorophyValue`
- `SorophyValueType`
- Entity and lore serialization
- Entity and lore storage
- Graph traversal and reachability
- Graph validation and mutation integrity

The repository also contains unit tests and the Sorophy™ stress-test arsenal.

## 2. Before You Start

For substantial architectural changes, open a GitHub issue or discussion before implementing the change.

This is especially important for changes involving:

- Public API behavior
- Graph invariants
- Relationship semantics
- Serialization formats
- Storage behavior
- Typed-value representation
- Internal adjacency or indexing structures
- Memory-management or pooling strategies
- Performance-sensitive code

A small bug fix or documentation correction usually does not require prior discussion.

## 3. Development Environment

The current engine targets **.NET 10**.

Clone the repository:

```bash
git clone https://github.com/Phantom-Con-Artist/Sorophy.git
cd Sorophy
```

Build:

```bash
dotnet build
```

Run the complete unit-test suite:

```bash
dotnet test
```

Build the release configuration:

```bash
dotnet build -c Release
```

Create the package:

```bash
dotnet pack -c Release
```

List the stress-test campaigns:

```bash
dotnet run --project Sorophy.Engine.StressTests -- list
```

## 4. Required Verification Before a Pull Request

A pull request that changes engine behavior should not be considered ready until the affected verification layers pass.

At minimum:

```bash
dotnet build
dotnet test
```

When the change affects graph behavior, serialization, storage, mutation, allocation, indexing, traversal, or recovery, run the relevant stress campaign as well.

For engine-core changes, the following command is useful for confirming the current campaign inventory:

```bash
dotnet run --project Sorophy.Engine.StressTests -- list
```

Do not report a campaign as passing unless the command actually completed successfully.

## 5. Testing Expectations

Sorophy™ uses both conventional unit tests and adversarial stress tests.

The unit-test suite currently verifies areas including:

- Graph mutation invariants
- Relationship validation
- Entity-removal cleanup
- Traversal and reachability
- Typed-value contracts
- Nested values
- Serialization and round-trip fidelity
- Storage behavior
- Public API surface
- Invalid-input and failure behavior

The stress-test arsenal is designed to exercise failure modes that ordinary examples do not expose.

Current campaign purposes include:

| Campaign | Purpose |
|---|---|
| Mutation Chaos | Randomized mutation sequences and graph integrity |
| Deterministic Replay | Reproducible operation journals and equivalent replay |
| Mutation Performance | Isolated mutation throughput and allocation behavior |
| Pool Reuse | Reuse of released adjacency storage after deletion |
| High-Degree Topology | Large incident-degree graphs, parallel edges, self-links, and mass unlinking |
| Performance Benchmark | General graph operation performance across scale points |
| Relationship Scaling | Query behavior as graph size increases |
| Memory Benchmark | Memory footprint and retention across entity/relationship scales |
| Differential Fuzzing | SorophyGraph behavior against an independent reference model |
| Serialization Torture | Large round trips and malformed-input rejection |
| Crash / Recovery Torture | Corruption detection and last-known-good recovery |
| Soak / Endurance | Repeated mutation/query/persistence cycles and cumulative degradation |

A change that modifies one of these areas should normally add or update tests covering the changed behavior.

## 6. Graph Invariants Matter

Do not treat graph validation as optional diagnostic output.

When changing mutation or relationship code, consider at least:

- Duplicate entity identity
- Duplicate relationship identity
- Missing relationship endpoints
- Self-links
- Parallel relationships
- Entity removal with incident relationships
- Outgoing adjacency
- Incoming adjacency
- Neighbor deduplication
- Traversal
- Reachability
- Failed mutation rollback
- Index consistency
- Adjacency consistency
- Pool-slot reuse

A mutation must either complete according to its contract or leave the graph in a valid state.

## 7. Serialization Rules

Serialization changes require particular care because the serialized representation is part of the engine's observable behavior.

When modifying serialization:

1. Add or update round-trip tests.
2. Test malformed input.
3. Test unsupported or invalid values.
4. Preserve typed-value semantics.
5. Check nested `List` and `Object` values.
6. Check `Guid`, `DateTime`, integer, floating-point, and decimal handling where applicable.
7. Check that failed deserialization does not leave a partially constructed graph exposed.
8. Consider compatibility implications before changing document structure.

Do not silently change serialized field names, value categories, or format-version behavior without documenting the change.

## 8. Public API Discipline

The public API is intentionally smaller than the internal implementation.

Internal helpers, document models, codecs, pools, and indexing structures should remain internal unless there is a documented reason to expose them.

Before adding a public type or member, ask:

- Is this required by consumers?
- Can the requirement be solved internally?
- Does exposing it create a long-term compatibility obligation?
- Does it fit the existing API model?
- Is there a test protecting the intended public surface?

Public API changes should be treated as architectural changes, not cosmetic changes.

## 9. Performance Contributions

Do not optimize solely from a single benchmark number.

For performance-sensitive changes, record:

- The workload
- Graph size
- Relationship topology
- Operation count
- Allocation behavior where relevant
- Runtime/framework
- Hardware
- Before/after measurements
- Whether correctness and validation remained unchanged

Benchmark output is evidence for a specific workload. It is not a universal performance guarantee.

Avoid changes that improve a micro-benchmark while weakening correctness, readability, failure behavior, or memory stability.

## 10. Memory and Pooling Changes

Sorophy™ uses internal graph indexes and adjacency storage intended to reduce repeated allocation.

Changes to these structures should be tested for:

- Correct removal
- Reuse after deletion
- Stale references
- Slot/index validity
- Self-links
- Parallel relationships
- High-degree entities
- Repeated add/remove churn
- Final graph validation

A memory optimization that leaves stale graph state is a regression, not an optimization.

## 11. Error Handling

Prefer explicit failure over silent corruption.

Invalid inputs should be rejected according to the engine's established exception and validation contracts.

Do not:

- Swallow exceptions without a documented reason
- Convert malformed data into a plausible but incorrect graph
- Leave partial mutation state behind
- Return success for an operation whose contract was not satisfied
- Hide corruption to make a test pass

## 12. Documentation

Documentation changes are welcome.

When changing behavior, update the relevant documentation in the same change where practical:

- `README.md`
- `CHANGELOG.md`
- API documentation
- Test documentation
- Release notes

Version references must remain consistent across documentation and package metadata.

Do not claim a capability, compatibility guarantee, or release-grade status that has not been verified.

## 13. Commit Guidelines

Use clear commit messages that describe the actual change.

Examples:

```text
Add differential fuzzing campaign
Fix adjacency removal invariant
Optimize relationship slab reuse
Document serialization failure contract
Add high-degree topology tests
```

Avoid messages such as:

```text
stuff
fix
changes
final final
```

The latter are fine for a private branch at 2 a.m.; they are not particularly useful history.

## 14. Pull Requests

A pull request should explain:

- What changed
- Why it changed
- Which files or subsystems were affected
- What tests were added or modified
- Which tests were run
- Whether public API behavior changed
- Whether serialized formats changed
- Whether performance or memory behavior changed

For performance or memory changes, include the relevant benchmark output or a concise before/after summary.

For bug fixes, include a minimal reproduction when possible.

## 15. Review Expectations

Review focuses on correctness before cleverness.

Reviewers may request changes when a contribution:

- Weakens an established invariant
- Introduces silent data loss
- Expands the public API without a clear requirement
- Changes serialization behavior without compatibility analysis
- Adds untested mutation paths
- Relies on a benchmark without representative workload evidence
- Makes the code materially harder to reason about without a measurable benefit

Disagreement is welcome. Personal attacks are not.

## 16. Reporting Bugs

A useful bug report should include:

- Sorophy™ version
- Commit or package version when known
- .NET version
- Operating system
- Minimal reproduction
- Expected behavior
- Actual behavior
- Exception or diagnostic output
- Relevant test command
- Seed and operation count for deterministic stress-test failures

For a stress-test failure, preserve the seed. Reproducibility is considerably more useful than "it broke once."

## 17. Security Issues

Do not publish sensitive security issues as public issues before the maintainer has had an opportunity to assess them.

Report security-sensitive problems privately through the maintainer contact mechanism provided by the repository.

Include enough information to reproduce the issue, but do not disclose credentials, private data, or exploit material that would unnecessarily increase risk.

## 18. License

Sorophy™ is released under the **GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later)**.

See [`LICENSE`](LICENSE) for the authoritative license text.

By submitting a contribution, you agree that the project may distribute your contribution under the applicable project license unless a separate written agreement states otherwise.

## 19. Final Principle

The project's development standard is simple:

> **Do not make the engine appear correct. Make it remain correct under hostile conditions.**

A successful contribution should improve the engine without weakening the invariants, verification coverage, or clarity that make the foundation dependable.

---

**Sorophy™**  
Structured information. Connected by design.

Copyright © 2026 Subhradeep Sarkar
