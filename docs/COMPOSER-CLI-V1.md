# FoundationKit Composer CLI v1

The Composer is the first executable developer-facing layer over the FoundationKit Capability Model.

Its v1 responsibility is deliberately narrow: **list, validate, and explain compositions before project generation exists**.

This prevents the future `foundationkit new` command from becoming a collection of hidden hard-coded templates.

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

Parses the manifest strictly, validates profile/capability/provider choices, resolves transitive dependencies, enforces any explicit capability-contract requirements, and reports maturity warnings.

```bash
dotnet run --project tools/FoundationKit.Composer -- validate docs/examples/foundationkit.project.minimal.json --require-stable
```

Uses the same validation but returns a non-zero exit code if any resolved capability is not `Stable`. This is intended for future generation/release automation that must fail closed.

```bash
dotnet run --project tools/FoundationKit.Composer -- explain docs/examples/foundationkit.project.example.json
```

Prints the dependency-first resolved composition, current contract version, and why each item is present. An explicit compatible requirement is shown on the relevant capability, for example:

```text
authorization [Optional/ReferenceOnly/contract:v1] <- required-by:approvals | requires:v1=compatible
kernel [Kernel/Stable/contract:v1] <- profile:enterprise, required-by:web-api
```

Exact output may evolve; capability IDs, dependency semantics, and declared contract compatibility remain the contract.

## Manifest v1

The JSON shape is documented by:

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

Capability contract versions are independent from NuGet package versions and capability maturity. They version the machine-visible composition contract for a capability identity.

The v1 rule is intentionally small and deterministic:

- every current catalog capability/provider/tooling identity publishes contract version `1`;
- a manifest may optionally require an exact positive integer contract version;
- requirements may target explicitly selected capabilities, providers, or transitive dependencies, as long as they resolve in the final composition;
- a requirement for an unknown or unresolved capability is rejected;
- a requested contract version that does not exactly match the available version fails composition validation;
- compatibility mismatch is an error, not a warning;
- omitting `capabilityContracts` preserves the previous manifest behavior.

Example: if a manifest requires `"approvals": 2` while the catalog provides v1, validation fails closed rather than silently accepting an unknown contract shape.

This first contract-version model does **not** implement SemVer ranges, package upgrade/downgrade, runtime negotiation, provider handshakes, or automatic migration. Those require concrete compatibility evidence before expanding the language.

### Strictness

The parser rejects:

- unsupported schema versions;
- unknown JSON properties;
- missing name/profile;
- unsafe project names;
- duplicate capability IDs within a list;
- the same capability in include and exclude lists;
- unknown capabilities/providers;
- provider IDs placed in capability include/exclude lists;
- non-provider IDs placed in `providers`;
- tooling IDs selected as runtime capabilities;
- invalid capability-contract versions;
- unknown capability-contract IDs;
- capability-contract requirements that do not resolve in the selected composition;
- incompatible capability-contract versions;
- exclusions that break required dependency closure;
- dependency cycles.

## Maturity behavior

A valid manifest is not automatically generatable.

`validate` distinguishes **structural validity**, **contract compatibility**, and **capability maturity**. Planned, reference-only, and preview capabilities are reported as warnings. `--require-stable` turns those maturity warnings into a failing readiness gate. Contract incompatibility always fails regardless of maturity mode.

This is intentional: FoundationKit must never generate a project and imply a capability exists merely because its name appears in the roadmap/catalog, and it must not compose against a contract version it does not provide.

## Security considerations

The Composer:

- never executes code from the manifest;
- does not accept script hooks in v1;
- does not print the raw manifest contents during `validate` or `explain`;
- treats providers as catalog identities, not arbitrary package names or shell commands;
- bounds capability contract versions to positive integers from 1 through 9999;
- has no network/package-install behavior in v1.

Future generation/provider installation must preserve these boundaries and add explicit supply-chain controls.

## Next step

After v1 is verified, the Composer can grow toward:

```text
foundationkit new
  -> choose profile
  -> choose capabilities
  -> resolve dependencies
  -> verify capability contracts
  -> choose providers
  -> show maturity/security warnings
  -> produce deterministic project plan
  -> generate only supported templates
  -> build/test generated result
```

Generation should consume the same compiled capability graph, contract metadata, and manifest parser rather than introducing a parallel model.
