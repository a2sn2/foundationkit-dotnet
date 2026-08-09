# Workbench Dual Full-Stack Reference Architecture

## The Workbench in one sentence

`FoundationKit.Workbench` is the executable architecture/reference consumer that demonstrates two complete vertical slices—User and Admin—connected through one explicit SQL-backed workflow boundary.

> This document describes **Workbench**, not the entire repository. The repository also contains Athar as the complete Arabic reference product and Madar as the operational product under `apps/`.

## Mental model

```text
                         FOUNDATIONKIT REUSABLE CORE
          Domain | Application | Infrastructure | WebApi | Blazor
                                      │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
              USER FULL STACK                     ADMIN FULL STACK
                    │                                   │
        Blazor user UI and UX              Blazor admin UI and UX
                    │                                   │
          WorkbenchApiClient                  WorkbenchApiClient
                    │                                   │
        CreateUserRequest contract            AdminReviewRequest contract
                    │                 GET/POST admin request APIs
        POST /api/user/requests                         │
                    │                                   │
       CreateUserRequestUseCase          ReviewUserRequestUseCase
                    │                                   │
            BuildBrief domain             AdminReview + state transition
                    │                                   │
            BuildBriefs table                   AdminReviews table
                    └─────────────────┬─────────────────┘
                                      │
                          SHARED WORKFLOW BOUNDARY
                   submitted → approved or rejected
```

## User slice

```text
SQL Server
  ↓
WorkbenchDbContext.BuildBriefs
  ↓
BuildBrief aggregate
  ↓
CreateUserRequestUseCase
  ↓
CreateUserRequest / UserRequestResponse
  ↓
/api/user/requests
  ↓
WorkbenchApiClient
  ↓
Pages/UserPortal.razor
```

Routes:

- `POST /api/user/requests` — create a request in `submitted` state.
- `GET /api/user/requests/{id}` — read the latest request state.

Code map:

```text
samples/FoundationKit.Workbench.Contracts/User/
samples/FoundationKit.Workbench/Application/User/
samples/FoundationKit.Workbench/Endpoints/UserPortalEndpoints.cs
samples/FoundationKit.Workbench.Client/Pages/UserPortal.razor
```

## Admin slice

```text
SQL Server
  ↓
BuildBriefs + AdminReviews
  ↓
EfAdminQueueReader / AdminReview
  ↓
ReviewUserRequestUseCase
  ↓
Admin contracts
  ↓
/api/admin/requests
  ↓
WorkbenchApiClient
  ↓
Pages/AdminPortal.razor
```

Routes:

- `GET /api/admin/requests?status=submitted` — SQL-backed review queue.
- `POST /api/admin/requests/{id}/review` — approve or reject a request.

## Integration boundary

The portals do not call each other. They meet through domain state and persistence:

```text
User creates request
        ↓
BuildBrief.Status = Submitted
        ↓
Admin reads submitted queue
        ↓
Admin records approve/reject decision
        ↓
AdminReview inserted + BuildBrief status changed
        ↓
User reads updated status
```

The review record and request transition are committed through the same `IUnitOfWork`.

## Shared vs separate concerns

Shared intentionally:

- FoundationKit packages;
- Workbench SQL Server/EF unit-of-work composition;
- runtime/health/catalog/platform-reference endpoints;
- request workflow vocabulary;
- typed HTTP infrastructure.

Separate intentionally:

- user/admin DTOs;
- use cases;
- route groups;
- UI/UX;
- admin queue/review behavior;
- user create/status behavior.

A UI component is never the integration boundary.

## Reading order

1. `README.md` — repository-wide purpose.
2. `docs/ARCHITECTURE.md` — current repository architecture.
3. `docs/WORKBENCH.md` — Workbench operation and surfaces.
4. this document — dual vertical slices.
5. Workbench contracts/application/endpoints/client pages.
6. Workbench migrations and Postman collection.

## Complete vertical-slice rule

A Workbench feature is complete when the relevant path is visible end-to-end:

```text
Database/external source
        ↓
Infrastructure adapter
        ↓
Domain/application use case
        ↓
Contracts
        ↓
API
        ↓
Typed client
        ↓
Blazor UI states
        ↓
Automated test/smoke evidence
```

## Boundary

Workbench is deliberately a reference consumer, not a Production product. Authentication, product authorization, tenant/user ownership, hardened public ingress, durable integration messaging, and deployment governance are demonstrated more deeply by product consumers or remain product/deployment decisions.
