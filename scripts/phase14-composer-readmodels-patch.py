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
normalizer = normalizer_path.read_text(encoding="utf-8")
normalizer = replace_once(
    normalizer,
    """            if (!resourceMap.TryGetValue(source, out var sourceResource) || !sourceResource.IsExecutable)\n            {\n                throw new ComposerManifestException(\n                    $\"Read model '{moduleName}.{name}' source resource '{source}' must be an executable resource in the same module.\");\n            }\n\n            var join = NormalizeJoin(document.Join, moduleName, name, sourceResource, resourceMap);\n""",
    """            if (!resourceMap.TryGetValue(source, out var sourceResource) || !sourceResource.IsExecutable)\n            {\n                throw new ComposerManifestException(\n                    $\"Read model '{moduleName}.{name}' source resource '{source}' must be an executable resource in the same module.\");\n            }\n            if (!sourceResource.Behaviors.Contains(ComposerResourceBehavior.Authorization))\n            {\n                throw new ComposerManifestException(\n                    $\"Read model '{moduleName}.{name}' source resource must enable authorization in the current generated contract.\");\n            }\n\n            var join = NormalizeJoin(document.Join, moduleName, name, sourceResource, resourceMap);\n""",
    "read-model source authorization",
)
normalizer = replace_once(
    normalizer,
    """        if (string.Equals(resourceName, source.Name, StringComparison.OrdinalIgnoreCase))\n            throw new ComposerManifestException($\"Read model '{moduleName}.{readModelName}' cannot join its source resource to itself yet.\");\n\n        var leftField = ResolveResourceField(\n""",
    """        if (string.Equals(resourceName, source.Name, StringComparison.OrdinalIgnoreCase))\n            throw new ComposerManifestException($\"Read model '{moduleName}.{readModelName}' cannot join its source resource to itself yet.\");\n        if (!joined.Behaviors.Contains(ComposerResourceBehavior.Authorization))\n        {\n            throw new ComposerManifestException(\n                $\"Read model '{moduleName}.{readModelName}' join resource must enable authorization in the current generated contract.\");\n        }\n\n        var leftField = ResolveResourceField(\n""",
    "read-model join authorization",
)
normalizer = replace_once(normalizer, "var maximumFilters = document?.MaximumFilters ?? 10;", "var maximumFilters = document?.MaximumFilters ?? 0;", "read-model default filters")
normalizer = replace_once(normalizer, "var maximumSorts = document?.MaximumSorts ?? 5;", "var maximumSorts = document?.MaximumSorts ?? 0;", "read-model default sorts")
normalizer_path.write_text(normalizer, encoding="utf-8")

generator_path = Path("tools/FoundationKit.Composer/ComposerExecutableResourceGenerator.cs")
generator = generator_path.read_text(encoding="utf-8")
generator = replace_once(
    generator,
    """        files[\"ARCHITECTURE.md\"] = BuildExecutableArchitecture(manifest, executable);\n        return files;\n""",
    """        files[\"ARCHITECTURE.md\"] = BuildExecutableArchitecture(manifest, executable);\n        ComposerReadModelGenerator.Apply(manifest, projectPrefix, files);\n        return files;\n""",
    "read-model generator integration",
)
generator_path.write_text(generator, encoding="utf-8")

schema_path = Path("catalog/foundationkit.project.schema.json")
schema = json.loads(schema_path.read_text(encoding="utf-8"))
module_properties = schema["$defs"]["module"]["properties"]
module_properties["readModels"] = {
    "type": "array",
    "maxItems": 32,
    "items": {"$ref": "#/$defs/readModel"},
}
schema["$defs"]["readModel"] = {
    "type": "object",
    "additionalProperties": False,
    "required": ["name", "route", "kind", "source", "join", "fields"],
    "properties": {
        "name": {"type": "string", "minLength": 1, "maxLength": 96},
        "route": {"type": "string", "minLength": 1, "maxLength": 96},
        "kind": {"type": "string", "enum": ["query", "report"]},
        "source": {"type": "string", "minLength": 1, "maxLength": 96},
        "join": {"$ref": "#/$defs/readModelJoin"},
        "fields": {
            "type": "array",
            "minItems": 1,
            "maxItems": 64,
            "items": {"$ref": "#/$defs/readModelField"},
        },
        "api": {"$ref": "#/$defs/readModelApi"},
    },
}
schema["$defs"]["readModelJoin"] = {
    "type": "object",
    "additionalProperties": False,
    "required": ["type", "resource", "leftField", "rightField"],
    "properties": {
        "type": {"type": "string", "enum": ["left"]},
        "resource": {"type": "string", "minLength": 1, "maxLength": 96},
        "leftField": {"type": "string", "minLength": 1, "maxLength": 96},
        "rightField": {"type": "string", "minLength": 1, "maxLength": 96},
    },
}
schema["$defs"]["readModelField"] = {
    "type": "object",
    "additionalProperties": False,
    "required": ["name", "from", "type"],
    "properties": {
        "name": {"type": "string", "minLength": 1, "maxLength": 96},
        "from": {"type": "string", "minLength": 3, "maxLength": 193},
        "type": {"type": "string", "enum": ["text", "guid"]},
        "required": {"type": "boolean"},
        "maximumLength": {"type": "integer", "minimum": 1, "maximum": 4000},
        "query": {"$ref": "#/$defs/fieldQuery"},
    },
}
schema["$defs"]["readModelApi"] = {
    "type": "object",
    "additionalProperties": False,
    "properties": {
        "routePrefix": {"type": "string", "minLength": 1, "maxLength": 48},
        "maximumFilters": {"type": "integer", "minimum": 0, "maximum": 25},
        "maximumSorts": {"type": "integer", "minimum": 0, "maximum": 1},
        "rateLimitPolicyName": {"type": ["string", "null"], "maxLength": 96},
    },
}
schema_path.write_text(json.dumps(schema, indent=2) + "\n", encoding="utf-8")


def configure_example(path: Path) -> None:
    document = json.loads(path.read_text(encoding="utf-8"))
    module = document["modules"][0]
    customer = next(resource for resource in module["resources"] if resource["name"] == "Customer")
    if not any(field["name"] == "Code" for field in customer["fields"]):
        customer["fields"].insert(0, {
            "name": "Code",
            "type": "text",
            "required": True,
            "maximumLength": 64,
            "query": {"filter": "exact", "sortable": False},
            "index": {"enabled": True, "unique": True},
        })
    customer["api"]["maximumFilters"] = 3
    customer["api"]["maximumSorts"] = 1

    profiles = [resource for resource in module["resources"] if resource["name"] == "CustomerProfile"]
    if not profiles:
        module["resources"].append({
            "name": "CustomerProfile",
            "route": "customer-profiles",
            "idType": "guid",
            "behaviors": ["crud", "auditing", "authorization", "concurrency"],
            "fields": [
                {
                    "name": "CustomerCode",
                    "type": "text",
                    "required": True,
                    "maximumLength": 64,
                    "query": {"filter": "exact", "sortable": False},
                    "index": {"enabled": True, "unique": True},
                },
                {
                    "name": "Status",
                    "type": "text",
                    "required": True,
                    "maximumLength": 40,
                    "query": {"filter": "exact", "sortable": False},
                    "index": {"enabled": True, "unique": False},
                },
                {
                    "name": "Detail",
                    "type": "text",
                    "required": False,
                    "maximumLength": 200,
                },
            ],
            "api": {
                "routePrefix": "api",
                "idempotency": "required",
                "concurrency": "require-if-match",
                "maximumFilters": 2,
                "maximumSorts": 0,
            },
        })

    join = {
        "type": "left",
        "resource": "CustomerProfile",
        "leftField": "Code",
        "rightField": "CustomerCode",
    }
    module["readModels"] = [
        {
            "name": "CustomerDirectory",
            "route": "customer-directory",
            "kind": "query",
            "source": "Customer",
            "join": join,
            "fields": [
                {"name": "Id", "from": "Customer.Id", "type": "guid", "required": True},
                {
                    "name": "Code", "from": "Customer.Code", "type": "text", "required": True,
                    "maximumLength": 64, "query": {"filter": "exact", "sortable": False},
                },
                {
                    "name": "Name", "from": "Customer.Name", "type": "text", "required": True,
                    "maximumLength": 120, "query": {"filter": "prefix", "sortable": True},
                },
                {
                    "name": "ProfileStatus", "from": "CustomerProfile.Status", "type": "text", "required": False,
                    "maximumLength": 40, "query": {"filter": "exact", "sortable": False},
                },
            ],
            "api": {"routePrefix": "api", "maximumFilters": 3, "maximumSorts": 1},
        },
        {
            "name": "CustomerStatement",
            "route": "customer-statements",
            "kind": "report",
            "source": "Customer",
            "join": join,
            "fields": [
                {"name": "CustomerId", "from": "Customer.Id", "type": "guid", "required": True},
                {
                    "name": "Code", "from": "Customer.Code", "type": "text", "required": True,
                    "maximumLength": 64, "query": {"filter": "exact", "sortable": False},
                },
                {"name": "Name", "from": "Customer.Name", "type": "text", "required": True, "maximumLength": 120},
                {"name": "ProfileStatus", "from": "CustomerProfile.Status", "type": "text", "required": False, "maximumLength": 40},
            ],
            "api": {"routePrefix": "api", "maximumFilters": 1, "maximumSorts": 0},
        },
    ]
    path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")


for relative in (
    "docs/examples/foundationkit.project.fullstack-a.json",
    "docs/examples/foundationkit.project.fullstack-b.json",
):
    configure_example(Path(relative))
