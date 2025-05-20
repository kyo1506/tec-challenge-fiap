namespace TecChallenge.Domain.Entities;

public class Promotion : Entity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ICollection<PromotionGame> GamesOnSale { get; set; } = [];
}