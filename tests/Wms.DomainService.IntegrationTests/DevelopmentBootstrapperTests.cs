using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;
using Wms.DomainService.Persistence;
using Wms.DomainService.Seed;

namespace Wms.DomainService.IntegrationTests;

public sealed class DevelopmentBootstrapperTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public DevelopmentBootstrapperTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Initialize_should_seed_demo_return_flow_for_development()
    {
        await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);

        await DevelopmentBootstrapper.InitializeAsync(
            app.Services,
            new StubHostEnvironment(Environments.Development));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            Assert.Contains(db.Roles.Select(x => x.Name), x => x == "WarehouseManager");
        }

        var client = app.CreateClient();

        var returnResponse = await client.GetAsync($"/internal/returns/{DevelopmentBootstrapper.DemoReturnOrderId}");
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var order = await returnResponse.Content.ReadFromJsonAsync<ReturnOrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Broken", order!.QualityState);

        var sopResponse = await client.GetAsync("/internal/sop/candidates?operationCode=RETURNS&stepCode=DISPOSITION");
        Assert.Equal(HttpStatusCode.OK, sopResponse.StatusCode);

        var candidates = await sopResponse.Content.ReadFromJsonAsync<IReadOnlyList<SopCandidateDto>>();
        Assert.NotNull(candidates);
        Assert.Single(candidates!);
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
