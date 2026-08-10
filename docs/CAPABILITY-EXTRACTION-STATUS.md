# Capability Extraction Status

Status date: 2026-08-11.

The active repository is Core-focused. Runtime evidence is derived from reusable package tests, generated-project tests, dedicated generated A/B runtime proof, and Workbench reference execution rather than retained application products.

## Current reusable packages

Exactly 17 reusable packages are shipped. No eighteenth package is justified by the current Core vNext evidence.

Base: Domain, Application, Infrastructure, WebApi, Blazor.

Optional/reference: Auditing, Security, Identity, Authorization, Workflow, Approvals, Notifications, Notifications.Smtp, Settings, FeatureManagement, Localization, Caching.

Core vNext phases have deliberately hardened/composed those existing packages instead of converting every new behavior into another package. Project isolation, Module/Service Engine, API Engine, contract-source-of-truth tooling, module capability composition, durable idempotency, Composer schema v2, and the Phase 12 executable generated-product proof all remain within the justified existing boundaries.

## Current extraction decision

The current generated full-stack proof does **not** justify a package #18. Phase 12 composes existing reusable surfaces and emits product-owned source where the generated product must own semantics or schema:

- product Domain entities/contracts derived from explicit bounded manifest fields;
- product DbContext, SQL Server mapping, resource/idempotency tables, and migrations;
- product reference authentication adapter;
- FoundationKit generic CRUD/application/API/audit/concurrency/idempotency wiring;
- runtime OpenAPI and deterministic Postman derivation.

Product-owned generated source is not automatically a reusable runtime package.

## Capability maturity changes

Durable HTTP idempotency now has implementation, quality, Workbench adoption, and generated A/B adoption evidence. Its canonical maturity is therefore `ReferenceOnly` rather than `Planned`.

That assessment remains conservative: broader provider compatibility and long-term support evidence are incomplete, so Phase 12 does not promote idempotency to Preview or Stable.

Concurrency remains `ReferenceOnly` with generic policy/EF/API evidence and generated-product use.

## Still separate evidence-driven decisions

Files, documents, jobs, durable messaging/outbox/inbox, webhooks, realtime, organization, tenancy, search, reporting, privacy, retention, money, numbering, and additional provider families remain separate decisions. An implementation should be extracted only when its provider-neutral boundary is clear and its tests/reference/adoption evidence are strong enough to avoid encoding one generated product or one consumer's semantics.

## Maturity interpretation

Maturity is conservative and machine-checked. Generated code, reference execution, and real reuse evidence can support a maturity assessment, but existence alone does not imply broad compatibility or production support. A roadmap checkbox is never a reason to create a package or promote maturity.
