namespace TecChallenge.Data.Repositories;

public class WalletTransactionRepository(AppDbContext context)
    : Repository<WalletTransaction>(context), IWalletTransactionRepository;