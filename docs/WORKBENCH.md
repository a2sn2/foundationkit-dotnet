# FoundationKit Workbench

## Purpose

`FoundationKit.Workbench` is the **executable architecture/reference consumer** for FoundationKit. It is not the only consumer and it is not a Production product. Athar is the complete Arabic reference product and Madar is the operational product under `apps/`.

Workbench demonstrates two complete SQL-backed vertical slices:

```text
User Full Stack   database → domain → use case → contracts → API → Blazor UI/UX
Admin Full Stack  database → domain → use case → contracts → API → Blazor UI/UX
```

They meet through:

```text
submitted → approved | rejected
```

It also supplies runtime evidence for Settings, Feature Management, Localization, and Caching without moving Workbench-specific behavior into reusable packages.

## Projects

```text
FoundationKit.Workbench.Api        ASP.NET Core host + product domain/use cases + EF/SQL/migrations
FoundationKit.Workbench.Client     Blazor WebAssembly + MudBlazor + user/admin UX
FoundationKit.Workbench.Contracts  shared/user/admin/workflow/runtime contracts
```

Detailed architecture: [`DUAL-FULL-STACK.md`](DUAL-FULL-STACK.md).

## Local run

Canonical Windows manager:

```powershell
.\foundationkit.ps1 start  -Target Workbench -Mode Auto
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs   -Target Workbench
.\foundationkit.ps1 stop   -Target Workbench
```

Direct project launch is also supported when the Workbench SQL connection is configured:

```powershell
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

Docker helpers:

```powershell
.\scripts\run-workbench.ps1
.\scripts\stop-workbench.ps1
```

```bash
./scripts/run-workbench.sh
./scripts/stop-workbench.sh
```

For Windows SQL Server/Visual Studio troubleshooting, use [`LOCAL-RUN-WINDOWS-AR.md`](LOCAL-RUN-WINDOWS-AR.md).

## Main surfaces

```text
/                         architecture/reference landing
/user                     user vertical slice
/admin                    admin vertical slice
/swagger                  OpenAPI UI in supported local mode
/api/health               API + SQL health
/api/catalog              implemented package catalog via reusable Caching boundary
/api/platform-reference   Settings + Feature Management + Localization reference
```

## User slice

- `POST /api/user/requests` creates a request in `submitted` state.
- `GET /api/user/requests/{id}` reads its latest state.

The path runs through contracts → endpoint → use case → aggregate → repository/unit of work → SQL Server, then back through a typed Blazor client.

## Admin slice

- `GET /api/admin/requests?status=submitted` reads the SQL-backed review queue.
- `POST /api/admin/requests/{id}/review` approves or rejects the request.

The review record and request status transition are committed through the same `IUnitOfWork`; the portals communicate through domain/persistence state rather than calling each other's UI.

## Platform reference

`GET /api/platform-reference` proves the current Workbench usage of:

- Settings resolution;
- Feature Management decision evaluation;
- Localization culture/direction/time-zone identity.

`CatalogService` uses `FoundationKit.Caching.ICacheStore` around the embedded human package catalog. The embedded resource remains source of truth; Redis/distributed coherence and object serialization are not implied.

## Database ownership

Workbench owns SQL Server and migrations under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

Current workflow tables are `BuildBriefs` and `AdminReviews`. Reusable FoundationKit packages do not own these tables or their provider choice.

## Postman and automated proof

Postman collection:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

The SQL integration path verifies catalog cache behavior, platform-reference behavior, user request creation, admin queue/review, and user status update against a real SQL Server container.

## GitHub Pages boundary

The Pages portal/static demo can display client/reference surfaces but cannot execute ASP.NET Core or SQL Server. The API-hosted local/integration path is authoritative for persistence claims.

## Production warning

Workbench is intentionally sample/reference scope. It does not claim production identity, product authorization/ownership, public ingress, durable messaging, production secrets, SIEM, backup operations, high availability, or deployment governance. Use Athar/Madar and the Production Readiness document for deeper product/deployment evidence.
