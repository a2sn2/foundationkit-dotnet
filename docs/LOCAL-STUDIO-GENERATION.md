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

The visual starter lets you choose the project/profile/module/resource baseline, ID type, authorization, auditing, concurrency/idempotency, and whether to include a Blazor client. Apply those selections to the schema-v2 manifest.

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
Generate Project
  ↓
ComposerProjectModelGenerator
  ↓
<FoundationKit repo>\generated\<ProjectName>
```

Generation is enabled only after the current schema-v2 manifest has passed canonical validation.

## Output safety

The browser cannot submit an arbitrary filesystem destination. Core Studio always derives the project directory from the Composer-validated project name and writes only under the configured generation workspace:

```text
generated/<ProjectName>
```

When started through the repository Docker manager:

- FoundationKit `src/` is mounted read-only into Workbench;
- `generated/` is the writable host workspace;
- generated project references point back to the local FoundationKit source tree, so the generated solution can be opened from Windows immediately;
- `generated/` is ignored by the FoundationKit repository itself.

A non-empty generated destination is not overwritten by default. The optional regeneration switch uses Composer's existing ownership marker and SHA-256 checks. It refuses destructive regeneration if generated files changed or a user-added file exists.

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

The generated application remains a starting point. Product-specific domain rules, roles/policies, secrets, deployment configuration, external integrations, and other business decisions remain consumer-owned.

## Local manager commands

```powershell
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs   -Target Workbench
.\foundationkit.ps1 stop   -Target Workbench
```

Core Studio local generation is a development workflow. Production branch governance and go-live controls remain separate under issue #35.
