using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;

namespace INCIS_SaaP.Infrastructure.Repository;

/// <summary>
/// Generic EF Core repository implementation
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly INCISDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(INCISDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        // Read-only listing: AsNoTracking για ταχύτητα & λιγότερη μνήμη (χωρίς change tracking).
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        // Read-only filtered listing: AsNoTracking.
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public virtual IQueryable<T> QueryNoTracking()
    {
        // Composable read-only query — προτίμησέ το για λίστες/reports/projections.
        return _dbSet.AsNoTracking();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
