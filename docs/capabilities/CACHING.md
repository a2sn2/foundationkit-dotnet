# Caching Capability

`FoundationKit.Caching` keeps its bounded byte-cache compatibility contract with explicit hit/miss/remove/TTL semantics and an in-memory reference provider, and now also exposes a typed `IValueCache` backed by the native .NET `HybridCache` implementation.

Use `AddFoundationHybridCache()` when the consumer wants the native typed cache path. FoundationKit delegates HybridCache serialization/provider composition, stampede protection and tag invalidation mechanics to .NET instead of maintaining a competing cache engine.

The existing `ICacheStore` remains a small provider-neutral compatibility/reference seam. Encryption, distributed topology, Redis selection and cross-region consistency remain application/provider decisions.

Workbench registers both the reference store and the native HybridCache path. Current maturity remains `ReferenceOnly`; adding the native provider does not by itself expand production/provider claims.

See `docs/PLATFORM-LEVERAGE-AUDIT.md`.
