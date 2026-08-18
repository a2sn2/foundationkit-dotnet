using System.Security;
using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

public static class ComposerStudioBusinessUiOverlay
{
    public static async Task ApplyAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(generated);

        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var clientRoot = Path.Combine(generated.OutputDirectory, "src", $"{prefix}.Client");
        if (!Directory.Exists(clientRoot))
            return;

        foreach (var module in compilation.Blueprint.Modules)
        {
            foreach (var resource in module.Resources)
            {
                await WriteAsync(
                    Path.Combine(clientRoot, "Pages", "Generated", $"{module.Name}{resource.Name}.razor"),
                    BuildResourcePage(module, resource),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await PatchNavigationAsync(compilation, clientRoot, cancellationToken).ConfigureAwait(false);
        await PatchCssAsync(clientRoot, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildResourcePage(
        StudioModuleBlueprint module,
        StudioResourceBlueprint resource)
    {
        var filterField = resource.Fields.FirstOrDefault(field =>
            field.Type == StudioFieldType.Text && field.Filterable);
        var output = new StringBuilder();

        output.AppendLine($"@page \"/data/{resource.Route}\"");
        output.AppendLine("@using System.Globalization");
        output.AppendLine("@using System.Net.Http.Json");
        output.AppendLine("@using System.Text.Json");
        output.AppendLine("@inject HttpClient Http");
        output.AppendLine();
        output.AppendLine($"<PageTitle>{Html(resource.Name)} · FoundationKit</PageTitle>");
        output.AppendLine($"<FkPageHeader Eyebrow=\"{Html(module.Name)}\" Title=\"{Html(resource.Name)}\" Description=\"Generated full-stack CRUD screen. Extend the product under Custom instead of editing generated files.\" />");
        output.AppendLine();
        output.AppendLine("@if (!string.IsNullOrWhiteSpace(_message))");
        output.AppendLine("{");
        output.AppendLine("    <FkCard Variant=\"FoundationCardVariant.Muted\"><p class=\"studio-generated-message\">@_message</p></FkCard>");
        output.AppendLine("}");
        output.AppendLine();
        output.AppendLine("<div class=\"studio-generated-grid\">");
        output.AppendLine("    <FkCard><div class=\"fk-stack\">");
        output.AppendLine("        <div class=\"studio-generated-toolbar\"><div><span class=\"fk-caption\">LIVE DATA</span><h2>Records</h2></div><button class=\"fk-button fk-button--primary\" type=\"button\" @onclick=\"NewItem\">New</button></div>");

        if (filterField is not null)
        {
            output.AppendLine("        <div class=\"studio-generated-search\">");
            output.AppendLine($"            <input class=\"fk-input\" placeholder=\"Filter {Html(filterField.Name)}\" @bind=\"_search\" />");
            output.AppendLine("            <button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"LoadAsync\">Filter</button>");
            output.AppendLine("            <button class=\"fk-button fk-button--ghost\" type=\"button\" @onclick=\"ClearSearchAsync\">Clear</button>");
            output.AppendLine("        </div>");
        }

        output.AppendLine("        @if (_loading)");
        output.AppendLine("        {");
        output.AppendLine("            <p class=\"fk-muted\">Loading…</p>");
        output.AppendLine("        }");
        output.AppendLine("        else if (_items.Length == 0)");
        output.AppendLine("        {");
        output.AppendLine("            <FkEmptyState Title=\"No records yet\" Description=\"Create the first record from the generated form.\" />");
        output.AppendLine("        }");
        output.AppendLine("        else");
        output.AppendLine("        {");
        output.AppendLine("            <div class=\"studio-generated-table-wrap\"><table class=\"studio-generated-table\"><thead><tr><th>Id</th>");
        foreach (var field in resource.Fields)
            output.AppendLine($"                <th>{Html(field.Name)}</th>");
        output.AppendLine("                <th>Actions</th></tr></thead><tbody>");
        output.AppendLine("            @foreach (var item in _items)");
        output.AppendLine("            {");
        output.AppendLine("                <tr><td class=\"fk-mono\">@Read(item, \"id\")</td>");
        foreach (var field in resource.Fields)
            output.AppendLine($"                    <td>@Read(item, {JsonSerializer.Serialize(field.Name)})</td>");
        output.AppendLine("                    <td><div class=\"fk-row fk-wrap\"><button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"() => EditAsync(item)\">Edit</button><button class=\"fk-button fk-button--ghost\" type=\"button\" @onclick=\"() => DeleteAsync(item)\">Delete</button></div></td></tr>");
        output.AppendLine("            }");
        output.AppendLine("            </tbody></table></div>");
        output.AppendLine("        }");
        output.AppendLine("    </div></FkCard>");
        output.AppendLine();
        output.AppendLine("    <FkCard Variant=\"FoundationCardVariant.Muted\"><div class=\"fk-stack\">");
        output.AppendLine($"        <div><span class=\"fk-caption\">@(_editingId is null ? \"CREATE\" : \"EDIT\")</span><h2>@(_editingId is null ? \"New {Html(resource.Name)}\" : \"Edit {Html(resource.Name)}\")</h2></div>");
        foreach (var field in resource.Fields)
            output.AppendLine(BuildFormField(field));
        output.AppendLine("        <div class=\"fk-row fk-wrap\"><button class=\"fk-button fk-button--primary\" type=\"button\" disabled=\"@_saving\" @onclick=\"SaveAsync\">@(_saving ? \"Saving…\" : \"Save\")</button><button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"NewItem\">Reset</button></div>");
        output.AppendLine("    </div></FkCard>");
        output.AppendLine("</div>");
        output.AppendLine();
        output.AppendLine("@code {");
        output.AppendLine("    private JsonElement[] _items = [];");
        output.AppendLine("    private bool _loading;");
        output.AppendLine("    private bool _saving;");
        output.AppendLine("    private Guid? _editingId;");
        output.AppendLine("    private string? _etag;");
        output.AppendLine("    private string _message = string.Empty;");
        output.AppendLine("    private string _search = string.Empty;");
        foreach (var field in resource.Fields)
            output.AppendLine(BuildStateField(field));
        output.AppendLine();
        output.AppendLine("    protected override async Task OnInitializedAsync() => await LoadAsync();");
        output.AppendLine();
        output.AppendLine("    private async Task LoadAsync()");
        output.AppendLine("    {");
        output.AppendLine("        _loading = true; _message = string.Empty;");
        output.AppendLine("        try");
        output.AppendLine("        {");
        output.AppendLine($"            var uri = \"api/{resource.Route}/?page=1&pageSize=100\";");
        if (filterField is not null)
            output.AppendLine($"            if (!string.IsNullOrWhiteSpace(_search)) uri += $\"&filter={filterField.Name}:eq:{{Uri.EscapeDataString(_search)}}\";");
        output.AppendLine("            using var request = new HttpRequestMessage(HttpMethod.Get, uri);");
        output.AppendLine("            Prepare(request);");
        output.AppendLine("            using var response = await Http.SendAsync(request);");
        output.AppendLine("            if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); _items = []; return; }");
        output.AppendLine("            await using var stream = await response.Content.ReadAsStreamAsync();");
        output.AppendLine("            using var document = await JsonDocument.ParseAsync(stream);");
        output.AppendLine("            _items = document.RootElement.TryGetProperty(\"items\", out var items) ? items.EnumerateArray().Select(item => item.Clone()).ToArray() : [];");
        output.AppendLine("        }");
        output.AppendLine("        catch (Exception exception) { _message = exception.Message; _items = []; }");
        output.AppendLine("        finally { _loading = false; }");
        output.AppendLine("    }");
        output.AppendLine();
        output.AppendLine("    private async Task ClearSearchAsync() { _search = string.Empty; await LoadAsync(); }");
        output.AppendLine();
        output.AppendLine("    private void NewItem()");
        output.AppendLine("    {");
        output.AppendLine("        _editingId = null; _etag = null;");
        foreach (var field in resource.Fields)
            output.AppendLine(BuildClearField(field));
        output.AppendLine("    }");
        output.AppendLine();
        output.AppendLine("    private async Task EditAsync(JsonElement row)");
        output.AppendLine("    {");
        output.AppendLine("        if (!TryGuid(row, \"id\", out var id)) return;");
        output.AppendLine($"        using var request = new HttpRequestMessage(HttpMethod.Get, $\"api/{resource.Route}/{{id:D}}\");");
        output.AppendLine("        Prepare(request);");
        output.AppendLine("        using var response = await Http.SendAsync(request);");
        output.AppendLine("        if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); return; }");
        output.AppendLine("        _etag = response.Headers.ETag?.ToString();");
        output.AppendLine("        var item = await response.Content.ReadFromJsonAsync<JsonElement>();");
        output.AppendLine("        _editingId = id;");
        foreach (var field in resource.Fields)
            output.AppendLine(BuildHydrateField(field));
        output.AppendLine("    }");
        output.AppendLine();
        output.AppendLine("    private async Task SaveAsync()");
        output.AppendLine("    {");
        output.AppendLine("        _saving = true; _message = string.Empty;");
        output.AppendLine("        try");
        output.AppendLine("        {");
        output.AppendLine("            var payload = new Dictionary<string, object?>");
        output.AppendLine("            {");
        for (var index = 0; index < resource.Fields.Count; index++)
        {
            var suffix = index == resource.Fields.Count - 1 ? string.Empty : ",";
            output.AppendLine(BuildPayloadEntry(resource.Fields[index]) + suffix);
        }
        output.AppendLine("            };");
        output.AppendLine("            var method = _editingId is null ? HttpMethod.Post : HttpMethod.Put;");
        output.AppendLine($"            var path = _editingId is null ? \"api/{resource.Route}/\" : $\"api/{resource.Route}/{{_editingId.Value:D}}\";");
        output.AppendLine("            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };");
        output.AppendLine("            Prepare(request);");
        if (resource.Idempotency)
            output.AppendLine("            request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));");
        if (resource.Concurrency)
            output.AppendLine("            if (_editingId is not null && !string.IsNullOrWhiteSpace(_etag)) request.Headers.TryAddWithoutValidation(\"If-Match\", _etag);");
        output.AppendLine("            using var response = await Http.SendAsync(request);");
        output.AppendLine("            if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); return; }");
        output.AppendLine("            NewItem();");
        output.AppendLine("            await LoadAsync();");
        output.AppendLine("        }");
        output.AppendLine("        catch (Exception exception) { _message = exception.Message; }");
        output.AppendLine("        finally { _saving = false; }");
        output.AppendLine("    }");
        output.AppendLine();
        output.AppendLine("    private async Task DeleteAsync(JsonElement row)");
        output.AppendLine("    {");
        output.AppendLine("        if (!TryGuid(row, \"id\", out var id)) return;");
        output.AppendLine($"        using var request = new HttpRequestMessage(HttpMethod.Delete, $\"api/{resource.Route}/{{id:D}}\");");
        output.AppendLine("        Prepare(request);");
        if (resource.Idempotency)
            output.AppendLine("        request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));");
        output.AppendLine("        using var response = await Http.SendAsync(request);");
        output.AppendLine("        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent) { _message = await ErrorAsync(response); return; }");
        output.AppendLine("        if (_editingId == id) NewItem();");
        output.AppendLine("        await LoadAsync();");
        output.AppendLine("    }");
        output.AppendLine();
        AppendReadHelpers(output);
        AppendParseHelpers(output);
        output.AppendLine();
        output.AppendLine("    private void Prepare(HttpRequestMessage request)");
        output.AppendLine("    {");
        if (resource.Authorization)
        {
            output.AppendLine("        request.Headers.TryAddWithoutValidation(\"X-Foundation-User\", \"11111111-1111-1111-1111-111111111111\");");
            output.AppendLine("        request.Headers.TryAddWithoutValidation(\"X-Foundation-Roles\", \"admin\");");
        }
        output.AppendLine("    }");
        output.AppendLine();
        output.AppendLine("    private static async Task<string> ErrorAsync(HttpResponseMessage response)");
        output.AppendLine("    {");
        output.AppendLine("        var body = await response.Content.ReadAsStringAsync();");
        output.AppendLine("        return $\"{(int)response.StatusCode} {response.ReasonPhrase}: {body}\";");
        output.AppendLine("    }");
        output.AppendLine("}");
        return output.ToString();
    }

    private static string BuildFormField(StudioFieldBlueprint field)
    {
        var state = StateName(field);
        var label = Html(field.Name) + (field.Required ? " *" : string.Empty);
        return field.Type switch
        {
            StudioFieldType.Boolean => $"        <label class=\"studio-generated-check\"><input type=\"checkbox\" @bind=\"{state}\" /><span>{label}</span></label>",
            StudioFieldType.Integer => $"        <label class=\"studio-generated-field\"><span>{label}</span><input class=\"fk-input\" type=\"number\" step=\"1\" @bind=\"{state}\" /></label>",
            StudioFieldType.Decimal => $"        <label class=\"studio-generated-field\"><span>{label}</span><input class=\"fk-input\" type=\"number\" step=\"0.01\" @bind=\"{state}\" /></label>",
            StudioFieldType.Date => $"        <label class=\"studio-generated-field\"><span>{label}</span><input class=\"fk-input\" type=\"date\" @bind=\"{state}\" /></label>",
            StudioFieldType.DateTime => $"        <label class=\"studio-generated-field\"><span>{label}</span><input class=\"fk-input\" type=\"datetime-local\" @bind=\"{state}\" /></label>",
            StudioFieldType.Guid or StudioFieldType.Reference => $"        <label class=\"studio-generated-field\"><span>{label}{(field.Type == StudioFieldType.Reference ? $" → {Html(field.ReferenceResource ?? string.Empty)}" : string.Empty)}</span><input class=\"fk-input fk-mono\" @bind=\"{state}\" placeholder=\"GUID\" /></label>",
            _ => $"        <label class=\"studio-generated-field\"><span>{label}</span><input class=\"fk-input\" maxlength=\"{field.MaximumLength}\" @bind=\"{state}\" /></label>"
        };
    }

    private static string BuildStateField(StudioFieldBlueprint field) =>
        field.Type == StudioFieldType.Boolean
            ? $"    private bool {StateName(field)};"
            : $"    private string {StateName(field)} = string.Empty;";

    private static string BuildClearField(StudioFieldBlueprint field) =>
        field.Type == StudioFieldType.Boolean
            ? $"        {StateName(field)} = false;"
            : $"        {StateName(field)} = string.Empty;";

    private static string BuildHydrateField(StudioFieldBlueprint field) => field.Type switch
    {
        StudioFieldType.Boolean => $"        {StateName(field)} = ReadBool(item, {JsonSerializer.Serialize(field.Name)});",
        StudioFieldType.Date => $"        {StateName(field)} = ReadDate(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-dd\", CultureInfo.InvariantCulture) ?? string.Empty;",
        StudioFieldType.DateTime => $"        {StateName(field)} = ReadDateTime(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-ddTHH:mm\", CultureInfo.InvariantCulture) ?? string.Empty;",
        _ => $"        {StateName(field)} = Read(item, {JsonSerializer.Serialize(field.Name)});"
    };

    private static string BuildPayloadEntry(StudioFieldBlueprint field)
    {
        var state = StateName(field);
        var name = JsonSerializer.Serialize(field.Name);
        var value = field.Type switch
        {
            StudioFieldType.Boolean => state,
            StudioFieldType.Integer => $"ParseInt({state}, {Bool(field.Required)}, {name})",
            StudioFieldType.Decimal => $"ParseDecimal({state}, {Bool(field.Required)}, {name})",
            StudioFieldType.Date => $"ParseDate({state}, {Bool(field.Required)}, {name})",
            StudioFieldType.DateTime => $"ParseDateTime({state}, {Bool(field.Required)}, {name})",
            StudioFieldType.Guid or StudioFieldType.Reference => $"ParseGuid({state}, {Bool(field.Required)}, {name})",
            _ => field.Required
                ? $"RequireText({state}, {name})"
                : $"string.IsNullOrWhiteSpace({state}) ? null : {state}.Trim()"
        };
        return $"                [{name}] = {value}";
    }

    private static void AppendReadHelpers(StringBuilder output)
    {
        output.AppendLine("    private static string Read(JsonElement item, string name)");
        output.AppendLine("    {");
        output.AppendLine("        if (!item.TryGetProperty(name, out var value) && !item.TryGetProperty(char.ToUpperInvariant(name[0]) + name[1..], out value)) return string.Empty;");
        output.AppendLine("        return value.ValueKind switch { JsonValueKind.Null => string.Empty, JsonValueKind.String => value.GetString() ?? string.Empty, JsonValueKind.True => \"Yes\", JsonValueKind.False => \"No\", _ => value.ToString() }; ");
        output.AppendLine("    }");
        output.AppendLine("    private static bool TryGuid(JsonElement item, string name, out Guid id) => Guid.TryParse(Read(item, name), out id);");
        output.AppendLine("    private static bool ReadBool(JsonElement item, string name) => bool.TryParse(Read(item, name), out var value) && value;");
        output.AppendLine("    private static DateOnly? ReadDate(JsonElement item, string name) => DateOnly.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;");
        output.AppendLine("    private static DateTimeOffset? ReadDateTime(JsonElement item, string name) => DateTimeOffset.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;");
    }

    private static void AppendParseHelpers(StringBuilder output)
    {
        output.AppendLine("    private static string RequireText(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($\"{name} is required.\"); return value.Trim(); }");
        output.AppendLine("    private static int? ParseInt(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) throw new InvalidOperationException($\"{name} must be an integer.\"); return parsed; }");
        output.AppendLine("    private static decimal? ParseDecimal(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) throw new InvalidOperationException($\"{name} must be a decimal.\"); return parsed; }");
        output.AppendLine("    private static DateOnly? ParseDate(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) throw new InvalidOperationException($\"{name} must be a valid date.\"); return parsed; }");
        output.AppendLine("    private static DateTimeOffset? ParseDateTime(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)) throw new InvalidOperationException($\"{name} must be a valid date/time.\"); return parsed; }");
        output.AppendLine("    private static Guid? ParseGuid(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty) throw new InvalidOperationException($\"{name} must be a non-empty GUID.\"); return parsed; }");
    }

    private static async Task PatchNavigationAsync(
        StudioBlueprintCompilation compilation,
        string clientRoot,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(clientRoot, "Layout", "MainLayout.razor");
        if (!File.Exists(path))
            return;

        var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        const string closeTag = "</Navigation>";
        var closeIndex = source.IndexOf(closeTag, StringComparison.Ordinal);
        if (closeIndex < 0)
            throw new ComposerGenerationException("Studio could not locate generated Blazor navigation boundary.");

        var lineStart = source.LastIndexOf('\n', Math.Max(0, closeIndex - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var indentation = source[lineStart..closeIndex];
        var items = new StringBuilder();
        foreach (var resource in compilation.Blueprint.Modules.SelectMany(module => module.Resources))
        {
            items.Append(indentation).Append("<FkNavItem Href=\"data/").Append(resource.Route).AppendLine("\">");
            items.Append(indentation).AppendLine("    <Icon><span aria-hidden=\"true\">▦</span></Icon>");
            items.Append(indentation).Append("    <ChildContent>").Append(Html(resource.Name)).AppendLine("</ChildContent>");
            items.Append(indentation).AppendLine("</FkNavItem>");
        }

        source = source.Insert(lineStart, items.ToString());
        await File.WriteAllTextAsync(path, Normalize(source), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task PatchCssAsync(
        string clientRoot,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(clientRoot, "wwwroot", "css", "app.css");
        if (!File.Exists(path))
            return;

        var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        if (source.Contains(".studio-generated-grid", StringComparison.Ordinal))
            return;

        source += """

            .studio-generated-grid { display:grid; grid-template-columns:minmax(0,2fr) minmax(18rem,1fr); gap:1.25rem; align-items:start; }
            .studio-generated-toolbar,.studio-generated-search { display:flex; gap:.75rem; align-items:center; justify-content:space-between; flex-wrap:wrap; }
            .studio-generated-search .fk-input { min-width:min(28rem,100%); }
            .studio-generated-table-wrap { overflow-x:auto; }
            .studio-generated-table { width:100%; border-collapse:collapse; }
            .studio-generated-table th,.studio-generated-table td { padding:.75rem; border-bottom:1px solid rgba(127,127,127,.2); text-align:start; white-space:nowrap; }
            .studio-generated-field { display:grid; gap:.4rem; }
            .studio-generated-check { display:flex; gap:.65rem; align-items:center; }
            .studio-generated-message { overflow-wrap:anywhere; }
            @media (max-width:980px) { .studio-generated-grid { grid-template-columns:1fr; } }
            """;

        await File.WriteAllTextAsync(path, Normalize(source), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string StateName(StudioFieldBlueprint field) =>
        "_" + char.ToLowerInvariant(field.Name[0]) + field.Name[1..];

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Html(string value) =>
        SecurityElement.Escape(value) ?? value;

    private static async Task WriteAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        await File.WriteAllTextAsync(
            path,
            Normalize(content),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";
}
