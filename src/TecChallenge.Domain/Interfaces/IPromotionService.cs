namespace TecChallenge.Domain.Interfaces;

public interface IPromotionService: IDisposable
{
    Task<bool> AddAsync(Promotion model, CancellationToken ct = default);
    Task<bool?> UpdateAsync(Guid id, Promotion model, CancellationToken ct = default);
    Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default);
}