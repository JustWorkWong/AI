using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.IntegrationTests;

public static class TestAppFactory
{
    public static async Task<WebApplicationFactory<Program>> CreateDomainServiceAsync(string connectionString)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<WmsDbContext>>();
                    services.AddDbContext<WmsDbContext>(options => options.UseNpgsql(connectionString));
                });
            });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        await db.Database.EnsureCreatedAsync();

        return factory;
    }
}
