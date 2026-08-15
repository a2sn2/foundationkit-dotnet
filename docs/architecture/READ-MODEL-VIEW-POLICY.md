# FoundationKit Read Model / SQL View Policy

## Purpose

FoundationKit separates write models from read models so application services do not accumulate multi-table join/reporting logic merely to shape API responses.

## Default rule

- Commands and mutations operate through domain entities, repositories, units of work, and product-owned tables.
- A simple read that is naturally represented by one aggregate/entity may read through the normal repository/specification path.
- A response that combines multiple tables, a statement, dashboard, report, export, or other complex projection should use a dedicated read model.
- For relational products, the preferred first-party read-model source for stable multi-table projections is a product-owned SQL view (or another explicitly configured read source when a view is not appropriate).

```text
Write path
API -> Application command/service -> Entity/Aggregate -> Repository/UoW -> Tables

Read path (multi-table / report)
API -> Query service -> Read-model contract -> SQL View -> Tables
```

## Why

A dedicated view keeps join/calculation/projection logic close to the relational database, lets SQL Server optimize the query plan, keeps application services small, and allows the database projection to evolve without rewriting join logic throughout the application layer.

The view does **not** replace the public DTO contract. The DTO/OpenAPI shape remains versioned. A view can change its internal joins/calculations without changing the DTO, but adding/removing/renaming columns that change the public response still requires normal contract/versioning and migration discipline.

## EF Core model

Relational adapters should map SQL views to explicit keyless/read-only models (for example `HasNoKey().ToView(...)`) or another read-only projection contract. Read models are never tracked for mutation and are not passed to `IRepository<TEntity,TId>` as writable aggregates.

## Query execution

Filtering, sorting, counting and paging over a table-backed resource or a view-backed read model must stay `IQueryable` until the EF Core async terminal operation so the provider performs `WHERE`, `ORDER BY`, counting and paging in SQL. Materialize-then-filter/sort is not an accepted FoundationKit path.

For generated executable resources, filter/sort intent is explicit per field rather than inferred from every property. The first SQL Server contract supports bounded text `exact` and `prefix` filtering plus explicitly enabled sorting. A field exposed for generated filtering or sorting must also opt into a product-owned index. Unsupported fields/operators fail closed before query execution.

Indexes belong to the product schema. Normal indexes are created on the underlying tables unless a provider-specific indexed/materialized-view strategy is explicitly selected. FoundationKit must not imply that every SQL Server view is automatically indexable or should be indexed. Substring patterns such as `%term%` are deliberately not the default indexed-search path because a normal B-tree index may not serve them efficiently.

## Reports and statements

Complex reports/statements should not require deeply nested application joins just to produce the requested shape. Prefer a purpose-built read model/view with a stable contract and a thin query service. Provider-specific alternatives such as stored procedures, table-valued functions, indexed views, materialized views, search engines, or warehouses may be supported through explicit adapters when their requirements justify them.

## Boundaries

- Core remains provider-neutral at the Application contract layer.
- SQL view DDL, EF mappings, migrations, and provider-specific indexes remain product/provider-owned.
- Views are read-only by default.
- Authorization is still enforced before exposing the read model; a database view is not an authorization boundary by itself.
- Tenant/project scoping must be explicit in every reusable query contract.
- No `SELECT *` in generated stable views; columns are explicit and deterministic.
- Avoid hidden N+1 fallbacks after reading a view.
- Do not expose arbitrary client-provided SQL or dynamic column expressions; API query intent is parsed into bounded typed policies and expressions.

## Current proof

The Phase 12.C2 baseline now proves the read-side contract that was originally tracked by issues #134 and #135; both issues are closed as completed.

The SQL Server proof includes:

- explicit searchable/sortable/indexed field intent for generated resources;
- server-side filter/sort/count/page execution without materialize-then-query fallbacks;
- deterministic product-owned index generation and direct SQL evidence;
- generated multi-table and report/statement read models backed by product-owned SQL views;
- deterministic view DDL/migration ownership;
- EF keyless/read-only mapping;
- authorization/project-isolation boundaries;
- runtime OpenAPI, deterministic Postman, and typed-client contract alignment.

This proves the first-party SQL Server read path. It does not claim that every relational provider shares identical view/index semantics, and it does not convert a database view into an authorization or production-readiness boundary.
