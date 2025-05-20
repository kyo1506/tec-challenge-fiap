namespace TecChallenge.Data.Repositories;

public class UserWalletRepository(AppDbContext context) : Repository<UserWallet>(context), IUserWalletRepository;