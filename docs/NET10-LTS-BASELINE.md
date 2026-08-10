# .NET 10 Baseline

FoundationKit targets `net10.0` and uses the .NET 10 SDK/runtime line for the active baseline. Microsoft framework-aligned package versions are centrally managed.

`global.json` establishes the repository SDK floor/roll-forward policy, while CI installs the supported .NET 10 line. Any change to SDK roll-forward, target framework, Microsoft framework packages, containers, or generated-project targets is a coordinated compatibility decision and must be tested across the solution, Composer generation, package output, and Workbench runtime.
