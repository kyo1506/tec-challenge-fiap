namespace TecChallenge.Data.Repositories;

public class PromotionRepository(AppDbContext context) : Repository<Promotion>(context), IPromotionRepository
{
    public async Task<PromotionGame?> GetPromotionGameById(Guid promotionGameId)
    {
        return await context.PromotionGames.FirstOrDefaultAsync(pg => pg.Id == promotionGameId);
    }
}