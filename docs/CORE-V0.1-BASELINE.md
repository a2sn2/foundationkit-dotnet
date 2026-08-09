# FoundationKit Core v0.1 Composable Baseline

Status date: 2026-08-09.

## Purpose

This document closes the current **FoundationKit Core v0.1 composable baseline**.

Closure means the current reusable foundation, composition model, compatibility model, maturity-evidence model, tooling surface, generated metadata, and repository verification path form a coherent baseline that future work can build on.

It does **not** mean every capability named in the roadmap is implemented, Stable, production-ready, or available as a reusable package.

## Closed baseline

The reusable package invariant is:

```text
17 FoundationKit .nupkg
17 FoundationKit .snupkg
```

The baseline includes the five architectural packages and the extracted optional/reference capability/provider packages currently justified by consumer evidence:

- Domain;
- Application;
- Infrastructure;
- Web API;
- Blazor;
- Auditing;
- Security;
- Identity;
- Authorization;
- Workflow;
- Approvals;
- Notifications;
- SMTP Notifications provider;
- Settings;
- Feature Management;
- Localization;
- Caching.

No eighteenth reusable runtime package is required to close v0.1.

## Architecture contract

The v0.1 baseline preserves these rules:

1. the kernel/base stays small;
2. optional capabilities remain opt-in;
3. providers remain separate from provider-neutral capabilities;
4. products own product-specific schemas, migrations, policies, copy, and deployment choices;
5. dependency direction is explicit and machine validated;
6. a capability is not extracted merely because a roadmap entry exists;
7. product evidence may guide future extraction without being relabeled as reusable implementation prematurely.

## Composition contract

The current composition layer includes:

- canonical capability IDs, kinds, categories, maturity, and dependencies;
- seven reusable composition profiles;
- dependency resolution with unknown/cycle protection;
- strict schema-v1 project manifests;
- include/exclude/provider validation;
- exact capability contract-version metadata;
- fail-closed compatibility requirements through optional `capabilityContracts`;
- machine-readable maturity-evidence assessments;
- fail-closed maturity-evidence policy for `Planned`, `ReferenceOnly`, `Preview`, and `Stable`;
- generated capability and maturity-evidence machine documents protected by CI drift checks.

The machine documents are:

```text
catalog/foundationkit.capabilities.json
catalog/foundationkit.maturity-evidence.json
```

## Composer v1 reference tooling

The current `FoundationKit.Composer` surface is real reference tooling and supports:

```text
capabilities
profiles
validate <manifest>
validate <manifest> --require-stable
explain <manifest>
```

It validates composition, compatibility, and maturity but deliberately does not generate projects yet.

Interactive `foundationkit new`, deterministic scaffolding, provider wiring generation, generated architecture reports, and visual composition remain future tooling work rather than v0.1 Core blockers.

## Consumer evidence

The baseline is exercised by three different repository consumers:

### Workbench

Workbench proves the reusable architecture path and provides runtime evidence for Settings, Feature Management, Localization, and Caching.

### Athar

Athar proves a complete reference-product shape including Identity, Security, Authorization, Workflow, Approvals, Notifications/SMTP, auditing, SQL Server, idempotency/concurrency reference behavior, Arabic UX, readiness, E2E, and backup/restore evidence.

### Madar

Madar proves a second product shape and reuses FoundationKit contracts while keeping case-specific behavior product-owned. Through v0.10 it provides concrete evidence around lifecycle, SLA, comments, approvals, notifications, departments/routing, transfer/reassignment, secure attachments, and authorized case search/reporting.

That evidence is intentionally not interpreted as automatic extraction of generic Files, Organization, Jobs, Search, or Reporting packages.

## Verification baseline

The immediately preceding Core integrity work was verified on exact PR heads before merge:

- capability compatibility/version metadata v1;
- capability maturity evidence v1.

The maturity-evidence exact head completed:

- Release build with 0 warnings / 0 errors;
- 303 automated tests with 0 failures / 0 skipped;
- Workbench, Athar, and Madar publish;
- Workbench/Athar/Madar SQL integration and product regressions;
- repository Security Scan;
- CodeQL;
- generated capability and maturity-evidence checks;
- independent package-artifact inspection confirming exactly 17 `.nupkg` + 17 `.snupkg`.

This closure change is documentation-only and must pass the repository's normal exact-head gates before merge as an additional regression check.

## Deliberately deferred capabilities

Unchecked roadmap entries remain future work rather than defects in the closed v0.1 baseline.

The following areas require new consumer/provider/owner evidence before runtime extraction:

- Files / Documents and object-storage lifecycle;
- Organization / Multi-Tenancy hierarchy and isolation topology;
- Background Jobs / scheduler semantics;
- Messaging / outbox / inbox / broker semantics;
- Webhooks and Realtime providers;
- reusable Idempotency beyond current product reference behavior;
- reusable Concurrency contracts beyond current product SQL behavior;
- Search / Reporting beyond Madar's product-owned v0.10 behavior;
- Privacy / Retention policy;
- Money / Numbering semantics;
- Redis, object-storage, messaging, search, observability, and additional provider families;
- advanced approvals and communication orchestration;
- deterministic project generation and visual composition;
- provider-neutral AI capabilities.

## Stop rule

After this closure, FoundationKit Core should not gain a new reusable runtime package unless at least one of these becomes true:

1. a real independent consumer demonstrates a reusable boundary;
2. a provider-neutral contract is independently useful outside one product;
3. a provider integration requires a reusable adapter family with clear ownership;
4. compatibility pressure proves the current reference surface insufficient.

The goal is not to maximize package count. The goal is to keep a trustworthy reusable foundation whose abstractions are earned by evidence.

## Governance boundary

Core v0.1 closure is an **experimental / pre-production repository baseline**.

It is not:

- Production Approval;
- independent Segregation-of-Duties evidence;
- ISO/IEC 27001 certification;
- a production infrastructure/security attestation;
- a legal privacy/retention determination;
- provider operational certification.

Issue #35 remains intentionally open and is the mandatory reminder for protected branch/ruleset enforcement, independent approval, required checks, and other Production-governance controls before real go-live.

## Closure statement

FoundationKit Core v0.1 is closed as the current **composable reference baseline** when this documentation-only closure passes its exact-head repository gates and merges to `main`.

Future work starts from this baseline. It should be driven by real product/provider evidence or the next explicit tooling objective, not by unchecked roadmap boxes alone.
