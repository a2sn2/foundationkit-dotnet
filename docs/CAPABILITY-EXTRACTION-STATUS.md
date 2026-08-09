# Capability Extraction Status

Status date: 2026-08-09.

This document records the current consumer-driven FoundationKit capability-extraction state. It distinguishes reusable packages that actually exist from machine vocabulary, product-owned behavior, and future roadmap ideas.

## Reusable/reference baseline

| Capability | Package / boundary | Consumer evidence | Current maturity |
|---|---|---|---|
| Auditing | `FoundationKit.Auditing` | Athar + Madar composition | ReferenceOnly |
| Security | `FoundationKit.Security` | Athar + Madar host security use | Preview |
| Identity | `FoundationKit.Identity` | Athar + Madar identity-adjacent composition | ReferenceOnly |
| Authorization | `FoundationKit.Authorization` | Athar + Madar | ReferenceOnly |
| Workflow | `FoundationKit.Workflow` | Athar + Madar lifecycle workflows | ReferenceOnly |
| Approvals v1 | `FoundationKit.Approvals` | Athar + Madar independent approval shapes | ReferenceOnly |
| Notifications v1 | `FoundationKit.Notifications` | Athar + Madar independent notification shapes | ReferenceOnly |
| SMTP provider v1 | `FoundationKit.Notifications.Smtp` | Athar + optional Madar email transport | ReferenceOnly |
| Settings v1 | `FoundationKit.Settings` | Workbench platform reference | ReferenceOnly |
| Feature Management v1 | `FoundationKit.FeatureManagement` | Workbench platform reference | ReferenceOnly |
| Localization v1 | `FoundationKit.Localization` | Workbench platform reference | ReferenceOnly |
| Caching v1 | `FoundationKit.Caching` | Workbench embedded catalog read path | ReferenceOnly |

The reusable output remains **17 FoundationKit NuGet packages plus 17 symbol packages**. Package existence does not imply a capability is Stable or production-approved.

## Composition compatibility v1

The composition layer has explicit capability contract/version metadata without creating another runtime package.

The v1 model provides:

- one machine-visible contract version for every capability/provider/tooling identity in the canonical graph;
- current contract version `1` for every catalog identity;
- generated contract metadata in `catalog/foundationkit.capabilities.json`;
- optional `capabilityContracts` requirements in manifest schema v1;
- exact-match compatibility validation for selected capabilities, providers, and transitive dependencies;
- fail-closed handling for unknown, unresolved, invalid, or incompatible requirements;
- Composer `capabilities`, `validate`, and `explain` diagnostics for contract compatibility;
- backward compatibility for existing manifests that omit `capabilityContracts`.

Contract version is deliberately separate from NuGet package version and capability maturity. v1 does not introduce SemVer ranges, runtime provider negotiation, package upgrade/downgrade, or automatic migrations.

## Maturity Evidence v1

Capability maturity is now paired with one canonical machine-readable assessment for every capability/provider/tooling identity.

The assessment records four broad evidence signals:

- implementation/proof;
- repository quality gates;
- adoption;
- compatibility/support.

The fail-closed minimum policy is:

- `Planned` — bounded rationale;
- `ReferenceOnly` — implementation/proof;
- `Preview` — implementation/proof + quality evidence;
- `Stable` — implementation/proof + quality + adoption + compatibility/support evidence.

Catalog generation verifies complete one-to-one coverage, maturity agreement, bounded rationales, and the minimum policy before generated capability metadata can pass CI. A maturity promotion therefore requires an explicit synchronized evidence change rather than an enum edit alone.

The gate does **not** automatically promote capabilities and does not map consumer count directly to maturity. It also does not equate repository evidence with Production Approval, Segregation of Duties, ISO certification, or production operations.

Canonical details: `docs/CAPABILITY-MATURITY-EVIDENCE-V1.md`.

## Consumer-driven extraction rule

A new reusable runtime package still requires both:

1. an independently useful provider-neutral boundary; and
2. concrete consumer evidence strong enough to avoid baking one product's semantics into FoundationKit.

A roadmap item, a product implementation, an evidence flag, or a second checkbox is not sufficient by itself.

## Madar evidence after v0.10

Madar is now a substantial product consumer and provides useful evidence for future extraction decisions, but several capabilities deliberately remain product-owned:

- v0.2 SLA deadlines/breach evaluation proves product SLA semantics and an evaluator seam, not a generic background-jobs/scheduler contract;
- v0.3 comments prove append-only case collaboration, not a reusable comments/activity package;
- v0.4 approvals reuses the existing `FoundationKit.Approvals` v1 surface without requiring API expansion;
- v0.5 notifications reuses the existing `FoundationKit.Notifications` surface without requiring API expansion;
- v0.6-v0.8 departments, queues, memberships, transfer, and reassignment prove Madar organization/routing semantics, not a general `FoundationKit.Organization` model;
- v0.9 secure case attachments prove a concrete file lifecycle for one product, not yet a cross-product `FoundationKit.Files` or `FoundationKit.Documents` boundary;
- v0.10 authorized SQL-backed case search and same-scope operational reporting prove a bounded product search/reporting implementation, not a provider-neutral `FoundationKit.Search` or `FoundationKit.Reporting` abstraction.

This evidence improves future design quality precisely because it remains product-owned until another independent shape demonstrates what is truly reusable.

## Capabilities that still require stronger evidence

### Files / Documents

Madar has secure product-owned case attachments with bounded type/size validation, private storage keys, SQL metadata, authorized list/download, and audit evidence.

That is real product evidence, but it is still one product shape. There is no independent storage/document consumer or provider family proving what belongs in a reusable contract.

Status: **Planned — no reusable package extraction yet**.

### Background Jobs / SLA

Madar has bounded authorized SLA evaluation that a future scheduler could call, but there is still no reusable delayed/scheduled/recurring job contract, worker lifecycle, retry policy, or provider selection.

Status: **Planned — no reusable Jobs package yet**.

### Messaging

`FoundationKit.Infrastructure` has an in-process domain-event dispatcher. That is not integration messaging, a broker abstraction, outbox/inbox, retry/dead-letter handling, or cross-service delivery.

Status: **Planned — do not relabel in-process domain events as Messaging**.

### Idempotency

Athar has owner-scoped `ClientRequestId` duplicate-write protection plus a database constraint, but no independent reusable reservation/store/completion/replay contract is proven.

Status: **ReferenceOnly behavior — no separate package extraction yet**.

### Concurrency

Athar and Madar use SQL optimistic-concurrency behavior and conflict handling, but no reusable client-visible provider-neutral precondition/token contract has been established.

Status: **ReferenceOnly behavior — no separate package extraction yet**.

### Organization / Multi-Tenancy

Madar proves real departments, memberships, queues, routing, transfer, and reassignment. Those semantics are intentionally Madar-owned. They do not establish organizations/branches/teams/positions, tenant identity/resolution, or data-isolation topology.

Status: **Planned — stronger cross-product organization/tenancy evidence required**.

### Search / Reporting

Madar v0.10 proves authorization-preserving relational case search, bounded filters/paging, and same-scope operational summary counts. It deliberately does not introduce an external search provider, indexing contract, saved searches, exports, generic report definitions, or cross-product query semantics.

Status: **Planned — keep Madar implementation product-owned until another independent consumer proves a reusable boundary**.

### Privacy / Retention / Money / Numbering

These remain capability vocabulary until real products establish the required semantics, policies, and provider boundaries.

Status: **Planned — no extraction yet**.

## Maturity interpretation

Approvals and Notifications have two independent product consumers, Athar and Madar. That strengthens adoption evidence, but both remain `ReferenceOnly` because broader compatibility/provider/support evidence required for a stronger maturity commitment remains incomplete.

The new evidence gate records this explicitly. It does not automatically promote them because a consumer-count threshold was reached.

## Core v0.1 closure state

The current consumer-driven extraction cycle is **closed for the FoundationKit Core v0.1 composable baseline** documented in `docs/CORE-V0.1-BASELINE.md`.

Compatibility/version metadata and Maturity Evidence v1 close the remaining machine-integrity gaps in the current composition model without adding an eighteenth reusable package. The present 17-package baseline is therefore the intentional reference starting point for future work.

This closure does not promote Planned capabilities, does not make unchecked roadmap items defects, and does not reinterpret Madar product behavior as reusable FoundationKit implementation.

A future runtime extraction cycle should begin only when new independent consumer/provider evidence demonstrates a reusable boundary. Without that evidence, the next legitimate FoundationKit direction is tooling evolution such as deterministic project-generation planning rather than speculative package creation.

## Current continuation boundary

There is still no justified eighteenth reusable runtime package. Further Core runtime extraction should wait for another independent consumer/provider shape or a clearly reusable contract rather than generalizing Madar-specific behavior prematurely.

The next FoundationKit work, if continuing without new product evidence, should focus on deterministic Composer/project-generation planning rather than fabricating Files, Organization, Search, Reporting, Jobs, or another runtime package.

## Governance boundary

This document tracks repository capability extraction and technical evidence only. It is not Production Approval, independent Segregation of Duties evidence, ISO/IEC 27001 certification, or a deployment/security attestation.

Production branch/ruleset enforcement, independent approval, provider choices, retention/legal policy, monitoring/SIEM, production secrets/KMS, and other external organizational controls remain separate deployment/governance work under `docs/security/` and Issue #35.
