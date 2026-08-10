using FoundationKit.Infrastructure.Idempotency;
using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class WorkbenchDbContext(DbContextOptions<WorkbenchDbContext> options)
    : DbContext(options)
{
    public DbSet<BuildBrief> BuildBriefs => Set<BuildBrief>();
    public DbSet<AdminReview> AdminReviews => Set<AdminReview>();
    public DbSet<CoreCrudRecord> CoreCrudRecords => Set<CoreCrudRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkbenchDbContext).Assembly);
        modelBuilder.AddFoundationIdempotencyStore();
        base.OnModelCreating(modelBuilder);
    }
}
