using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using INCIS_SaaP.Data;

namespace INCIS_SaaP.Tests.Helpers;

/// <summary>
/// Factory for creating InMemory DbContext for unit tests
/// </summary>
public static class TestDbContextFactory
{
    public static INCISDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<INCISDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var context = new INCISDbContext(options, httpContextAccessor.Object);
        context.Database.EnsureCreated();
        return context;
    }
}
