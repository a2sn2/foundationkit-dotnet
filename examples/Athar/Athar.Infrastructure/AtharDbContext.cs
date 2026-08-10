using Athar.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Athar.Infrastructure;

public sealed class AtharDbContext(
    DbContextOptions<AtharDbContext> options)
    : IdentityDbContext<AtharUser, IdentityRole<Guid>, Guid>(options)
{
    private const int IdentityCompositeKeyMaxLength = 128;

    public DbSet<Initiative> Initiatives => Set<Initiative>();

    public DbSet<InitiativeReview> InitiativeReviews => Set<InitiativeReview>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureInitiatives(builder);
        ConfigureReviews(builder);
        ConfigureAudit(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<AtharUser>(entity =>
        {
            entity.ToTable("Users", "identity");
            entity.Property(user => user.DisplayName)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(user => user.CreatedUtc).IsRequired();
        });

        builder.Entity<IdentityRole<Guid>>()
            .ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins", "identity");
            entity.Property(login => login.LoginProvider)
                .HasMaxLength(IdentityCompositeKeyMaxLength);
            entity.Property(login => login.ProviderKey)
                .HasMaxLength(IdentityCompositeKeyMaxLength);
        });
        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens", "identity");
            entity.Property(token => token.LoginProvider)
                .HasMaxLength(IdentityCompositeKeyMaxLength);
            entity.Property(token => token.Name)
                .HasMaxLength(IdentityCompositeKeyMaxLength);
        });
    }

    private static void ConfigureInitiatives(ModelBuilder builder)
    {
        builder.Entity<Initiative>(entity =>
        {
            entity.ToTable("Initiatives", "athar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title)
                .HasMaxLength(140)
                .IsRequired();
            entity.Property(item => item.Summary)
                .HasMaxLength(1800)
                .IsRequired();
            entity.Property(item => item.Category)
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(item => item.City)
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(item => item.RequestedBudget)
                .HasPrecision(18, 2);
            entity.Property(item => item.Status)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Ignore(item => item.DomainEvents);
            entity.HasIndex(item => new
            {
                item.OwnerUserId,
                item.ClientRequestId
            }).IsUnique();
            entity.HasIndex(item => new
            {
                item.Status,
                item.CreatedUtc
            });
            entity.HasOne<AtharUser>()
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReviews(ModelBuilder builder)
    {
        builder.Entity<InitiativeReview>(entity =>
        {
            entity.ToTable("InitiativeReviews", "athar");
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Decision)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(review => review.Notes)
                .HasMaxLength(1200)
                .IsRequired();
            entity.HasIndex(review => new
            {
                review.InitiativeId,
                review.ReviewedUtc
            });
            entity.HasOne<Initiative>()
                .WithMany()
                .HasForeignKey(review => review.InitiativeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AtharUser>()
                .WithMany()
                .HasForeignKey(review => review.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAudit(ModelBuilder builder)
    {
        builder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries", "athar");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Action)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(entry => entry.EntityType)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(entry => entry.Details)
                .HasMaxLength(2000)
                .IsRequired();
            entity.HasIndex(entry => new
            {
                entry.EntityType,
                entry.EntityId,
                entry.CreatedUtc
            });
        });
    }
}
