# Capability Model v1

The capability model is FoundationKit's canonical machine-visible composition vocabulary.

Each descriptor contains identity, display name, kind, maturity, category, description, dependencies, and a separate capability contract version. Profiles resolve capability selections through the same dependency graph.

Maturity values are:

- `Planned` — bounded vocabulary/boundary only;
- `ReferenceOnly` — an implementation or concrete proof exists but broader adoption/compatibility is limited;
- `Preview` — implementation and repository quality evidence exist but compatibility/adoption is evolving;
- `Stable` — implementation, quality, adoption, and compatibility/support evidence are all explicitly asserted.

The generated files `catalog/foundationkit.capabilities.json` and `catalog/foundationkit.maturity-evidence.json` are projections of the canonical compiled model. Catalog generation fails on missing evidence, maturity disagreement, invalid dependencies, or drift.

Composer consumes this same graph for discovery, manifest validation, dependency resolution, compatibility diagnostics, deterministic generation, and interactive generation. There is no parallel Composer capability model.

Module/CRUD v1 is currently an application-composition surface inside the existing packages, not a new package or a fabricated roadmap identity.
