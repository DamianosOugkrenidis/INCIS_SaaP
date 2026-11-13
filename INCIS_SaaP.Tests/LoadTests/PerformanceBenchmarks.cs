using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Models;
using INCIS_SaaP.Infrastructure.Repository;
using INCIS_SaaP.Tests.Helpers;
using System.Diagnostics;
using Xunit;

namespace INCIS_SaaP.Tests.LoadTests;

/// <summary>
/// Performance benchmarks and load simulation tests
/// </summary>
public class PerformanceBenchmarks : IDisposable
{
    private readonly Data.INCISDbContext _context;

    public PerformanceBenchmarks()
    {
        _context = TestDbContextFactory.Create();
    }

    [Fact]
    public async Task BulkInsert_1000RawMaterials_ShouldCompleteUnder5Seconds()
    {
        var sw = Stopwatch.StartNew();

        var supplier = new Supplier { Name = "Bulk Supplier" };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var materials = Enumerable.Range(1, 1000).Select(i => new RawMaterial
        {
            Code = $"RM-{i:D5}",
            ProductName = $"Raw Material {i}",
            SupplierId = supplier.Id,
            Category = "EMOLLIENTS/OILS",
            Price = 10 + (i % 100),
            IsActive = true
        }).ToList();

        _context.RawMaterials.AddRange(materials);
        await _context.SaveChangesAsync();

        sw.Stop();
        sw.Elapsed.TotalSeconds.Should().BeLessThan(5, "bulk insert of 1000 records should be fast");

        var count = await _context.RawMaterials.CountAsync();
        count.Should().Be(1000);
    }

    [Fact]
    public async Task ConcurrentReads_ShouldHandleParallelQueries()
    {
        // Seed data
        for (int i = 0; i < 100; i++)
        {
            _context.Clients.Add(new Client { Name = $"Client {i}" });
        }
        await _context.SaveChangesAsync();

        var sw = Stopwatch.StartNew();

        // Simulate 50 concurrent reads
        var tasks = Enumerable.Range(1, 50).Select(async i =>
        {
            var clients = await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Take(20)
                .ToListAsync();
            return clients.Count;
        }).ToList();

        var results = await Task.WhenAll(tasks);

        sw.Stop();
        sw.Elapsed.TotalSeconds.Should().BeLessThan(3, "50 concurrent reads should complete quickly");
        results.All(r => r == 20).Should().BeTrue();
    }

    [Fact]
    public async Task ComplexQuery_FormulasWithRawMaterials_ShouldBeEfficient()
    {
        // Create formula with many raw materials
        var supplier = new Supplier { Name = "Complex Supplier" };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var materials = Enumerable.Range(1, 50).Select(i => new RawMaterial
        {
            Code = $"CRM-{i:D3}",
            ProductName = $"Complex Material {i}",
            SupplierId = supplier.Id
        }).ToList();
        _context.RawMaterials.AddRange(materials);
        await _context.SaveChangesAsync();

        var client = new Client { Name = "Complex Client" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var formulas = Enumerable.Range(1, 20).Select(i => new Formula
        {
            FormulaCode = $"CX-{i:D4}-T01",
            ProductName = $"Complex Product {i}",
            ClientId = client.Id
        }).ToList();
        _context.Formulas.AddRange(formulas);
        await _context.SaveChangesAsync();

        // Add raw materials to formulas
        foreach (var formula in formulas)
        {
            var randomMaterials = materials.Take(10 + (formula.Id % 10));
            decimal pctEach = 100m / randomMaterials.Count();
            foreach (var rm in randomMaterials)
            {
                _context.FormulaRawMaterials.Add(new FormulaRawMaterial
                {
                    FormulaId = formula.Id,
                    RawMaterialId = rm.Id,
                    Percentage = Math.Round(pctEach, 2),
                    Phase = "A",
                    IsActive = true
                });
            }
        }
        await _context.SaveChangesAsync();

        // Benchmark the complex query
        var sw = Stopwatch.StartNew();

        var result = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.Client)
            .Where(f => f.FormulaRawMaterials.Any(frm => frm.IsActive))
            .OrderByDescending(f => f.CreationDate)
            .Take(10)
            .ToListAsync();

        sw.Stop();
        sw.Elapsed.TotalSeconds.Should().BeLessThan(2, "complex formula query should be efficient");
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Repository_CountPerformance_ShouldBeEfficient()
    {
        // Seed 500 items
        var items = Enumerable.Range(1, 500).Select(i => new Client
        {
            Name = $"Perf Client {i}",
            IsActive = i % 3 != 0 // ~333 active
        }).ToList();
        _context.Clients.AddRange(items);
        await _context.SaveChangesAsync();

        var repo = new Repository<Client>(_context);
        var sw = Stopwatch.StartNew();

        var totalCount = await repo.CountAsync();
        var activeCount = await repo.CountAsync(c => c.IsActive);

        sw.Stop();
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(500);
        totalCount.Should().Be(500);
        activeCount.Should().BeGreaterThan(300);
    }

    [Fact]
    public async Task SearchPerformance_FilteredQuery_ShouldBeEfficient()
    {
        // Seed data
        var supplier = new Supplier { Name = "Search Supplier" };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var categories = new[] { "PRESERVATIVES", "UV FILTERS", "EMOLLIENTS/OILS", "ACTIVES-ANTI AGING", "SURFACTANTS/CLEANING" };
        var materials = Enumerable.Range(1, 200).Select(i => new RawMaterial
        {
            Code = $"SRM-{i:D4}",
            ProductName = $"Search Material {i} {(i % 2 == 0 ? "Premium" : "Standard")}",
            SupplierId = supplier.Id,
            Category = categories[i % categories.Length],
            IsActive = true
        }).ToList();
        _context.RawMaterials.AddRange(materials);
        await _context.SaveChangesAsync();

        var sw = Stopwatch.StartNew();

        // Complex search simulation
        var results = await _context.RawMaterials
            .Include(rm => rm.Supplier)
            .Where(rm => rm.IsActive
                && rm.ProductName.Contains("Premium")
                && rm.Category == "EMOLLIENTS/OILS")
            .OrderBy(rm => rm.ProductName)
            .Take(20)
            .ToListAsync();

        sw.Stop();
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(500, "filtered search should be fast");
        results.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
