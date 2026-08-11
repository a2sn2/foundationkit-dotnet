# Workbench Full-Stack Reference

Workbench keeps executable database-to-client paths to demonstrate architectural boundaries while FoundationKit remains reusable and schema-neutral.

The original connected user/admin workflow remains only as historical integration-smoke evidence. It is **not** the active product/frontend model. The active Core vNext reference paths are the generic CRUD/API engine, generated SQL/read-model products, runtime OpenAPI/typed transport, Core Studio, canonical visual Composer validation, and deterministic generated Blazor shell.

The generic CRUD proof follows:

```text
HTTP request
  -> generic FoundationKit endpoint
  -> generic CRUD application service
  -> host mapper / validator / authorization / manager
  -> generic repository + UoW
  -> Workbench DbContext / SQL Server
  -> Result / Problem Details / response
```

The multi-table/report read proof follows:

```text
HTTP GET
  -> read-model endpoint/query service
  -> read-only specification
  -> EF keyless view mapping
  -> product-owned SQL View
  -> SQL Server
  -> versioned DTO / OpenAPI
  -> deterministic typed client
  -> presentation-only UI
```

The generation/tooling proof follows:

```text
schema-v2 manifest
  -> canonical Composer parser/analyzer
  -> deterministic generated product
  -> live runtime OpenAPI
  -> Postman + C# typed client
  -> deterministic Blazor app shell
```

The host/generated product owns its database schema, migrations, business rules, authorization semantics and deployment decisions. Reusable FoundationKit packages own repeatable orchestration and bounded contracts only. Browser state never becomes an authorization or relational-composition boundary.
