namespace TecChallenge.Domain.Interfaces;

public interface IUserLibraryService : IDisposable
{
    Task<bool> AddAsync(UserLibrary model, CancellationToken ct = default);
}