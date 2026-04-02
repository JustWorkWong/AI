using Agent.Runtime.Persistence;
using Agent.Runtime.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

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
            services.AddSingleton<IDomainKnowledgeClient, StubDomainKnowledgeClient>();
            services.AddSingleton<IDomainDispositionClient, StubDomainDispositionClient>();
        });
    }

    private sealed class StubDomainKnowledgeClient : IDomainKnowledgeClient
    {
        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(null);

        public Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalCaseDto>>([]);

        public Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
            string operationCode,
            string stepCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopCandidateDto>>([]);

        public Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
            RetrieveSopChunksQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopChunkDto>>([]);
    }

    private sealed class StubDomainDispositionClient : IDomainDispositionClient
    {
        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
