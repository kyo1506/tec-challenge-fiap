using TecChallenge.Domain.Entities.Validations;

namespace TecChallenge.Domain.Services;

public class PromotionService(
    INotifier notifier,
    IPromotionRepository promotionRepository,
    IPromotionGameRepository promotionGameRepository,
    IUnitOfWork unitOfWork)
    : BaseService(notifier), IPromotionService
{
    public async Task<bool> AddAsync(Promotion model, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            if (!ExecuteValidation(new PromotionValidation(), model))
                return false;

            if (await promotionRepository.AnyAsync(x => x.Name == model.Name, ct))
            {
                Notify("There is already a promotion with this name in the records");
                return false;
            }

            await promotionRepository.AddAsync(model, ct);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool?> UpdateAsync(Guid id, Promotion model, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            if (!ExecuteValidation(new PromotionValidation(), model))
                return false;

            var promotion =
                await promotionRepository.FirstOrDefaultAsync(x => x.Id == id, true);

            if (promotion == null)
            {
                Notify("Promotion not found");
                return null;
            }

            if (await promotionRepository.AnyAsync(x => x.Name == model.Name && x.Id != id, ct))
            {
                Notify("There is already a promotion with this name in the records");
                return false;
            }

            promotion.Name = model.Name;
            promotion.StartDate = model.StartDate;
            promotion.EndDate = model.EndDate;

            promotionRepository.Update(promotion);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool?> AddGamesOnSaleAsync(Guid id, List<PromotionGame> gamesOnSale,
        CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var promotion =
                await promotionRepository.FirstOrDefaultAsync(x => x.Id == id, true, includes: x => x.GamesOnSale);

            if (promotion == null)
            {
                Notify("Promotion not found");
                return null;
            }

            gamesOnSale = gamesOnSale.Where(g => !promotion.GamesOnSale.Contains(g)).ToList();
            
            if (gamesOnSale.Count == 0)
            {
                Notify("All items passed in the list already exist in the entity");
                return false;
            }
            
            foreach (var game in gamesOnSale)
            {
                var isGameInOtherPromotion = await promotionGameRepository.AnyAsync(
                    x => x.GameId == game.GameId 
                         && x.Promotion.EndDate >= promotion.StartDate 
                         && x.Promotion.StartDate <= promotion.EndDate,
                    ct);
    
                if (isGameInOtherPromotion)
                {
                    Notify($"O jogo {game.GameId} já está em outra promoção neste período");
                    return false;
                }
                
                promotion.GamesOnSale.Add(game);
            }

            promotionRepository.Update(promotion);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool?> UpdatePromotionGameAsync(Guid id, PromotionGame model,
        CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var promotionGame =
                await promotionGameRepository.FirstOrDefaultAsync(x => x.Id == id, true);

            if (promotionGame == null)
            {
                Notify("Promotion item not found");
                return null;
            }

            if (await promotionGameRepository.AnyAsync(
                    x => x.PromotionId == model.PromotionId && x.GameId == model.GameId && x.PromotionId != id, ct))
            {
                Notify("There is already a promotion with this name in the records");
                return false;
            }

            promotionGame.PromotionId = promotionGame.PromotionId;
            promotionGame.GameId = promotionGame.GameId;
            promotionGame.DiscountPercentage = model.DiscountPercentage;

            promotionGameRepository.Update(promotionGame);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool?> DeletePromotionGameAsync(Guid id, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var promotionGame = await promotionGameRepository.FirstOrDefaultAsync(x => x.Id == id, true);

            if (promotionGame == null)
            {
                Notify("Promotion not found");
                return null;
            }

            if (promotionGame.WalletTransactions.Count != 0)
            {
                Notify("Promotion item has transactions");
                return false;
            }

            promotionGameRepository.Delete(promotionGame);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }


    public async Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var promotion = await promotionRepository.GetByIdAsync(id, ct);

            if (promotion == null)
            {
                Notify("Promotion not found");
                return null;
            }

            if (promotion.GamesOnSale.Count != 0)
            {
                Notify("Promotion cannot be deleted");
                return null;
            }

            promotionRepository.Delete(promotion);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}