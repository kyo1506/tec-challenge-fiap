namespace TecChallenge.Domain.Entities;

public class Game : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Coleção inicializada
    public virtual ICollection<LibraryItem> LibraryItems { get; set; } = [];
    public virtual ICollection<PromotionGame> GamesOnSale { get; set; } = [];
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
}