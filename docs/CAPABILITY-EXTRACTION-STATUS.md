# Capability Extraction Status

Status date: 2026-08-10.

The active repository is Core-focused. Runtime evidence is now derived from reusable package tests, generated-project tests, and Workbench reference execution rather than retained application projects.

## Current reusable packages

Exactly 17 reusable packages are shipped. No eighteenth package is justified by the current phases 1–6 work.

Base: Domain, Application, Infrastructure, WebApi, Blazor.

Optional/reference: Auditing, Security, Identity, Authorization, Workflow, Approvals, Notifications, Notifications.Smtp, Settings, FeatureManagement, Localization, Caching.

## vNext extraction decision

The selected increment is existing-package hardening:

- project isolation contracts in Application;
- module definitions and generic CRUD application service in Application;
- EF module composition/concurrency translation in Infrastructure;
- generic CRUD HTTP mapping in WebApi;
- CRUD audit observer in Auditing;
- SQL/runtime proof in Workbench.

This boundary is independently useful without adding a new package.

## Planned capability rule

Files, documents, jobs, durable messaging, webhooks, realtime, organization, tenancy, search, reporting, privacy, retention, money, numbering, and provider families remain separate decisions. An implementation should be extracted only when its provider-neutral boundary is clear and its tests/reference evidence are strong enough to avoid encoding one application's semantics.

## Maturity interpretation

Maturity is conservative and machine-checked. Removing an old application proof must not leave a rationale that claims adoption that no longer exists in the active repository. Reference-level implementation can remain `ReferenceOnly` without pretending it is broadly adopted or compatibility-supported.
