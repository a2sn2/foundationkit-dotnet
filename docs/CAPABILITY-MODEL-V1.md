# FoundationKit Capability Model v1

## Purpose

FoundationKit is evolving from a reusable core into a **composable system-building foundation**. The design goal is not to place every feature inside the kernel. The goal is to keep a small, stable kernel and expose reusable capabilities that a project can opt into deliberately.

The model in this document is the machine-oriented contract for that direction.

## Core rules

1. **Kernel stays small.** Product features do not move into `FoundationKit.Domain` or other core packages merely because many products may need them.
2. **Everything beyond the kernel is opt-in.** A project should be able to use FoundationKit without taking identity, workflow, files, multi-tenancy, AI, or another unrelated concern.
3. **Capabilities declare dependencies.** Selecting `approvals`, for example, can pull the workflow/audit/authorization contracts it requires.
4. **Providers are separate from capabilities.** SQL Server, Redis, SMTP, cloud services, search engines, message brokers, and AI vendors are adapters rather than business-core dependencies.
5. **Tooling consumes the same graph.** CLI and future visual composition must use the same capability IDs, contract metadata, and dependency rules instead of maintaining a second hidden model.
6. **Maturity is explicit.** A capability listed in the catalog is not automatically implemented or production-ready.
7. **Profiles are starting points, not frameworks inside the framework.** A project can start from a profile, include more capabilities, and remove independent capabilities.
8. **A required dependency cannot be excluded.** Composition must fail rather than silently generate an invalid project.
9. **Contract compatibility fails closed.** A manifest that explicitly requires a capability contract version FoundationKit does not provide is invalid even if dependency resolution succeeds.

## Capability kinds

| Kind | Meaning |
|---|---|
| `Kernel` | Stable primitives every composition starts from. |
| `Optional` | Reusable capability that a project selects only when needed. |
| `Provider` | Technology/vendor adapter that implements a capability boundary. |
| `Tooling` | CLI, Workbench, generators, analyzers, or other developer experience. |

## Maturity states

| Maturity | Meaning |
|---|---|
| `Stable` | Reusable FoundationKit contract/implementation is part of the current supported core. |
| `Preview` | Reusable direction exists but is still being hardened or broadened. |
| `ReferenceOnly` | A real reusable boundary, package, provider adapter, or tooling surface is implemented/proven, but adoption, compatibility, provider, or production evidence is still too limited for `Preview` or `Stable`. |
| `Planned` | Defined in the capability graph so dependencies and future composition remain coherent; implementation must not be claimed yet. |

This distinction is mandatory. A profile containing a planned capability describes a **target system composition**, not a claim that the feature can already be generated. `ReferenceOnly` likewise does not mean production approval; it means the stated reference-level surface is real and must be described without implying broader unimplemented behavior.

## Capability contract versions

Capability contract version is a machine-readable composition concept separate from both **NuGet package version** and **maturity**.

FoundationKit v1 publishes a `CapabilityContractDescriptor` for every capability/provider/tooling identity in the canonical graph. Every current identity starts at contract version `1`.

A project manifest may optionally add exact requirements such as:

```json
"capabilityContracts": {
  "authorization": 1,
  "provider-sqlserver": 1
}
```

The v1 compatibility rules are intentionally narrow:

- contract versions are positive integers;
- the requirement must refer to a capability that resolves in the final composition;
- transitive dependencies may be constrained explicitly;
- providers may be constrained when selected;
- the required version must exactly match the catalog version;
- incompatibility is a composition error, not a maturity warning;
- omitting requirements preserves previous manifest behavior.

The model deliberately does not define SemVer ranges, runtime negotiation, package installation, downgrade/upgrade behavior, or migration orchestration. Those concepts can be added only if real compatibility pressure demonstrates the need.

## Current catalog groups

### Foundation and experience

- `kernel`
- `validation`
- `web-api`
- `blazor`
- `localization`

### Identity and security

- `security`
- `identity`
- `authorization`
- `privacy`

### Governance and process

- `auditing`
- `workflow`
- `approvals`
- `tasks`
- `retention`

### Platform and organization

- `settings`
- `feature-management`
- `organization`
- `multi-tenancy`

### Communication and integration

- `notifications`
- `messaging`
- `webhooks`
- `realtime`

### Data and content

- `files`
- `documents`
- `caching`
- `search`
- `reporting`

### Reliability and operations

- `observability`
- `jobs`
- `idempotency`
- `concurrency`

### Business building blocks

- `money`
- `numbering`

### Intelligence

- `ai`

### Initial provider/tooling identities

- `provider-sqlserver`
- `provider-redis`
- `provider-smtp`
- `tooling-cli`
- `tooling-workbench`

The catalog grows only when a capability has a clear boundary, dependency model, ownership, tests, and documentation.

## Profiles

FoundationKit v1 defines seven composition profiles:

| Profile | Intent |
|---|---|
| `minimal` | Small API/service baseline. |
| `standard` | General business-system baseline. |
| `enterprise` | Organization/process/approval/automation baseline. |
| `fintech` | Enterprise baseline plus finance/privacy/retention-oriented controls. |
| `saas` | Tenant/feature/integration/search-oriented baseline. |
| `internal-business` | Line-of-business systems with organization, workflow, tasks and reporting. |
| `public-portal` | Externally facing portal baseline. |

Profiles are deliberately editable through includes/excludes. They are not hard-coded product templates.

## Dependency examples

### Approvals

```text
approvals
  -> workflow
      -> auditing
  -> authorization
      -> identity
          -> security
              -> web-api
                  -> kernel
```

### Feature Management

```text
feature-management
  -> settings
      -> kernel
```

### Localization

```text
localization
  -> kernel
```

### Caching

```text
caching
  -> kernel
```

The dependency above is composition metadata. `FoundationKit.Caching` itself is BCL-only and does not create a package dependency back into the kernel.

### Documents

```text
documents
  -> files
      -> authorization
  -> auditing
```

### Redis provider

```text
provider-redis
  -> caching
      -> kernel
```

### SMTP provider

```text
provider-smtp
  -> notifications
      -> kernel
```

The resolver returns dependencies before dependants and rejects unknown IDs or cycles.

## Project manifest direction

The current Composer consumes a manifest shaped like this for validation/explanation:

```json
{
  "schemaVersion": 1,
  "name": "MySystem",
  "profile": "enterprise",
  "includeCapabilities": ["documents", "search"],
  "excludeCapabilities": ["localization"],
  "providers": ["provider-sqlserver"],
  "capabilityContracts": {
    "authorization": 1,
    "provider-sqlserver": 1
  }
}
```

`capabilityContracts` is optional. Existing schema-v1 manifests that omit it remain valid.

Today that manifest drives capability/profile resolution, strict validation, exact contract compatibility checks, maturity checks, and dependency explanation. It does **not** generate projects yet.

Future generation may use the same manifest for:

- project/package selection;
- provider wiring;
- compatibility-aware generated architecture documentation;
- visual Workbench composition;
- golden-template tests proving generated projects build/test.

## Implementation sequence

The capability catalog is not permission to create dozens of empty packages. Extraction is vertical, consumer-driven, and evidence-driven.

Current sequence status:

1. Capability model, resolver, profiles, manifest contract, and exact capability-contract metadata — **implemented**.
2. Composer validation, compatibility enforcement, explanation, and machine-readable catalog export — **implemented at reference/tooling level**.
3. Auditing, Security, Identity, and Authorization reusable boundaries — **extracted with conservative maturity levels**.
4. Workflow and the narrow Approvals v1 decision/maker-checker surface — **extracted as `ReferenceOnly`**.
5. Notifications bounded message/delivery contracts — **extracted as `ReferenceOnly`; Athar and Madar provide two independent consumer shapes**.
6. SMTP notification provider v1 — **extracted as reusable `FoundationKit.Notifications.Smtp`; maturity remains `ReferenceOnly`**.
7. Settings v1 and Feature Management v1 — **extracted as reusable packages with Workbench runtime evidence; both remain `ReferenceOnly`**.
8. Localization v1 — **extracted as `FoundationKit.Localization` with Workbench runtime proof; maturity remains `ReferenceOnly`**.
9. Caching v1 — **extracted as `FoundationKit.Caching` with bounded byte-cache contracts, an in-memory reference provider, and Workbench consumer evidence; maturity remains `ReferenceOnly`**.
10. Repository consistency baseline — **17 reusable package projects, human metadata, Atlas, unified packaging, and drift-prevention checks are aligned**.
11. Capability compatibility/version metadata v1 — **implemented without adding an eighteenth reusable package**.
12. Files/Documents, Jobs/Messaging, Organization/Multi-Tenancy, Search/Reporting/Privacy/Retention, and finance building blocks — **remain planned until cross-product evidence and/or required provider semantics justify extraction**.
13. Idempotency and Concurrency — **retain current product/reference behavior, but no separate reusable package is claimed yet**.
14. Provider-family expansion — **planned beyond current SQL Server reference behavior and SMTP provider v1**.
15. Composer generation expansion — **interactive `foundationkit new`, project generation, provider wiring generation, and visual composition remain planned**.
16. AI abstractions — **planned only after provider-neutral boundaries and observability rules are established**.

Advanced approvals such as sequential, parallel, quorum, delegation, escalation, and dynamic approver routing remain future work even though the narrow v1 capability is implemented. Notification templates, preferences, queues, retry orchestration, delivery history, and additional channels likewise remain future work beyond the reference v1 boundary.

Madar's product-owned departments, attachments, SLA evaluation, and authorized search/reporting provide useful concrete evidence but are intentionally not promoted into FoundationKit packages until an independent second product/provider shape establishes a reusable boundary.

Settings v1 deliberately does not become a secret store, Feature Management v1 deliberately does not become a percentage-rollout/experimentation engine, Localization v1 deliberately does not become a translation store or OS-specific time-zone conversion provider, and Caching v1 deliberately does not become a Redis/distributed-consistency policy layer. These capabilities express reusable deterministic boundaries while leaving provider, organizational, data-classification, and product policies to consuming systems.

Each extraction must preserve the dependency direction and current security baseline. A new package should not be created merely to reduce the number of planned items: it must have a concrete consumer and an independently useful boundary.

See `docs/CAPABILITY-EXTRACTION-STATUS.md` for current extraction evidence and product-owned boundaries.

## Non-goals of v1

The catalog does **not** claim that every item is implemented, production-ready, or available as a NuGet package. It establishes shared vocabulary, dependency rules, contract compatibility metadata, and maturity signals, while each capability's dedicated documentation states what is actually implemented.
