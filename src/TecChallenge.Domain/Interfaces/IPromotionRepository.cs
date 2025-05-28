namespace TecChallenge.Domain.Interfaces;

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<PromotionGame?> GetPromotionGameById(Guid promotionGameId);
}