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
                var pagePath = Path.Combine(clientRoot, "Pages", "Generated", $"{module.Name}{resource.Name}.razor");
                await WriteAsync(
                    pagePath,
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
        var auth = resource.Authorization;
        var formFields = string.Join("\n", resource.Fields.Select(BuildFormField));
        var headers = string.Join("\n", resource.Fields.Select(field => $"                <th>{Html(field.Name)}</th>"));
        var cells = string.Join("\n", resource.Fields.Select(field =>
            $"                <td>@Read(item, {JsonSerializer.Serialize(field.Name)})</td>"));
        var stateFields = string.Join("\n", resource.Fields.Select(BuildStateField));
        var clearFields = string.Join("\n", resource.Fields.Select(BuildClearField));
        var hydrateFields = string.Join("\n", resource.Fields.Select(BuildHydrateField));
        var payloadEntries = string.Join(",\n", resource.Fields.Select(BuildPayloadEntry));
        var searchField = resource.Fields.FirstOrDefault(field => field.Filterable && field.Type == StudioFieldType.Text);
        var searchUi = searchField is null
            ? string.Empty
            : $$"""
                <div class="studio-generated-search">
                    <input class="fk-input" placeholder="Filter {{Html(searchField.Name)}}" @bind="_search" />
                    <button class="fk-button fk-button--secondary" type="button" @onclick="LoadAsync">Filter</button>
                    <button class="fk-button fk-button--ghost" type="button" @onclick="ClearSearchAsync">Clear</button>
                </div>
                """;
        var filterQuery = searchField is null
            ? "string.Empty"
            : $"string.IsNullOrWhiteSpace(_search) ? string.Empty : $\"&filter={Uri.EscapeDataString({JsonSerializer.Serialize(searchField.Name)})}:eq:{{Uri.EscapeDataString(_search)}}\"";

        return $$"""
            @page "/data/{{resource.Route}}"
            @using System.Globalization
            @using System.Net.Http.Json
            @using System.Text.Json
            @inject HttpClient Http

            <PageTitle>{{Html(resource.Name)}} · FoundationKit</PageTitle>

            <FkPageHeader Eyebrow="{{Html(module.Name)}}"
                          Title="{{Html(resource.Name)}}"
                          Description="Generated CRUD screen backed by the runtime API. Custom product UI may replace or extend this screen under a Custom directory." />

            @if (!string.IsNullOrWhiteSpace(_message))
            {
                <FkCard Variant="FoundationCardVariant.Muted">
                    <p class="studio-generated-message">@_message</p>
                </FkCard>
            }

            <div class="studio-generated-grid">
                <FkCard>
                    <div class="fk-stack">
                        <div class="studio-generated-toolbar">
                            <div>
                                <span class="fk-caption">LIVE DATA</span>
                                <h2>{{Html(resource.Name)}} records</h2>
                            </div>
                            <button class="fk-button fk-button--primary" type="button" @onclick="NewItem">New</button>
                        </div>
                        {{searchUi}}
                        @if (_loading)
                        {
                            <p class="fk-muted">Loading…</p>
                        }
                        else if (_items.Length == 0)
                        {
                            <FkEmptyState Title="No records yet" Description="Create the first record from the generated form." />
                        }
                        else
                        {
                            <div class="studio-generated-table-wrap">
                                <table class="studio-generated-table">
                                    <thead>
                                        <tr>
                                            <th>Id</th>
            {{headers}}
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                    @foreach (var item in _items)
                                    {
                                        <tr>
                                            <td class="fk-mono">@Read(item, "id")</td>
            {{cells}}
                                            <td>
                                                <div class="fk-row fk-wrap">
                                                    <button class="fk-button fk-button--secondary" type="button" @onclick="() => EditAsync(item)">Edit</button>
                                                    <button class="fk-button fk-button--ghost" type="button" @onclick="() => DeleteAsync(item)">Delete</button>
                                                </div>
                                            </td>
                                        </tr>
                                    }
                                    </tbody>
                                </table>
                            </div>
                        }
                    </div>
                </FkCard>

                <FkCard Variant="FoundationCardVariant.Muted">
                    <div class="fk-stack">
                        <div>
                            <span class="fk-caption">@(_editingId is null ? "CREATE" : "EDIT")</span>
                            <h2>@(_editingId is null ? "New {{Html(resource.Name)}}" : "Edit {{Html(resource.Name)}}")</h2>
                        </div>
            {{formFields}}
                        <div class="fk-row fk-wrap">
                            <button class="fk-button fk-button--primary" type="button" disabled="@_saving" @onclick="SaveAsync">@(_saving ? "Saving…" : "Save")</button>
                            <button class="fk-button fk-button--secondary" type="button" @onclick="NewItem">Reset</button>
                        </div>
                    </div>
                </FkCard>
            </div>

            @code {
                private JsonElement[] _items = [];
                private bool _loading;
                private bool _saving;
                private Guid? _editingId;
                private string? _etag;
                private string _message = string.Empty;
                private string _search = string.Empty;
            {{stateFields}}

                protected override async Task OnInitializedAsync() => await LoadAsync();

                private async Task LoadAsync()
                {
                    _loading = true;
                    _message = string.Empty;
                    try
                    {
                        var filter = {{filterQuery}};
                        using var request = new HttpRequestMessage(HttpMethod.Get, "api/{{resource.Route}}/?page=1&pageSize=100" + filter);
                        Prepare(request);
                        using var response = await Http.SendAsync(request);
                        if (!response.IsSuccessStatusCode)
                        {
                            _message = await ErrorAsync(response);
                            _items = [];
                            return;
                        }
                        await using var stream = await response.Content.ReadAsStreamAsync();
                        using var document = await JsonDocument.ParseAsync(stream);
                        _items = document.RootElement.TryGetProperty("items", out var items)
                            ? items.EnumerateArray().Select(item => item.Clone()).ToArray()
                            : [];
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                        _items = [];
                    }
                    finally
                    {
                        _loading = false;
                    }
                }

                private async Task ClearSearchAsync()
                {
                    _search = string.Empty;
                    await LoadAsync();
                }

                private void NewItem()
                {
                    _editingId = null;
                    _etag = null;
                    _message = string.Empty;
            {{clearFields}}
                }

                private async Task EditAsync(JsonElement row)
                {
                    if (!TryGuid(row, "id", out var id))
                        return;
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"api/{{resource.Route}}/{id:D}");
                    Prepare(request);
                    using var response = await Http.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        _message = await ErrorAsync(response);
                        return;
                    }
                    _etag = response.Headers.ETag?.ToString();
                    var item = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _editingId = id;
            {{hydrateFields}}
                }

                private async Task SaveAsync()
                {
                    _saving = true;
                    _message = string.Empty;
                    try
                    {
                        var payload = new Dictionary<string, object?>
                        {
            {{payloadEntries}}
                        };
                        var method = _editingId is null ? HttpMethod.Post : HttpMethod.Put;
                        var path = _editingId is null
                            ? "api/{{resource.Route}}/"
                            : $"api/{{resource.Route}}/{_editingId.Value:D}";
                        using var request = new HttpRequestMessage(method, path)
                        {
                            Content = JsonContent.Create(payload)
                        };
                        Prepare(request);
                        {{(resource.Idempotency ? "request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));" : string.Empty)}}
                        if (_editingId is not null && {{resource.Concurrency.ToString().ToLowerInvariant()}} && !string.IsNullOrWhiteSpace(_etag))
                            request.Headers.TryAddWithoutValidation("If-Match", _etag);
                        using var response = await Http.SendAsync(request);
                        if (!response.IsSuccessStatusCode)
                        {
                            _message = await ErrorAsync(response);
                            return;
                        }
                        _message = _editingId is null ? "Created successfully." : "Updated successfully.";
                        NewItem();
                        await LoadAsync();
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                    }
                    finally
                    {
                        _saving = false;
                    }
                }

                private async Task DeleteAsync(JsonElement row)
                {
                    if (!TryGuid(row, "id", out var id))
                        return;
                    using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/{{resource.Route}}/{id:D}");
                    Prepare(request);
                    {{(resource.Idempotency ? "request.Headers.TryAddWithoutValidation(\"Idempotency-Key\", Guid.NewGuid().ToString(\"N\"));" : string.Empty)}}
                    using var response = await Http.SendAsync(request);
                    if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                    {
                        _message = await ErrorAsync(response);
                        return;
                    }
                    _message = "Deleted successfully.";
                    if (_editingId == id)
                        NewItem();
                    await LoadAsync();
                }

                private static string Read(JsonElement item, string name)
                {
                    if (!item.TryGetProperty(name, out var value) &&
                        !item.TryGetProperty(char.ToUpperInvariant(name[0]) + name[1..], out value))
                        return string.Empty;
                    return value.ValueKind switch
                    {
                        JsonValueKind.Null => string.Empty,
                        JsonValueKind.String => value.GetString() ?? string.Empty,
                        JsonValueKind.True => "Yes",
                        JsonValueKind.False => "No",
                        _ => value.ToString()
                    };
                }

                private static bool TryGuid(JsonElement item, string name, out Guid id)
                {
                    var raw = Read(item, name);
                    return Guid.TryParse(raw, out id);
                }

                private static string ReadString(JsonElement item, string name) => Read(item, name);
                private static int? ReadInt(JsonElement item, string name) => int.TryParse(Read(item, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
                private static decimal? ReadDecimal(JsonElement item, string name) => decimal.TryParse(Read(item, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
                private static bool ReadBool(JsonElement item, string name) => bool.TryParse(Read(item, name), out var value) && value;
                private static DateOnly? ReadDate(JsonElement item, string name) => DateOnly.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;
                private static DateTimeOffset? ReadDateTime(JsonElement item, string name) => DateTimeOffset.TryParse(Read(item, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : null;
                private static Guid? ReadGuid(JsonElement item, string name) => Guid.TryParse(Read(item, name), out var value) ? value : null;

                private void Prepare(HttpRequestMessage request)
                {
            {{(auth ? "        request.Headers.TryAddWithoutValidation(\"X-Foundation-User\", \"11111111-1111-1111-1111-111111111111\");\n        request.Headers.TryAddWithoutValidation(\"X-Foundation-Roles\", \"admin\");" : string.Empty)}}
                }

                private static async Task<string> ErrorAsync(HttpResponseMessage response)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    return $"{(int)response.StatusCode} {response.ReasonPhrase}: {body}";
                }
            }
            """;
    }

    private static string BuildFormField(StudioFieldBlueprint field)
    {
        var label = Html(field.Name);
        return field.Type switch
        {
            StudioFieldType.Boolean => $$"""
                        <label class="studio-generated-check">
                            <input type="checkbox" @bind="{{StateName(field)}}" />
                            <span>{{label}}</span>
                        </label>
                """,
            StudioFieldType.Integer or StudioFieldType.Decimal => $$"""
                        <label class="studio-generated-field">
                            <span>{{label}}{{(field.Required ? " *" : string.Empty)}}</span>
                            <input class="fk-input" type="number" step="{{(field.Type == StudioFieldType.Decimal ? "0.01" : "1")}}" @bind="{{StateName(field)}}" />
                        </label>
                """,
            StudioFieldType.Date => $$"""
                        <label class="studio-generated-field">
                            <span>{{label}}{{(field.Required ? " *" : string.Empty)}}</span>
                            <input class="fk-input" type="date" @bind="{{StateName(field)}}" />
                        </label>
                """,
            StudioFieldType.DateTime => $$"""
                        <label class="studio-generated-field">
                            <span>{{label}}{{(field.Required ? " *" : string.Empty)}}</span>
                            <input class="fk-input" type="datetime-local" @bind="{{StateName(field)}}" />
                        </label>
                """,
            StudioFieldType.Guid or StudioFieldType.Reference => $$"""
                        <label class="studio-generated-field">
                            <span>{{label}}{{(field.Required ? " *" : string.Empty)}}{{(field.Type == StudioFieldType.Reference ? $" → {Html(field.ReferenceResource ?? string.Empty)}" : string.Empty)}}</span>
                            <input class="fk-input fk-mono" type="text" @bind="{{StateName(field)}}" placeholder="GUID" />
                        </label>
                """,
            _ => $$"""
                        <label class="studio-generated-field">
                            <span>{{label}}{{(field.Required ? " *" : string.Empty)}}</span>
                            <input class="fk-input" type="text" maxlength="{{field.MaximumLength}}" @bind="{{StateName(field)}}" />
                        </label>
                """
        };
    }

    private static string BuildStateField(StudioFieldBlueprint field) => field.Type switch
    {
        StudioFieldType.Boolean => $"    private bool {StateName(field)};",
        _ => $"    private string {StateName(field)} = string.Empty;"
    };

    private static string BuildClearField(StudioFieldBlueprint field) => field.Type switch
    {
        StudioFieldType.Boolean => $"        {StateName(field)} = false;",
        _ => $"        {StateName(field)} = string.Empty;"
    };

    private static string BuildHydrateField(StudioFieldBlueprint field) => field.Type switch
    {
        StudioFieldType.Boolean => $"        {StateName(field)} = ReadBool(item, {JsonSerializer.Serialize(field.Name)});",
        StudioFieldType.Date => $"        {StateName(field)} = ReadDate(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-dd\", CultureInfo.InvariantCulture) ?? string.Empty;",
        StudioFieldType.DateTime => $"        {StateName(field)} = ReadDateTime(item, {JsonSerializer.Serialize(field.Name)})?.ToString(\"yyyy-MM-ddTHH:mm\", CultureInfo.InvariantCulture) ?? string.Empty;",
        _ => $"        {StateName(field)} = ReadString(item, {JsonSerializer.Serialize(field.Name)});"
    };

    private static string BuildPayloadEntry(StudioFieldBlueprint field)
    {
        var value = field.Type switch
        {
            StudioFieldType.Boolean => StateName(field),
            StudioFieldType.Integer => $"ParseInt({StateName(field)}, {field.Required.ToString().ToLowerInvariant()}, {JsonSerializer.Serialize(field.Name)})",
            StudioFieldType.Decimal => $"ParseDecimal({StateName(field)}, {field.Required.ToString().ToLowerInvariant()}, {JsonSerializer.Serialize(field.Name)})",
            StudioFieldType.Date => $"ParseDate({StateName(field)}, {field.Required.ToString().ToLowerInvariant()}, {JsonSerializer.Serialize(field.Name)})",
            StudioFieldType.DateTime => $"ParseDateTime({StateName(field)}, {field.Required.ToString().ToLowerInvariant()}, {JsonSerializer.Serialize(field.Name)})",
            StudioFieldType.Guid or StudioFieldType.Reference => $"ParseGuid({StateName(field)}, {field.Required.ToString().ToLowerInvariant()}, {JsonSerializer.Serialize(field.Name)})",
            _ => field.Required
                ? $"RequireText({StateName(field)}, {JsonSerializer.Serialize(field.Name)})"
                : $"string.IsNullOrWhiteSpace({StateName(field)}) ? null : {StateName(field)}.Trim()"
        };
        return $"            [{JsonSerializer.Serialize(field.Name)}] = {value}";
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
        const string marker = "                </Navigation>";
        if (!source.Contains(marker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Studio could not locate generated Blazor navigation boundary.");

        var items = new StringBuilder();
        foreach (var module in compilation.Blueprint.Modules)
        {
            foreach (var resource in module.Resources)
            {
                items.AppendLine($"                    <FkNavItem Href=\"data/{resource.Route}\">");
                items.AppendLine("                        <Icon><span aria-hidden=\"true\">▦</span></Icon>");
                items.AppendLine($"                        <ChildContent>{Html(resource.Name)}</ChildContent>");
                items.AppendLine("                    </FkNavItem>");
            }
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
        const string sentinel = ".studio-generated-grid";
        if (source.Contains(sentinel, StringComparison.Ordinal))
            return;
        source += """

            .studio-generated-grid {
                display: grid;
                grid-template-columns: minmax(0, 2fr) minmax(18rem, 1fr);
                gap: var(--fk-space-5, 1.25rem);
                align-items: start;
            }
            .studio-generated-toolbar,
            .studio-generated-search {
                display: flex;
                gap: .75rem;
                align-items: center;
                justify-content: space-between;
                flex-wrap: wrap;
            }
            .studio-generated-search .fk-input { min-width: min(28rem, 100%); }
            .studio-generated-table-wrap { overflow-x: auto; }
            .studio-generated-table { width: 100%; border-collapse: collapse; }
            .studio-generated-table th,
            .studio-generated-table td { padding: .75rem; border-bottom: 1px solid var(--fk-border, rgba(127,127,127,.2)); text-align: start; white-space: nowrap; }
            .studio-generated-field { display: grid; gap: .4rem; }
            .studio-generated-check { display: flex; gap: .65rem; align-items: center; }
            .studio-generated-message { overflow-wrap: anywhere; }
            @media (max-width: 980px) {
                .studio-generated-grid { grid-template-columns: 1fr; }
            }
            """;
        await File.WriteAllTextAsync(path, Normalize(source), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string StateName(StudioFieldBlueprint field) => "_" + char.ToLowerInvariant(field.Name[0]) + field.Name[1..];
    private static string Html(string value) => System.Security.SecurityElement.Escape(value) ?? value;

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
