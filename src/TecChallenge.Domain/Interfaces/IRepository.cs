using System.Linq.Expressions;

namespace TecChallenge.Domain.Interfaces;

public interface IRepository<T> where T : Entity
{
    // Operações básicas
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    // Consultas avançadas
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        params Expression<Func<T, object>>[] includes);

    Task<IReadOnlyList<T>> WhereAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes);

    // Operações de escrita
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Attach(T entity);
    void Delete(T entity);

    // Métodos utilitários
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}