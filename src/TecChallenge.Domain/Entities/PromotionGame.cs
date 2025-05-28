namespace TecChallenge.Domain.Entities;

public class PromotionGame : Entity
{
    public Guid PromotionId { get; set; }
    public Guid GameId { get; set; }
    public decimal DiscountPercentage { get; set; }

    //EF Mapping
    public virtual Game Game { get; set; } = null!;
    public virtual Promotion Promotion { get; set; } = null!;
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
}