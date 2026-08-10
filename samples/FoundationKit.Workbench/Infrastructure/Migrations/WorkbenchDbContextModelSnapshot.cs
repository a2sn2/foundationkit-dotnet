using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace FoundationKit.Workbench.Infrastructure.Migrations;

[DbContext(typeof(WorkbenchDbContext))]
public partial class WorkbenchDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("FoundationKit.Workbench.Domain.BuildBrief", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<string>("Audience").IsRequired().HasMaxLength(300).HasColumnType("nvarchar(300)");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Goal").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<string>("Notes").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            entity.Property<string>("Priorities").IsRequired().HasMaxLength(800).HasColumnType("nvarchar(800)");
            entity.Property<string>("ProjectName").IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)");
            entity.Property<string>("ProjectType").IsRequired().HasMaxLength(80).HasColumnType("nvarchar(80)");
            entity.Property<string>("SelectedCapabilityIdsJson").IsRequired().HasColumnType("nvarchar(max)");
            entity.Property<BuildBriefStatus>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("nvarchar(32)");
            entity.Property<DateTimeOffset>("UpdatedUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("CreatedUtc");
            entity.HasIndex("Status");
            entity.ToTable("BuildBriefs");
        });

        modelBuilder.Entity("FoundationKit.Workbench.Domain.AdminReview", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("BuildBriefId").HasColumnType("uniqueidentifier");
            entity.Property<AdminReviewDecision>("Decision").HasConversion<string>().HasMaxLength(20).HasColumnType("nvarchar(20)");
            entity.Property<string>("Notes").IsRequired().HasMaxLength(1200).HasColumnType("nvarchar(1200)");
            entity.Property<string>("ReviewedBy").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<DateTimeOffset>("ReviewedUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("BuildBriefId");
            entity.HasIndex("ReviewedUtc");
            entity.ToTable("AdminReviews");
        });

        modelBuilder.Entity("FoundationKit.Workbench.Domain.CoreCrudRecord", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<int>("Version").IsConcurrencyToken().HasColumnType("int");
            entity.HasKey("Id");
            entity.HasIndex("Name");
            entity.ToTable("CoreCrudRecords");
        });

        modelBuilder.Entity("FoundationKit.Workbench.Domain.AdminReview", entity =>
        {
            entity.HasOne("FoundationKit.Workbench.Domain.BuildBrief", null)
                .WithMany()
                .HasForeignKey("BuildBriefId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
