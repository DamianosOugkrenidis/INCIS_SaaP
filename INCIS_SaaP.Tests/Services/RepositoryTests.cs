using FluentAssertions;
using INCIS_SaaP.Infrastructure.Repository;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Integration tests for the generic repository pattern
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly Repository<Client> _repository;

    public RepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new Repository<Client>(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        var client = new Client { Name = "Test Client", Email = "test@test.com" };

        await _repository.AddAsync(client);
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(client.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Client");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        _context.Clients.Add(new Client { Name = "Client 1" });
        _context.Clients.Add(new Client { Name = "Client 2" });
        _context.Clients.Add(new Client { Name = "Client 3" });
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task FindAsync_ShouldFilterByPredicate()
    {
        _context.Clients.Add(new Client { Name = "Alpha Corp", IsActive = true });
        _context.Clients.Add(new Client { Name = "Beta Inc", IsActive = false });
        _context.Clients.Add(new Client { Name = "Alpha Ltd", IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _repository.FindAsync(c => c.Name.Contains("Alpha"));

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        _context.Clients.Add(new Client { Name = "A" });
        _context.Clients.Add(new Client { Name = "B" });
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ShouldReturnFilteredCount()
    {
        _context.Clients.Add(new Client { Name = "Active", IsActive = true });
        _context.Clients.Add(new Client { Name = "Inactive", IsActive = false });
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync(c => c.IsActive);

        count.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueWhenExists()
    {
        _context.Clients.Add(new Client { Name = "Existing" });
        await _context.SaveChangesAsync();

        var exists = await _repository.ExistsAsync(c => c.Name == "Existing");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseWhenNotExists()
    {
        var exists = await _repository.ExistsAsync(c => c.Name == "NonExistent");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldModifyEntity()
    {
        var client = new Client { Name = "Original" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        client.Name = "Updated";
        _repository.Update(client);
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(client.Id);
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Remove_ShouldDeleteEntity()
    {
        var client = new Client { Name = "ToDelete" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _repository.Remove(client);
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(client.Id);
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
