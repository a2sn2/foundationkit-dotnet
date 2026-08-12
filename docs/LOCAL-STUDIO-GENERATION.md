# Local Core Studio project generation

This is the normal local developer path for starting a new FoundationKit consumer project without manually invoking Composer commands.

## Prerequisites on Windows

- Docker Desktop running;
- PowerShell;
- Git if cloning from GitHub;
- .NET 10 SDK for opening/building the generated solution on the host.

## Download and start

Clone the repository:

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
```

If the repository was downloaded as a ZIP instead, extract it and open PowerShell in the extracted repository root.

Start Core Studio:

```powershell
.\foundationkit.ps1 start -Target Workbench
```

The manager creates the local `generated` workspace if needed, builds/starts the Workbench + SQL Server containers, waits for health, then opens:

```text
http://localhost:8080
```

## Choose, validate, generate

Open:

```text
http://localhost:8080/compose
```

The visual starter lets you choose the project/profile/module/resource baseline, ID type, authorization, auditing, concurrency/idempotency, whether to include a Blazor client, and how the generated project should consume FoundationKit itself. Apply those selections to the schema-v2 manifest.

For advanced project models, edit the manifest JSON directly to add explicit fields, search/sort/index intent, and SQL-view-backed read models. The browser does not own a second schema or generator.

Use this flow:

```text
Choose
  ↓
Apply choices to Manifest
  ↓
Validate
  ↓
ComposerManifestParser + CompositionAnalyzer
  ↓
Choose Foundation binding
  ↓
Generate Project
  ↓
ComposerProjectModelGenerator + Foundation binding finalizer
  ↓
<FoundationKit repo>\generated\<ProjectName>
```

Generation is enabled only after the current schema-v2 manifest has passed canonical validation.

## Foundation binding modes

Core Studio supports two explicit local-source strategies. The default is **Linked** because it keeps the consumer on the canonical reusable Core while the product is actively being developed.

### Linked / reference local Core

- Product projects keep `ProjectReference` links to the canonical local `FoundationKit.*` source projects.
- The generated solution includes the complete FoundationKit project dependency closure required by those references, so opening the generated `.sln` directly in Visual Studio can restore/build without requiring a separate FoundationKit solution to be open.
- Updating the local FoundationKit source can immediately flow into the consumer on the next build.
- The generated workspace is therefore not portable by itself; it expects the parent FoundationKit source tree to remain available at the referenced paths.

### Standalone / source copy

- Composer discovers the same required FoundationKit project dependency closure and copies only those Core project directories into `generated/<ProjectName>/foundation/`.
- Root Core build/package props required by the copied projects are copied into that `foundation/` subtree.
- Product references are rewritten to the vendored Core copy, while the copied FoundationKit projects preserve their internal project-reference topology.
- Generation verifies that no `ProjectReference` escapes the generated workspace.
- The generated solution is portable without the parent FoundationKit repository, but it is a Core snapshot: later FoundationKit updates do not flow into it automatically.

The selected strategy is recorded in `.foundationkit-generated.json`, `FOUNDATION-BINDING.md`, and the generated README. The standalone mode copies required Core **projects and their dependency closure**, not arbitrary individual source files; project-level copying preserves compilation, static assets, package props, and internal dependencies deterministically.

## Output safety

The browser cannot submit an arbitrary filesystem destination. Core Studio always derives the project directory from the Composer-validated project name and writes only under the configured generation workspace:

```text
generated/<ProjectName>
```

When started through the repository Docker manager:

- FoundationKit `src/` is mounted read-only into Workbench;
- `generated/` is the writable host workspace;
- Linked mode references the local canonical FoundationKit source and includes the required Core projects in the generated solution;
- Standalone mode copies the required Core project closure inside the generated workspace;
- `generated/` is ignored by the FoundationKit repository itself.

A non-empty generated destination is not overwritten by default. The optional regeneration switch uses Composer's ownership marker and SHA-256 checks. It refuses destructive regeneration if generated files changed or a user-added file exists. The binding finalizer refreshes that ownership evidence after the solution and any standalone Core copy are finalized.

## After generation

For a project named `MySystem`, open:

```text
<repo>\generated\MySystem\MySystem.sln
```

Then restore/build from the generated project directory as normal:

```powershell
dotnet restore .\MySystem.sln
dotnet build .\MySystem.sln
```

Both Linked and Standalone modes are required to pass direct generated-solution build proof in CI.

The generated application remains a starting point. Product-specific domain rules, roles/policies, secrets, deployment configuration, external integrations, and other business decisions remain consumer-owned.

## Local manager commands

```powershell
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs   -Target Workbench
.\foundationkit.ps1 stop   -Target Workbench
```

Core Studio local generation is a development workflow. Production branch governance and go-live controls remain separate under issue #35.
