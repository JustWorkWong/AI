using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
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

    [Fact]
    public async Task Initialize_should_apply_unique_index_upgrade_idempotently_outside_development()
    {
        var connectionString = await CreateIsolatedDatabaseAsync(_fixture.ConnectionString, "wms_bootstrap");
        await using var app = await TestAppFactory.CreateDomainServiceAsync(connectionString);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX IF EXISTS "IX_ApprovalActions_ApprovalTaskId";
                """);
        }

        var testing = new StubHostEnvironment("Testing");
        await DevelopmentBootstrapper.InitializeAsync(app.Services, testing);
        await DevelopmentBootstrapper.InitializeAsync(app.Services, testing);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'ApprovalActions'
              AND indexname = 'IX_ApprovalActions_ApprovalTaskId';
            """,
            connection);

        var indexCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1L, indexCount);
    }

    private static async Task<string> CreateIsolatedDatabaseAsync(string connectionString, string prefix)
    {
        var databaseName = $"{prefix}_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName };
        var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"""CREATE DATABASE "{databaseName}";""", connection);
        await command.ExecuteNonQueryAsync();

        return builder.ConnectionString;
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
