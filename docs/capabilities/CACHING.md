# Caching Capability

`FoundationKit.Caching` defines a bounded byte-cache contract with explicit hit/miss/remove/TTL semantics and an in-memory reference provider.

Serialization, encryption, distributed coherence, eviction topology, Redis and cross-region consistency remain provider/application decisions.

Workbench uses this boundary for a reference catalog-read path. Current maturity: `ReferenceOnly`.
