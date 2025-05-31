namespace TecChallenge.Data.Contexts;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts)
        : base(opts)
    {
        ChangeTracker.AutoDetectChangesEnabled = true;
    }

    public DbSet<Log> Logs { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<UserLibrary> UserLibraries { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<LibraryItem> LibraryItems { get; set; }
    public DbSet<PromotionGame> PromotionGames { get; set; }
    public DbSet<UserWallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (
            var property in modelBuilder
                .Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties().Where(p => p.ClrType == typeof(string)))
        )
            property.SetColumnType("varchar(100)");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            switch (entry.State)
            {
                case EntityState.Added:
                {
                    // Seta CreatedAt se existir
                    if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    {
                        entry.Property("CreatedAt").CurrentValue = now;
                    }

                    // Seta IsActive como true se existir
                    if (entry.Properties.Any(p => p.Metadata.Name == "IsActive"))
                    {
                        entry.Property("IsActive").CurrentValue = true;
                    }

                    break;
                }
                case EntityState.Modified:
                {
                    if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    {
                        entry.Property("CreatedAt").IsModified = false;
                    }
                
                    if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    {
                        if (entry.Properties.Any(p => p.IsModified && p.Metadata.Name != "UpdatedAt"))
                        {
                            entry.Property("UpdatedAt").CurrentValue = now;
                        }
                        else
                        {
                            entry.Property("UpdatedAt").IsModified = false;
                        }
                    }

                    break;
                }
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}