using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class GameMapping : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasColumnType("VARCHAR").HasMaxLength(100);

        builder.Property(p => p.Price).IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.Property(p => p.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => x.Name, "IX_Game_Name");

        builder.HasMany(p => p.LibraryItems)
            .WithOne(l => l.Game)
            .HasForeignKey(l => l.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.GamesOnSale)
            .WithOne(l => l.Game)
            .HasForeignKey(l => l.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.WalletTransactions)
            .WithOne(g => g.Game)
            .HasForeignKey(g => g.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Game");
    }
}