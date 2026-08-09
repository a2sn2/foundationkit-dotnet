# FoundationKit Composer CLI v1

FoundationKit Composer is the executable developer-facing layer over the canonical Capability Model.

Its v1 surface now has two responsibilities:

1. **analyze compositions** — list, validate, and explain profiles/capabilities/contracts/maturity;
2. **generate a deterministic product skeleton** from the same strict manifest and resolved capability graph.

The generator does not introduce a second hidden template model. It consumes the existing manifest parser, dependency resolver, capability contract metadata, and maturity diagnostics.

## Commands

From the repository root:

```bash
dotnet run --project tools/FoundationKit.Composer -- capabilities
```

Lists every capability with its contract version, kind, maturity, category, and direct dependencies.

```bash
dotnet run --project tools/FoundationKit.Composer -- profiles
```

Lists the current composition profiles.

```bash
dotnet run --project tools/FoundationKit.Composer -- validate docs/examples/foundationkit.project.minimal.json
```

Parses the manifest strictly, validates profile/capability/provider choices, resolves transitive dependencies, enforces explicit capability-contract requirements, and reports maturity warnings.

```bash
dotnet run --project tools/FoundationKit.Composer -- validate docs/examples/foundationkit.project.minimal.json --require-stable
```

Returns a non-zero exit code when any resolved capability is not `Stable`.

```bash
dotnet run --project tools/FoundationKit.Composer -- explain docs/examples/foundationkit.project.example.json
```

Prints the dependency-first resolved composition, contract version, maturity, selection reasons, and explicit compatibility requirements.

```text
authorization [Optional/ReferenceOnly/contract:v1] <- required-by:approvals | requires:v1=compatible
kernel [Kernel/Stable/contract:v1] <- profile:enterprise, required-by:web-api
```

### Deterministic project generation

```bash
dotnet run --project tools/FoundationKit.Composer -- \
  new docs/examples/foundationkit.project.minimal.json \
  --output ./generated/MinimalApi
```

`new` performs the same strict parse, dependency resolution, and contract compatibility validation before writing files. The default mode writes FoundationKit `PackageReference` declarations for reusable packages that exist today.

For repository-local development and CI proof, use project-reference mode:

```bash
dotnet run --project tools/FoundationKit.Composer -- \
  new docs/examples/foundationkit.project.minimal.json \
  --output ./artifacts/composer-golden \
  --foundation-root .
```

`--foundation-root` must point at a FoundationKit source tree containing `src/FoundationKit.Domain/FoundationKit.Domain.csproj`. This mode emits `ProjectReference` entries instead of requiring FoundationKit packages from an external NuGet feed. It is the mode used by the repository's generated-project CI gate.

Optional generation flags:

- `--require-stable` — refuse generation when any resolved capability is not `Stable`;
- `--force` — regenerate only a directory previously created by Composer whose recorded file set and generated-file SHA-256 hashes are still unchanged;
- `--foundation-root <directory>` — use known FoundationKit source projects for repository-local verification.

Unknown options, duplicate options, incompatible contracts, invalid manifests, unsafe destinations, non-empty unowned directories, user-added files, and edited generated files fail closed.

## What is generated

The v1 generator creates a bounded product skeleton rather than business-domain code:

```text
<Output>/
├─ <Product>.sln
├─ Directory.Build.props
├─ Directory.Packages.props
├─ foundationkit.project.json
├─ .foundationkit-generated.json
├─ README.md
├─ ARCHITECTURE.md
├─ src/
│  ├─ <Product>.Domain/
│  ├─ <Product>.Application/
│  ├─ <Product>.Infrastructure/
│  ├─ <Product>.Api/          when web-api resolves
│  └─ <Product>.Client/       when blazor resolves
└─ tests/
   └─ <Product>.Tests/
```

The generated source is intentionally small:

- Domain/Application/Infrastructure marker boundaries prove dependency direction;
- the API skeleton exposes only a basic `/health` endpoint;
- the Client project is only created when `blazor` resolves;
- a generated test proves the product markers agree on the manifest name;
- no product aggregate, workflow, role, tenant model, database schema, migration, secret, or deployment policy is invented.

`ARCHITECTURE.md` is the generated decision report. For every resolved identity it records:

- why the capability is present;
- contract version;
- kind and maturity;
- whether an actual reusable package binding is generated;
- when a selected capability has no reusable runtime package and therefore remains an explicit product/composition concern.

This is important for profiles that contain planned/preview/reference vocabulary: the generator does **not** manufacture `FoundationKit.Files`, `FoundationKit.Search`, `FoundationKit.Organization`, or any other package merely because the capability identity exists in the catalog.

## Determinism contract

For the same manifest, generator contract, reference mode, and FoundationKit baseline, Composer produces the same file names and content:

- generated file ordering is stable;
- solution project GUIDs are deterministic;
- normalized manifests and marker metadata are stable;
- no timestamp, machine name, random GUID, or local absolute path is written into generated content;
- line endings and UTF-8 output are normalized.

The dedicated `FoundationKit Composer Generation` workflow proves this by generating a golden project, hashing every generated file, force-regenerating it, comparing hashes, then restoring/building/testing the generated solution.

The ownership marker also stores SHA-256 for every generated file except the marker itself. `--force` validates both the exact owned file list and those hashes before deleting/recreating anything. This means a user-added file **or an edit to a generated file** blocks destructive regeneration rather than being silently overwritten.

## Manifest v1

The JSON shape remains documented by:

`catalog/foundationkit.project.schema.json`

Existing v1 manifests remain valid. `capabilityContracts` is optional.

Example:

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

### Capability contract compatibility

Capability contract versions remain independent from NuGet package versions and capability maturity.

The v1 rule is intentionally exact and deterministic:

- every catalog capability/provider/tooling identity publishes a positive integer contract version;
- a manifest may require an exact version;
- requirements may target explicitly selected identities or resolved transitive dependencies;
- unknown or unresolved requirements are rejected;
- a requested version that does not exactly match the available version fails closed;
- omitting `capabilityContracts` preserves previous manifest behavior.

This model does **not** implement SemVer ranges, package upgrade/downgrade, runtime negotiation, provider handshakes, or automatic migrations.

### Strictness

The parser rejects:

- unsupported schema versions;
- unknown JSON properties;
- missing or unsafe project names;
- duplicate capability IDs within a list;
- the same capability in include and exclude lists;
- unknown capabilities/providers;
- provider IDs placed in capability include/exclude lists;
- non-provider IDs placed in `providers`;
- tooling IDs selected as runtime capabilities;
- invalid capability-contract versions;
- unknown or unresolved capability-contract requirements;
- incompatible capability-contract versions;
- exclusions that break required dependency closure;
- dependency cycles.

Generation adds destination and output safety checks on top of parser strictness.

## Maturity behavior

`validate` and `new` distinguish **structural validity**, **contract compatibility**, and **capability maturity**.

Planned, ReferenceOnly, and Preview capabilities are warnings by default. `--require-stable` converts those warnings into a failing readiness gate; for `new`, the failure occurs before any file is written. Contract incompatibility always fails regardless of maturity mode.

A default generation therefore means "a deterministic scaffold for this resolved composition", not "every selected capability is fully implemented or production-ready". The generated architecture report makes missing runtime bindings explicit.

## Package mode versus project mode

Package mode is the portable declaration mode. It emits the known FoundationKit package IDs at the current FoundationKit package baseline. Restoring such a generated project requires a NuGet source that actually contains those FoundationKit packages.

Project mode (`--foundation-root`) is the verified repository-local mode. It references the source projects directly and is used by CI to prove that the generated solution builds and tests against the exact repository head.

Neither mode downloads arbitrary packages, executes provider hooks, or infers a package name from user-controlled manifest text.

## Security and destructive-operation boundaries

Composer:

- never executes code from the manifest;
- does not support script/template hooks in v1;
- uses catalog-owned package/project mappings only;
- does not print raw manifest contents during validation/explanation;
- bounds capability contract versions to positive integers from 1 through 9999;
- refuses filesystem-root generation;
- refuses to overwrite non-empty destinations by default;
- `--force` requires a valid Composer marker, the exact recorded file set, and matching SHA-256 for every generated file; user-added or edited files block deletion;
- generated content contains no secrets;
- project-reference mode verifies the supplied FoundationKit source-tree marker before generation.

## Still future work

The deterministic engine now exists. The following remain separate tooling work:

```text
interactive foundationkit new questionnaire
visual Workbench composer
richer provider-specific wiring templates
generated deployment topology
business-domain templates
```

Any interactive or visual composer must consume this same deterministic generation engine instead of introducing a parallel capability model.
