namespace TecChallenge.Data.Repositories;

public class PromotionGameRepository(AppDbContext context)
    : Repository<PromotionGame>(context), IPromotionGameRepository;