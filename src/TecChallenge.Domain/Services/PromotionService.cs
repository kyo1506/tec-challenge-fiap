using TecChallenge.Domain.Entities.Validations;

namespace TecChallenge.Domain.Services;

public class PromotionService(INotifier notifier, IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
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
                Notify("Já existe uma promoção com este nome nos registros");
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

            var promotion = await promotionRepository.FirstOrDefaultAsync(x => x.Id == id, true, includes: x => x.GamesOnSale);

            if (promotion == null)
            {
                Notify("Promotion not found");
                return null;
            }

            if (await promotionRepository.AnyAsync(x => x.Name == model.Name && x.Id != id, ct))
            {
                Notify("Já existe uma promoção com este nome nos registros");
                return false;
            }

            promotion.Name = model.Name;
            promotion.StartDate = model.StartDate;
            promotion.EndDate = model.EndDate;
            promotion.GamesOnSale = model.GamesOnSale;

            promotionRepository.Update(promotion);

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
                Notify("Game not found");
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