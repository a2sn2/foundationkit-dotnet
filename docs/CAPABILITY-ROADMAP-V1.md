# FoundationKit Capability Roadmap v1

This roadmap turns FoundationKit from a reusable core into a composable system-building foundation while keeping the kernel small and provider-neutral.

## Delivery rule

A capability moves through the following lifecycle:

1. **Planned** — vocabulary, boundary, and dependencies are defined.
2. **ReferenceOnly** — a real reference-level boundary/package or consumer proof exists, but broader adoption, compatibility, provider, or production evidence is still limited.
3. **Preview** — reusable package/contracts exist and pass repository quality/security gates, but compatibility or provider/adoption evidence is still evolving.
4. **Stable** — reusable contract is documented, independently composable, tested, adopted, compatibility-supported, packaged where appropriate, and supported as part of the FoundationKit public surface.

No capability is promoted merely because a class or empty package exists. Every declared maturity must also satisfy the machine-readable Maturity Evidence v1 policy.

## Phase A — Composition foundation

- [x] Capability vocabulary and typed catalog.
- [x] Dependency resolver with cycle/unknown-dependency protection.
- [x] Seven initial profiles.
- [x] Project-manifest model.
- [x] Machine-readable generated capability graph.
- [x] CI drift protection between compiled graph and exported JSON.
- [x] Strict manifest parsing/validation through current Composer tooling.
- [x] Composition dependency diagnostics through current Composer explain/validation flow.
- [x] Capability contract/version metadata with deterministic Composer compatibility validation.
- [x] Machine-readable maturity-evidence assessments and fail-closed promotion policy.

## Phase B — Governance and security foundations

- [x] Auditing reusable package extracted and packaged.
- [ ] Auditing provider/adoption/compatibility proof sufficient for maturity promotion beyond `ReferenceOnly`.
- [x] Security reusable capability boundary.
- [x] Identity reusable capability boundary.
- [x] Authorization roles, permissions, ownership, and scoped access primitives.
- [x] Sensitive-action/step-up requirement contracts.
- [x] Maker-checker/four-eyes reusable policy primitive in Approvals v1.

## Phase C — Business process capabilities

- [x] Deterministic Workflow/state-transition kernel.
- [x] Approvals v1: single approve/reject decision, permission gate, maker-checker, workflow resolution, and audit intent.
- [ ] Advanced approvals: sequential, parallel, quorum/voting, delegation, escalation, and dynamic routing.
- [ ] Tasks/work items.
- [ ] SLA/business-hours/escalation capability.
- [ ] Timeline/activity stream.
- [ ] Comments/notes/mentions.
- [ ] Tags and favorites.

## Phase D — Communication and content

- [x] Notifications v1: bounded channel-neutral message/delivery contracts with real consumer evidence.
- [x] SMTP provider v1: reusable `FoundationKit.Notifications.Smtp` transport package consumed by products.
- [ ] Notification templates and localization.
- [ ] Notification preferences, routing/fallback, queues, retries, and delivery history.
- [ ] SMTP provider family beyond the narrow reference transport: relay/provider ecosystems, retries, routing/fallback, bounce processing, credential-rotation integration, and delivery history.
- [ ] File storage abstraction.
- [ ] Document metadata/versioning/classification.
- [ ] Local-development file provider.
- [ ] Object-storage provider boundary.
- [ ] Realtime abstraction.

## Phase E — Platform and organization

- [x] Settings hierarchy v1: provider-neutral scopes, deterministic fallback/source precedence, bounded values, and Workbench runtime proof.
- [x] Feature Management v1: settings-backed Boolean decisions with explicit defaults and fail-closed invalid configuration, proven by Workbench.
- [x] Localization v1: bounded culture metadata, RTL/LTR directionality, deterministic supported-culture fallback, and opaque time-zone identity, proven by Workbench.
- [ ] Organization/branch/department/team hierarchy.
- [ ] Multi-tenancy context and isolation contracts.
- [ ] Numbering/sequences.
- [ ] Lifecycle/archive/soft-delete primitives.

## Phase F — Reliability and integration

- [ ] Background jobs abstraction.
- [ ] Messaging/integration events.
- [ ] Outbox/inbox contracts.
- [ ] Webhooks with signing/replay/retry contracts.
- [ ] Idempotency reusable package extraction beyond current Athar reference behavior.
- [ ] Optimistic concurrency reusable package extraction beyond current product reference behavior.
- [x] Caching v1: bounded byte-cache contracts, explicit TTL/hit/miss/remove semantics, bounded in-memory reference provider, and Workbench catalog-read consumer proof.
- [ ] External HTTP integration resilience conventions.

## Phase G — Search, reporting, privacy, finance

- [ ] Search abstraction.
- [ ] Reporting definitions and export boundaries.
- [ ] Import/export capability.
- [ ] Privacy/PII classification and masking hooks.
- [ ] Retention/anonymization contracts.
- [ ] Money/currency value model.
- [ ] Finance-oriented approval and audit composition profile improvements.

## Phase H — Providers

Providers remain outside business capabilities and are selected explicitly.

- [ ] SQL Server provider family where reusable provider code is justified.
- [ ] PostgreSQL provider family.
- [ ] Redis provider.
- [x] SMTP provider v1 reference package extracted and consumed.
- [ ] SMTP provider family expansion beyond the current reference transport.
- [ ] Object storage providers.
- [ ] Search providers.
- [ ] Messaging providers.
- [ ] Observability/OpenTelemetry provider wiring.

## Phase I — Project Composer

- [x] `FoundationKit.Composer` reference CLI tooling.
- [x] Capability/profile discovery.
- [x] Strict manifest validation.
- [x] Dependency explanation/current composition diagnostics.
- [x] Exact capability-contract compatibility requirements and diagnostics.
- [x] Machine-readable capability maturity and evidence metadata aligned with human documentation.
- [ ] `foundationkit new` interactive composer.
- [x] Deterministic manifest-driven project generation engine.
- [x] Generated architecture/decision report.
- [ ] Workbench visual composer using the same capability graph.
- [x] Golden generated-project tests proving deterministic output plus restore/build/test.

The completed generation slice is intentionally non-interactive. `FoundationKit.Composer new` consumes the existing strict manifest and writes a bounded Domain/Application/Infrastructure/API/Client/Test skeleton. Repository-local project-reference mode is verified by a dedicated CI workflow; portable package-reference mode is emitted for environments that provide the FoundationKit packages through a NuGet source. The generator does not synthesize product semantics or packages for catalog identities that lack a reusable runtime package.

Interactive prompts and the visual Workbench composer remain future UX layers over this same deterministic engine, not parallel composition models.

## Phase J — AI as an optional capability

AI is deliberately late so it cannot distort the core architecture.

- [ ] Provider-neutral chat model abstraction.
- [ ] Embedding abstraction.
- [ ] Retriever/vector-store abstraction.
- [ ] Prompt-template contracts.
- [ ] Tool/agent execution boundary.
- [ ] AI observability, redaction, rate/cost controls.
- [ ] Provider adapters only after those boundaries are stable.

## Definition of Done for each reusable capability

A capability is not considered complete until applicable items below are satisfied:

- explicit purpose and non-goals;
- dependency graph entry;
- no unnecessary dependency from the kernel back to the capability;
- provider-neutral public contracts;
- bounded and validated public inputs;
- security/privacy threat review;
- unit tests for success and failure paths;
- architecture tests where dependency boundaries matter;
- package included in Release build/pack when appropriate;
- generated catalog is synchronized;
- README/capability documentation;
- reference-consumer proof when runtime behavior is involved;
- CI, security scan, and CodeQL green;
- compatibility and migration impact documented;
- maturity-evidence assessment synchronized with the declared maturity and current repository evidence.

A **Planned** or current-reference capability is not extracted into a package merely to reduce unchecked roadmap boxes. A new reusable package should require at least one concrete consumer and a boundary that is useful independently of that consumer.

## Current baseline

The repository currently has extracted reusable/reference packages for:

- Auditing;
- Security;
- Identity;
- Authorization;
- Workflow;
- Approvals v1;
- Notifications v1;
- SMTP notification provider v1 (`FoundationKit.Notifications.Smtp`);
- Settings v1 (`FoundationKit.Settings`);
- Feature Management v1 (`FoundationKit.FeatureManagement`);
- Localization v1 (`FoundationKit.Localization`);
- Caching v1 (`FoundationKit.Caching`).

Together with the five base packages, the current reusable output remains seventeen NuGet packages plus seventeen symbol packages. Athar provides consumer evidence for security/identity/authorization/workflow/approval/notification/SMTP surfaces. Madar adds a second independent consumer for the narrow Approvals and Notifications contracts. Workbench provides runtime consumer evidence for Settings, Feature Management, Localization, and Caching. Capability maturity remains conservative and does not imply production certification.

The composition model publishes an explicit contract version and one maturity-evidence assessment for every capability identity. Contract metadata is separate from NuGet version and maturity. Catalog generation fails when a capability's declared maturity lacks the minimum repository evidence required by `docs/CAPABILITY-MATURITY-EVIDENCE-V1.md`.

The Composer now adds deterministic project generation on top of this composition baseline without changing the 17-package runtime surface or capability maturity. Generation is tooling behavior: it references only reusable packages that actually exist and reports unresolved/planned capability semantics instead of manufacturing runtime implementations.

## Core v0.1 baseline closure

As of 2026-08-09, the current **FoundationKit Core v0.1 composable baseline is closed** at the repository/reference level described by `docs/CORE-V0.1-BASELINE.md`.

Closure means the current 17-package reusable baseline, capability graph, seven profiles, strict manifests, contract compatibility, maturity-evidence enforcement, Composer reference tooling, generated metadata, and repository verification form a coherent starting point for future work.

Composer deterministic generation is a subsequent tooling extension over that closed baseline; it is not a new runtime package and does not reopen the Core extraction cycle.

Unchecked roadmap items remain future evidence-driven capabilities and tooling objectives. They are **not blockers** to the v0.1 baseline closure and must not be implemented or extracted merely to make this roadmap visually complete.

## Current continuation boundary

The reusable extraction cycle remains consumer-driven. Madar supplies real product semantics for departments/routing, secure attachments, SLA evaluation, and authorized search/reporting, but those remain product-owned implementations rather than automatic evidence for new FoundationKit packages.

There is still **no additional reusable package candidate justified by both an independently useful provider-neutral boundary and sufficient cross-product evidence**. Further runtime extraction should wait for another independent consumer or a clearly reusable provider contract rather than generalizing Madar-specific behavior prematurely.

The maturity-evidence gate strengthens this stop boundary: product evidence can be recorded without pretending it is equivalent to a reusable capability implementation or a Stable compatibility/support commitment.

The following areas therefore remain not ready for package extraction:

- **Files / Documents** — Madar has secure product-owned case attachments, but there is not yet an independent second consumer proving a reusable storage/document lifecycle contract.
- **Background Jobs / SLA** — Madar exposes a bounded SLA evaluation seam, but no scheduler/provider-neutral recurring-work contract is proven across products.
- **Messaging** — the existing in-process domain-event dispatcher is not integration-event/outbox/inbox delivery.
- **Idempotency** — Athar has product-specific duplicate-write protection, but no independent reusable reservation/store/replay contract.
- **Concurrency** — products use SQL concurrency behavior, but no reusable provider-neutral public token/precondition contract is proven.
- **Organization / Multi-Tenancy** — Madar departments and memberships are product-owned and do not establish a general organization hierarchy or tenant-isolation topology.
- **Search / Reporting** — Madar v0.10 proves bounded SQL-backed authorized case search and same-scope operational counts, not a provider-neutral cross-product search/reporting abstraction.
- **Privacy / Retention / Money / Numbering** — reusable semantics still require concrete product/provider evidence.

Provider/vendor choices, legal retention, rollout targeting policy, tenancy topology, business organization semantics, distributed-cache consistency, and data-classification policy remain explicit owner/organization decisions rather than defaults embedded into FoundationKit.
