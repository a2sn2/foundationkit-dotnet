# FoundationKit Core v0.1 Baseline

The v0.1 baseline is the closed composable starting point for Core vNext.

It contains:

- .NET 10 / `net10.0` baseline;
- exactly 17 reusable NuGet packages + 17 symbol packages;
- capability graph and seven profiles;
- capability contract versions separate from package versions;
- machine-readable maturity evidence and fail-closed maturity validation;
- strict Composer manifests, validation and dependency explanation;
- deterministic project generation and interactive questionnaire using the same engine;
- Workbench executable reference;
- repository/build/test/package/security gates.

Closure does not mean every roadmap capability is implemented or Stable. It means the current package/composition surface is coherent enough to serve as the compatibility baseline for evidence-driven vNext work.

Core vNext is additive over this baseline unless an explicitly governed breaking release says otherwise.
