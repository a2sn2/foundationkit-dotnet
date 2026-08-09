using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
public sealed class MadarDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Madar.Infrastructure.Auditing.MadarAuditRecord", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("Action")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("ActorId")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<string>("AttributesJson")
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            entity.Property<string>("CorrelationId")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<DateTimeOffset>("OccurredAtUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<int>("Outcome")
                .HasColumnType("int");
            entity.Property<string>("ReasonCode")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("Source")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("SubjectId")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<string>("SubjectType")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("TenantId")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.HasKey("Id");
            entity.HasIndex("ActorId", "OccurredAtUtc");
            entity.HasIndex("SubjectType", "SubjectId", "OccurredAtUtc");
            entity.ToTable("AuditEvents", "audit");
        });

        modelBuilder.Entity("Madar.Infrastructure.Identity.MadarUser", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<int>("AccessFailedCount").HasColumnType("int");
            entity.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("nvarchar(max)");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(120)
                .HasColumnType("nvarchar(120)");
            entity.Property<string>("Email")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<bool>("EmailConfirmed").HasColumnType("bit");
            entity.Property<bool>("LockoutEnabled").HasColumnType("bit");
            entity.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("datetimeoffset");
            entity.Property<string>("NormalizedEmail")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<string>("NormalizedUserName")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<string>("PasswordHash").HasColumnType("nvarchar(max)");
            entity.Property<string>("PhoneNumber").HasColumnType("nvarchar(max)");
            entity.Property<bool>("PhoneNumberConfirmed").HasColumnType("bit");
            entity.Property<string>("SecurityStamp").HasColumnType("nvarchar(max)");
            entity.Property<bool>("TwoFactorEnabled").HasColumnType("bit");
            entity.Property<string>("UserName")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail")
                .HasDatabaseName("EmailIndex");
            entity.HasIndex("NormalizedUserName")
                .IsUnique()
                .HasDatabaseName("UserNameIndex")
                .HasFilter("[NormalizedUserName] IS NOT NULL");
            entity.ToTable("Users", "identity");
        });

        modelBuilder.Entity("Madar.Domain.Cases.Case", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid?>("AssignedToUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("CaseType")
                .IsRequired()
                .HasMaxLength(80)
                .HasColumnType("nvarchar(80)");
            entity.Property<DateTimeOffset?>("ClosedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<Guid>("CreatedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("CreatedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<Guid?>("DepartmentId")
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(4000)
                .HasColumnType("nvarchar(4000)");
            entity.Property<DateTimeOffset?>("EscalatedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<string>("Priority")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");
            entity.Property<DateTimeOffset?>("ResolvedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("RoutedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            entity.Property<DateTimeOffset?>("SlaBreachedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("SlaTargetUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnType("nvarchar(30)");
            entity.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(160)
                .HasColumnType("nvarchar(160)");
            entity.Property<DateTimeOffset>("UpdatedUtc")
                .HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("AssignedToUserId", "Status", "UpdatedUtc");
            entity.HasIndex("CreatedByUserId", "CreatedUtc");
            entity.HasIndex("DepartmentId", "Status", "UpdatedUtc");
            entity.HasIndex("SlaBreachedUtc", "ResolvedUtc", "SlaTargetUtc");
            entity.HasIndex("Status", "Priority", "CreatedUtc");
            entity.ToTable("Cases", "madar");
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseApproval", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid>("CaseId")
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("DecisionNotes")
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)");
            entity.Property<DateTimeOffset?>("DecidedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<Guid?>("ReviewedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid>("RequestedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("RequestedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");
            entity.HasKey("Id");
            entity.HasIndex("CaseId", "RequestedUtc", "Id");
            entity.HasIndex("RequestedByUserId");
            entity.HasIndex("ReviewedByUserId");
            entity.ToTable("CaseApprovals", "madar");
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseAttachment", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid>("CaseId")
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("ContentType")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");
            entity.Property<DateTimeOffset>("CreatedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<string>("OriginalFileName")
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("nvarchar(255)");
            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            entity.Property<long>("SizeBytes")
                .HasColumnType("bigint");
            entity.Property<string>("StorageKey")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");
            entity.Property<Guid>("UploadedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("CaseId", "CreatedUtc", "Id");
            entity.HasIndex("StorageKey")
                .IsUnique();
            entity.HasIndex("UploadedByUserId");
            entity.ToTable("CaseAttachments", "madar");
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseComment", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid>("AuthorUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("Body")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("nvarchar(2000)");
            entity.Property<Guid>("CaseId")
                .HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("CreatedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            entity.HasKey("Id");
            entity.HasIndex("AuthorUserId");
            entity.HasIndex("CaseId", "CreatedUtc", "Id");
            entity.ToTable("CaseComments", "madar");
        });

        modelBuilder.Entity("Madar.Domain.Organization.Department", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(60)
                .HasColumnType("nvarchar(60)");
            entity.Property<DateTimeOffset>("CreatedUtc")
                .HasColumnType("datetimeoffset");
            entity.Property<bool>("IsActive")
                .HasColumnType("bit");
            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(120)
                .HasColumnType("nvarchar(120)");
            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");
            entity.Property<DateTimeOffset>("UpdatedUtc")
                .HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("Code")
                .IsUnique();
            entity.ToTable("Departments", "madar");
        });

        modelBuilder.Entity("Madar.Domain.Organization.DepartmentMembership", entity =>
        {
            entity.Property<Guid>("DepartmentId")
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid>("UserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("JoinedUtc")
                .HasColumnType("datetimeoffset");
            entity.HasKey("DepartmentId", "UserId");
            entity.HasIndex("UserId", "DepartmentId");
            entity.ToTable("DepartmentMemberships", "madar");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("nvarchar(max)");
            entity.Property<string>("Name")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.Property<string>("NormalizedName")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedName")
                .IsUnique()
                .HasDatabaseName("RoleNameIndex")
                .HasFilter("[NormalizedName] IS NOT NULL");
            entity.ToTable("Roles", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property<string>("ClaimType").HasColumnType("nvarchar(max)");
            entity.Property<string>("ClaimValue").HasColumnType("nvarchar(max)");
            entity.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("RoleId");
            entity.ToTable("RoleClaims", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property<string>("ClaimType").HasColumnType("nvarchar(max)");
            entity.Property<string>("ClaimValue").HasColumnType("nvarchar(max)");
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("UserClaims", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", entity =>
        {
            entity.Property<string>("LoginProvider")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("ProviderKey")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("ProviderDisplayName")
                .HasColumnType("nvarchar(max)");
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.HasKey("LoginProvider", "ProviderKey");
            entity.HasIndex("UserId");
            entity.ToTable("UserLogins", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", entity =>
        {
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            entity.HasKey("UserId", "RoleId");
            entity.HasIndex("RoleId");
            entity.ToTable("UserRoles", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", entity =>
        {
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.Property<string>("LoginProvider")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("Name")
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");
            entity.Property<string>("Value").HasColumnType("nvarchar(max)");
            entity.HasKey("UserId", "LoginProvider", "Name");
            entity.ToTable("UserTokens", "identity");
        });

        modelBuilder.Entity("Madar.Domain.Cases.Case", entity =>
        {
            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("AssignedToUserId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("Madar.Domain.Organization.Department", null)
                .WithMany()
                .HasForeignKey("DepartmentId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseApproval", entity =>
        {
            entity.HasOne("Madar.Domain.Cases.Case", null)
                .WithMany()
                .HasForeignKey("CaseId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("RequestedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("ReviewedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseAttachment", entity =>
        {
            entity.HasOne("Madar.Domain.Cases.Case", null)
                .WithMany()
                .HasForeignKey("CaseId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UploadedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Madar.Domain.Cases.CaseComment", entity =>
        {
            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("AuthorUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("Madar.Domain.Cases.Case", null)
                .WithMany()
                .HasForeignKey("CaseId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Madar.Domain.Organization.DepartmentMembership", entity =>
        {
            entity.HasOne("Madar.Domain.Organization.Department", null)
                .WithMany()
                .HasForeignKey("DepartmentId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", entity =>
        {
            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", entity =>
        {
            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", entity =>
        {
            entity.HasOne("Madar.Infrastructure.Identity.MadarUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
