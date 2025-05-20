using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TecChallenge.Data.Mappings;

public class PromotionMapping : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasColumnType("VARCHAR").HasMaxLength(100);

        builder.Property(p => p.StartDate).IsRequired();

        builder.Property(p => p.EndDate).IsRequired();
        
        builder.HasMany(p => p.GamesOnSale)
            .WithOne(g => g.Promotion)
            .HasForeignKey(g => g.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Promotion");
    }
}