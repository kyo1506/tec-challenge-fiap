namespace TecChallenge.Data.Repositories;

public class GameRepository(AppDbContext context)
    : Repository<Game>(context),
        IGameRepository;