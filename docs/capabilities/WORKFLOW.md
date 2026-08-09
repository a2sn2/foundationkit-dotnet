# FoundationKit.Workflow

`FoundationKit.Workflow` is the deterministic state-transition capability above `FoundationKit.Auditing`. A product defines states, triggers and transitions; FoundationKit validates a deterministic graph, resolves allowed transitions, fails closed for unknown transitions, and can create bounded audit intent.

It is not a BPMN engine, scheduler, workflow database, task system, visual designer, or approval-routing platform.

## Current v1 surface

- `WorkflowTransitionDefinition` — transition ID, source state, trigger, destination state;
- `WorkflowDefinition` — immutable validated transition graph;
- `WorkflowTransition` — resolved transition record;
- `WorkflowId` — bounded workflow identity;
- `WorkflowTransitionAudit` — mapping into FoundationKit Auditing metadata.

Construction rejects duplicate transition IDs and ambiguous `fromState + trigger` pairs. Runtime resolution never invents fallback destinations.

Persistence/history, timers, workflow-version migration, human tasks, escalation, webhooks and dynamic expression execution remain product/provider concerns.

## Current consumer evidence

Athar defines its product-owned initiative review graph (`submitted + approve/reject`) and retains self-review validation, mutation, persistence, concurrency, domain events and HTTP authorization.

Madar independently defines its case lifecycle (`new → assigned → in-progress → resolved → closed`) through the reusable deterministic transition boundary while keeping assignment, departments/routing, SLA, approvals, transfer/reassignment and product persistence in Madar.

The two products prove that the small deterministic transition contract survives distinct domains without requiring product-specific API expansion.

## Maturity

Workflow remains `ReferenceOnly`. Cross-product evidence is stronger than at first extraction, but v1 still intentionally excludes persistent workflow instances, version migration, scheduler/task semantics, advanced routing and a broader compatibility/support commitment. Maturity Evidence v1—not consumer count alone—governs promotion.
