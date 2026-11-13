using Microsoft.EntityFrameworkCore.Storage;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Infrastructure.Repository;

/// <summary>
/// Unit of Work implementation with EF Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly INCISDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<Formula>? _formulas;
    private IRepository<RawMaterial>? _rawMaterials;
    private IRepository<Client>? _clients;
    private IRepository<Supplier>? _suppliers;
    private IRepository<INCI>? _incis;
    private IRepository<Restriction>? _restrictions;
    private IRepository<FormulaRawMaterial>? _formulaRawMaterials;
    private IRepository<ProductionBatch>? _productionBatches;
    private IRepository<ProductionBatchLine>? _productionBatchLines;

    public UnitOfWork(INCISDbContext context)
    {
        _context = context;
    }

    public IRepository<Formula> Formulas => _formulas ??= new Repository<Formula>(_context);
    public IRepository<RawMaterial> RawMaterials => _rawMaterials ??= new Repository<RawMaterial>(_context);
    public IRepository<Client> Clients => _clients ??= new Repository<Client>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IRepository<INCI> INCIs => _incis ??= new Repository<INCI>(_context);
    public IRepository<Restriction> Restrictions => _restrictions ??= new Repository<Restriction>(_context);
    public IRepository<FormulaRawMaterial> FormulaRawMaterials => _formulaRawMaterials ??= new Repository<FormulaRawMaterial>(_context);
    public IRepository<ProductionBatch> ProductionBatches => _productionBatches ??= new Repository<ProductionBatch>(_context);
    public IRepository<ProductionBatchLine> ProductionBatchLines => _productionBatchLines ??= new Repository<ProductionBatchLine>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
