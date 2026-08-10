# Auditing Capability

`FoundationKit.Auditing` provides bounded provider-neutral audit request/event/context, recorder and sink contracts. Core vNext also provides `CrudAuditObserver` so successful generic CRUD commands can emit audit intent without coupling Application to an audit provider.

The package rejects sensitive attribute names and bounds identifiers/attributes. Consumers still choose persistence/SIEM, retention, fail-open/fail-closed behavior, and any transactional audit design.

Current maturity: `ReferenceOnly`. Implementation/tests and Workbench CRUD composition exist; broad provider and compatibility support are not claimed.
