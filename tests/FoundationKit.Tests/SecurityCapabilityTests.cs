using System.Net;
using System.Security.Claims;
using FoundationKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FoundationKit.Tests;

public sealed class SecurityCapabilityTests
{
    [Fact]
    public void Disabled_trusted_proxy_configuration_does_not_require_proxy_addresses()
    {
        TrustedProxySecurity.Validate(new TrustedProxyOptions { Enabled = false });
    }

    [Fact]
    public void Enabled_trusted_proxy_configuration_requires_explicit_proxy_addresses()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TrustedProxySecurity.Validate(new TrustedProxyOptions { Enabled = true }));

        Assert.Contains("KnownProxies", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Trusted_proxy_configuration_rejects_invalid_and_duplicate_addresses()
    {
        var invalid = Assert.Throws<InvalidOperationException>(() =>
            TrustedProxySecurity.Validate(new TrustedProxyOptions
            {
                Enabled = true,
                KnownProxies = ["not-an-ip"]
            }));
        Assert.Contains("invalid IP", invalid.Message, StringComparison.OrdinalIgnoreCase);

        var duplicate = Assert.Throws<InvalidOperationException>(() =>
            TrustedProxySecurity.Validate(new TrustedProxyOptions
            {
                Enabled = true,
                KnownProxies = ["10.0.0.2", "10.0.0.2"]
            }));
        Assert.Contains("duplicate", duplicate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Forwarded_header_options_trust_only_configured_proxies()
    {
        var options = TrustedProxySecurity.CreateForwardedHeadersOptions(new TrustedProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = ["10.0.0.2"]
        });

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Empty(options.KnownIPNetworks);
        Assert.Single(options.KnownProxies);
        Assert.Equal(IPAddress.Parse("10.0.0.2"), options.KnownProxies[0]);
    }

    [Fact]
    public void Rate_limit_partitions_are_ip_based_for_auth_and_user_based_for_authenticated_writes()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        Assert.Equal("ip:192.0.2.10", FoundationRateLimitPartitions.Authentication(context));
        Assert.Equal("ip:192.0.2.10", FoundationRateLimitPartitions.Write(context));

        var userId = Guid.NewGuid().ToString();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "test"));

        Assert.Equal($"user:{userId}", FoundationRateLimitPartitions.Write(context));
    }

    [Fact]
    public void Multi_factor_assurance_requires_exact_amr_mfa_claim()
    {
        var passwordOnly = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(FoundationAuthenticationAssurance.AuthenticationMethodClaimType, "pwd")],
            "test"));
        var multiFactor = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(FoundationAuthenticationAssurance.AuthenticationMethodClaimType,
                FoundationAuthenticationAssurance.MultiFactorAuthenticationMethod)],
            "test"));

        Assert.False(FoundationAuthenticationAssurance.HasMultiFactorAuthentication(passwordOnly));
        Assert.True(FoundationAuthenticationAssurance.HasMultiFactorAuthentication(multiFactor));
    }

    [Fact]
    public void Multi_factor_policy_extension_adds_the_shared_amr_requirement()
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireFoundationMultiFactor()
            .Build();

        var requirement = Assert.Single(policy.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal(FoundationAuthenticationAssurance.AuthenticationMethodClaimType, requirement.ClaimType);
        Assert.Contains(
            FoundationAuthenticationAssurance.MultiFactorAuthenticationMethod,
            requirement.AllowedValues ?? Array.Empty<string>(),
            StringComparer.Ordinal);
    }

    [Fact]
    public async Task Forwarded_headers_are_applied_only_when_request_arrives_from_a_trusted_proxy()
    {
        var trustedProxy = IPAddress.Parse("10.0.0.2");
        var forwardedClient = IPAddress.Parse("203.0.113.42");
        var trustedContext = new DefaultHttpContext();
        trustedContext.Connection.RemoteIpAddress = trustedProxy;
        trustedContext.Request.Scheme = "http";
        trustedContext.Request.Headers["X-Forwarded-For"] = forwardedClient.ToString();
        trustedContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await ApplyForwardedHeadersAsync(trustedContext, trustedProxy);

        Assert.Equal(forwardedClient, trustedContext.Connection.RemoteIpAddress);
        Assert.Equal("https", trustedContext.Request.Scheme);

        var directClient = IPAddress.Parse("198.51.100.77");
        var untrustedContext = new DefaultHttpContext();
        untrustedContext.Connection.RemoteIpAddress = directClient;
        untrustedContext.Request.Scheme = "http";
        untrustedContext.Request.Headers["X-Forwarded-For"] = forwardedClient.ToString();
        untrustedContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await ApplyForwardedHeadersAsync(untrustedContext, trustedProxy);

        Assert.Equal(directClient, untrustedContext.Connection.RemoteIpAddress);
        Assert.Equal("http", untrustedContext.Request.Scheme);
    }

    private static async Task ApplyForwardedHeadersAsync(HttpContext context, IPAddress trustedProxy)
    {
        var options = TrustedProxySecurity.CreateForwardedHeadersOptions(new TrustedProxyOptions
        {
            Enabled = true,
            KnownProxies = [trustedProxy.ToString()]
        });

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));

        await middleware.Invoke(context);
    }
}
