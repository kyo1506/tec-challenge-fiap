namespace TecChallenge.Domain.Entities;

public class WalletTransaction : Entity
{
    public Guid WalletId { get; private set; }
    public Guid? GameId { get; private set; }
    public Guid? PromotionGameId { get; private set; }
    public decimal Amount { get; private set; }
    public ETransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navegações (EF Core)
    public virtual Game? Game { get; private set; } = null!;
    public virtual PromotionGame? PromotionGame { get; private set; }
    public virtual UserWallet Wallet { get; private set; } = null!;

    // Método Fábrica
    public static WalletTransaction Create(
        Guid walletId,
        Guid? gameId,
        Guid? promotionGameId,
        decimal amount,
        string description,
        ETransactionType type)
    {
        return new WalletTransaction
        {
            WalletId = walletId,
            GameId = gameId,
            PromotionGameId = promotionGameId,
            Amount = amount,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}