using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FoundationKit.Workbench.Client.Pages;

public partial class Compose
{
    [Inject]
    private ISnackbar StarterChoiceSnackbar { get; set; } = default!;

    private string? _lastStarterChoiceFingerprint;
    private bool _starterChoicesDirty;
    private bool _starterChoiceNoticeShown;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        var fingerprint = GetStarterChoicesFingerprint();
        var manifestMatchesChoices = ManifestMatchesStarterChoices();

        if (firstRender)
        {
            _lastStarterChoiceFingerprint = fingerprint;
            _starterChoicesDirty = !manifestMatchesChoices;
            return;
        }

        var choicesChanged = !string.Equals(
            _lastStarterChoiceFingerprint,
            fingerprint,
            StringComparison.Ordinal);
        _lastStarterChoiceFingerprint = fingerprint;

        if (manifestMatchesChoices)
        {
            _starterChoicesDirty = false;
            _starterChoiceNoticeShown = false;
            return;
        }

        if (choicesChanged)
        {
            _starterChoicesDirty = true;
            InvalidateValidationState();
            ShowStarterChoiceNotice();
            _ = InvokeAsync(StateHasChanged);
            return;
        }

        if (_starterChoicesDirty && IsCurrentManifestValidated)
        {
            InvalidateValidationState();
            StarterChoiceSnackbar.Add(
                T(
                    "لا يمكن اعتماد تحقق قديم بعد تغيير اختيارات المشروع. طبّق الاختيارات على الـManifest ثم تحقّق مجددًا.",
                    "A previous validation cannot be reused after project choices change. Apply the choices to the Manifest, then validate again."),
                Severity.Warning);
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private void ShowStarterChoiceNotice()
    {
        if (_starterChoiceNoticeShown)
            return;

        _starterChoiceNoticeShown = true;
        StarterChoiceSnackbar.Add(
            T(
                "تغيّرت اختيارات المشروع ولم تُطبّق على الـManifest بعد. طبّق الاختيارات ثم أعد التحقق قبل التوليد.",
                "Project choices changed but are not applied to the Manifest yet. Apply the choices, then validate again before generation."),
            Severity.Warning);
    }

    private string GetStarterChoicesFingerprint() => string.Join(
        '\u001f',
        _projectName,
        _profile,
        _moduleName,
        _resourceName,
        _route,
        _idType,
        _authorization,
        _auditing,
        _concurrency,
        _idempotency,
        _blazor);

    private bool ManifestMatchesStarterChoices()
    {
        if (string.IsNullOrWhiteSpace(_manifestJson))
            return false;

        try
        {
            using var document = JsonDocument.Parse(_manifestJson);
            var root = document.RootElement;

            if (!StringPropertyEquals(root, "name", _projectName.Trim()) ||
                !StringPropertyEquals(root, "profile", _profile) ||
                !StringArraySetEquals(
                    root,
                    "includeCapabilities",
                    ExpectedCapabilities()))
            {
                return false;
            }

            if (!TryGetFirstArrayObject(root, "modules", out var module) ||
                !StringPropertyEquals(module, "name", _moduleName.Trim()) ||
                !TryGetFirstArrayObject(module, "resources", out var resource))
            {
                return false;
            }

            return StringPropertyEquals(resource, "name", _resourceName.Trim()) &&
                   StringPropertyEquals(resource, "route", _route.Trim()) &&
                   StringPropertyEquals(resource, "idType", _idType) &&
                   StringArraySetEquals(resource, "behaviors", ExpectedBehaviors()) &&
                   ResourceApiMatches(resource);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private string[] ExpectedCapabilities()
    {
        var capabilities = new List<string>();
        if (_concurrency)
            capabilities.Add("concurrency");
        if (_idempotency)
            capabilities.Add("idempotency");
        if (_blazor)
            capabilities.Add("blazor");
        return capabilities.ToArray();
    }

    private string[] ExpectedBehaviors()
    {
        var behaviors = new List<string> { "crud" };
        if (_auditing)
            behaviors.Add("auditing");
        if (_authorization)
            behaviors.Add("authorization");
        if (_concurrency)
            behaviors.Add("concurrency");
        return behaviors.ToArray();
    }

    private bool ResourceApiMatches(JsonElement resource)
    {
        if (!resource.TryGetProperty("api", out var api) || api.ValueKind != JsonValueKind.Object)
            return false;

        var expectedIdempotency = _idempotency ? "required" : "disabled";
        var expectedConcurrency = _concurrency ? "require-if-match" : "application-policy";

        return StringPropertyEquals(api, "idempotency", expectedIdempotency) &&
               StringPropertyEquals(api, "concurrency", expectedConcurrency);
    }

    private static bool StringPropertyEquals(JsonElement element, string name, string expected) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        string.Equals(value.GetString(), expected, StringComparison.Ordinal);

    private static bool StringArraySetEquals(
        JsonElement element,
        string name,
        IEnumerable<string> expected)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            return false;

        var actual = value
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        return actual.SetEquals(expected);
    }

    private static bool TryGetFirstArrayObject(
        JsonElement element,
        string name,
        out JsonElement result)
    {
        result = default;
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            result = item;
            return true;
        }

        return false;
    }
}
