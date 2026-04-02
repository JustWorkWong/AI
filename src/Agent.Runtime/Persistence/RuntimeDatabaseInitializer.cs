using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.IO;

namespace Agent.Runtime.Persistence;

public static class RuntimeDatabaseInitializer
{
    private const int MaxDatabaseBootstrapAttempts = 10;

    public static async Task InitializeAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentRuntimeDbContext>();
        await EnsureDatabaseReadyAsync(db, cancellationToken);
        await ApplySchemaUpgradesAsync(db, cancellationToken);
    }

    private static async Task EnsureDatabaseReadyAsync(
        AgentRuntimeDbContext db,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxDatabaseBootstrapAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (IsTransientStartupFailure(ex) && attempt < MaxDatabaseBootstrapAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static bool IsTransientStartupFailure(Exception exception) =>
        exception is DbException
        || exception is IOException
        || exception is TimeoutException
        || exception.InnerException is not null && IsTransientStartupFailure(exception.InnerException);

    private static Task ApplySchemaUpgradesAsync(
        AgentRuntimeDbContext db,
        CancellationToken cancellationToken)
    {
        return db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE IF EXISTS agent_runtime.workflow_instances
                ADD COLUMN IF NOT EXISTS "Version" integer;

            UPDATE agent_runtime.workflow_instances
            SET "Version" = 0
            WHERE "Version" IS NULL;

            ALTER TABLE IF EXISTS agent_runtime.workflow_instances
                ALTER COLUMN "Version" SET DEFAULT 0;

            ALTER TABLE IF EXISTS agent_runtime.workflow_instances
                ALTER COLUMN "Version" SET NOT NULL;
            """,
            cancellationToken);
    }
}
