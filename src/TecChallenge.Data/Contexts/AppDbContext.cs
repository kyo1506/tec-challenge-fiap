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

        foreach (var entry in ChangeTracker.Entries().Where(e =>
                     e.State is EntityState.Added or EntityState.Modified))
        {
            var entity = entry.Entity;

            // CreatedAt
            if (entry.State == EntityState.Added && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
            {
                entry.Property("CreatedAt").CurrentValue = now;

                if (entry.Properties.Any(p => p.Metadata.Name == "IsActive"))
                {
                    entry.Property("IsActive").CurrentValue = true;
                }
            }

            // Impede alteração do CreatedAt
            if (entry.State == EntityState.Modified && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
            {
                entry.Property("CreatedAt").IsModified = false;
            }

            // UpdatedAt
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}