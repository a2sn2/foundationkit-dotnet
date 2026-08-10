# Workbench Full-Stack Reference

Workbench keeps connected database-to-client paths to demonstrate architectural boundaries while FoundationKit remains reusable and schema-neutral.

Current reference paths include a connected user/admin workflow plus the Core vNext generic CRUD engine.

The CRUD proof follows:

```text
HTTP request
  -> generic FoundationKit endpoint
  -> generic CRUD application service
  -> host mapper / validator / authorization / manager
  -> generic repository + UoW
  -> Workbench DbContext / SQL Server
  -> Result / Problem Details / response
```

The host owns its database schema, migration, business demonstration rules, and transport contracts. The reusable packages own the repeatable orchestration only.
