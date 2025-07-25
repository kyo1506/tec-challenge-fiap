using TecChallenge.Domain.Entities.Validations;

namespace TecChallenge.Domain.Services;

public class UserLibraryService(
    INotifier notifier,
    IUserLibraryRepository userLibraryRepository,
    IUnitOfWork unitOfWork
) : BaseService(notifier), IUserLibraryService
{
    public async Task<bool> AddAsync(UserLibrary model, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            if (!ExecuteValidation(new UserLibraryValidation(), model))
                return false;

            if (userLibraryRepository.WhereAsync(x => x.UserId == model.UserId).Result.Any())
            {
                Notify("There is already a library created for this user");
                return false;
            }

            await userLibraryRepository.AddAsync(model, ct);

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
