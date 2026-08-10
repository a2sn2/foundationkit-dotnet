# FoundationKit Core v0.1 Composable Baseline

Status date: **2026-08-09**.  
Status: **closed and merged baseline**.

## What closure means

FoundationKit Core v0.1 is the current coherent reusable/composition baseline. Closure covers the reusable packages, dependency model, capability graph, compatibility model, maturity-evidence policy, Composer validation surface, generated metadata, and repository verification path.

Closure does **not** mean every roadmap capability exists, every package is `Stable`, or any deployment is Production Approved.

## Package invariant

```text
17 FoundationKit .nupkg
17 FoundationKit .snupkg
```

The reusable set is:

```text
Domain
Application
Infrastructure
WebApi
Blazor
Auditing
Security
Identity
Authorization
Workflow
Approvals
Notifications
Notifications.Smtp
Settings
FeatureManagement
Localization
Caching
```

No eighteenth reusable runtime package is required for v0.1.

## Composition baseline

The current model includes:

- canonical capability IDs/kinds/categories/dependencies;
- explicit maturity;
- seven composition profiles;
- strict schema-v1 manifests;
- dependency/cycle/unknown-capability validation;
- exact capability contract versions and optional fail-closed requirements;
- machine-readable maturity evidence and fail-closed promotion policy;
- generated graph/evidence files protected by CI drift checks.

Machine documents:

```text
catalog/foundationkit.capabilities.json
catalog/foundationkit.maturity-evidence.json
```

## Composer v1

Current commands include:

```text
capabilities
profiles
validate <manifest>
validate <manifest> --require-stable
explain <manifest>
new <manifest> --output <directory>
new --interactive --output <directory>
```

Composer now validates/explains composition and provides deterministic project generation plus an interactive questionnaire over the same capability graph. Generation remains bounded: it creates structural product layers and actual FoundationKit package/project bindings without inventing product schemas, business policy, providers, or a speculative runtime package.

## Consumer evidence

```text
Workbench → executable architecture/reference consumer
Athar     → complete Arabic reference product
Madar     → operational case-management product through v0.10
```

Madar's product-owned departments, SLA, attachments, search and reporting are evidence—not automatic justification for generic `Organization`, `Jobs`, `Files`, `Search`, or `Reporting` packages.

## Post-closure maintenance

Core closure does not freeze dependencies or prevent bug/security/support fixes. PR #103 refreshed the supported .NET 8 servicing dependencies and Madar Docker dependency monitoring without changing public capability semantics, package count, or the v0.1 closure decision.

The repository baseline is now migrating coherently to **.NET 10 LTS / `net10.0`** under Issue #104. This support-lifecycle migration changes the framework target, SDK/runtime/container line, and matching Microsoft dependencies while preserving the closed capability model, package count, package version, product schemas, and conservative maturity policy unless exact compatibility evidence requires an explicit additional change.

## Stop rule

A new reusable runtime package should be created only when real evidence demonstrates an independently useful provider-neutral boundary. Product behavior stays product-owned until that threshold is met.

Unchecked roadmap entries are therefore future opportunities, not defects in Core v0.1.

## Governance boundary

This is an experimental/pre-production technical baseline. It is not:

- Production Approval;
- Segregation-of-Duties evidence;
- ISO/IEC 27001 certification;
- production infrastructure/security attestation;
- legal privacy/retention approval.

Issue #35 remains the mandatory production-governance gate before real go-live.

## Closure statement

**FoundationKit Core v0.1 is closed as the current composable technical baseline.** Future Core changes must be driven by defects, security/support maintenance, compatibility pressure, real product/provider evidence, or an explicitly approved tooling objective—not by package-count growth or unchecked roadmap boxes.
