# FoundationKit Workbench

Workbench is the executable architecture/reference consumer for FoundationKit Core. It is not a production service and does not define universal business semantics.

## What it proves

- SQL Server provider selection owned by the host;
- host-owned EF schema/migrations;
- database/domain/application/API/client boundaries;
- connected user/admin reference workflow;
- Settings, Feature Management, Localization, and Caching runtime paths;
- Core vNext Module/CRUD Engine through a real SQL table and generic endpoints.

## Core CRUD reference

The `CoreCrud` module is configured with `.Crud().Auditing().Authorization().Concurrency().UseManager<CoreCrudManager>()`.

Its generic endpoints are:

```text
POST   /api/core-crud
GET    /api/core-crud
GET    /api/core-crud/{id}
PUT    /api/core-crud/{id}
DELETE /api/core-crud/{id}
```

The Workbench host supplies the request/response contracts, mapper, validator, semantic authorization policy, concurrency policy, manager override, SQL entity configuration/migration, and audit sink. FoundationKit supplies the generic orchestration and endpoint plumbing.

## Run

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Swagger/OpenAPI: `/swagger`
Health: `/api/health`

The SQL integration smoke exercises create/read/list/update, stale-version conflict, custom manager rejection, delete, and post-delete not-found behavior.
