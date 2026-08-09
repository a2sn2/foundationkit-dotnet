# FoundationKit Capability Maturity Evidence v1

## Purpose

Capability maturity is a repository contract, not a marketing label.

FoundationKit already distinguishes `Planned`, `ReferenceOnly`, `Preview`, and `Stable`. Maturity Evidence v1 adds a machine-enforced assessment so a capability cannot be promoted merely by changing the `CapabilityMaturity` enum value in the catalog.

The canonical evidence model lives in:

```text
src/FoundationKit.Application/Capabilities/CapabilityMaturityEvidence.cs
```

and is exported as a dedicated generated machine document:

```text
catalog/foundationkit.maturity-evidence.json
```

The existing `catalog/foundationkit.capabilities.json` remains focused on capability identities, dependencies, contract versions, maturity declarations, and profiles. Keeping evidence separate avoids coupling graph consumers to evidence-policy evolution while both files remain generated from the same compiled model.

## Evidence signals

v1 deliberately uses four broad Boolean signals instead of inventing a scoring system:

| Signal | Meaning |
|---|---|
| `hasImplementationEvidence` | A real implementation, reusable boundary, provider/tooling surface, or product/reference proof exists for the stated capability identity. |
| `hasQualityEvidence` | Repository tests, CI, security, or equivalent technical quality evidence exercises the implemented/proven surface. |
| `hasAdoptionEvidence` | At least one real consumer/product/tooling use demonstrates the surface outside an isolated declaration. |
| `hasCompatibilityEvidence` | The repository has enough compatibility/support evidence to make the stronger supported-surface commitment required by `Stable`. |

Each capability also carries a bounded rationale explaining why the current flags and maturity are conservative.

These signals are intentionally broad. They are not percentages, risk scores, certification controls, or automatically inferred claims.

## Policy

The minimum evidence required by v1 is:

| Maturity | Minimum evidence |
|---|---|
| `Planned` | bounded rationale only |
| `ReferenceOnly` | implementation/proof |
| `Preview` | implementation/proof + quality gates |
| `Stable` | implementation/proof + quality gates + adoption + compatibility/support |

The policy also requires:

- exactly one maturity-evidence assessment for every capability/provider/tooling identity;
- no assessment for an unknown capability;
- assessment maturity must equal the canonical descriptor maturity;
- non-empty rationale no longer than 500 characters;
- no duplicate capability/evidence IDs.

`FoundationKit.CatalogGenerator` evaluates the entire catalog before generating or checking both machine documents. Therefore a maturity change that no longer satisfies the evidence policy fails the normal generated-metadata/CI gate.

## What the gate prevents

For example, changing:

```text
approvals: ReferenceOnly -> Stable
```

without also establishing explicit adoption and compatibility/support evidence causes the maturity-evidence policy to fail.

Likewise, a new `ReferenceOnly` capability cannot have `hasImplementationEvidence = false` and still pass catalog validation.

The gate is intentionally fail-closed, but it does **not** auto-promote a capability when all flags happen to be true. Promotion remains an explicit design/support decision that must be reviewed together with the evidence metadata and documentation.

## Current baseline

Current repository maturity remains unchanged by this gate:

- `Stable`: Kernel, Validation, Web API, Blazor;
- `Preview`: Observability, Security;
- `ReferenceOnly`: current implemented/proven identity, authorization, auditing, workflow, approvals, notifications, settings, feature-management, localization, caching, idempotency/concurrency reference behavior, SQL Server/SMTP provider identities, and current CLI/Workbench tooling;
- `Planned`: organization/multi-tenancy, tasks, files/documents, jobs/messaging/webhooks/realtime, search/reporting, money/numbering, privacy/retention, AI, Redis provider, and other deliberately unextracted future semantics.

Madar's departments, attachments, SLA evaluation, and v0.10 search/reporting improve product evidence but remain product-owned; the evidence gate does not reinterpret them as reusable FoundationKit packages.

## What this evidence is not

Maturity Evidence v1 is **repository capability evidence only**. It does not establish:

- Production Approval;
- independent Segregation of Duties;
- ISO/IEC 27001 certification;
- production provider/security architecture;
- legal retention/privacy compliance;
- production monitoring/SIEM operations;
- penetration/load acceptance;
- a guarantee that every consumer environment is secure or production-ready.

Issue #35 and the production/security documents remain the separate go-live governance boundary.

## Why there is no hard consumer-count threshold

A fixed rule such as "two consumers means Preview" would create false confidence. Two consumers can exercise the same narrow shape, while one consumer can expose substantial compatibility pressure.

FoundationKit therefore records adoption as evidence but does not automatically map a consumer count to maturity. Approvals and Notifications, for example, have Athar and Madar consumer evidence and still remain `ReferenceOnly` because broader compatibility/provider/operational evidence is intentionally incomplete.

## Future evolution

A later version may add typed evidence references, compatibility windows, provider-family evidence, or generated evidence links if real maintenance pressure justifies them.

v1 deliberately does not add:

- weighted scoring;
- automatic promotion/demotion;
- production-readiness scoring;
- organization-specific compliance controls;
- vendor/provider certification;
- external service calls during catalog generation.

The goal is smaller: make maturity changes explicit, reviewable, machine-visible, and fail-closed when the minimum repository evidence is absent.
