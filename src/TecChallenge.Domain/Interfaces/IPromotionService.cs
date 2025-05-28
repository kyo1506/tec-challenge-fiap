namespace TecChallenge.Domain.Interfaces;

public interface IPromotionService : IDisposable
{
    Task<bool> AddAsync(Promotion model, CancellationToken ct = default);
    Task<bool?> UpdateAsync(Guid id, Promotion model, CancellationToken ct = default);

    Task<bool?> AddGamesOnSaleAsync(Guid id, List<PromotionGame> gamesOnSale,
        CancellationToken ct = default);

    Task<bool?> UpdatePromotionGameAsync(Guid id, PromotionGame model,
        CancellationToken ct = default);

    Task<bool?> DeletePromotionGameAsync(Guid id, CancellationToken ct = default);
    Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default);
}