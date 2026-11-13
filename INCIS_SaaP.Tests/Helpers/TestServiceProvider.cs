using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using INCIS_SaaP.Data;

namespace INCIS_SaaP.Tests.Helpers;

/// <summary>
/// Builds a DI ServiceProvider with an InMemory INCISDbContext (shared name across scopes),
/// for services that resolve dependencies via IServiceProvider.CreateScope().
/// </summary>
public static class TestServiceProvider
{
    public static ServiceProvider Create(out string dbName)
    {
        dbName = "svc-" + Guid.NewGuid();
        var name = dbName;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(accessor.Object);
        services.AddDbContext<INCISDbContext>(o => o.UseInMemoryDatabase(name));
        return services.BuildServiceProvider();
    }
}
