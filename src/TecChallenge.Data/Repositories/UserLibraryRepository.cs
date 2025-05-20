namespace TecChallenge.Data.Repositories;

public class UserLibraryRepository(AppDbContext context)
    : Repository<UserLibrary>(context),
        IUserLibraryRepository
{
    public void RemoveLibraryItem(LibraryItem item, CancellationToken ct = default)
    {
        context.LibraryItems.Remove(item);
    }
}