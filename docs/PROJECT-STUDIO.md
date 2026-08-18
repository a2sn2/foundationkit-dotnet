# FoundationKit Project Studio

## Purpose

Project Studio is the visual project-composition surface layered over the canonical FoundationKit Composer. It is not a second generator and it does not replace the advanced `/compose` engineering surface.

```text
Project Studio
  project/profile/binding
  platform features/providers
  modules/resources/typed fields
  resource policies
          |
          v
Studio Blueprint (`foundationkit.studio.json`)
          |
          v
feature dependency + provider resolution
          |
          v
canonical Composer schema-v2 manifest
          |
          v
ComposerProjectModelGenerator
          |
          +--> typed CLR/SQL relationship overlay
          +--> selected .NET/ABP platform overlay
          +--> generated business Blazor UI
          +--> customization hooks
          |
          v
full generated project
```

The Workbench remains the executable Core reference and test host. `/studio` is the product-oriented visual composition experience; `/compose` remains the advanced Composer/manifest harness.

## Project inputs

Project Studio accepts:

- project name;
- FoundationKit profile preset;
- Linked or Standalone/source-copy binding;
- selected reusable platform features;
- provider choice per selected feature when alternatives exist;
- modules;
- executable resources;
- typed resource fields;
- resource-level auditing, authorization, idempotency and concurrency choices.

Supported Studio field vocabulary is:

- Text;
- Integer;
- Decimal;
- Boolean;
- Date;
- DateTime;
- Guid;
- Reference.

A `Reference` field names another Studio resource. Generation creates a Guid foreign-key property, EF relationship metadata, an index when needed, and a deterministic SQL Server foreign-key migration.

## Platform feature catalog

Studio resolves feature dependencies before compilation. The catalog deliberately separates **feature availability** from **feature maturity**:

- `Generated`: Studio/Core emits a directly executable implementation or integration;
- `ProviderReady`: Studio can wire a concrete provider, while environment/provider-specific setup may remain;
- `Reference`: FoundationKit exposes a bounded reference contract/implementation but does not claim universal production completeness;
- `Planned`: the vocabulary can be selected for architecture/design visibility, but the capability is not represented as fully implemented.

The UI must display this maturity instead of presenting every checkbox as equivalent.

### Provider strategy

The preferred order is:

```text
native .NET / ASP.NET Core
        ↓ when sufficient
FoundationKit differentiating conventions
        ↓ when broader infrastructure is valuable
ABP OSS provider
        ↓ when deployment/product specifics remain
consumer implementation
```

ABP is optional and provider-driven. Project Studio uses the open-source framework packages only; it does not silently introduce ABP Commercial modules.

Current provider-oriented Studio vocabulary includes Identity, Authorization/Permissions, Settings, Feature Management, Multi-Tenancy, Background Jobs, Background Workers, Event Bus, BLOB storage and Distributed Locking, alongside FoundationKit/native features such as security, auditing, localization, caching, observability and HTTP resilience.

For infrastructure that requires a durable external topology, Studio does not invent credentials or production stores. For example, background-job execution is not enabled merely because the ABP job abstraction is present; the consuming product must configure its durable store/operational policy first.

## Canonical Composer boundary

Current Composer schema-v2 executable resources support the proven CRUD/Audit/Authorization/Concurrency execution contract. Studio therefore does **not** inject unsupported platform features as fake resource behaviors.

Instead:

1. Studio compiles resource behavior to the existing executable Composer subset.
2. Project-level platform capabilities are resolved separately.
3. The canonical Composer output is generated first.
4. Typed data, platform/provider and business-UI overlays specialize that safe base.

This preserves the existing Parser → Analyzer → Generator contract rather than introducing a parallel generator.

## Generated project surface

For executable resources, Project Studio produces or specializes:

- Domain entities;
- request/response contracts;
- CRUD mappers/policies;
- EF Core entity configuration;
- SQL Server migrations;
- deterministic table/index/FK names;
- product DbContext;
- CRUD API endpoints;
- OpenAPI surface;
- FoundationKit/ABP platform registration when selected;
- runnable Blazor WebAssembly application;
- navigation entries;
- generated CRUD list/create/edit/delete pages;
- typed inputs for Studio field types;
- idempotency headers where required;
- ETag/If-Match update behavior where concurrency is selected;
- generated platform manifest/reporting documentation.

The generated business UI is a usable starting point, not a promise that every product should keep the generated UX unchanged.

## Preview before write

`POST /api/studio/preview` performs a complete generation in an isolated temporary workspace and compares it with the target project without writing the target.

The preview returns:

- resolved features and whether they were directly selected or pulled as dependencies;
- effective providers;
- ABP OSS packages that will be emitted;
- generated files to create;
- generated files to update;
- obsolete generated files to delete;
- consumer-owned files that will be preserved;
- a bounded sample diff;
- maturity/production-boundary warnings.

The Workbench UI enables Generate only when the current design still matches the successful Preview fingerprint.

## Ownership and safe customization

Project Studio separates three kinds of ownership.

### Generated-owned

Files recorded in `.foundationkit-generated.json` are generator-owned. They may be replaced by regeneration. Hashes are validated before replacement.

A direct manual edit to a generated-owned file causes regeneration to fail closed instead of silently deleting the edit.

### Consumer-owned

Normal hard-coded product work is supported. Existing files that are not owned by the generated marker are preserved during Studio regeneration. A directory segment named `Custom` is the recommended explicit convention.

The generated API includes partial customization hooks:

```csharp
public static partial class GeneratedCustomization
{
    static partial void ConfigureServicesCore(WebApplicationBuilder builder)
    {
        // Product-specific DI registrations.
    }

    static partial void ConfigurePipelineCore(WebApplication app)
    {
        // Product-specific middleware/endpoints.
    }
}
```

The consumer implementation can live under `src/<Project>.Api/Custom/` and survives regeneration.

If a future generated file would collide with a preserved consumer-owned path, regeneration fails rather than overwriting consumer code.

### Blueprint-owned

`foundationkit.studio.json` is deliberately not hash-owned as generated output. It is the editable Studio project description used for reopening, changing, previewing and regenerating the project.

## Security and production boundary

Project Studio generates development/reference wiring where required to keep a project executable. It does not claim that reference authentication headers, in-memory evidence sinks, local connection strings or default provider wiring are production controls.

The consuming product/environment still owns, as applicable:

- real authentication/account UX;
- secrets/KMS;
- database credentials and least privilege;
- tenant isolation policy;
- background-job/event-bus durable transports;
- BLOB provider credentials/storage policy;
- observability/SIEM operations;
- privacy/retention rules;
- backup/restore;
- load/security testing;
- deployment/rollback/incident operations;
- Production governance under issue #35.

## Endpoints

Project Studio:

- `GET /api/studio/catalog`
- `POST /api/studio/preview`
- `POST /api/studio/generate`

Advanced Composer remains available:

- `POST /api/composer/validate`
- `POST /api/composer/generate`

## Local path

```text
.\foundationkit.ps1 start -Target Workbench
        ↓
http://localhost:8080/studio
        ↓
Project → Features/Providers → Modules/Data
        ↓
Preview Changes
        ↓
Generate Project
        ↓
<repository>/generated/<ProjectName>
        ↓
optional Custom hard-coded product work
        ↓
reopen Blueprint → Preview → Regenerate safely
```

Project Studio expands the developer experience after the frozen consumer-ready Core baseline. It does not reopen the old numbered Core roadmap and it does not alter the historical `v0.1.0-consumer-baseline.1` release.
