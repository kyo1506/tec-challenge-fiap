using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class WalletTransactionMapping : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        
        builder.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(p => p.Type).IsRequired();

        builder.Property(p => p.Description).IsRequired().HasColumnType("VARCHAR").HasMaxLength(100);

        builder.HasIndex(x => new { x.WalletId, x.CreatedAt });

        builder.HasIndex(x => x.GameId);

        builder.ToTable("WalletTransaction");
    }
}