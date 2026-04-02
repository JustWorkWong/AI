using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;

namespace Ops.Bff.Tests.TestHost;

internal sealed class BffTestApplicationFactory(
    IDomainServiceClient domainServiceClient,
    IAgentRuntimeClient agentRuntimeClient) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDomainServiceClient>();
            services.RemoveAll<IAgentRuntimeClient>();
            services.AddSingleton(domainServiceClient);
            services.AddSingleton(agentRuntimeClient);
        });
    }
}
