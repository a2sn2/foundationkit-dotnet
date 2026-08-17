# Feature Management Capability

`FoundationKit.FeatureManagement` evaluates bounded Boolean feature definitions through a provider-neutral `IFeatureEvaluator` contract.

The existing `SettingBackedFeatureEvaluator` preserves explicit FoundationKit defaults and fail-closed invalid settings. `AbpFeatureEvaluator` is an optional ABP OSS provider bridge that delegates current-context feature enablement to ABP `IFeatureChecker` while returning the same FoundationKit `FeatureDecision` model.

ABP is not required by consumers that continue to use the settings-backed evaluator. Percentage rollout, experiments, targeting and organization semantics are not added by this bridge unless the selected provider/consumer explicitly supplies them.

Workbench provides runtime reference evidence. Current maturity remains `ReferenceOnly`.

See `docs/PLATFORM-LEVERAGE-AUDIT.md`.
