using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class PromotionGameMapping : IEntityTypeConfiguration<PromotionGame>
{
    public void Configure(EntityTypeBuilder<PromotionGame> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.PromotionId).IsRequired();

        builder.Property(p => p.GameId).IsRequired();

        builder.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)")
            .HasPrecision(5, 2);

        builder.HasMany(p => p.WalletTransactions)
            .WithOne(g => g.PromotionGame)
            .HasForeignKey(g => g.PromotionGameId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.ToTable("PromotionGame");
    }
}