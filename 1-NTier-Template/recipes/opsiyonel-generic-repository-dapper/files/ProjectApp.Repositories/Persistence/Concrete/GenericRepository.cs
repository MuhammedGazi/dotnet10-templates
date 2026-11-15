using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Repositories;

public class GenericRepository<T, TId>(AppDbContext context)
    : IGenericRepository<T, TId> where T : BaseEntity<TId> where TId : struct
{
    private readonly DbSet<T> _dbSet = context.Set<T>();
    protected AppDbContext Context { get; } = context;

    public Task<bool> AnyAsync(TId id, CancellationToken cancellationToken = default)
    {
        return _dbSet.AnyAsync(x => x.Id.Equals(id), cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public IQueryable<T> GetAll()
    {
        return _dbSet.AsQueryable().AsNoTracking();
    }

    public IQueryable<T> Where(Expression<Func<T, bool>> predicate)
    {
        return _dbSet.Where(predicate).AsNoTracking();
    }

    public ValueTask<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return _dbSet.FindAsync([id], cancellationToken);
    }

    public async ValueTask AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void SetOriginalRowVersion(T entity, byte[] rowVersion)
    {
        Context.Entry(entity).Property(nameof(AuditableEntity.RowVersion)).OriginalValue = rowVersion;
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
