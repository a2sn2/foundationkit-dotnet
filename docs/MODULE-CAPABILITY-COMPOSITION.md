# FoundationKit Module Capability Composition

## Purpose

FoundationKit packages must feel like parts of one platform rather than unrelated helper libraries. Phase 9 establishes one module-level composition vocabulary for the reusable capabilities that already exist in the 17-package Core.

The composition model is metadata and dependency intent. It does not pretend that declaring a capability automatically provisions an identity store, SMTP server, cache provider, database, or any other environment-specific runtime implementation.

## Declared versus effective capabilities

Every module exposes two views:

- `DeclaredCapabilities` — capabilities the module author explicitly requested.
- `Capabilities` — the effective dependency closure FoundationKit computes from the declared set.

Example:

```csharp
module
    .Named("Orders", "orders")
    .Crud()
    .Approvals()
    .FeatureManagement()
    .Localization()
    .Caching();
```

The module declares:

```text
Crud
Approvals
FeatureManagement
Localization
Caching
```

The effective closure also includes dependencies required by those platform capabilities.

## Canonical capability set

Closed built-in module capabilities use the `[Flags]` enum `FoundationModuleCapability`:

```text
Crud
Auditing
Authorization
Concurrency
Workflow
Caching
Security
Identity
Approvals
Notifications
Settings
FeatureManagement
Localization
```

`Authorization` represents permissions/policy authorization. Provider-specific transports such as SMTP are not module capabilities; they remain provider choices behind the reusable capability.

## Dependency closure

`FoundationModuleCapabilityRules` is the single canonical closure implementation for module composition metadata.

Current rules are:

```text
Identity          -> Security
Authorization     -> Identity -> Security
Workflow          -> Auditing
Approvals         -> Workflow + Authorization + Auditing
FeatureManagement -> Settings
```

The closure is transitive and deterministic.

These are platform capability dependencies, not production-readiness claims. For example, an effective `Identity` capability does not mean that a database-backed user store or OAuth/OIDC server has been configured.

## Module builder vocabulary

Existing and new modules use one fluent composition style:

```csharp
module
    .Crud()
    .Auditing()
    .Authorization("policy-name")
    .Concurrency()
    .Workflow()
    .Caching()
    .Security()
    .Identity()
    .Approvals()
    .Notifications()
    .Settings()
    .FeatureManagement()
    .Localization();
```

This is intentionally configuration metadata in `FoundationKit.Application`. The Application package does not reference every cross-cutting package and therefore does not create dependency cycles.

Concrete package services remain owned by their packages and the host. Phase 9 unifies **how a module expresses what it needs**; later Composer phases use that intent to generate and validate host registrations/providers.

## Runtime discovery

`FoundationModuleComposition.Describe(...)` returns a deterministic snapshot containing:

- module name;
- logical route;
- effective API route;
- declared capability names;
- effective capability names;
- API configuration.

`IFoundationModuleRegistry.Describe()` exposes snapshots for all modules. Workbench publishes this reference view at:

```text
GET /api/modules
```

The endpoint is architecture/composition evidence, not an administrative production endpoint requirement for every consumer.

## Compatibility

Phase 9 preserves existing public behavior:

- numeric values of existing capability flags `Crud` through `Caching` remain unchanged;
- new flags use new bits;
- `IFoundationModuleDefinition.DeclaredCapabilities` has a default interface implementation returning `Capabilities`, so older external implementations continue to compile;
- `IFoundationModuleDefinition.Api` retains its default API options behavior;
- existing builder methods retain their signatures.

The new closure can make `Capabilities` contain additional dependency flags when a module explicitly opts into one of the new cross-cutting builder methods. Existing Phase 7 declarations remain valid.

## What Phase 9 does not claim

This phase does not:

- auto-register every package service from one central package;
- make `FoundationKit.Application` depend on all cross-cutting packages;
- create package #18 just to act as a service locator;
- claim provider configuration merely because a capability flag exists;
- promote capability maturity from metadata alone;
- hide host-specific settings, secrets, storage, or transport decisions.

Those constraints preserve package boundaries and keep the composition model usable by Composer without turning the Core into a dependency cycle.

## Evidence

Phase 9 acceptance requires:

- unique single-bit enum values for the closed capability set;
- rejection of unknown flag bits;
- deterministic transitive dependency closure;
- declared/effective separation tests;
- compatibility proof for legacy `IFoundationModuleDefinition` implementations;
- deterministic registry snapshots;
- Workbench runtime `/api/modules` proof;
- existing Settings/FeatureManagement/Localization/Cache runtime reference remains green;
- API Engine/OpenAPI/Postman source-of-truth gates remain green;
- no package-count increase.
