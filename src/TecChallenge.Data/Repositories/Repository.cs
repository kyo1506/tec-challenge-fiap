using System.Linq.Expressions;

namespace TecChallenge.Data.Repositories;

public abstract class Repository<T>(DbContext context) : IRepository<T>
    where T : Entity
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        params Expression<Func<T, object>>[] includes
    )
    {
        var query = includes.Aggregate<Expression<Func<T, object>>?, IQueryable<T>>(
            _dbSet,
            (current, include) => current.Include(include)
        );

        query = !trackChanges ? query.AsNoTracking() : query.AsTracking();

        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<IReadOnlyList<T>> WhereAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes
    )
    {
        var query = includes.Aggregate<Expression<Func<T, object>>?, IQueryable<T>>(
            _dbSet,
            (current, include) => current.Include(include)
        );

        return await query.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
        context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Attach(T entity)
    {
        _dbSet.Attach(entity);
        context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await _dbSet.CountAsync(predicate, ct);
    }
}
