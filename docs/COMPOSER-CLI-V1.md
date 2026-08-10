# FoundationKit Composer CLI and Project Model

`COMPOSER-CLI-V1.md` remains at this path for existing links, but Composer now supports both manifest schema v1 and schema v2.

FoundationKit Composer is the developer-facing deterministic layer over the canonical capability model. It validates project intent, explains dependency/maturity/contract decisions, and generates inspectable project scaffolds without introducing a second capability graph.

## Commands

From the repository root:

```bash
dotnet run --project tools/FoundationKit.Composer -- capabilities
dotnet run --project tools/FoundationKit.Composer -- profiles
dotnet run --project tools/FoundationKit.Composer -- validate <manifest.json> [--require-stable]
dotnet run --project tools/FoundationKit.Composer -- explain <manifest.json>
dotnet run --project tools/FoundationKit.Composer -- new <manifest.json> --output <directory> [--foundation-root <directory>] [--force] [--require-stable]
dotnet run --project tools/FoundationKit.Composer -- new --interactive --output <directory> [--foundation-root <directory>] [--force] [--require-stable]
```

`validate`, `explain`, and `new` inspect `schemaVersion` and use the correct compatible path automatically. There is no separate `new-v2` command.

## Manifest schema v1

Schema v1 remains supported unchanged for project/profile/capability/provider composition:

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

A v1 manifest does not accept `modules`. Existing v1 manifests require no migration merely to continue using FoundationKit and continue through Composer generator contract 1.

## Manifest schema v2

Schema v2 adds the bounded project model:

```text
Project → Modules → Resources → Behaviors → Overrides → API
                    ↓
              canonical capability graph
```

Example:

```bash
dotnet run --project tools/FoundationKit.Composer -- \
  validate docs/examples/foundationkit.project.v2.json

dotnet run --project tools/FoundationKit.Composer -- \
  new docs/examples/foundationkit.project.v2.json \
  --output ./generated/ComposerProjectModel
```

The full schema, field boundaries, compatibility rules, security model, and generated artifacts are documented in `COMPOSER-PROJECT-MODEL-V2.md`. The machine-readable schema is `catalog/foundationkit.project.schema.json`.

### Why `behaviors` is not the global capability list

Top-level Foundation capability IDs describe canonical platform/runtime/provider/tooling composition. Resource `behaviors` describe module intent such as `crud`, `authorization`, and `concurrency`.

Where an existing Core capability corresponds to a behavior, Composer feeds it into the same canonical capability resolver. This means resource authorization still resolves through the established Authorization → Identity → Security graph. Composer does not maintain a parallel dependency model.

Schema v2 currently requires `crud` for every resource because the proven executable Module Engine is CRUD-based. It does not accept unsupported non-CRUD resource semantics merely to make the manifest look broader than the runtime.

### Safe project-model inputs

Schema v2 uses bounded, closed inputs:

- ID type: `guid`, `string`, `long`, or `int`;
- module/resource/manager names: safe C# identifiers, not arbitrary source;
- routes: bounded ASCII route segments;
- behaviors: closed FoundationKit vocabulary;
- API idempotency: `disabled`, `optional`, `required`;
- API concurrency: `application-policy`, `require-if-match`;
- filter/sort counts: bounded integers.

Duplicate module/resource names and duplicate effective API routes fail closed. Resource-required capabilities cannot be globally excluded.

## Validation and explanation

`validate` performs strict JSON parsing, profile/capability/provider validation, canonical dependency resolution, capability-contract compatibility, and project-model validation. With v2 it also reports module/resource counts.

`explain` prints the dependency-first resolved composition and v2 resource intent. A resource-driven reason appears explicitly, for example:

```text
authorization <- resource:Customers.Customer:authorization
identity      <- required-by:authorization
security      <- required-by:identity
```

Maturity remains independent from structural validity and contract compatibility. `--require-stable` refuses generation when the resolved composition contains non-Stable capabilities.

## Deterministic generation

### Schema v1

The v1 generator creates the existing bounded structural scaffold:

```text
<Product>.sln
Directory.Build.props
Directory.Packages.props
foundationkit.project.json
.foundationkit-generated.json
README.md
ARCHITECTURE.md
src/<Product>.Domain
src/<Product>.Application
src/<Product>.Infrastructure
src/<Product>.Api       when web-api resolves
src/<Product>.Client    when blazor resolves
tests/<Product>.Tests
```

### Schema v2

The v2 generator reuses that proven structural scaffold and adds inspectable project-model artifacts:

```text
PROJECT-MODEL.md
src/<Product>.Application/GeneratedModules/<Module>/<Resource>Definition.g.cs
```

The normalized `foundationkit.project.json` retains the full schema-v2 project model, and `.foundationkit-generated.json` records `generatorContractVersion: "2"` plus SHA-256 ownership for the complete generated file set.

The resource descriptors contain configuration intent only. They do not synthesize domain fields, database schemas, role semantics, external integration code, or project business rules.

## `--foundation-root`

Package mode emits known FoundationKit package references. Repository-local project mode is enabled with:

```bash
--foundation-root .
```

The supplied root must contain the known FoundationKit source-tree marker. Project mode is used by CI to prove generated output against the exact repository head rather than an external package feed.

## `--force` safety

`--force` is not a general overwrite switch. Composer only regenerates a directory that has a valid FoundationKit ownership marker and whose exact generated file set and recorded SHA-256 hashes are unchanged.

A user-added file or any edit to a generated file blocks destructive regeneration.

Schema v1 and v2 use the same ownership safety model. v1 retains generator contract 1; v2 stamps generator contract 2.

## Interactive mode

The current interactive CLI questionnaire still produces schema v1. It asks for a project name, canonical profile, optional additional runtime capabilities/providers, shows the resolved dependency/maturity preview, and requires confirmation before writing files.

It does not yet collect Modules/Resources. The future visual Workbench/Studio composer should author schema v2, serialize the same manifest, and call the same deterministic analyzer/generator rather than creating another project model.

## Contract-version compatibility

Capability contract versions remain independent from NuGet package versions, Composer schema versions, and capability maturity.

Manifest `capabilityContracts` requirements are exact positive integers. Unknown, unresolved, or incompatible requirements fail closed. Omitting the field preserves previous manifest behavior.

Composer manifest versioning is separate:

```text
schema v1 → original composition model
schema v2 → additive module/resource project model
```

A future breaking change to accepted v2 semantics requires a new manifest schema version; FoundationKit must not silently reinterpret an existing v2 document.

## Security boundary

Composer never executes manifest content. It does not support arbitrary source/script/template hooks, does not infer package names from user text, does not write secrets, refuses unsafe output locations, and keeps package/project bindings owned by FoundationKit's canonical mapping.

Schema v2 manager overrides are safe identifiers only. Generated descriptors are inspectable configuration artifacts, not opaque runtime magic.

## CI evidence

The dedicated `FoundationKit Composer Generation` workflow proves both generations on one exact head:

```text
v1 generate
→ hash
→ force-regenerate
→ byte-identical files
→ restore
→ build
→ test

v2 validate
→ generate
→ hash
→ force-regenerate
→ byte-identical files
→ verify project-model artifacts
→ restore
→ build
→ test
```

This is the compatibility gate that allows FoundationKit to evolve Composer without breaking existing v1 users.

## Current boundary before frontend

Phase 11 gives FoundationKit a real deterministic Project → Modules → Resources model, but it does not yet claim that a schema-v2 resource automatically becomes a complete SQL-backed CRUD/API/OpenAPI/Postman application.

That executable generated-resource proof is the next pre-frontend phase and must be validated independently before the platform moves into the frontend/UI system.
