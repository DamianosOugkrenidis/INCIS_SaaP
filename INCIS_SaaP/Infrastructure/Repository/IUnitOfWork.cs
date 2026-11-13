using INCIS_SaaP.Models;

namespace INCIS_SaaP.Infrastructure.Repository;

/// <summary>
/// Unit of Work pattern - coordinates multiple repositories under a single transaction
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<Formula> Formulas { get; }
    IRepository<RawMaterial> RawMaterials { get; }
    IRepository<Client> Clients { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<INCI> INCIs { get; }
    IRepository<Restriction> Restrictions { get; }
    IRepository<FormulaRawMaterial> FormulaRawMaterials { get; }
    IRepository<ProductionBatch> ProductionBatches { get; }
    IRepository<ProductionBatchLine> ProductionBatchLines { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
