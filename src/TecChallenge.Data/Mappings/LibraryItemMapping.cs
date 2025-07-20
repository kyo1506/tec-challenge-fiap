using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class LibraryItemMapping : IEntityTypeConfiguration<LibraryItem>
{
    public void Configure(EntityTypeBuilder<LibraryItem> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.UserLibraryId).IsRequired();

        builder.Property(p => p.GameId).IsRequired();

        builder.Property(p => p.PurchasedAt).IsRequired().HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.Property(p => p.PurchasePrice).IsRequired().HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.ToTable("LibraryItem");
    }
}