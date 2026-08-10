using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class CoreCrudRecordConfiguration : IEntityTypeConfiguration<CoreCrudRecord>
{
    public void Configure(EntityTypeBuilder<CoreCrudRecord> builder)
    {
        builder.ToTable("CoreCrudRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Name).HasMaxLength(120).IsRequired();
        builder.Property(record => record.Version).IsConcurrencyToken().IsRequired();
        builder.Property(record => record.CreatedUtc).IsRequired();
        builder.HasIndex(record => record.Name);
    }
}
