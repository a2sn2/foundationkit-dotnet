using System.Security.Claims;
using FoundationKit.Application.Abstractions;
using FoundationKit.Authorization;
using FoundationKit.Caching;
using FoundationKit.FeatureManagement;
using FoundationKit.Identity;
using FoundationKit.Infrastructure.Http;
using FoundationKit.Settings;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Settings;
using AbpCurrentUser = Volo.Abp.Users.ICurrentUser;
using Xunit;

namespace FoundationKit.Tests;

public sealed class PlatformLeverageTests
{
    [Fact]
    public async Task Hybrid_cache_reuses_native_value_and_supports_removal()
    {
        var services = new ServiceCollection();
        services.AddFoundationHybridCache();
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IValueCache>();
        var calls = 0;

        var first = await cache.GetOrCreateAsync(
            "platform-leverage",
            _ => new ValueTask<string>($"value-{++calls}"),
            TimeSpan.FromMinutes(5));
        var second = await cache.GetOrCreateAsync(
            "platform-leverage",
            _ => new ValueTask<string>($"value-{++calls}"),
            TimeSpan.FromMinutes(5));

        Assert.Equal("value-1", first);
        Assert.Equal(first, second);
        Assert.Equal(1, calls);

        await cache.RemoveAsync("platform-leverage");
        var third = await cache.GetOrCreateAsync(
            "platform-leverage",
            _ => new ValueTask<string>($"value-{++calls}"));

        Assert.Equal("value-2", third);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Standard_resilient_http_client_is_resolvable_without_custom_pipeline_code()
    {
        var services = new ServiceCollection();
        services.AddFoundationResilientHttpClient("proof", client =>
            client.BaseAddress = new Uri("https://example.invalid/"));
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("proof");

        Assert.Equal(new Uri("https://example.invalid/"), client.BaseAddress);
    }

    [Fact]
    public async Task Abp_setting_provider_maps_into_foundation_setting_contract()
    {
        var reader = new AbpSettingReader(
            new FakeSettingProvider(new Dictionary<string, string?>
            {
                ["foundation.locale"] = "ar-YE"
            }));

        var result = await reader.ResolveAsync(
            "foundation.locale",
            SettingResolutionContext.Global);

        Assert.NotNull(result);
        Assert.Equal("ar-YE", result.Value);
        Assert.Equal("provider", result.Scope.Kind);
        Assert.Equal("abp-current-context", result.Scope.Identifier);
    }

    [Fact]
    public async Task Abp_feature_checker_maps_into_foundation_feature_decision()
    {
        var evaluator = new AbpFeatureEvaluator(
            new FakeFeatureChecker(new Dictionary<string, bool>
            {
                ["catalog.preview"] = true
            }));

        var result = await evaluator.EvaluateAsync(
            new FeatureDefinition("catalog.preview"),
            FeatureEvaluationContext.Global);

        Assert.True(result.IsEnabled);
        Assert.Equal(FeatureDecisionSource.Provider, result.Source);
    }

    [Fact]
    public async Task Abp_permission_checker_preserves_authentication_and_ownership_semantics()
    {
        var userId = Guid.NewGuid();
        var checker = new FakePermissionChecker("records.manage");
        var evaluator = new AbpPermissionAuthorizationEvaluator(
            checker,
            new FakeAuthorizationSubject(true, userId));

        Assert.True(await evaluator.HasPermissionAsync("records.manage"));
        Assert.False(await evaluator.HasPermissionAsync("records.delete"));
        Assert.True(await evaluator.CanAccessOwnedResourceAsync(userId, "records.delete"));
    }

    [Fact]
    public void Abp_current_user_maps_into_minimal_foundation_user_contract()
    {
        var userId = Guid.NewGuid();
        ICurrentUser currentUser = new AbpCurrentUserAdapter(
            new FakeAbpCurrentUser(userId, "dev@example.test", ["admin"]));

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal("dev@example.test", currentUser.Email);
        Assert.True(currentUser.IsInRole("admin"));
        Assert.False(currentUser.IsInRole("user"));
    }

    private sealed class FakeSettingProvider(IReadOnlyDictionary<string, string?> values) : ISettingProvider
    {
        public Task<string?> GetOrNullAsync(string name) =>
            Task.FromResult(values.TryGetValue(name, out var value) ? value : null);

        public Task<List<SettingValue>> GetAllAsync(string[] names) =>
            Task.FromResult(names
                .Where(values.ContainsKey)
                .Select(name => new SettingValue(name, values[name]))
                .ToList());

        public Task<List<SettingValue>> GetAllAsync() =>
            Task.FromResult(values
                .Select(pair => new SettingValue(pair.Key, pair.Value))
                .ToList());
    }

    private sealed class FakeFeatureChecker(IReadOnlyDictionary<string, bool> values) : IFeatureChecker
    {
        public Task<string?> GetOrNullAsync(string name) =>
            Task.FromResult(values.TryGetValue(name, out var enabled)
                ? enabled.ToString().ToLowerInvariant()
                : null);

        public Task<bool> IsEnabledAsync(string name) =>
            Task.FromResult(values.TryGetValue(name, out var enabled) && enabled);

        public Task<Dictionary<string, bool>> IsEnabledAsync(string[] names) =>
            Task.FromResult(names.ToDictionary(
                name => name,
                name => values.TryGetValue(name, out var enabled) && enabled,
                StringComparer.Ordinal));
    }

    private sealed class FakePermissionChecker(params string[] granted) : IPermissionChecker
    {
        private readonly HashSet<string> _granted = new(granted, StringComparer.Ordinal);

        public Task<bool> IsGrantedAsync(string name) =>
            Task.FromResult(_granted.Contains(name));

        public Task<bool> IsGrantedAsync(ClaimsPrincipal? claimsPrincipal, string name) =>
            IsGrantedAsync(name);

        public Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names) =>
            Task.FromResult(CreateResult(names));

        public Task<MultiplePermissionGrantResult> IsGrantedAsync(
            ClaimsPrincipal? claimsPrincipal,
            string[] names) => IsGrantedAsync(names);

        private MultiplePermissionGrantResult CreateResult(string[] names)
        {
            var result = new MultiplePermissionGrantResult();
            foreach (var name in names)
            {
                result.Result[name] = _granted.Contains(name)
                    ? PermissionGrantResult.Granted
                    : PermissionGrantResult.Prohibited;
            }

            return result;
        }
    }

    private sealed class FakeAuthorizationSubject(bool authenticated, Guid? userId) : IAuthorizationSubject
    {
        public bool IsAuthenticated { get; } = authenticated;
        public Guid? UserId { get; } = userId;
        public bool IsInRole(string role) => false;
    }

    private sealed class FakeAbpCurrentUser(Guid id, string email, string[] roles) : AbpCurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? Id => id;
        public string? UserName => email;
        public string? Name => null;
        public string? SurName => null;
        public string? PhoneNumber => null;
        public bool PhoneNumberVerified => false;
        public string? Email => email;
        public bool EmailVerified => true;
        public Guid? TenantId => null;
        public string[] Roles => roles;
        public Claim? FindClaim(string claimType) => FindClaims(claimType).FirstOrDefault();
        public Claim[] FindClaims(string claimType) =>
            GetAllClaims().Where(claim => claim.Type == claimType).ToArray();
        public Claim[] GetAllClaims() =>
            roles.Select(role => new Claim(ClaimTypes.Role, role)).ToArray();
        public bool IsInRole(string roleName) =>
            roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
}
