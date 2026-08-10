using FoundationKit.Application.Results;
using FoundationKit.Approvals;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Blazor.Api;
using FoundationKit.Domain.Primitives;
using FoundationKit.Identity;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using FoundationKit.Security;
using FoundationKit.WebApi.Results;
using FoundationKit.Workflow;

namespace FoundationKit.Tests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_has_no_outer_layer_or_framework_dependencies() => AssertNoReferences(
        typeof(Entity<>).Assembly,
        "FoundationKit.Application", "FoundationKit.Infrastructure", "FoundationKit.WebApi", "FoundationKit.Blazor",
        "FoundationKit.Auditing", "FoundationKit.Security", "FoundationKit.Identity", "FoundationKit.Authorization",
        "FoundationKit.Workflow", "FoundationKit.Approvals", "FoundationKit.Notifications", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    [Fact]
    public void Application_stays_provider_and_transport_neutral() => AssertNoReferences(
        typeof(Result).Assembly,
        "FoundationKit.Infrastructure", "FoundationKit.WebApi", "FoundationKit.Blazor", "FoundationKit.Auditing",
        "FoundationKit.Security", "FoundationKit.Identity", "FoundationKit.Authorization", "FoundationKit.Workflow",
        "FoundationKit.Approvals", "FoundationKit.Notifications", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    [Fact]
    public void Infrastructure_does_not_select_relational_provider_or_web_host() => AssertNoReferences(
        typeof(EfRepository<,,>).Assembly,
        "FoundationKit.WebApi", "FoundationKit.Blazor", "FoundationKit.Auditing", "FoundationKit.Security",
        "FoundationKit.Identity", "FoundationKit.Authorization", "FoundationKit.Workflow", "FoundationKit.Approvals",
        "FoundationKit.Notifications", "Microsoft.EntityFrameworkCore.SqlServer", "Npgsql.EntityFrameworkCore.PostgreSQL",
        "Microsoft.EntityFrameworkCore.Sqlite", "Microsoft.AspNetCore");

    [Fact]
    public void WebApi_does_not_depend_on_persistence_or_upper_capabilities() => AssertNoReferences(
        typeof(ResultHttpExtensions).Assembly,
        "FoundationKit.Infrastructure", "FoundationKit.Blazor", "FoundationKit.Auditing", "FoundationKit.Security",
        "FoundationKit.Identity", "FoundationKit.Authorization", "FoundationKit.Workflow", "FoundationKit.Approvals",
        "FoundationKit.Notifications", "Microsoft.EntityFrameworkCore");

    [Fact]
    public void Blazor_stays_client_oriented() => AssertNoReferences(
        typeof(ApiResult).Assembly,
        "FoundationKit.Infrastructure", "FoundationKit.WebApi", "FoundationKit.Auditing", "Microsoft.EntityFrameworkCore");

    [Fact]
    public void Optional_capabilities_keep_expected_direction()
    {
        AssertNoReferences(typeof(AuditRecorder).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(TrustedProxySecurity).Assembly, "FoundationKit.Identity", "FoundationKit.Authorization", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(AccountSecurityOptions).Assembly, "FoundationKit.Authorization", "FoundationKit.Workflow", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(PermissionDefinition).Assembly, "FoundationKit.Workflow", "FoundationKit.Approvals", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(WorkflowDefinition).Assembly, "FoundationKit.Authorization", "FoundationKit.Approvals", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(ApprovalPolicy).Assembly, "FoundationKit.Notifications", "Microsoft.EntityFrameworkCore");
        AssertNoReferences(typeof(NotificationMessage).Assembly, "FoundationKit.Notifications.Smtp", "Microsoft.EntityFrameworkCore", "System.Net.Mail");
        AssertNoReferences(typeof(SmtpNotificationSender).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi", "Microsoft.EntityFrameworkCore");
    }

    private static void AssertNoReferences(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var references = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        foreach (var name in forbidden)
            Assert.DoesNotContain(references, x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
