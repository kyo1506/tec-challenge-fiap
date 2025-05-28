using Microsoft.EntityFrameworkCore.Storage;

namespace TecChallenge.Data.UnitOfWork;

public class UnitOfWork(AppDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;

    public async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _transaction = await context.Database.BeginTransactionAsync(ct);
        
        return _transaction;
    }

    public async Task<bool> CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction active");
        }

        try
        {
            var result = await context.SaveChangesAsync(ct) > 0;
            await _transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        DisposeTransactionAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
        GC.SuppressFinalize(this);
    }
}