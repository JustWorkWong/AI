using Agent.Runtime.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Agent.Runtime.Tests;

public sealed class RuntimeApiFactory(string connectionString) : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AgentRuntimeDbContext>>();
            services.RemoveAll<AgentRuntimeDbContext>();
            services.AddDbContext<AgentRuntimeDbContext>(options => options.UseNpgsql(connectionString));
        });
    }
}
