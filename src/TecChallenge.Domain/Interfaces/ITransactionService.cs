using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Domain.Interfaces;

public interface ITransactionService
{
    Task<PurchaseResponse> ProcessPurchaseAsync(Guid userId, Guid gameId, Guid? promotionGameId = null, CancellationToken ct = default);
    Task<RefundResponse> RefundedPurchaseAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<DepositResponse> DepositAsync(Guid userId, decimal amount, CancellationToken ct = default);
    Task<WithdrawalResponse> WithdrawalAsync(Guid userId, decimal amount, CancellationToken ct = default);
}