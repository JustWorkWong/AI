using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.IO;
using Wms.DomainService.Auth;
using Wms.DomainService.Persistence;
using Wms.DomainService.Returns;
using Wms.DomainService.Sop;

namespace Wms.DomainService.Seed;

public static class DevelopmentBootstrapper
{
    private const int MaxDatabaseBootstrapAttempts = 10;
    public static readonly Guid DemoReturnOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DemoSopDocumentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        await EnsureDatabaseReadyAsync(db, cancellationToken);
        await ApplySchemaUpgradesAsync(db, cancellationToken);
        await SeedRolesAsync(db, cancellationToken);
        await SeedDemoReturnFlowAsync(db, cancellationToken);
    }

    private static async Task EnsureDatabaseReadyAsync(
        WmsDbContext db,
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
        WmsDbContext db,
        CancellationToken cancellationToken)
    {
        return db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ApprovalActions_ApprovalTaskId"
            ON "ApprovalActions" ("ApprovalTaskId");
            """,
            cancellationToken);
    }

    private static async Task SeedRolesAsync(
        WmsDbContext db,
        CancellationToken cancellationToken)
    {
        var existingRoles = await db.Roles
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var missingRoles = Seeder.DefaultRoles()
            .Except(existingRoles, StringComparer.Ordinal)
            .ToArray();

        foreach (var roleName in missingRoles)
        {
            db.Roles.Add(new Role(Guid.NewGuid(), roleName));
        }

        if (missingRoles.Length > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDemoReturnFlowAsync(
        WmsDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.ReturnOrders.AnyAsync(x => x.Id == DemoReturnOrderId, cancellationToken))
        {
            db.ReturnOrders.Add(new ReturnOrder(DemoReturnOrderId, "RMA-20260402-001"));
        }

        if (!await db.QualityInspections.AnyAsync(x => x.ReturnOrderId == DemoReturnOrderId, cancellationToken))
        {
            db.QualityInspections.Add(new QualityInspection(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                DemoReturnOrderId,
                "Broken",
                "Screen cracked during transport check."));
        }

        if (!await db.HistoricalCaseViews.AnyAsync(x => x.Condition == "Broken" && x.Outcome == "Scrap", cancellationToken))
        {
            db.HistoricalCaseViews.Add(new HistoricalCaseView(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "Broken",
                "Scrap"));
        }

        if (!await db.SopDocuments.AnyAsync(x => x.Id == DemoSopDocumentId, cancellationToken))
        {
            db.SopDocuments.Add(new SopDocument(
                DemoSopDocumentId,
                "SOP-RET-001",
                "RETURNS",
                "v1",
                "退货质检与处置"));
        }

        if (!await db.SopChunks.AnyAsync(x => x.DocumentId == DemoSopDocumentId, cancellationToken))
        {
            db.SopChunks.AddRange(
                new SopChunk(
                    Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    DemoSopDocumentId,
                    "STEP-01",
                    1,
                    "检查包装、序列号与外观是否存在破损。"),
                new SopChunk(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    DemoSopDocumentId,
                    "STEP-02",
                    2,
                    "确认质检结论并记录现场人员确认结果。"),
                new SopChunk(
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    DemoSopDocumentId,
                    "DISPOSITION",
                    3,
                    "破损且不可修复的退货必须判定为报废，并进入主管审批。"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
