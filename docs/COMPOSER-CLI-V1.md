# FoundationKit Composer CLI and Project Model

`COMPOSER-CLI-V1.md` remains at this path for existing links, but Composer supports both manifest schema v1 and schema v2.

FoundationKit Composer is the developer-facing deterministic layer over the canonical capability model. It validates project intent, explains dependency/maturity/contract decisions, and generates inspectable product-owned scaffolds and bounded executable full-stack overlays without introducing a second capability graph.

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

`validate`, `explain`, and `new` inspect `schemaVersion` and use the compatible path automatically. There is no separate `new-v2` command.

## Manifest schema v1

Schema v1 remains supported for project/profile/capability/provider composition:

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
Project
  → Modules
    → Resources
      → Behaviors
      → optional executable Fields
      → Overrides
      → API
    → Read Models
  → canonical capability/profile/provider graph
```

Example:

```bash
dotnet run --project tools/FoundationKit.Composer -- \
  validate docs/examples/foundationkit.project.v2.json

dotnet run --project tools/FoundationKit.Composer -- \
  new docs/examples/foundationkit.project.v2.json \
  --output ./generated/ComposerProjectModel
```

The full schema, field/query/read-model boundaries, compatibility rules, security model, and generated artifacts are documented in `COMPOSER-PROJECT-MODEL-V2.md`. The machine-readable schema is `catalog/foundationkit.project.schema.json`.

### Why `behaviors` is not the global capability list

Top-level Foundation capability IDs describe canonical platform/runtime/provider/tooling composition. Resource `behaviors` describe module intent such as `crud`, `authorization`, and `concurrency`.

Where an existing Core capability corresponds to a behavior, Composer feeds it into the same canonical capability resolver. Resource authorization therefore resolves through the established Authorization → Identity → Security graph. Composer does not maintain a parallel dependency model.

Schema v2 requires supported resource semantics and fails closed when executable intent exceeds the generator's proven surface.

### Safe project-model inputs

Schema v2 uses bounded, closed inputs. The descriptor model supports ID types `guid`, `string`, `long`, and `int`; executable generated resources currently use the proven Guid-based path. Names and routes are bounded safe identifiers/segments, behavior/API modes come from closed vocabularies, and executable fields/query intent are explicitly modeled rather than accepting arbitrary source or SQL.

For the current executable SQL Server path, explicit text fields can declare bounded query/index intent including exact/prefix filtering, sorting, indexing, and uniqueness where supported. Filter/sort counts remain bounded. Read-model declarations are explicit and generate read-only SQL-view-backed projections under the documented read-model policy.

Duplicate module/resource names, fields, read-model identities, and effective API routes fail closed. Resource-required capabilities cannot be globally excluded.

## Validation and explanation

`validate` performs strict JSON parsing, profile/capability/provider validation, canonical dependency resolution, capability-contract compatibility, and project-model validation. With v2 it also validates modules/resources, executable fields/query intent, and read-model declarations where present.

`explain` prints the dependency-first resolved composition and v2 resource intent. A resource-driven reason appears explicitly, for example:

```text
authorization <- resource:Customers.Customer:authorization
identity      <- required-by:authorization
security      <- required-by:identity
```

Maturity remains independent from structural validity and contract compatibility. `--require-stable` refuses generation when the resolved composition contains non-Stable capabilities.

## Deterministic generation

### Schema v1

The v1 generator creates the compatible bounded structural scaffold:

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

Schema v2 has two compatible resource modes:

```text
resource without explicit executable fields
→ descriptor-only project intent

resource with supported executable fields
→ bounded executable full-stack generation
```

The normalized `foundationkit.project.json` retains the full project model, and `.foundationkit-generated.json` records generator contract 2 plus SHA-256 ownership for the complete generated file set.

Descriptor-only resources remain configuration/intent and do not invent a domain/database model. Executable resources add inspectable product-owned Domain/Application/Infrastructure/API/SQL migration source. Proven query declarations generate server-side filtering/sorting/index mappings, and read models generate SQL-view-backed read-only projections. Runtime OpenAPI then drives deterministic Postman and typed C# client artifacts; generated Blazor applications consume the typed client path.

Business rules, production identity, deployment policy, secrets, environment-specific rate limits, and unsupported integrations remain consumer-owned. Unsupported intent fails closed instead of producing partially wired code.

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

## Interactive and visual composition

The interactive CLI questionnaire remains a compatible schema-v1 entry point. It asks for project name, canonical profile, optional additional runtime capabilities/providers, shows the resolved dependency/maturity preview, and requires confirmation before writing files.

Core Studio supplies the visual schema-v2 composition experience. It serializes the same canonical project model and invokes the same parser/analyzer/generator rather than maintaining a second graph, hidden project format, or alternate scaffold engine.

## Contract-version compatibility

Capability contract versions remain independent from NuGet package versions, Composer schema versions, and capability maturity.

Manifest `capabilityContracts` requirements are exact positive integers. Unknown, unresolved, or incompatible requirements fail closed. Omitting the field preserves previous manifest behavior.

Composer manifest versioning remains separate:

```text
schema v1 → original composition model
schema v2 → additive module/resource/read-model executable project model
```

A future breaking change to accepted v2 semantics requires a new manifest schema version; FoundationKit must not silently reinterpret an existing v2 document.

## Security boundary

Composer never executes manifest content. It does not support arbitrary source/script/template hooks, does not infer package names from user text, does not write secrets, refuses unsafe output locations, and keeps package/project bindings owned by FoundationKit's canonical mapping.

Schema-v2 identifiers and overrides are safe bounded values only. Generated code remains inspectable product source, not opaque runtime magic.

## CI evidence

Repository workflows prove compatible schema-v1 and schema-v2 generation plus the delivered executable path. The proof includes deterministic generate/force-regenerate behavior, restore/build/test, SQL Server migrations/runtime behavior, API/OpenAPI contract checks, read-engine behavior, typed-client generation/build, generated Blazor build/runtime, security gates, and the fixed 17-package reusable boundary.

## Current baseline

Composer is no longer only a structural scaffolder. The consumer-ready Core baseline proves:

```text
Project → Modules → Resources/Fields/Query Intent
                   + Read Models
        ↓
product-owned generated Domain/Application/Infrastructure/SQL/API
        ↓
runtime OpenAPI
       ↙         ↘
Postman        typed C# client
                  ↓
           generated Blazor application
```

Both Linked and Standalone/source-copy consumption paths have been exercised. This is a pre-production consumer baseline, not a claim that every future business capability or production environment is already implemented.
