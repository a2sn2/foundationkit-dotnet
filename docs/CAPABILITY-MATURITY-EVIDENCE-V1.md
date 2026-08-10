# Capability Maturity Evidence v1

Every capability/provider/tooling identity has exactly one canonical maturity assessment.

Evidence flags are:

- implementation/proof;
- repository quality gates;
- adoption/reference use;
- compatibility/support commitment.

Minimum fail-closed policy:

```text
Planned       -> bounded rationale
ReferenceOnly -> implementation/proof
Preview       -> implementation/proof + quality
Stable        -> implementation/proof + quality + adoption + compatibility/support
```

The policy does not automatically promote capabilities, equate one Workbench proof with broad production adoption, or convert repository tests into production approval.

Catalog generation verifies one-to-one descriptor/evidence coverage, declared/assessed maturity equality, bounded rationale, and minimum evidence requirements.

When active evidence is removed or replaced, the rationale and evidence flags must be updated in the same change. Historical repository state is not current adoption evidence.
