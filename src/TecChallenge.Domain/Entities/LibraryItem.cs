namespace TecChallenge.Domain.Entities;

public class LibraryItem : Entity
{
    public Guid UserLibraryId { get; set; }
    public Guid GameId { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public decimal PurchasePrice { get; set; }
    
    // EF Mapping
    public virtual Game Game { get; set; } = null!;
    public virtual UserLibrary UserLibrary { get; set; } = null!;
}