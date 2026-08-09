# FoundationKit Package Contracts

This document describes the **17 reusable package boundaries currently shipped by FoundationKit Core v0.1**. Package version, capability maturity, and capability contract version are separate concepts. The machine source of truth for maturity/compatibility is the capability model and generated catalog.

## Base packages

### FoundationKit.Domain

Domain primitives only: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, domain exceptions/events. No framework package dependency.

### FoundationKit.Application

Use-case contracts, classified Results/Errors, validation, pagination, current-user/clock/unit-of-work ports, repository/specification contracts, domain-event dispatch contracts, and the capability/composition model. It does not query SQL or inspect HTTP directly.

### FoundationKit.Infrastructure

Provider-neutral EF Core repository/unit-of-work/specification adapters and in-process domain-event dispatch. It does **not** select SQL Server or own a product `DbContext`/migrations.

### FoundationKit.WebApi

ASP.NET Core result-to-HTTP/Problem Details mapping, correlation IDs, baseline response headers, and request-pipeline helpers. Product authentication/authorization/CORS/OpenAPI/deployment policy remain consumer-owned.

### FoundationKit.Blazor

Typed API results/errors, resilient response parsing, `ApiClientBase`, async state and Blazor-oriented ViewModel primitives. No EF Core/SQL/server-host dependency.

## Optional/reference capabilities

### FoundationKit.Auditing

Provider-neutral bounded audit request/event/context and sink/recorder contracts. Sensitive attribute names are rejected and consumers own persistence, SIEM, retention and fail-open/fail-closed policy.

**Current evidence:** consumed by Athar and Madar. Maturity remains `ReferenceOnly`; two product consumers do not prove a universal production audit provider or retention model.

### FoundationKit.Security

Trusted reverse-proxy conventions, deterministic rate-limit partition helpers, and shared MFA-assurance convention. It does not authenticate users or choose rate limits/providers.

**Current evidence:** Athar and Madar consume the reusable security boundary. Maturity remains `Preview`; stable deployment compatibility/support across broader real ingress topologies is not yet claimed.

### FoundationKit.Identity

Reusable account-policy vocabulary, account notification port, security-event vocabulary, and step-up requirements. It does not provide a user store, Identity schema, OAuth/OIDC server, SMTP, or product email copy.

**Current evidence:** Athar is the deepest identity consumer; Madar provides a second identity-adjacent product composition. Maturity remains `ReferenceOnly` because broader provider/account-lifecycle compatibility is still limited.

### FoundationKit.Authorization

Permission IDs/definitions, role-to-permission grants, authorization subjects/evaluator, and owner-or-privileged access semantics. Product roles, permission names, persistence and tenant/organization scope remain product-owned.

**Current evidence:** Athar and Madar both use application-layer semantic authorization. Maturity remains `ReferenceOnly`; organization/tenant/scoped-policy compatibility is not yet generalized.

### FoundationKit.Workflow

Deterministic state/trigger transition definitions and resolution plus bounded audit intent. No workflow database, scheduler, BPMN engine or task routing.

**Current evidence:** Athar initiative review and Madar case lifecycle independently reuse the transition model. Maturity remains `ReferenceOnly`; persistence/version migration/scheduling/advanced workflow semantics are intentionally outside v1.

### FoundationKit.Approvals

Narrow `approve`/`reject` decision model, permission-first eligibility, maker-checker rule, Workflow resolution, and bounded audit intent.

**Current evidence:** Athar and Madar are independent consumers of the unchanged v1 boundary. Advanced sequential/parallel/quorum/delegation/escalation/routing behavior is not included, so maturity remains `ReferenceOnly`.

### FoundationKit.Notifications

Channel-neutral bounded `NotificationMessage`, `INotificationSender`, and delivery-result contracts with sensitive-safe diagnostics.

**Current evidence:** Athar and Madar independently consume the same generic boundary. Both currently rely on SMTP transport and no durable queue/multi-channel routing is proven; maturity remains `ReferenceOnly`.

### FoundationKit.Notifications.Smtp

Narrow SMTP transport adapter over `FoundationKit.Notifications`, including validated transport options and bounded observer diagnostics. It does not own relay policy, secrets, retries, templates, routing/fallback, bounces or delivery history.

**Current evidence:** Athar uses the adapter for account notifications; Madar can opt into it for operational notifications. Provider diversity and durable delivery operations remain unproven; maturity remains `ReferenceOnly`.

### FoundationKit.Settings

Bounded keys/scopes/values, deterministic scope fallback/source precedence, `ISettingSource`/`ISettingReader`, composite and in-memory reference sources. It is not a secret store or organization model.

**Current evidence:** Workbench runtime platform-reference path. Maturity `ReferenceOnly`.

### FoundationKit.FeatureManagement

Bounded Boolean feature definitions/evaluation backed by Settings with explicit defaults and fail-closed invalid explicit configuration. No percentage rollout, targeting, experimentation or vendor SDK.

**Current evidence:** Workbench runtime platform-reference path. Maturity `ReferenceOnly`.

### FoundationKit.Localization

Culture definitions, RTL/LTR metadata, deterministic supported-culture fallback, opaque bounded time-zone IDs. It does not own translation storage, user/tenant preferences, OS-specific time-zone mapping or conversion.

**Current evidence:** Workbench proves `ar-YE`, RTL direction and configured time-zone identity. Maturity `ReferenceOnly`.

### FoundationKit.Caching

Bounded byte-cache contracts with explicit hit/miss/remove/TTL semantics and a bounded in-memory reference provider. Serialization, encryption, object schemas and distributed coherence remain consumer/provider concerns.

**Current evidence:** Workbench `CatalogService` cache path plus tests and SQL-smoke reads. Maturity `ReferenceOnly`.

## Product ownership boundary

The following current product behavior is **not** a hidden reusable package:

```text
Madar departments/routing    ≠ FoundationKit.Organization
Madar SLA evaluation         ≠ FoundationKit.Jobs/SLA
Madar attachments            ≠ FoundationKit.Files/Documents
Madar case search            ≠ FoundationKit.Search
Madar operational reporting  ≠ FoundationKit.Reporting
Athar idempotency             ≠ standalone FoundationKit.Idempotency package
Product SQL rowversion usage ≠ standalone Concurrency package
```

A future extraction requires an independently useful provider-neutral boundary and enough independent evidence to avoid encoding one product's semantics.

## Provider/schema rule

Consuming products select relational providers and own their schemas/migrations:

```csharp
services.AddFoundationInfrastructure();

services.AddDbContext<ProductDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString); // product/provider decision
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});
```

Workbench, Athar, and Madar therefore own separate SQL Server contexts and migrations. No reusable package owns those schemas.

## Canonical detailed capability docs

- `docs/capabilities/AUDITING.md`
- `docs/capabilities/SECURITY.md`
- `docs/capabilities/IDENTITY.md`
- `docs/capabilities/AUTHORIZATION.md`
- `docs/capabilities/WORKFLOW.md`
- `docs/capabilities/APPROVALS.md`
- `docs/capabilities/NOTIFICATIONS.md`
- `docs/capabilities/SMTP-PROVIDER.md`
- `docs/capabilities/SETTINGS.md`
- `docs/capabilities/FEATURE-MANAGEMENT.md`
- `docs/capabilities/LOCALIZATION.md`
- `docs/capabilities/CACHING.md`

## Catalog contract

`catalog/foundationkit.catalog.json` is the human implemented-package catalog. `catalog/foundationkit.capabilities.json` is the generated composition graph. Do not infer maturity from the human catalog or infer capability contract version from the NuGet package version.
