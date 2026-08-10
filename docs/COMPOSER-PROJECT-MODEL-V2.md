# FoundationKit Composer Project Model v2

## Purpose

Composer schema v2 expands FoundationKit composition from project/profile/capability selection into a bounded project model:

```text
Project
  → Modules
    → Resources
      → Behaviors
      → Overrides
      → API
  → Providers
```

It reuses the same canonical FoundationKit capability catalog, dependency resolver, capability-contract versions, maturity evidence, and deterministic generator. It does not create a second capability graph or a low-code runtime.

The v2 model is configuration intent. Business rules, database fields, authorization semantics, external integrations, secrets, and product-specific workflows remain consumer code/configuration.

## Compatibility

Schema v1 remains supported unchanged.

```text
schemaVersion: 1
→ profile/capability/provider composition
→ Composer generator contract 1
```

Schema v2 is additive:

```text
schemaVersion: 2
→ the same profile/capability/provider composition
+ modules/resources/behaviors/overrides/API intent
→ Composer generator contract 2
```

Rules:

- v1 manifests do not accept `modules`;
- v2 manifests require at least one module;
- the `new` command chooses the generator from `schemaVersion`; there is no parallel v2 command;
- no v1 manifest rewrite is required to keep using FoundationKit;
- `--force` keeps the same owned-file and SHA-256 safety model in both versions;
- future breaking changes to v2 manifest semantics require a new schema version rather than redefining accepted v2 meaning.

The dedicated Composer CI workflow generates, force-regenerates, restores, builds, and tests both a v1 golden project and a v2 project-model project on the same repository head.

## Manifest shape

Example:

```json
{
  "schemaVersion": 2,
  "name": "MyPlatform",
  "profile": "minimal",
  "includeCapabilities": ["auditing", "authorization", "caching"],
  "excludeCapabilities": [],
  "providers": [],
  "capabilityContracts": {
    "authorization": 1
  },
  "modules": [
    {
      "name": "Customers",
      "resources": [
        {
          "name": "Customer",
          "route": "customers",
          "idType": "guid",
          "behaviors": [
            "crud",
            "auditing",
            "authorization",
            "concurrency",
            "caching"
          ],
          "overrides": {
            "manager": "CustomerManager"
          },
          "api": {
            "routePrefix": "api",
            "idempotency": "required",
            "concurrency": "require-if-match",
            "maximumFilters": 4,
            "maximumSorts": 2,
            "rateLimitPolicyName": "customer-write"
          }
        }
      ]
    }
  ]
}
```

The repository example is `docs/examples/foundationkit.project.v2.json`. The machine-readable schema is `catalog/foundationkit.project.schema.json`.

## Modules

A module is a bounded grouping of resources. Module names are safe C# identifiers because current generated resource descriptors use the module name in an inspectable namespace.

Current bounds:

- at least 1 module in schema v2;
- at most 32 modules;
- module names are unique case-insensitively;
- each module contains 1–64 resources;
- the entire project contains at most 256 resources.

## Resources

A resource declares reusable platform behavior for one named resource boundary.

Required fields:

```text
name
route
idType
behaviors
```

Resource names are safe C# identifiers. Effective API routes must be unique across the entire manifest.

### ID types

The current closed set is:

```text
guid
string
long
int
```

Composer does not accept arbitrary C# type text from JSON.

### Routes

Resource route and API route-prefix values are bounded ASCII route segments containing only letters, digits, and `-`. Empty segments, control characters, leading/trailing `-`, and unsafe arbitrary route syntax are rejected.

## Behaviors versus global capabilities

`behaviors` intentionally does not mean the same thing as the top-level capability ID list.

Top-level capability IDs represent the canonical FoundationKit capability graph, such as:

```text
authorization
workflow
provider-sqlserver
web-api
```

Resource behaviors describe module/resource intent:

```text
crud
auditing
authorization
concurrency
workflow
caching
security
identity
approvals
notifications
settings
feature-management
localization
```

Where a reusable Core capability exists, Composer maps the behavior back into the same canonical capability resolver. For example:

```text
Customer behavior: authorization
       ↓
canonical authorization capability
       ↓
canonical dependency graph
       ↓
identity
       ↓
security
```

`explain` therefore reports reasons such as:

```text
resource:Customers.Customer:authorization
required-by:authorization
```

There is no separate resource dependency graph.

`crud` and `concurrency` are current module/application-engine behaviors rather than separate catalog capability IDs, so they do not invent new global identities.

### Current executable boundary

Schema v2 currently requires every resource to include `crud` because the proven executable Module Engine is CRUD-based. Accepting a non-CRUD resource today would imply an executable engine that does not exist yet.

When FoundationKit gains independently proven non-CRUD resource engines, a later compatible schema/version decision can expand this boundary deliberately.

## Overrides

Current v2 override surface:

```json
{
  "manager": "CustomerManager"
}
```

The manager value is a safe identifier only. Composer does not accept arbitrary source code, namespaces, scripts, expressions, templates, or executable hooks from the manifest.

The generated descriptor records the override intent so later executable composition can bind it through the Foundation Module/Manager seam.

## API intent

Per-resource API configuration supports:

```text
routePrefix
idempotency
concurrency
maximumFilters
maximumSorts
rateLimitPolicyName
```

Idempotency modes:

```text
disabled
optional
required
```

Concurrency modes:

```text
application-policy
require-if-match
```

Bounds:

```text
maximumFilters: 0..25
maximumSorts:   0..10
```

The API section records intent compatible with the existing API Engine. Phase 11 does not yet claim that every v2 descriptor becomes an executable database/API resource automatically; that is the Phase 12 pre-frontend proof.

## Global capability/provider resolution

The existing top-level fields remain authoritative for project composition:

```text
profile
includeCapabilities
excludeCapabilities
providers
capabilityContracts
```

Resource behaviors contribute required runtime capability IDs to that same composition before resolution.

A resource-required capability cannot be globally excluded. Provider IDs still belong only in `providers`, tooling IDs cannot be selected as runtime capabilities, and capability-contract compatibility remains exact and fail-closed.

Because the current executable resource model is HTTP based, a valid schema-v2 resource composition must resolve `web-api`.

## Deterministic generation

For schema v2, `new` first invokes the proven v1 structural scaffold generator, then overlays v2 project-model artifacts and rebuilds the ownership marker.

Generated v2 additions include:

```text
foundationkit.project.json
PROJECT-MODEL.md
src/<Product>.Application/GeneratedModules/<Module>/<Resource>Definition.g.cs
.foundationkit-generated.json
```

`foundationkit.project.json` is the normalized machine-readable v2 manifest. `PROJECT-MODEL.md` is a human-readable report. Resource descriptors are small inspectable C# configuration artifacts; they do not contain hidden business logic.

The ownership marker records:

```json
"generatorContractVersion": "2"
```

and SHA-256 for the generated set. A user-added file or edited generated file blocks destructive `--force` regeneration.

The same input, FoundationKit baseline, reference mode, and generator contract produce the same filenames and bytes. No timestamp, random identifier, machine name, secret, or local absolute path is emitted.

## v1 preservation

The v1 generator itself remains the v1 generator. Phase 11 does not route v1 through the v2 overlay.

CI proves:

```text
v1 manifest
→ generate
→ hash
→ --force regenerate
→ identical hashes
→ restore
→ build
→ test

v2 manifest
→ validate
→ generate
→ hash
→ --force regenerate
→ identical hashes
→ verify project-model artifacts
→ restore
→ build
→ test
```

This is the compatibility gate for the Composer model evolution.

## Interactive and visual composition

The current interactive CLI questionnaire still produces schema v1. It remains a simple profile/capability/provider input layer over the same analyzer/generator.

A future FoundationKit Studio/Workbench composer should author the v2 model visually, but it must serialize the same v2 manifest and invoke the same deterministic engine. It must not maintain a parallel graph or hidden project model.

## Security and trust boundary

Composer v2:

- rejects unknown JSON fields at every modeled level;
- rejects unsafe identifiers/routes and unsupported ID types;
- rejects duplicate modules/resources/effective API routes;
- rejects duplicate/unknown behaviors;
- rejects resource-required capabilities that are globally excluded;
- never executes manifest content;
- never accepts arbitrary C# source through overrides;
- never infers package IDs from user-controlled text;
- keeps package/project bindings catalog-owned;
- keeps destructive regeneration protected by exact ownership/hash verification.

## Phase 11 acceptance boundary

Phase 11 is complete when one exact repository head proves:

```text
strict v1 parser compatibility
+ strict v2 parser/model validation
+ canonical capability resolution for resource behaviors
+ deterministic normalized v2 manifest
+ deterministic v2 descriptors/report/ownership marker
+ v1 generation/reproduction/build/test
+ v2 generation/reproduction/build/test
+ normal repository build/test/package/security gates
```

Phase 11 does **not** claim the final full-stack generated-resource scenario. Phase 12 must prove that separately before frontend work begins.
