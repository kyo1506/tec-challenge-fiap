using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class LocalizationRecordsMapping : IEntityTypeConfiguration<LocalizationRecord>
{
    public void Configure(EntityTypeBuilder<LocalizationRecord> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key).IsRequired().HasColumnType("nvarchar(450)");

        builder.Property(p => p.Text).IsRequired().HasColumnType("nvarchar(max)");

        builder
            .Property(p => p.LocalizationCulture)
            .IsRequired()
            .HasColumnType("nvarchar(450)");

        builder.Property(p => p.ResourceKey).IsRequired().HasColumnType("nvarchar(450)");

        builder.ToTable("LocalizationRecord");
    }
}