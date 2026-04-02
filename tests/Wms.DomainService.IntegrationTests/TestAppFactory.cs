using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wms.DomainService.Persistence;
using Wms.DomainService.Seed;

namespace Wms.DomainService.IntegrationTests;

public static class TestAppFactory
{
    public static async Task<WebApplicationFactory<Program>> CreateDomainServiceAsync(
        string connectionString,
        Action<IServiceCollection>? configureServices = null)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<WmsDbContext>>();
                    services.AddDbContext<WmsDbContext>(options => options.UseNpgsql(connectionString));
                    configureServices?.Invoke(services);
                });
            });

        await DevelopmentBootstrapper.InitializeAsync(factory.Services, new StubHostEnvironment("Testing"));

        return factory;
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Wms.DomainService.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
