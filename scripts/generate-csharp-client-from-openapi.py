#!/usr/bin/env python3
"""Generate a bounded deterministic C# client from FoundationKit runtime OpenAPI.

The generator intentionally supports the transport shapes emitted by the current
FoundationKit CRUD/API Engine. Unsupported OpenAPI shapes fail closed instead of
producing partial client code.
"""

from __future__ import annotations

import argparse
import json
import keyword
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

HTTP_METHODS = ("get", "post", "put", "delete", "patch")
FOUNDATION_OPERATIONS = {"list", "get", "create", "update", "delete"}
CSHARP_KEYWORDS = {
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
    "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
    "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
    "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
    "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
    "object", "operator", "out", "override", "params", "private", "protected", "public",
    "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
    "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
    "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
    "volatile", "while",
}


class GenerationError(RuntimeError):
    pass


@dataclass(frozen=True)
class Options:
    openapi: Path
    output: Path
    namespace: str
    class_name: str
    check: bool


@dataclass(frozen=True)
class Parameter:
    wire_name: str
    location: str
    required: bool
    schema: dict[str, Any]


@dataclass(frozen=True)
class Operation:
    module: str
    foundation_operation: str
    method: str
    path: str
    parameters: tuple[Parameter, ...]
    request_schema: dict[str, Any] | None
    response_schema: dict[str, Any] | None
    response_status: int


@dataclass(frozen=True)
class ModelProperty:
    wire_name: str
    name: str
    type_name: str


@dataclass(frozen=True)
class Model:
    name: str
    properties: tuple[ModelProperty, ...]


def parse_args() -> Options:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("openapi", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--class-name", required=True)
    parser.add_argument("--check", action="store_true")
    ns = parser.parse_args()
    validate_namespace(ns.namespace)
    validate_identifier(ns.class_name, "class name")
    return Options(ns.openapi, ns.output, ns.namespace, ns.class_name, ns.check)


def validate_namespace(value: str) -> None:
    parts = value.split(".")
    if not parts or any(not part for part in parts):
        raise GenerationError("C# namespace must contain non-empty identifier segments.")
    for part in parts:
        validate_identifier(part, "namespace segment")


def validate_identifier(value: str, label: str) -> None:
    if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", value) or value in CSHARP_KEYWORDS:
        raise GenerationError(f"Invalid C# {label}: {value!r}")


def load_openapi(path: Path) -> dict[str, Any]:
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise GenerationError(f"OpenAPI file not found: {path}") from error
    except json.JSONDecodeError as error:
        raise GenerationError(f"OpenAPI JSON is invalid: {error}") from error
    if not str(document.get("openapi", "")).startswith("3."):
        raise GenerationError("Foundation typed-client generation requires OpenAPI 3.x.")
    if not isinstance(document.get("paths"), dict):
        raise GenerationError("OpenAPI document is missing 'paths'.")
    if not isinstance(document.get("components", {}).get("schemas", {}), dict):
        raise GenerationError("OpenAPI components.schemas must be an object.")
    return document


def collect_operations(document: dict[str, Any]) -> list[Operation]:
    result: list[Operation] = []
    for path in sorted(document["paths"]):
        path_item = document["paths"][path]
        if not isinstance(path_item, dict):
            continue
        for method in HTTP_METHODS:
            raw = path_item.get(method)
            if not isinstance(raw, dict):
                continue
            module = raw.get("x-foundation-module")
            foundation_operation = raw.get("x-foundation-operation")
            if module is None and foundation_operation is None:
                continue
            if not isinstance(module, str) or not module.strip():
                raise GenerationError(f"{method.upper()} {path} has invalid x-foundation-module.")
            if foundation_operation not in FOUNDATION_OPERATIONS:
                raise GenerationError(
                    f"{method.upper()} {path} has unsupported x-foundation-operation {foundation_operation!r}."
                )
            validate_identifier(module, f"module identifier on {method.upper()} {path}")
            parameters = tuple(parse_parameter(item, method, path) for item in raw.get("parameters", []))
            request_schema = read_request_schema(raw, method, path)
            response_status, response_schema = read_success_response(raw, method, path)
            result.append(
                Operation(
                    module=module,
                    foundation_operation=foundation_operation,
                    method=method.upper(),
                    path=path,
                    parameters=parameters,
                    request_schema=request_schema,
                    response_schema=response_schema,
                    response_status=response_status,
                )
            )
    if not result:
        raise GenerationError("No FoundationKit operations were found in the OpenAPI document.")
    validate_operation_set(result)
    return sorted(result, key=lambda item: (item.module, operation_order(item.foundation_operation), item.path))


def parse_parameter(raw: Any, method: str, path: str) -> Parameter:
    if not isinstance(raw, dict):
        raise GenerationError(f"{method.upper()} {path} contains a non-object parameter.")
    if "$ref" in raw:
        raise GenerationError(f"Referenced parameters are not supported: {method.upper()} {path}.")
    name = raw.get("name")
    location = raw.get("in")
    schema = raw.get("schema")
    if not isinstance(name, str) or location not in {"path", "query", "header"} or not isinstance(schema, dict):
        raise GenerationError(f"Invalid parameter on {method.upper()} {path}: {raw!r}")
    required = bool(raw.get("required", False))
    if location == "path" and not required:
        raise GenerationError(f"Path parameter {name!r} must be required on {method.upper()} {path}.")
    return Parameter(name, location, required, schema)


def read_request_schema(operation: dict[str, Any], method: str, path: str) -> dict[str, Any] | None:
    body = operation.get("requestBody")
    if body is None:
        return None
    if not isinstance(body, dict):
        raise GenerationError(f"Invalid requestBody on {method.upper()} {path}.")
    content = body.get("content", {})
    if not isinstance(content, dict) or "application/json" not in content:
        raise GenerationError(f"Only application/json request bodies are supported: {method.upper()} {path}.")
    media = content["application/json"]
    if not isinstance(media, dict) or not isinstance(media.get("schema"), dict):
        raise GenerationError(f"Missing JSON request schema on {method.upper()} {path}.")
    return media["schema"]


def read_success_response(
    operation: dict[str, Any], method: str, path: str
) -> tuple[int, dict[str, Any] | None]:
    responses = operation.get("responses")
    if not isinstance(responses, dict):
        raise GenerationError(f"Responses are missing on {method.upper()} {path}.")
    successful: list[tuple[int, dict[str, Any]]] = []
    for code, response in responses.items():
        if isinstance(code, str) and code.isdigit() and 200 <= int(code) <= 299 and isinstance(response, dict):
            successful.append((int(code), response))
    if not successful:
        raise GenerationError(f"No 2xx response exists on {method.upper()} {path}.")
    status, response = sorted(successful, key=lambda item: item[0])[0]
    content = response.get("content")
    if content is None:
        return status, None
    if not isinstance(content, dict):
        raise GenerationError(f"Invalid response content on {method.upper()} {path}.")
    json_media = content.get("application/json") or content.get("application/*+json")
    if json_media is None:
        if content:
            raise GenerationError(f"Only JSON success responses are supported: {method.upper()} {path}.")
        return status, None
    if not isinstance(json_media, dict) or not isinstance(json_media.get("schema"), dict):
        raise GenerationError(f"Missing success JSON schema on {method.upper()} {path}.")
    return status, json_media["schema"]


def validate_operation_set(operations: Iterable[Operation]) -> None:
    seen: set[tuple[str, str]] = set()
    for operation in operations:
        key = (operation.module.lower(), operation.foundation_operation)
        if key in seen:
            raise GenerationError(
                f"Duplicate Foundation operation for module {operation.module!r}: {operation.foundation_operation!r}."
            )
        seen.add(key)
        expected_method = {
            "list": "GET",
            "get": "GET",
            "create": "POST",
            "update": "PUT",
            "delete": "DELETE",
        }[operation.foundation_operation]
        if operation.method != expected_method:
            raise GenerationError(
                f"Foundation operation {operation.foundation_operation!r} on {operation.path} must use {expected_method}."
            )


def operation_order(value: str) -> int:
    return {"list": 0, "get": 1, "create": 2, "update": 3, "delete": 4}[value]


def ref_name(schema: dict[str, Any]) -> str | None:
    ref = schema.get("$ref")
    if ref is None:
        return None
    if not isinstance(ref, str) or not ref.startswith("#/components/schemas/"):
        raise GenerationError(f"Only local component-schema references are supported: {ref!r}")
    name = ref.rsplit("/", 1)[1]
    validate_identifier(name, "schema name")
    return name


def resolve_schema(document: dict[str, Any], schema: dict[str, Any]) -> dict[str, Any]:
    name = ref_name(schema)
    if name is None:
        return schema
    components = document["components"]["schemas"]
    resolved = components.get(name)
    if not isinstance(resolved, dict):
        raise GenerationError(f"Referenced schema {name!r} does not exist.")
    return resolved


def collect_models(document: dict[str, Any], operations: Iterable[Operation]) -> list[Model]:
    needed: set[str] = set()

    def visit(schema: dict[str, Any]) -> None:
        name = ref_name(schema)
        if name is not None:
            if name in needed:
                return
            needed.add(name)
            visit(document["components"]["schemas"][name])
            return
        schema_type = schema.get("type")
        if schema_type == "array":
            items = schema.get("items")
            if not isinstance(items, dict):
                raise GenerationError("Array schema is missing items.")
            visit(items)
            return
        if schema_type == "object" or "properties" in schema:
            properties = schema.get("properties", {})
            if not isinstance(properties, dict):
                raise GenerationError("Object schema properties must be an object.")
            if schema.get("additionalProperties") not in (None, False):
                raise GenerationError("additionalProperties objects are not supported by the bounded client generator.")
            for child in properties.values():
                if not isinstance(child, dict):
                    raise GenerationError("Object property schema must be an object.")
                visit(child)

    for operation in operations:
        if operation.request_schema is not None:
            visit(operation.request_schema)
        if operation.response_schema is not None:
            visit(operation.response_schema)

    models: list[Model] = []
    for name in sorted(needed):
        schema = document["components"]["schemas"][name]
        if not isinstance(schema, dict):
            raise GenerationError(f"Schema {name!r} must be an object.")
        models.append(build_model(document, name, schema))
    return models


def build_model(document: dict[str, Any], name: str, schema: dict[str, Any]) -> Model:
    if schema.get("type") not in (None, "object"):
        raise GenerationError(f"Component schema {name!r} must be an object.")
    properties = schema.get("properties", {})
    if not isinstance(properties, dict) or not properties:
        raise GenerationError(f"Component schema {name!r} must contain properties.")
    if schema.get("additionalProperties") not in (None, False):
        raise GenerationError(f"Component schema {name!r} uses unsupported additionalProperties.")
    required = schema.get("required", [])
    if not isinstance(required, list) or any(not isinstance(item, str) for item in required):
        raise GenerationError(f"Component schema {name!r} has invalid required list.")
    required_set = set(required)
    result: list[ModelProperty] = []
    seen_names: set[str] = set()
    for wire_name in sorted(properties):
        raw = properties[wire_name]
        if not isinstance(raw, dict):
            raise GenerationError(f"Property {name}.{wire_name} has invalid schema.")
        property_name = pascal_identifier(wire_name)
        if property_name.lower() in seen_names:
            raise GenerationError(f"Schema {name!r} produces duplicate C# property {property_name!r}.")
        seen_names.add(property_name.lower())
        type_name = csharp_type(document, raw, nullable=wire_name not in required_set)
        result.append(ModelProperty(wire_name, property_name, type_name))
    return Model(name, tuple(result))


def csharp_type(document: dict[str, Any], schema: dict[str, Any], *, nullable: bool) -> str:
    name = ref_name(schema)
    if name is not None:
        return append_nullable(name, nullable, reference=True)
    schema_type = schema.get("type")
    if schema_type == "array":
        items = schema.get("items")
        if not isinstance(items, dict):
            raise GenerationError("Array schema is missing items.")
        inner = csharp_type(document, items, nullable=False)
        return append_nullable(f"IReadOnlyList<{inner}>", nullable, reference=True)
    if schema_type == "string":
        fmt = schema.get("format")
        if fmt in (None, ""):
            return append_nullable("string", nullable, reference=True)
        if fmt == "uuid":
            return append_nullable("Guid", nullable, reference=False)
        if fmt == "date-time":
            return append_nullable("DateTimeOffset", nullable, reference=False)
        raise GenerationError(f"Unsupported OpenAPI string format {fmt!r}.")
    if schema_type == "integer":
        fmt = schema.get("format")
        if fmt in (None, "int32"):
            return append_nullable("int", nullable, reference=False)
        if fmt == "int64":
            return append_nullable("long", nullable, reference=False)
        raise GenerationError(f"Unsupported OpenAPI integer format {fmt!r}.")
    if schema_type == "boolean":
        return append_nullable("bool", nullable, reference=False)
    if schema_type == "number":
        fmt = schema.get("format")
        if fmt in (None, "double"):
            return append_nullable("double", nullable, reference=False)
        if fmt == "float":
            return append_nullable("float", nullable, reference=False)
        raise GenerationError(f"Unsupported OpenAPI number format {fmt!r}.")
    if schema_type == "object" or "properties" in schema:
        raise GenerationError("Inline object schemas are not supported; expose a named component schema.")
    raise GenerationError(f"Unsupported OpenAPI schema: {schema!r}")


def append_nullable(type_name: str, nullable: bool, *, reference: bool) -> str:
    if not nullable:
        return type_name
    if type_name.endswith("?"):
        return type_name
    return type_name + "?"


def pascal_identifier(value: str) -> str:
    parts = [part for part in re.split(r"[^A-Za-z0-9]+", value) if part]
    if not parts:
        raise GenerationError(f"Cannot convert {value!r} into a C# identifier.")
    candidate = "".join(part[:1].upper() + part[1:] for part in parts)
    if candidate[0].isdigit():
        candidate = "_" + candidate
    if candidate in CSHARP_KEYWORDS:
        candidate = "_" + candidate
    validate_identifier(candidate, "generated identifier")
    return candidate


def camel_identifier(value: str) -> str:
    pascal = pascal_identifier(value)
    candidate = pascal[:1].lower() + pascal[1:]
    if candidate in CSHARP_KEYWORDS:
        candidate = "_" + candidate
    validate_identifier(candidate, "generated parameter")
    return candidate


def schema_type_for_parameter(document: dict[str, Any], parameter: Parameter) -> str:
    raw = parameter.schema
    if raw.get("type") == "array":
        items = raw.get("items")
        if not isinstance(items, dict):
            raise GenerationError(f"Array parameter {parameter.wire_name!r} is missing items.")
        inner = csharp_type(document, items, nullable=False)
        return f"IReadOnlyList<{inner}>" + ("" if parameter.required else "?")
    return csharp_type(document, raw, nullable=not parameter.required)


def request_schema_name(schema: dict[str, Any] | None, label: str) -> str | None:
    if schema is None:
        return None
    name = ref_name(schema)
    if name is None:
        raise GenerationError(f"{label} must use a named component schema.")
    return name


def response_type(document: dict[str, Any], schema: dict[str, Any] | None, label: str) -> str | None:
    if schema is None:
        return None
    name = ref_name(schema)
    if name is not None:
        return name
    return csharp_type(document, schema, nullable=False)


def render(document: dict[str, Any], options: Options) -> str:
    operations = collect_operations(document)
    models = collect_models(document, operations)
    lines: list[str] = [
        "// <auto-generated />",
        "#nullable enable",
        "",
        "using System.Globalization;",
        "using System.Net.Http.Json;",
        "using System.Text.Json.Serialization;",
        "using FoundationKit.Blazor.Api;",
        "",
        f"namespace {options.namespace};",
        "",
    ]
    for model in models:
        lines.extend(render_model(model))
        lines.append("")
    lines.extend(render_client(document, options.class_name, operations))
    return "\n".join(lines).rstrip() + "\n"


def render_model(model: Model) -> list[str]:
    lines = [f"public sealed record {model.name}("]
    for index, prop in enumerate(model.properties):
        suffix = "," if index < len(model.properties) - 1 else ");"
        lines.append(
            f"    [property: JsonPropertyName({json.dumps(prop.wire_name)})] {prop.type_name} {prop.name}{suffix}"
        )
    return lines


def render_client(
    document: dict[str, Any], class_name: str, operations: list[Operation]
) -> list[str]:
    lines = [
        f"public sealed class {class_name}(HttpClient httpClient) : ApiClientBase(httpClient)",
        "{",
    ]
    for index, operation in enumerate(operations):
        lines.extend(indent(render_operation(document, operation), 1))
        if index < len(operations) - 1:
            lines.append("")
    lines.extend(
        [
            "",
            "    private static string BuildQueryUri(string path, IReadOnlyList<KeyValuePair<string, string>> query)",
            "    {",
            "        if (query.Count == 0)",
            "            return path;",
            "",
            "        var encoded = query.Select(item =>",
            "            $\"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}\");",
            "        return path + \"?\" + string.Join(\"&\", encoded);",
            "    }",
            "",
            "    private static void AddHeader(HttpRequestMessage request, string name, string value)",
            "    {",
            "        ArgumentException.ThrowIfNullOrWhiteSpace(value);",
            "        if (!request.Headers.TryAddWithoutValidation(name, value))",
            "            throw new InvalidOperationException($\"Could not add required request header '{name}'.\");",
            "    }",
            "}",
        ]
    )
    return lines


def render_operation(document: dict[str, Any], operation: Operation) -> list[str]:
    method_name = f"{operation.foundation_operation[:1].upper()}{operation.foundation_operation[1:]}{operation.module}Async"
    request_name = request_schema_name(operation.request_schema, f"{operation.method} {operation.path} request")
    result_type = response_type(document, operation.response_schema, f"{operation.method} {operation.path} response")

    signature_parts: list[str] = []
    parameter_names: dict[tuple[str, str], str] = {}
    for parameter in operation.parameters:
        parameter_name = known_parameter_name(parameter)
        if parameter_name in {name for name in parameter_names.values()}:
            raise GenerationError(f"Duplicate generated parameter name {parameter_name!r} on {operation.method} {operation.path}.")
        parameter_names[(parameter.location, parameter.wire_name)] = parameter_name
        signature_parts.append(f"{schema_type_for_parameter(document, parameter)} {parameter_name}")
    if request_name is not None:
        signature_parts.append(f"{request_name} request")
    signature_parts.append("CancellationToken cancellationToken = default")

    response_wrapper = f"ApiResponse<{result_type}>" if result_type is not None else "ApiResponse"
    lines = [
        f"public async Task<{response_wrapper}> {method_name}(",
        *render_parameters(signature_parts),
        ")",
        "{",
    ]
    if request_name is not None:
        lines.append("    ArgumentNullException.ThrowIfNull(request);")

    path_expression = render_path_expression(operation.path, operation.parameters, parameter_names)
    query_parameters = [p for p in operation.parameters if p.location == "query"]
    if query_parameters:
        lines.append("    var query = new List<KeyValuePair<string, string>>();")
        for parameter in query_parameters:
            name = parameter_names[(parameter.location, parameter.wire_name)]
            lines.extend(render_query_parameter(parameter, name))
        lines.append(f"    var uri = BuildQueryUri({path_expression}, query);")
        uri_expression = "uri"
    else:
        uri_expression = path_expression

    lines.append(f"    using var message = new HttpRequestMessage(HttpMethod.{http_method_member(operation.method)}, {uri_expression});")
    if request_name is not None:
        lines.append("    message.Content = JsonContent.Create(request, options: JsonOptions);")
    for parameter in operation.parameters:
        if parameter.location != "header":
            continue
        name = parameter_names[(parameter.location, parameter.wire_name)]
        if not parameter.required:
            lines.append(f"    if (!string.IsNullOrWhiteSpace({name}))")
            lines.append(f"        AddHeader(message, {json.dumps(parameter.wire_name)}, {name}!);")
        else:
            lines.append(f"    AddHeader(message, {json.dumps(parameter.wire_name)}, {name});")
    if result_type is None:
        lines.append("    return await SendWithMetadataAsync(message, cancellationToken).ConfigureAwait(false);")
    else:
        lines.append(
            f"    return await SendWithMetadataAsync<{result_type}>(message, cancellationToken).ConfigureAwait(false);"
        )
    lines.append("}")
    return lines


def render_parameters(parameters: list[str]) -> list[str]:
    result: list[str] = []
    for index, parameter in enumerate(parameters):
        suffix = "," if index < len(parameters) - 1 else ""
        result.append(f"    {parameter}{suffix}")
    return result


def known_parameter_name(parameter: Parameter) -> str:
    if parameter.location == "header":
        lowered = parameter.wire_name.lower()
        if lowered == "idempotency-key":
            return "idempotencyKey"
        if lowered == "if-match":
            return "entityTag"
    return camel_identifier(parameter.wire_name)


def render_path_expression(
    path: str,
    parameters: tuple[Parameter, ...],
    names: dict[tuple[str, str], str],
) -> str:
    path_parameters = [parameter for parameter in parameters if parameter.location == "path"]
    if not path_parameters:
        return json.dumps(path.lstrip("/"))
    expression = path.lstrip("/")
    for parameter in path_parameters:
        token = "{" + parameter.wire_name + "}"
        if token not in expression:
            raise GenerationError(f"Path parameter {parameter.wire_name!r} is not present in {path!r}.")
        name = names[(parameter.location, parameter.wire_name)]
        replacement = path_value_expression(parameter.schema, name)
        expression = expression.replace(token, "{" + replacement + "}")
    if re.search(r"\{[^{}]+\}", expression):
        raise GenerationError(f"Unresolved path token remains in {path!r}.")
    return '$"' + expression.replace('"', '\\"') + '"'


def path_value_expression(schema: dict[str, Any], name: str) -> str:
    schema_type = schema.get("type")
    fmt = schema.get("format")
    if schema_type == "string" and fmt == "uuid":
        return f"Uri.EscapeDataString({name}.ToString(\"D\"))"
    if schema_type == "string" and fmt in (None, ""):
        return f"Uri.EscapeDataString({name})"
    if schema_type == "integer":
        return f"{name}.ToString(CultureInfo.InvariantCulture)"
    raise GenerationError(f"Unsupported path parameter schema: {schema!r}")


def render_query_parameter(parameter: Parameter, name: str) -> list[str]:
    schema = parameter.schema
    if schema.get("type") == "array":
        if parameter.required:
            return [
                f"    ArgumentNullException.ThrowIfNull({name});",
                f"    foreach (var value in {name})",
                f"        query.Add(new KeyValuePair<string, string>({json.dumps(parameter.wire_name)}, FormatQueryValue(value)));",
            ]
        return [
            f"    if ({name} is not null)",
            "    {",
            f"        foreach (var value in {name})",
            f"            query.Add(new KeyValuePair<string, string>({json.dumps(parameter.wire_name)}, FormatQueryValue(value)));",
            "    }",
        ]
    value_expression = query_value_expression(schema, name)
    if parameter.required:
        return [
            f"    query.Add(new KeyValuePair<string, string>({json.dumps(parameter.wire_name)}, {value_expression}));"
        ]
    if schema.get("type") == "string":
        return [
            f"    if ({name} is not null)",
            f"        query.Add(new KeyValuePair<string, string>({json.dumps(parameter.wire_name)}, {value_expression}));",
        ]
    return [
        f"    if ({name}.HasValue)",
        f"        query.Add(new KeyValuePair<string, string>({json.dumps(parameter.wire_name)}, {query_value_expression(schema, name + '.Value')}));",
    ]


def query_value_expression(schema: dict[str, Any], name: str) -> str:
    schema_type = schema.get("type")
    fmt = schema.get("format")
    if schema_type == "string":
        if fmt in (None, ""):
            return name
        if fmt == "uuid":
            return f"{name}.ToString(\"D\")"
        if fmt == "date-time":
            return f"{name}.ToString(\"O\", CultureInfo.InvariantCulture)"
    if schema_type in {"integer", "number"}:
        return f"{name}.ToString(CultureInfo.InvariantCulture)"
    if schema_type == "boolean":
        return f"({name} ? \"true\" : \"false\")"
    raise GenerationError(f"Unsupported query parameter schema: {schema!r}")


def http_method_member(method: str) -> str:
    return {"GET": "Get", "POST": "Post", "PUT": "Put", "DELETE": "Delete", "PATCH": "Patch"}[method]


def indent(lines: Iterable[str], depth: int) -> list[str]:
    prefix = "    " * depth
    return [prefix + line if line else "" for line in lines]


def main() -> int:
    try:
        options = parse_args()
        document = load_openapi(options.openapi)
        content = render(document, options)
        if options.check:
            if not options.output.exists():
                print(f"Generated typed client is missing: {options.output}", file=sys.stderr)
                return 1
            existing = options.output.read_text(encoding="utf-8")
            if existing != content:
                print(f"Generated typed client drift detected: {options.output}", file=sys.stderr)
                return 1
            print(f"Generated typed client matches: {options.output}")
            return 0
        options.output.parent.mkdir(parents=True, exist_ok=True)
        options.output.write_text(content, encoding="utf-8", newline="\n")
        print(f"Generated typed client: {options.output}")
        return 0
    except GenerationError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
