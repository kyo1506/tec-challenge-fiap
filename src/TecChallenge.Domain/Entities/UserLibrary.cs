namespace TecChallenge.Domain.Entities;

public class UserLibrary : Entity
{
    public Guid UserId { get; set; }
    public ICollection<LibraryItem> Items { get; set; } = [];

    public void AddGame(Guid gameId, decimal purchasePrice)
    {
        var libraryItem = new LibraryItem
        {
            UserLibraryId = Id,
            GameId = gameId,
            PurchasedAt = DateTime.UtcNow,
            PurchasePrice = purchasePrice
        };

        Items.Add(libraryItem);
    }

    public LibraryItem? RemoveGame(Guid gameId)
    {
        var item = Items.FirstOrDefault(i => i.GameId == gameId);

        if (item == null) return null;

        Items.Remove(item);

        return item;
    }

    public static UserLibrary Create(Guid userId)
    {
        return new UserLibrary
        {
            UserId = userId
        };
    }
}