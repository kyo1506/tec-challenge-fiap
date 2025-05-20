namespace TecChallenge.Domain.Interfaces;

public interface IUserLibraryRepository : IRepository<UserLibrary>
{
    void RemoveLibraryItem(LibraryItem item, CancellationToken ct = default);
}