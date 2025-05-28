using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class UserWalletMapping : IEntityTypeConfiguration<UserWallet>
{
    public void Configure(EntityTypeBuilder<UserWallet> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.UserId).IsRequired();
        
        builder.Property(p => p.Balance).IsRequired().HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);
        
        builder.HasMany(p => p.Transactions)
            .WithOne(g => g.Wallet)
            .HasForeignKey(g => g.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.ToTable("UserWallet");
    }
}