using TecChallenge.Domain.Entities.Validations;

namespace TecChallenge.Domain.Services;

public class GameService(INotifier notifier, IGameRepository gameRepository, IUnitOfWork unitOfWork)
    : BaseService(notifier),
        IGameService
{
    public async Task<bool> AddAsync(Game model, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        
        try
        {
            if (!ExecuteValidation(new GameValidation(), model))
                return false;

            if (await gameRepository.AnyAsync(x => x.Name == model.Name, ct))
            {
                Notify("There is already a game with this name in the records");
                return false;
            }

            await gameRepository.AddAsync(model, ct);

            return await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool?> UpdateAsync(Guid id, Game model, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        
        try
        {
            if (!ExecuteValidation(new GameValidation(), model))
                return false;

            var game = await gameRepository.GetByIdAsync(id, ct);

            if (game == null)
            {
                Notify("Game not found");
                return null;
            }

            if (await gameRepository.AnyAsync(x => x.Name == model.Name && x.Id != id, ct))
            {
                Notify("There is already a game with this name in the records");
                return false;
            }

            game.Name = model.Name;
            game.Price = model.Price;
            game.IsActive = model.IsActive;

            gameRepository.Update(game);

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
            var game = await gameRepository.GetByIdAsync(id, ct);

            if (game == null)
            {
                Notify("Game not found");
                return null;
            }

            game.IsActive = false;

            gameRepository.Update(game);

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