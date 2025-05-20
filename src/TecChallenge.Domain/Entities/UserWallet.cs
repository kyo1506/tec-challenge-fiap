using TecChallenge.Domain.Exceptions;

namespace TecChallenge.Domain.Entities;

public class UserWallet(Guid userId) : Entity
{
    public Guid UserId { get; private set; } = userId;
    public decimal Balance { get; private set; }
    private readonly List<WalletTransaction> _transactions = [];
    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    public void PurchaseGame(Game game, PromotionGame? promotionGame, UserLibrary library)
    {
        var finalPrice = CalculateFinalPrice(game.Price, promotionGame?.DiscountPercentage);
        
        ValidatePurchase(finalPrice);

        Balance -= finalPrice;
        
        RegisterTransaction(
            gameId: game.Id,
            promotionGameId: promotionGame?.Id,
            amount: -finalPrice,
            description: $"Compra: {game.Name}" +
                         (promotionGame != null ? $" ({promotionGame.DiscountPercentage}% off)" : ""),
            type: ETransactionType.Purchase
        );

        library.AddGame(game.Id, finalPrice);
    }

    public void RefundGame(Game game, decimal refundAmount, UserLibrary library)
    {
        ValidateRefund(refundAmount);

        Balance += refundAmount;
        
        RegisterTransaction(
            gameId: game.Id,
            promotionGameId: null,
            amount: refundAmount,
            description: $"Reembolso: {game.Name}",
            type: ETransactionType.Refund
        );
        
        library.RemoveGame(game.Id);
    }

    public void Deposit(decimal amount)
    {
        ValidateAmount(amount, "depósito");

        Balance += amount;
        RegisterTransaction(
            gameId: null,
            promotionGameId: null,
            amount: amount,
            description: "Depósito de créditos",
            type: ETransactionType.Deposit
        );
    }

    public void Withdraw(decimal amount)
    {
        ValidateAmount(amount, "saque");
        ValidateSufficientBalance(amount);

        Balance -= amount;
        RegisterTransaction(
            gameId: null,
            promotionGameId: null,
            amount: -amount,
            description: "Saque de créditos",
            type: ETransactionType.Withdrawal
        );
    }

    private static decimal CalculateFinalPrice(decimal basePrice, decimal? discountPercentage)
    {
        return discountPercentage.HasValue
            ? basePrice * (1 - discountPercentage.Value / 100)
            : basePrice;
    }

    private void ValidatePurchase(decimal finalPrice)
    {
        if (finalPrice <= 0)
            throw new DomainException("Valor da compra inválido");

        ValidateSufficientBalance(finalPrice);
    }

    private static void ValidateRefund(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Valor de reembolso inválido");
    }

    private static void ValidateAmount(decimal amount, string operationType)
    {
        if (amount <= 0)
            throw new DomainException($"Valor para {operationType} deve ser positivo");
    }

    private void ValidateSufficientBalance(decimal amount)
    {
        if (Balance < amount)
            throw new InsufficientBalanceException(Balance, amount);
    }

    private void RegisterTransaction(
        Guid? gameId,
        Guid? promotionGameId,
        decimal amount,
        string description,
        ETransactionType type)
    {
        var transaction = WalletTransaction.Create(
            walletId: Id,
            gameId: gameId,
            promotionGameId: promotionGameId,
            amount: amount,
            description: description,
            type: type
        );

        _transactions.Add(transaction);
    }
}