using FluentAssertions;
using INCIS_SaaP.Infrastructure.Repository;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Infrastructure;

/// <summary>
/// Tests for the UnitOfWork repository aggregator.
/// </summary>
public class UnitOfWorkTests
{
    [Fact]
    public void RepositoryProperties_AreLazilyCreated_AndCached()
    {
        using var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);

        uow.Formulas.Should().NotBeNull();
        uow.RawMaterials.Should().NotBeNull();
        uow.Clients.Should().NotBeNull();
        uow.Suppliers.Should().NotBeNull();
        uow.INCIs.Should().NotBeNull();
        uow.Restrictions.Should().NotBeNull();
        uow.FormulaRawMaterials.Should().NotBeNull();
        uow.ProductionBatches.Should().NotBeNull();
        uow.ProductionBatchLines.Should().NotBeNull();

        // Second access returns the same cached instance
        uow.Formulas.Should().BeSameAs(uow.Formulas);
        uow.RawMaterials.Should().BeSameAs(uow.RawMaterials);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsEntities()
    {
        using var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);

        await uow.Clients.AddAsync(new Client { Name = "Uow Client" });
        var affected = await uow.SaveChangesAsync();

        affected.Should().BeGreaterThan(0);
        (await uow.Clients.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CommitAndRollback_WithoutActiveTransaction_AreNoOps()
    {
        using var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);

        // No transaction started -> should not throw
        await uow.CommitTransactionAsync();
        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);

        Action act = () => uow.Dispose();
        act.Should().NotThrow();
    }
}
