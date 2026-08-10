#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
import uuid
from pathlib import Path
from typing import Any

HTTP_METHOD_ORDER = {
    "get": 0,
    "post": 1,
    "put": 2,
    "patch": 3,
    "delete": 4,
    "options": 5,
    "head": 6,
    "trace": 7,
}

POSTMAN_SCHEMA = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
UUID_EXAMPLE = "00000000-0000-0000-0000-000000000001"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a deterministic Postman v2.1 collection from an OpenAPI 3 document."
    )
    parser.add_argument("openapi", type=Path, help="OpenAPI JSON document.")
    parser.add_argument("output", type=Path, help="Postman collection JSON path.")
    parser.add_argument(
        "--base-url",
        default="http://localhost:5057",
        help="Default value for the Postman {{baseUrl}} variable.",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail when output does not exactly match deterministic generation.",
    )
    return parser.parse_args()


def load_document(path: Path) -> dict[str, Any]:
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SystemExit(f"Unable to read OpenAPI document '{path}': {exc}") from exc

    if not isinstance(document, dict) or not str(document.get("openapi", "")).startswith("3."):
        raise SystemExit("Only OpenAPI 3.x JSON documents are supported.")
    if not isinstance(document.get("paths"), dict):
        raise SystemExit("OpenAPI document does not contain a paths object.")
    return document


def resolve_ref(document: dict[str, Any], ref: str) -> dict[str, Any]:
    if not ref.startswith("#/"):
        raise ValueError(f"Only local OpenAPI references are supported: {ref}")
    value: Any = document
    for token in ref[2:].split("/"):
        token = token.replace("~1", "/").replace("~0", "~")
        if not isinstance(value, dict) or token not in value:
            raise ValueError(f"OpenAPI reference cannot be resolved: {ref}")
        value = value[token]
    if not isinstance(value, dict):
        raise ValueError(f"OpenAPI reference does not resolve to an object: {ref}")
    return value


def merged_schema(
    document: dict[str, Any],
    schema: dict[str, Any],
    trail: tuple[str, ...] = (),
) -> dict[str, Any]:
    if "$ref" in schema:
        ref = str(schema["$ref"])
        if ref in trail:
            return {"type": "object"}
        return merged_schema(document, resolve_ref(document, ref), trail + (ref,))

    if "allOf" in schema:
        merged: dict[str, Any] = {}
        properties: dict[str, Any] = {}
        required: list[str] = []
        for part in schema.get("allOf", []):
            if not isinstance(part, dict):
                continue
            expanded = merged_schema(document, part, trail)
            for key, value in expanded.items():
                if key == "properties" and isinstance(value, dict):
                    properties.update(value)
                elif key == "required" and isinstance(value, list):
                    for item in value:
                        if item not in required:
                            required.append(item)
                else:
                    merged[key] = value
        if properties:
            merged["properties"] = properties
        if required:
            merged["required"] = required
        return merged

    return schema


def schema_example(
    document: dict[str, Any],
    schema: dict[str, Any] | None,
    property_name: str | None = None,
    trail: tuple[str, ...] = (),
) -> Any:
    if not schema:
        return None

    if "$ref" in schema:
        ref = str(schema["$ref"])
        if ref in trail:
            return {}
        return schema_example(document, resolve_ref(document, ref), property_name, trail + (ref,))

    if "example" in schema:
        return schema["example"]
    if "default" in schema:
        return schema["default"]
    enum = schema.get("enum")
    if isinstance(enum, list) and enum:
        return enum[0]

    if "allOf" in schema:
        return schema_example(document, merged_schema(document, schema, trail), property_name, trail)

    schema_type = schema.get("type")
    if schema_type is None and "properties" in schema:
        schema_type = "object"

    if schema_type == "object":
        properties = schema.get("properties", {})
        if not isinstance(properties, dict):
            return {}
        return {
            name: schema_example(document, child, name, trail)
            for name, child in sorted(properties.items())
            if isinstance(child, dict) and not child.get("readOnly", False)
        }

    if schema_type == "array":
        items = schema.get("items")
        return [schema_example(document, items if isinstance(items, dict) else {}, property_name, trail)]

    if schema_type == "integer":
        minimum = schema.get("minimum")
        return int(minimum) if isinstance(minimum, (int, float)) else 1

    if schema_type == "number":
        minimum = schema.get("minimum")
        return float(minimum) if isinstance(minimum, (int, float)) else 1.0

    if schema_type == "boolean":
        return True

    if schema_type == "string" or schema_type is None:
        fmt = schema.get("format")
        lower_name = (property_name or "").lower()
        if fmt == "uuid":
            return UUID_EXAMPLE
        if fmt == "date-time":
            return "2026-01-01T00:00:00Z"
        if fmt == "date":
            return "2026-01-01"
        if fmt == "email" or "email" in lower_name:
            return "user@example.com"
        if fmt in {"uri", "url"} or lower_name.endswith("url"):
            return "https://example.com"
        if "decision" in lower_name:
            return "approve"
        if lower_name == "status":
            return "submitted"
        if lower_name.endswith("id"):
            return UUID_EXAMPLE
        min_length = schema.get("minLength")
        value = "value"
        if isinstance(min_length, int) and min_length > len(value):
            value = "x" * min(min_length, 32)
        max_length = schema.get("maxLength")
        if isinstance(max_length, int):
            value = value[:max_length]
        return value

    return None


def parameter_value(parameter: dict[str, Any]) -> str:
    name = str(parameter.get("name", "value"))
    schema = parameter.get("schema")
    if not isinstance(schema, dict):
        schema = {}
    lower = name.lower()

    if lower == "page":
        return "1"
    if lower == "pagesize":
        return "20"
    if lower == "filter":
        return "field|eq|value"
    if lower == "sort":
        return "field|asc"
    if lower == "idempotency-key":
        return "contract-example-key"
    if lower == "if-match":
        return '"1"'
    if lower == "id":
        return UUID_EXAMPLE

    if schema.get("type") == "array":
        return "value"
    if schema.get("format") == "uuid":
        return UUID_EXAMPLE
    if schema.get("type") in {"integer", "number"}:
        return "1"
    if schema.get("type") == "boolean":
        return "true"
    enum = schema.get("enum")
    if isinstance(enum, list) and enum:
        return str(enum[0])
    return "value"


def operation_parameters(
    path_item: dict[str, Any],
    operation: dict[str, Any],
) -> list[dict[str, Any]]:
    parameters: list[dict[str, Any]] = []
    for source in (path_item.get("parameters", []), operation.get("parameters", [])):
        if isinstance(source, list):
            parameters.extend(item for item in source if isinstance(item, dict))

    deduped: dict[tuple[str, str], dict[str, Any]] = {}
    for parameter in parameters:
        key = (str(parameter.get("in", "")), str(parameter.get("name", "")).lower())
        deduped[key] = parameter
    return [deduped[key] for key in sorted(deduped)]


def raw_url(path: str, query: list[dict[str, Any]]) -> str:
    converted = path
    for segment in path.split("/"):
        if segment.startswith("{") and segment.endswith("}"):
            name = segment[1:-1]
            converted = converted.replace(segment, "{{" + name + "}}")
    enabled = [item for item in query if not item.get("disabled", False)]
    if not enabled:
        return "{{baseUrl}}" + converted
    query_string = "&".join(f"{item['key']}={item['value']}" for item in enabled)
    return "{{baseUrl}}" + converted + "?" + query_string


def postman_url(path: str, parameters: list[dict[str, Any]]) -> dict[str, Any]:
    query: list[dict[str, Any]] = []
    variables: list[dict[str, Any]] = []

    converted_segments: list[str] = []
    for raw_segment in [segment for segment in path.split("/") if segment]:
        if raw_segment.startswith("{") and raw_segment.endswith("}"):
            name = raw_segment[1:-1]
            converted_segments.append("{{" + name + "}}")
        else:
            converted_segments.append(raw_segment)

    for parameter in parameters:
        location = parameter.get("in")
        name = str(parameter.get("name", ""))
        if location == "query":
            entry: dict[str, Any] = {
                "key": name,
                "value": parameter_value(parameter),
            }
            if not parameter.get("required", False):
                entry["disabled"] = True
            query.append(entry)
        elif location == "path":
            variables.append(
                {
                    "key": name,
                    "value": parameter_value(parameter),
                }
            )

    result: dict[str, Any] = {
        "raw": raw_url(path, query),
        "host": ["{{baseUrl}}"],
        "path": converted_segments,
    }
    if query:
        result["query"] = query
    if variables:
        result["variable"] = variables
    return result


def request_headers(parameters: list[dict[str, Any]], has_json_body: bool) -> list[dict[str, Any]]:
    headers: list[dict[str, Any]] = []
    if has_json_body:
        headers.append({"key": "Content-Type", "value": "application/json"})

    for parameter in parameters:
        if parameter.get("in") != "header":
            continue
        entry: dict[str, Any] = {
            "key": str(parameter.get("name", "")),
            "value": parameter_value(parameter),
        }
        if not parameter.get("required", False):
            entry["disabled"] = True
        headers.append(entry)

    return sorted(headers, key=lambda item: (item["key"].lower() != "content-type", item["key"].lower()))


def request_body(document: dict[str, Any], operation: dict[str, Any]) -> dict[str, Any] | None:
    body = operation.get("requestBody")
    if not isinstance(body, dict):
        return None
    content = body.get("content")
    if not isinstance(content, dict):
        return None
    media = content.get("application/json")
    if not isinstance(media, dict):
        return None
    schema = media.get("schema")
    if not isinstance(schema, dict):
        schema = {}
    example = schema_example(document, schema)
    return {
        "mode": "raw",
        "raw": json.dumps(example, indent=2, ensure_ascii=False),
        "options": {"raw": {"language": "json"}},
    }


def operation_item(
    document: dict[str, Any],
    path: str,
    method: str,
    path_item: dict[str, Any],
    operation: dict[str, Any],
) -> dict[str, Any]:
    parameters = operation_parameters(path_item, operation)
    body = request_body(document, operation)
    operation_id = str(operation.get("operationId") or f"{method.upper()} {path}")
    request: dict[str, Any] = {
        "method": method.upper(),
        "header": request_headers(parameters, body is not None),
        "url": postman_url(path, parameters),
        "description": f"Generated from OpenAPI operation `{operation_id}`. Do not edit this request manually.",
    }
    if body is not None:
        request["body"] = body
    return {
        "name": operation_id,
        "request": request,
    }


def generate_collection(document: dict[str, Any], base_url: str) -> dict[str, Any]:
    info = document.get("info", {})
    title = str(info.get("title") or "OpenAPI")
    version = str(info.get("version") or "")
    collection_id = str(uuid.uuid5(uuid.NAMESPACE_URL, f"foundationkit-openapi:{title}:{version}"))

    groups: dict[str, list[tuple[str, str, dict[str, Any], dict[str, Any]]]] = {}
    paths = document.get("paths", {})
    for path in sorted(paths):
        path_item = paths[path]
        if not isinstance(path_item, dict):
            continue
        for method in sorted(
            (key for key in path_item if key.lower() in HTTP_METHOD_ORDER),
            key=lambda value: HTTP_METHOD_ORDER[value.lower()],
        ):
            operation = path_item[method]
            if not isinstance(operation, dict):
                continue
            tags = operation.get("tags")
            tag = str(tags[0]) if isinstance(tags, list) and tags else "Other"
            groups.setdefault(tag, []).append((path, method.lower(), path_item, operation))

    folders: list[dict[str, Any]] = []
    for tag in sorted(groups, key=str.casefold):
        operations = sorted(
            groups[tag],
            key=lambda item: (item[0], HTTP_METHOD_ORDER[item[1]], str(item[3].get("operationId", ""))),
        )
        folders.append(
            {
                "name": tag,
                "item": [
                    operation_item(document, path, method, path_item, operation)
                    for path, method, path_item, operation in operations
                ],
            }
        )

    return {
        "info": {
            "_postman_id": collection_id,
            "name": title,
            "description": (
                "Generated deterministically from the runtime OpenAPI contract. "
                "Do not edit this collection manually; regenerate it from OpenAPI."
            ),
            "schema": POSTMAN_SCHEMA,
        },
        "variable": [{"key": "baseUrl", "value": base_url}],
        "item": folders,
    }


def render_collection(document: dict[str, Any], base_url: str) -> str:
    return json.dumps(generate_collection(document, base_url), indent=2, ensure_ascii=False) + "\n"


def main() -> int:
    args = parse_args()
    document = load_document(args.openapi)
    rendered = render_collection(document, args.base_url)

    if args.check:
        if not args.output.exists():
            print(f"Generated Postman collection is missing: {args.output}", file=sys.stderr)
            return 1
        current = args.output.read_text(encoding="utf-8")
        if current != rendered:
            print(
                f"Generated Postman collection is out of date: {args.output}. "
                "Regenerate it from the runtime OpenAPI document.",
                file=sys.stderr,
            )
            return 1
        print(f"Postman contract is synchronized: {args.output}")
        return 0

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(rendered, encoding="utf-8", newline="\n")
    print(f"Generated Postman collection: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
