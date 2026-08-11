#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{label} anchor count was {count}, expected 1")
    return text.replace(old, new, 1)


manifest_path = Path("tools/FoundationKit.Composer/ComposerManifest.cs")
text = manifest_path.read_text(encoding="utf-8")
text = replace_once(
    text,
    """public sealed record ComposerModuleDefinition(\n    string Name,\n    IReadOnlyList<ComposerResourceDefinition> Resources);\n\npublic sealed record ComposerProjectModel(IReadOnlyList<ComposerModuleDefinition> Modules)\n{\n    public IReadOnlyList<ComposerResourceDefinition> Resources =>\n        Modules.SelectMany(module => module.Resources).ToArray();\n}\n""",
    """public sealed record ComposerModuleDefinition(\n    string Name,\n    IReadOnlyList<ComposerResourceDefinition> Resources)\n{\n    public IReadOnlyList<ComposerReadModelDefinition> ReadModels { get; init; } =\n        Array.Empty<ComposerReadModelDefinition>();\n}\n\npublic sealed record ComposerProjectModel(IReadOnlyList<ComposerModuleDefinition> Modules)\n{\n    public IReadOnlyList<ComposerResourceDefinition> Resources =>\n        Modules.SelectMany(module => module.Resources).ToArray();\n\n    public IReadOnlyList<ComposerReadModelDefinition> ReadModels =>\n        Modules.SelectMany(module => module.ReadModels).ToArray();\n}\n""",
    "module/project model",
)
text = replace_once(
    text,
    """            normalizedModules.Add(new ComposerModuleDefinition(\n                moduleName,\n                resources.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase).ToArray()));\n""",
    """            var orderedResources = resources\n                .OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)\n                .ToArray();\n            var readModels = ComposerReadModelManifestNormalizer.Normalize(\n                module.ReadModels,\n                moduleName,\n                orderedResources);\n            foreach (var readModel in readModels)\n            {\n                var apiRoute = $\"{readModel.Api.RoutePrefix}/{readModel.Route}\";\n                if (!apiRoutes.Add(apiRoute))\n                    throw new ComposerManifestException($\"Duplicate read-model API route '{apiRoute}'.\");\n            }\n\n            normalizedModules.Add(new ComposerModuleDefinition(moduleName, orderedResources)\n            {\n                ReadModels = readModels\n            });\n""",
    "module read-model normalization",
)
text = replace_once(
    text,
    """    private sealed record ModuleDocument(string? Name, IReadOnlyList<ResourceDocument>? Resources);\n""",
    """    private sealed record ModuleDocument(\n        string? Name,\n        IReadOnlyList<ResourceDocument>? Resources,\n        JsonElement? ReadModels);\n""",
    "module document",
)
manifest_path.write_text(text, encoding="utf-8")

normalizer_path = Path("tools/FoundationKit.Composer/ComposerReadModelManifest.cs")
normalizer = normalizer_path.read_text(encoding="utf-8")n