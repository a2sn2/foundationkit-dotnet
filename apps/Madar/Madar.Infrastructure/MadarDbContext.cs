using Madar.Domain.Cases;
using Madar.Domain.Organization;
using Madar.Infrastructure.Auditing;
using Madar.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure;

public sealed class MadarDbContext(
    DbContextOptions<MadarDbContext> options)
    : IdentityDbContext<MadarUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Case> Cases => Set<Case>();

    public DbSet<CaseComment> CaseComments => Set<CaseComment>();

    public DbSet<CaseAttachment> CaseAttachments => Set<CaseAttachment>();

    public DbSet<CaseApproval> CaseApprovals => Set<CaseApproval>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<DepartmentMembership> DepartmentMemberships => Set<DepartmentMembership>();

    public DbSet<MadarAuditRecord> AuditEvents => Set<MadarAuditRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureOrganization(builder);
        ConfigureCases(builder);
        ConfigureComments(builder);
        ConfigureAttachments(builder);
        ConfigureApprovals(builder);
        ConfigureAudit(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<MadarUser>(entity =>
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
        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens", "identity");
    }

    private static void ConfigureOrganization(ModelBuilder builder)
    {
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments", "madar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code)
                .HasMaxLength(60)
                .IsRequired();
            entity.Property(item => item.Name)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(item => item.IsActive)
                .IsRequired();
            entity.Property(item => item.CreatedUtc)
                .IsRequired();
            entity.Property(item => item.UpdatedUtc)
                .IsRequired();
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(item => item.Code)
                .IsUnique();
        });

        builder.Entity<DepartmentMembership>(entity =>
        {
            entity.ToTable("DepartmentMemberships", "madar");
            entity.HasKey(item => new
            {
                item.DepartmentId,
                item.UserId
            });
            entity.Property(item => item.JoinedUtc)
                .IsRequired();

            entity.HasIndex(item => new
            {
                item.UserId,
                item.DepartmentId
            });

            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCases(ModelBuilder builder)
    {
        builder.Entity<Case>(entity =>
        {
            entity.ToTable("Cases", "madar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(item => item.Description)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(item => item.CaseType)
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(item => item.Priority)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.Status)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Ignore(item => item.DomainEvents);

            entity.HasIndex(item => new
            {
                item.CreatedByUserId,
                item.CreatedUtc
            });
            entity.HasIndex(item => new
            {
                item.DepartmentId,
                item.Status,
                item.UpdatedUtc
            });
            entity.HasIndex(item => new
            {
                item.AssignedToUserId,
                item.Status,
                item.UpdatedUtc
            });
            entity.HasIndex(item => new
            {
                item.Status,
                item.Priority,
                item.CreatedUtc
            });
            entity.HasIndex(item => new
            {
                item.SlaBreachedUtc,
                item.ResolvedUtc,
                item.SlaTargetUtc
            });

            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureComments(ModelBuilder builder)
    {
        builder.Entity<CaseComment>(entity =>
        {
            entity.ToTable("CaseComments", "madar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Body)
                .HasMaxLength(2000)
                .IsRequired();
            entity.Property(item => item.CreatedUtc)
                .IsRequired();
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(item => new
            {
                item.CaseId,
                item.CreatedUtc,
                item.Id
            });

            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAttachments(ModelBuilder builder)
    {
        builder.Entity<CaseAttachment>(entity =>
        {
            entity.ToTable("CaseAttachments", "madar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(item => item.ContentType)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.SizeBytes)
                .IsRequired();
            entity.Property(item => item.StorageKey)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.CreatedUtc)
                .IsRequired();
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(item => item.StorageKey)
                .IsUnique();
            entity.HasIndex(item => new
            {
                item.CaseId,
                item.CreatedUtc,
                item.Id
            });

            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureApprovals(ModelBuilder builder)
    {
        builder.Entity<CaseApproval>(entity =>
        {
            entity.ToTable("CaseApprovals", "madar");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.RequestedUtc)
                .IsRequired();
            entity.Property(item => item.DecisionNotes)
                .HasMaxLength(1000);
            entity.Property(item => item.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(item => new
            {
                item.CaseId,
                item.RequestedUtc,
                item.Id
            });
            entity.HasIndex(item => item.RequestedByUserId);
            entity.HasIndex(item => item.ReviewedByUserId);

            entity.HasOne<Case>()
                .WithMany()
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MadarUser>()
                .WithMany()
                .HasForeignKey(item => item.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAudit(ModelBuilder builder)
    {
        builder.Entity<MadarAuditRecord>(entity =>
        {
            entity.ToTable("AuditEvents", "audit");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(item => item.SubjectType)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(item => item.SubjectId)
                .HasMaxLength(256);
            entity.Property(item => item.ActorId)
                .HasMaxLength(256);
            entity.Property(item => item.CorrelationId)
                .HasMaxLength(256);
            entity.Property(item => item.TenantId)
                .HasMaxLength(256);
            entity.Property(item => item.Source)
                .HasMaxLength(128);
            entity.Property(item => item.ReasonCode)
                .HasMaxLength(128);
            entity.Property(item => item.AttributesJson)
                .IsRequired();

            entity.HasIndex(item => new
            {
                item.SubjectType,
                item.SubjectId,
                item.OccurredAtUtc
            });
            entity.HasIndex(item => new
            {
                item.ActorId,
                item.OccurredAtUtc
            });
        });
    }
}
