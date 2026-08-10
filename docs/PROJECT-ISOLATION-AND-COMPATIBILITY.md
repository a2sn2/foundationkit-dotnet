# Project Isolation and Compatibility Contract

Status: Core vNext contract.

## Goal

FoundationKit is shared implementation, not shared application state. Two applications may run at the same time and consume the same packages without one application executing the other application's business policy, reading its configuration, or reusing its runtime state.

## Isolation rules

1. Every executable host registers one immutable `FoundationProjectId` through `AddFoundationProject(...)`.
2. Project-specific services, managers, policies, database contexts, credentials, and configuration are registered in that host's own DI container.
3. Reusable packages must not expose mutable global runtime state. Public mutable static fields are regression-tested.
4. Product/business policy never enters a global FoundationKit registry. A module definition belongs to the current host/service collection.
5. Relational provider selection, `DbContext`, schema, migrations, connection string, and database credentials remain host-owned.
6. Shared external resources must be project-scoped. `FoundationResourceNamespace` produces the canonical prefix `foundation:{projectId}:{resourceKind}:{localKey}`.
7. Future cache, file, job, message, webhook, telemetry, and similar providers must preserve the project identity boundary rather than inventing an unscoped global key space.
8. Project identity is not a security credential and is not a tenant identifier. Authorization and tenant isolation remain explicit concerns.

## Service lifetimes

- Project identity and immutable module metadata may be singleton within one application process.
- Request/business services and EF repositories/UoW are scoped.
- Singleton services must be immutable or thread-safe and must not carry request/user/entity state.
- A host must not obtain service instances from another host's service provider.

## Compatibility and upgrades

NuGet package version and capability contract version are separate contracts.

- **Patch**: compatible fixes, security fixes, diagnostics, internal implementation changes.
- **Minor**: additive compatible public functionality. Existing supported calls continue to work.
- **Major**: intentional breaking public contract change.

A consuming application is never silently upgraded by FoundationKit. It keeps the package version it references until its owner performs an explicit upgrade.

Breaking behavior must not be introduced under an unchanged supported contract. When practical, an older public member is first retained and marked obsolete, a replacement is added, and migration guidance is published before removal in a major version.

Changes to generated source, configuration shape, database/provider assumptions, resource naming, or persisted data require an explicit migration note even when the C# signature itself did not change.

## Upgrade verification

Before FoundationKit 1.0 is called compatibility-supported, release CI must prove at least:

- current Core unit/architecture tests;
- current generated-project restore/build/test;
- current Workbench SQL integration;
- public API/breaking-change detection;
- a previous supported generated project upgraded to the candidate version;
- migration notes for every state/configuration change.

## Business customization boundary

FoundationKit supplies the repeatable engine. The consuming host supplies business-specific managers, validators, authorization policies, mappings, and provider configuration. Updating another project's manager or policy cannot change this project unless the consuming project explicitly references that implementation.
