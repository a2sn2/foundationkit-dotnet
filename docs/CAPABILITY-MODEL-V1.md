# FoundationKit Capability Model v1

## Purpose

FoundationKit is evolving from a reusable core into a **composable system-building foundation**. The design goal is not to place every feature inside the kernel. The goal is to keep a small, stable kernel and expose reusable capabilities that a project can opt into deliberately.

The model in this document is the machine-oriented contract for that direction.

## Core rules

1. **Kernel stays small.** Product features do not move into `FoundationKit.Domain` or other core packages merely because many products may need them.
2. **Everything beyond the kernel is opt-in.** A project should be able to use FoundationKit without taking identity, workflow, files, multi-tenancy, AI, or another unrelated concern.
3. **Capabilities declare dependencies.** Selecting `approvals`, for example, can pull the workflow/audit/authorization contracts it requires.
4. **Providers are separate from capabilities.** SQL Server, Redis, SMTP, cloud services, search engines, message brokers, and AI vendors are adapters rather than business-core dependencies.
5. **Tooling consumes the same graph.** CLI, deterministic generation, and future visual composition must use the same capability IDs, contract metadata, maturity evidence, and dependency rules instead of maintaining a second hidden model.
6. **Maturity is explicit.** A capability listed in the catalog is not automatically implemented or production-ready.
7. **Profiles are starting points, not frameworks inside the framework.** A project can start from a profile, include more capabilities, and remove independent capabilities.
8. **A required dependency cannot be excluded.** Composition must fail rather than silently generate an invalid project.
9. **Contract compatibility fails closed.** A manifest that explicitly requires a capability contract version FoundationKit does not provide is invalid even if dependency resolution succeeds.
10. **Maturity requires evidence.** A maturity promotion must satisfy the canonical machine-readable evidence policy; changing the maturity enum alone must not be sufficient.
11. **Generation cannot invent implementation.** A resolved capability without a reusable package remains an explicit product/composition concern; the generator must report that boundary rather than synthesize a fake package or business rule.

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
| `ReferenceOnly` | A real reusable boundary, package, provider adapter, tooling surface, or product/reference proof is implemented, but adoption, compatibility, provider, or support evidence remains too limited for `Preview` or `Stable`. |
| `Planned` | Defined in the capability graph so dependencies and future composition remain coherent; reusable implementation must not be claimed yet. |

This distinction is mandatory. A profile containing a planned capability describes a **target system composition**, not a claim that the feature can already be generated as a completed runtime capability. `ReferenceOnly` likewise does not mean production approval; it means the stated reference-level surface or proof is real and must be described without implying broader unimplemented behavior.

## Maturity Evidence v1

Every capability/provider/tooling identity has one `CapabilityMaturityEvidenceDescriptor` in the canonical Application capability model. The assessment records four broad signals plus a bounded rationale:

- implementation/proof evidence;
- repository quality-gate evidence;
- adoption evidence;
- compatibility/support evidence.

The v1 policy is intentionally conservative:

| Maturity | Minimum machine evidence |
|---|---|
| `Planned` | bounded rationale |
| `ReferenceOnly` | implementation/proof |
| `Preview` | implementation/proof + quality gates |
| `Stable` | implementation/proof + quality gates + adoption + compatibility/support |

Catalog validation also requires exactly one assessment for every capability identity and requires the assessment's declared maturity to match the canonical descriptor.

This gate does **not** auto-promote a capability. It only prevents the repository from declaring a maturity level whose minimum evidence is absent. Adoption is deliberately not reduced to a fixed consumer-count formula.

See `docs/CAPABILITY-MATURITY-EVIDENCE-V1.md` for the detailed policy and boundaries.

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

Composer consumes the same manifest for validation, explanation, and deterministic project generation:

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

That manifest now drives:

- capability/profile resolution;
- strict validation;
- exact contract compatibility checks;
- maturity diagnostics and optional stable-only gating;
- dependency explanation;
- deterministic project/layer selection;
- reusable package/project-reference bindings that actually exist;
- normalized generated manifest metadata;
- generated architecture/decision documentation.

The current non-interactive `new` command writes a bounded Domain/Application/Infrastructure/API/Client/Test skeleton. It does **not** translate every selected catalog identity into generated implementation. Planned or unbound identities stay visible in the generated architecture report with no fake runtime package or product semantic attached.

Package-reference mode expresses portable FoundationKit dependencies; repository-local `--foundation-root` mode uses source `ProjectReference`s and is the CI-proven build/test path.

Future composition work may add:

- interactive questionnaire UX over the same deterministic engine;
- richer provider wiring templates where provider contracts actually exist;
- visual Workbench composition;
- additional generated topology only when it can be produced without inventing business policy.

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
12. Capability maturity evidence gate v1 — **machine-enforced evidence coverage/promotion policy without changing current maturity levels or adding a package**.
13. Files/Documents, Jobs/Messaging, Organization/Multi-Tenancy, Search/Reporting/Privacy/Retention, and finance building blocks — **remain planned until cross-product evidence and/or required provider semantics justify extraction**.
14. Idempotency and Concurrency — **retain current product/reference behavior, but no separate reusable package is claimed yet**.
15. Provider-family expansion — **planned beyond current SQL Server reference behavior and SMTP provider v1**.
16. Composer deterministic generation — **implemented as a manifest-driven, architecture-reporting, golden-build-tested tooling slice; interactive `foundationkit new` UX, richer provider wiring, and visual composition remain future work**.
17. AI abstractions — **planned only after provider-neutral boundaries and observability rules are established**.

Advanced approvals such as sequential, parallel, quorum, delegation, escalation, and dynamic approver routing remain future work even though the narrow v1 capability is implemented. Notification templates, preferences, queues, retry orchestration, delivery history, and additional channels likewise remain future work beyond the reference v1 boundary.

Madar's product-owned departments, attachments, SLA evaluation, and authorized search/reporting provide useful concrete evidence but are intentionally not promoted into FoundationKit packages until an independent second product/provider shape establishes a reusable boundary. The maturity-evidence gate records that distinction rather than erasing it.

Settings v1 deliberately does not become a secret store, Feature Management v1 deliberately does not become a percentage-rollout/experimentation engine, Localization v1 deliberately does not become a translation store or OS-specific time-zone conversion provider, and Caching v1 deliberately does not become a Redis/distributed-consistency policy layer. These capabilities express reusable deterministic boundaries while leaving provider, organizational, data-classification, and product policies to consuming systems.

Each extraction must preserve the dependency direction and current security baseline. A new package should not be created merely to reduce the number of planned items: it must have a concrete consumer and an independently useful boundary.

See `docs/CAPABILITY-EXTRACTION-STATUS.md` for current extraction evidence and product-owned boundaries.

## Non-goals of v1

The catalog does **not** claim that every item is implemented, production-ready, or available as a NuGet package. It establishes shared vocabulary, dependency rules, contract compatibility metadata, machine-enforced maturity evidence, and maturity signals. Composer may generate a structural product scaffold from that composition, but it must keep unimplemented capability semantics explicit rather than treating catalog presence as runtime implementation.
