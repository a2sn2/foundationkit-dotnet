# Authorization Capability

`FoundationKit.Authorization` provides permission definitions, role-to-permission grants, authorization subjects/evaluator contracts, and owner-or-privileged access semantics.

Application role names, persistence, tenant/organization scope, and route policy names remain host-owned.

Core vNext generic CRUD uses its own semantic authorization extension point and fails closed when a module declares authorization but supplies no policy. A future adapter may compose that policy directly with richer Authorization primitives without pushing application roles into Core.

Current maturity: `ReferenceOnly`.
