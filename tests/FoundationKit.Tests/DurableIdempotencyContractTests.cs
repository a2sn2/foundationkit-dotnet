using FoundationKit.Application.Idempotency;
using FoundationKit.Application.Isolation;
using FoundationKit.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Tests;

public sealed class DurableIdempotencyContractTests
{
    [Fact]
    public void Acquisition_identity_is_normalized_and_bounded()
    {
        var now = DateTimeOffset.Parse("2026-08-10T20:00:00+00:00");
        var request = new IdempotencyAcquireRequest(
            new FoundationProjectId("Project-A"),
            "CoreCrud:Create",
            new string('A', 64),
            new string('B', 64),
            now,
            now.AddHours(1));

        var normalized = request.Normalize();

        Assert.Equal("project-a", normalized.ProjectId.Value);
        Assert.Equal("corecrud:create", normalized.OperationScope);
        Assert.Equal(new string('a', 64), normalized.KeyHash);
        Assert.Equal(new string('b', 64), normalized.RequestFingerprint);
    }

    [Fact]
    public void Acquisition_rejects_invalid_hashes_and_non_forward_replay_windows()
    {
        var now = DateTimeOffset.Parse("2026-08-10T20:00:00+00:00");

        Assert.Throws<ArgumentException>(() => new IdempotencyAcquireRequest(
            new FoundationProjectId("project-a"),
            "core:create",
            "not-a-hash",
            new string('b', 64),
            now,
            now.AddHours(1)).Normalize());

        Assert.Throws<InvalidOperationException>(() => new IdempotencyAcquireRequest(
            new FoundationProjectId("project-a"),
            "core:create",
            new string('a', 64),
            new string('b', 64),
            now,
            now).Normalize());
    }

    [Fact]
    public void Replay_response_is_bounded_and_defensively_copied()
    {
        var source = new byte[] { 1, 2, 3 };
        var response = new IdempotencyResponse(201, " application/json ", source, " /resource/1 ", " \"1\" ");

        var normalized = response.Normalize(3);
        source[0] = 9;

        Assert.Equal(201, normalized.StatusCode);
        Assert.Equal("application/json", normalized.ContentType);
        Assert.Equal("/resource/1", normalized.Location);
        Assert.Equal("\"1\"", normalized.EntityTag);
        Assert.Equal(new byte[] { 1, 2, 3 }, normalized.Body);
        Assert.Throws<InvalidOperationException>(() => response.Normalize(2));
    }

    [Fact]
    public void Relational_model_owns_one_project_scoped_composite_key()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        modelBuilder.AddFoundationIdempotencyStore();

        var entity = modelBuilder.Model.FindEntityType("FoundationKit.Infrastructure.Idempotency.FoundationIdempotencyEntry");
        Assert.NotNull(entity);
        Assert.Equal(FoundationIdempotencyModelBuilderExtensions.DefaultTableName, entity!.GetTableName());
        Assert.Equal(
            ["ProjectId", "OperationScope", "KeyHash"],
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Single(entity.GetIndexes(), index => index.Properties.Single().Name == "ReplayUntilUtc");
    }

    [Fact]
    public void Ef_store_registration_is_opt_in_and_replaces_no_other_platform_service()
    {
        var services = new ServiceCollection();

        services.AddFoundationEfIdempotencyStore<TestDbContext>();

        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(IIdempotencyStore));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(EfIdempotencyStore<TestDbContext>), descriptor.ImplementationType);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
