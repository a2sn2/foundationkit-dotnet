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

Current commands:

```text
capabilities
profiles
validate <manifest>
validate <manifest> --require-stable
explain <manifest>
```

Composer validates and explains composition. It does **not** scaffold projects or install providers yet; `foundationkit new` remains future tooling.

## Consumer evidence

```text
Workbench → executable architecture/reference consumer
Athar     → complete Arabic reference product
Madar     → operational case-management product through v0.10
```

Madar's product-owned departments, SLA, attachments, search and reporting are evidence—not automatic justification for generic `Organization`, `Jobs`, `Files`, `Search`, or `Reporting` packages.

## Post-closure maintenance

Core closure does not freeze dependencies or prevent bug/security fixes. PR #103 refreshed the supported .NET 8 servicing dependencies and Madar Docker dependency monitoring without changing public capability semantics, package count, or the v0.1 closure decision.

The current framework target remains `net8.0`; migration to .NET 10 LTS is tracked separately in Issue #104 before .NET 8 end of support.

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
