using TecChallenge.Domain.Exceptions;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Domain.Services;

public class TransactionService(
    IUserWalletRepository walletRepository,
    IUserLibraryRepository libraryRepository,
    IGameRepository gameRepository,
    IPromotionRepository promotionRepository,
    IWalletTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork) : ITransactionService
{
    public async Task<PurchaseResponse> ProcessPurchaseAsync(
        Guid userId,
        Guid gameId,
        Guid? promotionGameId = null,
        CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var wallet =
                await walletRepository.FirstOrDefaultAsync(x => x.UserId == userId, true,
                    includes: x => x.Transactions);
            var library =
                await libraryRepository.FirstOrDefaultAsync(x => x.UserId == userId, true, includes: x => x.Items);
            var game = await gameRepository.FirstOrDefaultAsync(x => x.Id == gameId, true);

            PromotionGame? promotionGame = null;

            if (promotionGameId.HasValue)
            {
                promotionGame = await promotionRepository
                    .GetPromotionGameById(promotionGameId.Value, gameId);

                if (promotionGame == null)
                    throw new PromotionNotApplicableException("Promoção não aplicável a este jogo");
            }

            wallet.PurchaseGame(game, promotionGame, library);

            await unitOfWork.CommitAsync(ct);

            return new PurchaseResponse
            {
                GameName = game?.Name,
                DiscountPercentage = promotionGame?.DiscountPercentage,
                Price = game?.Price,
                Balance = wallet?.Balance
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<RefundResponse> RefundedPurchaseAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var wallet =
                await walletRepository.FirstOrDefaultAsync(x => x.UserId == userId, true,
                    includes: x => x.Transactions);

            var library =
                await libraryRepository.FirstOrDefaultAsync(x => x.UserId == userId, true, includes: x => x.Items);

            var game = await gameRepository.FirstOrDefaultAsync(x => x.Id == gameId, true);

            DomainException.ThrowIfNull(wallet, "Carteira não encontrada");
            DomainException.ThrowIfNull(library, "Biblioteca não encontrada");
            DomainException.ThrowIfNull(game, "Jogo não encontrado");

            // Encontra a transação original de compra
            var originalTransaction = wallet.Transactions
                .FirstOrDefault(t => t.GameId == gameId && t.Type == ETransactionType.Purchase);

            if (originalTransaction == null)
                throw new DomainException("Transação de compra não encontrada");

            wallet.RefundGame(game, Math.Abs(originalTransaction.Amount), library);
            
            var removedItem = library.RemoveGame(game.Id);
            
            if (removedItem != null)
            {
                libraryRepository.RemoveLibraryItem(removedItem, ct);
            }

            await unitOfWork.CommitAsync(ct);

            return new RefundResponse
            {
                GameName = game.Name,
                RefundAmount = Math.Abs(originalTransaction.Amount),
                NewBalance = wallet.Balance
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<DepositResponse> DepositAsync(Guid userId, decimal amount, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var wallet = await walletRepository.FirstOrDefaultAsync(x => x.UserId == userId, true, x => x.Transactions);

            DomainException.ThrowIfNull(wallet, "Carteira não encontrada");

            wallet.Deposit(amount);

            await unitOfWork.CommitAsync(ct);

            return new DepositResponse
            {
                Amount = amount,
                NewBalance = wallet.Balance
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<WithdrawalResponse> WithdrawalAsync(Guid userId, decimal amount, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var wallet = await walletRepository.FirstOrDefaultAsync(x => x.UserId == userId, true, x => x.Transactions);

            DomainException.ThrowIfNull(wallet, "Carteira não encontrada");

            wallet.Withdraw(amount);

            await unitOfWork.CommitAsync(ct);

            return new WithdrawalResponse
            {
                Amount = amount,
                NewBalance = wallet.Balance
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}