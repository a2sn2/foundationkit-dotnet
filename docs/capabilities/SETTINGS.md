# FoundationKit.Settings

## Purpose

`FoundationKit.Settings` is the provider-neutral settings-resolution boundary for FoundationKit consumers. It provides deterministic lookup across caller-defined scopes without selecting a database, configuration system, tenant model, secret store, or administration UI.

Current maturity: **ReferenceOnly**.

## Public surface

- `SettingKey` — normalized bounded setting identifiers.
- `SettingScope` / `SettingScopeKind` — opaque caller-owned scope type + identifier, with an explicit global scope.
- `SettingEntry` — one bounded key/value at one scope.
- `SettingResolutionContext` — ordered most-specific-to-least-specific scopes; global fallback is appended automatically.
- `ISettingSource` — exact-scope source port.
- `ISettingReader` / `SettingReader` — deterministic hierarchical resolver.
- `CompositeSettingSource` — deterministic source precedence within the same scope.
- `InMemorySettingSource` — immutable reference/development source.
- `ResolvedSetting` — resolved value plus matched scope.
- `AbpSettingReader` — optional ABP OSS bridge that resolves through ABP's current tenant/user setting context while preserving the FoundationKit reader contract.

## Resolution semantics

FoundationKit-owned resolution is deliberate and stable:

1. the consumer supplies non-global scopes in most-specific-first order;
2. global is appended automatically;
3. for each scope, `CompositeSettingSource` evaluates sources in constructor order;
4. the first exact match wins;
5. no match returns `null`; FoundationKit does not invent a value.

This means scope specificity is evaluated before source priority. A user-scoped value in a later source therefore wins over a global value in an earlier source.

When `AbpSettingReader` is selected, scope resolution is intentionally delegated to ABP's current provider context. FoundationKit reports a synthetic `provider:abp-current-context` matched scope rather than pretending that ABP's internal provider order is the same as the explicit FoundationKit hierarchy.

## Validation and safety

- setting keys are normalized to lower case and limited to 160 characters;
- scope kinds are normalized and bounded;
- non-global scopes require an identifier; global cannot have one;
- a resolution context is limited to 16 non-global scopes;
- setting values are limited to 16 KiB and cannot contain a null character;
- duplicate in-memory addresses are rejected instead of silently overwritten;
- `SettingScope.ToString()` exposes only the scope kind, not its identifier;
- `SettingEntry.ToString()` and `ResolvedSetting.ToString()` never include the setting value.

These diagnostics constraints reduce accidental leakage, but they do **not** make settings a secret-management system.

## Explicit non-goals

FoundationKit does not provide:

- secret/password/API-key storage;
- encryption or key management;
- a mandatory SQL/Redis/cloud configuration provider;
- writes, optimistic concurrency, administration UI, or change approval;
- a mandatory built-in tenant, organization, department, or user identity model;
- policy deciding which scope kinds a product is allowed to use.

ABP is an optional provider integration, not a mandatory FoundationKit runtime. Secrets must stay in the product's approved secret/KMS mechanism. Scope semantics remain consumer-owned unless the chosen provider owns them explicitly.

## Workbench consumer evidence

Workbench registers an `InMemorySettingSource`, resolves `workbench.experience.default-culture`, and exposes the resolved value/scope through `GET /api/platform-reference`. The SQL Server integration smoke flow asserts the runtime value and the global resolution scope.

No database migration or product schema is introduced by the ABP setting bridge.

## Dependency direction

`FoundationKit.Settings` has no dependency on another FoundationKit package. It may optionally delegate provider behavior to ABP OSS. Lower FoundationKit layers do not depend back on Settings.

See `docs/PLATFORM-LEVERAGE-AUDIT.md`.
