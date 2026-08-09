using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Security;

public sealed class TrustedProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; set; }

    public int ForwardLimit { get; set; } = 1;

    public string[] KnownProxies { get; set; } = [];
}

public static class TrustedProxySecurity
{
    public static void Validate(TrustedProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return;
        }

        if (options.ForwardLimit is < 1 or > 10)
        {
            throw new InvalidOperationException(
                "ReverseProxy:ForwardLimit must be between 1 and 10 when trusted proxy forwarding is enabled.");
        }

        if (options.KnownProxies.Length == 0)
        {
            throw new InvalidOperationException(
                "ReverseProxy:Enabled=true requires at least one explicit trusted proxy IP in ReverseProxy:KnownProxies. Trust-all forwarded headers are not permitted.");
        }

        var seen = new HashSet<IPAddress>();
        foreach (var proxy in options.KnownProxies)
        {
            if (!IPAddress.TryParse(proxy, out var address))
            {
                throw new InvalidOperationException(
                    $"ReverseProxy:KnownProxies contains an invalid IP address: '{proxy}'.");
            }

            if (!seen.Add(address))
            {
                throw new InvalidOperationException(
                    $"ReverseProxy:KnownProxies contains duplicate IP address '{proxy}'.");
            }
        }
    }

    public static ForwardedHeadersOptions CreateForwardedHeadersOptions(TrustedProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = options.ForwardLimit
        };

        forwardedHeaders.KnownIPNetworks.Clear();
        forwardedHeaders.KnownProxies.Clear();

        foreach (var proxy in options.KnownProxies)
        {
            forwardedHeaders.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        return forwardedHeaders;
    }
}

public static class TrustedProxySecurityExtensions
{
    public static IServiceCollection AddFoundationTrustedProxyForwarding(
        this IServiceCollection services,
        TrustedProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        TrustedProxySecurity.Validate(options);
        if (!options.Enabled)
        {
            return services;
        }

        var configured = TrustedProxySecurity.CreateForwardedHeadersOptions(options);
        services.Configure<ForwardedHeadersOptions>(target =>
        {
            target.ForwardedHeaders = configured.ForwardedHeaders;
            target.ForwardLimit = configured.ForwardLimit;
            target.KnownIPNetworks.Clear();
            target.KnownProxies.Clear();

            foreach (var proxy in configured.KnownProxies)
            {
                target.KnownProxies.Add(proxy);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseFoundationTrustedProxyForwarding(
        this IApplicationBuilder app,
        TrustedProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        TrustedProxySecurity.Validate(options);
        return options.Enabled ? app.UseForwardedHeaders() : app;
    }
}
