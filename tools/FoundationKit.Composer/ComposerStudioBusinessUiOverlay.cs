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

    private static string BuildResourcePage(StudioModuleBlueprint module, StudioResourceBlueprint resource)
    {
        var searchField = resource.Fields.FirstOrDefault(field =>
            field.Type == StudioFieldType.Text && field.Filterable);
        var builder = new StringBuilder();
        builder.AppendLine($"@page \"/data/{resource.Route}\"");
        builder.AppendLine("@using System.Globalization");
        builder.AppendLine("@using System.Net.Http.Json");
        builder.AppendLine("@using System.Text.Json");
        builder.AppendLine("@inject HttpClient Http");
        builder.AppendLine();
        builder.AppendLine($"<PageTitle>{Html(resource.Name)} · FoundationKit</PageTitle>");
        builder.AppendLine();
        builder.AppendLine($"<FkPageHeader Eyebrow=\"{Html(module.Name)}\" Title=\"{Html(resource.Name)}\" Description=\"Generated CRUD screen backed by the runtime API. Product-specific UI can be added under Custom without editing generated files.\" />");
        builder.AppendLine();
        builder.AppendLine("@if (!string.IsNullOrWhiteSpace(_message))");
        builder.AppendLine("{");
        builder.AppendLine("    <FkCard Variant=\"FoundationCardVariant.Muted\"><p class=\"studio-generated-message\">@_message</p></FkCard>");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("<div class=\"studio-generated-grid\">");
        builder.AppendLine("    <FkCard>");
        builder.AppendLine("        <div class=\"fk-stack\">");
        builder.AppendLine("            <div class=\"studio-generated-toolbar\">");
        builder.AppendLine("                <div><span class=\"fk-caption\">LIVE DATA</span><h2>Records</h2></div>");
        builder.AppendLine("                <button class=\"fk-button fk-button--primary\" type=\"button\" @onclick=\"NewItem\">New</button>");
        builder.AppendLine("            </div>");
        if (searchField is not null)
        {
            builder.AppendLine("            <div class=\"studio-generated-search\">");
            builder.AppendLine($"                <input class=\"fk-input\" placeholder=\"Filter {Html(searchField.Name)}\" @bind=\"_search\" />");
            builder.AppendLine("                <button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"LoadAsync\">Filter</button>");
            builder.AppendLine("                <button class=\"fk-button fk-button--ghost\" type=\"button\" @onclick=\"ClearSearchAsync\">Clear</button>");
            builder.AppendLine("            </div>");
        }
        builder.AppendLine("            @if (_loading)");
        builder.AppendLine("            {");
        builder.AppendLine("                <p class=\"fk-muted\">Loading…</p>");
        builder.AppendLine("            }");
        builder.AppendLine("            else if (_items.Length == 0)");
        builder.AppendLine("            {");
        builder.AppendLine("                <FkEmptyState Title=\"No records yet\" Description=\"Create the first record from the generated form.\" />");
        builder.AppendLine("            }");
        builder.AppendLine("            else");
        builder.AppendLine("            {");
        builder.AppendLine("                <div class=\"studio-generated-table-wrap\"><table class=\"studio-generated-table\"><thead><tr><th>Id</th>");
        foreach (var field in resource.Fields)
            builder.AppendLine($"                    <th>{Html(field.Name)}</th>");
        builder.AppendLine("                    <th>Actions</th></tr></thead><tbody>");
        builder.AppendLine("                @foreach (var item in _items)");
        builder.AppendLine("                {");
        builder.AppendLine("                    <tr><td class=\"fk-mono\">@Read(item, \"id\")</td>");
        foreach (var field in resource.Fields)
            builder.AppendLine($"                        <td>@Read(item, {JsonSerializer.Serialize(field.Name)})</td>");
        builder.AppendLine("                        <td><div class=\"fk-row fk-wrap\">");
        builder.AppendLine("                            <button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"() => EditAsync(item)\">Edit</button>");
        builder.AppendLine("                            <button class=\"fk-button fk-button--ghost\" type=\"button\" @onclick=\"() => DeleteAsync(item)\">Delete</button>");
        builder.AppendLine("                        </div></td></tr>");
        builder.AppendLine("                }");
        builder.AppendLine("                </tbody></table></div>");
        builder.AppendLine("            }");
        builder.AppendLine("        </div>");
        builder.AppendLine("    </FkCard>");
        builder.AppendLine();
        builder.AppendLine("    <FkCard Variant=\"FoundationCardVariant.Muted\"><div class=\"fk-stack\">");
        builder.AppendLine($"        <div><span class=\"fk-caption\">@(_editingId is null ? \"CREATE\" : \"EDIT\")</span><h2>@(_editingId is null ? \"New {Html(resource.Name)}\" : \"Edit {Html(resource.Name)}\")</h2></div>");
        foreach (var field in resource.Fields)
            builder.AppendLine(BuildFormField(field));
        builder.AppendLine("        <div class=\"fk-row fk-wrap\">");
        builder.AppendLine("            <button class=\"fk-button fk-button--primary\" type=\"button\" disabled=\"@_saving\" @onclick=\"SaveAsync\">@(_saving ? \"Saving…\" : \"Save\")</button>");
        builder.AppendLine("            <button class=\"fk-button fk-button--secondary\" type=\"button\" @onclick=\"NewItem\">Reset</button>");
        builder.AppendLine("        </div>");
        builder.AppendLine("    </div></FkCard>");
        builder.AppendLine("</div>");
        builder.AppendLine();
        builder.AppendLine("@code {");
        builder.AppendLine("    private JsonElement[] _items = [];");
        builder.AppendLine("    private bool _loading;");
        builder.AppendLine("    private bool _saving;");
        builder.AppendLine("    private Guid? _editingId;");
        builder.AppendLine("    private string? _etag;");
        builder.AppendLine("    private string _message = string.Empty;");
        builder.AppendLine("    private string _search = string.Empty;");
        foreach (var field in resource.Fields)
            builder.AppendLine(BuildStateField(field));
        builder.AppendLine();
        builder.AppendLine("    protected override async Task OnInitializedAsync() => await LoadAsync();");
        builder.AppendLine();
        builder.AppendLine("    private async Task LoadAsync()");
        builder.AppendLine("    {");
        builder.AppendLine("        _loading = true;");
        builder.AppendLine("        try");
        builder.AppendLine("        {");
        builder.AppendLine($"            var uri = \"api/{resource.Route}/?page=1&pageSize=100\";");
        if (searchField is not null)
            builder.AppendLine($"            if (!string.IsNullOrWhiteSpace(_search)) uri += $\"&filter={searchField.Name}:eq:{{Uri.EscapeDataString(_search)}}\";");
        builder.AppendLine("            using var request = new HttpRequestMessage(HttpMethod.Get, uri);");
        builder.AppendLine("            Prepare(request);");
        builder.AppendLine("            using var response = await Http.SendAsync(request);");
        builder.AppendLine("            if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); _items = []; return; }");
        builder.AppendLine("            await using var stream = await response.Content.ReadAsStreamAsync();");
        builder.AppendLine("            using var document = await JsonDocument.ParseAsync(stream);");
        builder.AppendLine("            _items = document.RootElement.TryGetProperty(\"items\", out var items) ? items.EnumerateArray().Select(item => item.Clone()).ToArray() : [];");
        builder.AppendLine("        }");
        builder.AppendLine("        catch (Exception exception) { _message = exception.Message; _items = []; }");
        builder.AppendLine("        finally { _loading = false; }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private async Task ClearSearchAsync() { _search = string.Empty; await LoadAsync(); }");
        builder.AppendLine();
        builder.AppendLine("    private void NewItem()");
        builder.AppendLine("    {");
        builder.AppendLine("        _editingId = null; _etag = null;");
        foreach (var field in resource.Fields)
            builder.AppendLine(BuildClearField(field));
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private async Task EditAsync(JsonElement row)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!TryGuid(row, \"id\", out var id)) return;");
        builder.AppendLine($"        using var request = new HttpRequestMessage(HttpMethod.Get, $\"api/{resource.Route}/{{id:D}}\");");
        builder.AppendLine("        Prepare(request);");
        builder.AppendLine("        using var response = await Http.SendAsync(request);");
        builder.AppendLine("        if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); return; }");
        builder.AppendLine("        _etag = response.Headers.ETag?.ToString();");
        builder.AppendLine("        var item = await response.Content.ReadFromJsonAsync<JsonElement>();");
        builder.AppendLine("        _editingId = id;");
        foreach (var field in resource.Fields)
            builder.AppendLine(BuildHydrateField(field));
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private async Task SaveAsync()");
        builder.AppendLine("    {");
        builder.AppendLine("        _saving = true; _message = string.Empty;");
        builder.AppendLine("        try");
        builder.AppendLine("        {");
        builder.AppendLine("            var payload = new Dictionary<string, object?>");
        builder.AppendLine("            {");
        for (var index = 0; index < resource.Fields.Count; index++)
        {
            var suffix = index == resource.Fields.Count - 1 ? string.Empty : ",";
            builder.AppendLine(BuildPayloadEntry(resource.Fields[index]) + suffix);
        }
        builder.AppendLine("            };");
        builder.AppendLine("            var method = _editingId is null ? HttpMethod.Post : HttpMethod.Put;");
        builder.AppendLine($"            var path = _editingId is null ? \"api/{resource.Route}/\" : $\"api/{resource.Route}/{{_editingId.Value:D}}\";");
        builder.AppendLine("            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };");
        builder.AppendLine("            Prepare(request);");
        if (resource.Idempotency)
            builder.AppendLine("            request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));");
        if (resource.Concurrency)
            builder.AppendLine("            if (_editingId is not null && !string.IsNullOrWhiteSpace(_etag)) request.Headers.TryAddWithoutValidation(\"If-Match\", _etag);");
        builder.AppendLine("            using var response = await Http.SendAsync(request);");
        builder.AppendLine("            if (!response.IsSuccessStatusCode) { _message = await ErrorAsync(response); return; }");
        builder.AppendLine("            NewItem();");
        builder.AppendLine("            await LoadAsync();");
        builder.AppendLine("        }");
        builder.AppendLine("        catch (Exception exception) { _message = exception.Message; }");
        builder.AppendLine("        finally { _saving = false; }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private async Task DeleteAsync(JsonElement row)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!TryGuid(row, \"id\", out var id)) return;");
        builder.AppendLine($"        using var request = new HttpRequestMessage(HttpMethod.Delete, $\"api/{resource.Route}/{{id:D}}\");");
        builder.AppendLine("        Prepare(request);");
        if (resource.Idempotency)
            builder.AppendLine("        request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));");
        builder.AppendLine("        using var response = await Http.SendAsync(request);");
        builder.AppendLine("        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent) { _message = await ErrorAsync(response); return; }");
        builder.AppendLine("        if (_editingId == id) NewItem();");
        builder.AppendLine("        await LoadAsync();");
        builder.AppendLine("    }");
        builder.AppendLine();
        AppendReadHelpers(builder);
        AppendParseHelpers(builder);
        builder.AppendLine();
        builder.AppendLine("    private void Prepare(HttpRequestMessage request)");
        builder.AppendLine("    {");
        if (resource.Authorization)
        {
            builder.AppendLine("        request.Headers.TryAddWithoutValidation(\"X-Foundation-User\", \"11111111-1111-1111-1111-111111111111\");");
            builder.AppendLine("        request.Headers.TryAddWithoutValidation(\"X-Foundation-Roles\", \"admin\");");
        }
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static async Task<string> ErrorAsync(HttpResponseMessage response)");
        builder.AppendLine("    {");
        builder.AppendLine("        var body = await response.Content.ReadAsStringAsync();");
        builder.AppendLine("        return $\"{(int)response.StatusCode} {response.ReasonPhrase}: {body}\";");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
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

    private static string BuildStateField(StudioFieldBlueprint field) => field.Type == StudioFieldType.Boolean
        ? $"    private bool {StateName(field)};"
        : $"    private string {StateName(field)} = string.Empty;";

    private static string BuildClearField(StudioFieldBlueprint field) => field.Type == StudioFieldType.Boolean
        ? $"        {StateName(field)} = false;"
        : $"        {StateName(field)} = string.Empty;";

    private static string BuildHydrateField(StudioFieldBlueprint field) => field.Type switch
    {
        StudioFieldType.Boolean => $"        {StateName(field)} = ReadBool(item, {JsonSerializer.Serialize(field.Name)});",
        StudioFieldType.Date => $"        {StateName(field)} = ReadDate(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-dd\", CultureInfo.InvariantCulture) ?? string.Empty;",
        StudioFieldType.DateTime => $"        {StateName(field)} = ReadDateTime(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-ddTHH:mm\", CultureInfo.InvariantCulture) ?? string.Empty;",
        _ => $"        {StateName(field)} = ReadString(item, {JsonSerializer.Serialize(field.Name)});"
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
            _ => field.Required ? $"RequireText({state}, {name})" : $"string.IsNullOrWhiteSpace({state}) ? null : {state}.Trim()"
        };
        return $"                [{name}] = {value}";
    }

    private static void AppendReadHelpers(StringBuilder builder)
    {
        builder.AppendLine("    private static string Read(JsonElement item, string name)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!item.TryGetProperty(name, out var value) && !item.TryGetProperty(char.ToUpperInvariant(name[0]) + name[1..], out value)) return string.Empty;");
        builder.AppendLine("        return value.ValueKind switch { JsonValueKind.Null => string.Empty, JsonValueKind.String => value.GetString() ?? string.Empty, JsonValueKind.True => \"Yes\", JsonValueKind.False => \"No\", _ => value.ToString() }; ");
        builder.AppendLine("    }");
        builder.AppendLine("    private static bool TryGuid(JsonElement item, string name, out Guid id) => Guid.TryParse(Read(item, name), out id);");
        builder.AppendLine("    private static string ReadString(JsonElement item, string name) => Read(item, name);");
        builder.AppendLine("    private static bool ReadBool(JsonElement item, string name) => bool.TryParse(Read(item, name), out var value) && value;");
        builder.AppendLine("    private static DateOnly? ReadDate(JsonElement item, string name) => DateOnly.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;");
        builder.AppendLine("    private static DateTimeOffset? ReadDateTime(JsonElement item, string name) => DateTimeOffset.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;");
    }

    private static void AppendParseHelpers(StringBuilder builder)
    {
        builder.AppendLine("    private static string RequireText(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($\"{name} is required.\"); return value.Trim(); }");
        builder.AppendLine("    private static int? ParseInt(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) throw new InvalidOperationException($\"{name} must be an integer.\"); return parsed; }");
        builder.AppendLine("    private static decimal? ParseDecimal(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) throw new InvalidOperationException($\"{name} must be a decimal.\"); return parsed; }");
        builder.AppendLine("    private static DateOnly? ParseDate(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) throw new InvalidOperationException($\"{name} must be a valid date.\"); return parsed; }");
        builder.AppendLine("    private static DateTimeOffset? ParseDateTime(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)) throw new InvalidOperationException($\"{name} must be a valid date/time.\"); return parsed; }");
        builder.AppendLine("    private static Guid? ParseGuid(string value, bool required, string name) { if (string.IsNullOrWhiteSpace(value)) { if (required) throw new InvalidOperationException($\"{name} is required.\"); return null; } if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty) throw new InvalidOperationException($\"{name} must be a non-empty GUID.\"); return parsed; }");
    }

    private static async Task PatchNavigationAsync(StudioBlueprintCompilation compilation, string clientRoot, CancellationToken cancellationToken)
    {
        var path = Path.Combine(clientRoot, "Layout", "MainLayout.razor");
        if (!File.Exists(path))
            return;
        var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        const string marker = "                </Navigation>";
        if (!source.Contains(marker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Studio could not locate generated Blazor navigation boundary.");
        var items = new StringBuilder();
        foreach (var resource in compilation.Blueprint.Modules.SelectMany(module => module.Resources))
        {
            items.AppendLine($"                    <FkNavItem Href=\"data/{resource.Route}\">");
            items.AppendLine("                        <Icon><span aria-hidden=\"true\">▦</span></Icon>");
            items.AppendLine($"                        <ChildContent>{Html(resource.Name)}</ChildContent>");
            items.AppendLine("                    </FkNavItem>");
        }
        source = source.Replace(marker, items + marker, StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, Normalize(source), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task PatchCssAsync(string clientRoot, CancellationToken cancellationToken)
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

    private static string StateName(StudioFieldBlueprint field) => "_" + char.ToLowerInvariant(field.Name[0]) + field.Name[1..];
    private static string Bool(bool value) => value ? "true" : "false";
    private static string Html(string value) => SecurityElement.Escape(value) ?? value;

    private static async Task WriteAsync(string path, string content, CancellationToken cancellationToken)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        await File.WriteAllTextAsync(path, Normalize(content), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";
}
