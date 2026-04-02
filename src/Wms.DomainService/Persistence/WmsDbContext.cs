using Microsoft.EntityFrameworkCore;
using Wms.DomainService.Approvals;
using Wms.DomainService.Auth;
using Wms.DomainService.Commands;
using Wms.DomainService.Integration;
using Wms.DomainService.Returns;
using Wms.DomainService.Sop;
using Wms.DomainService.Storage;

namespace Wms.DomainService.Persistence;

public sealed class WmsDbContext(DbContextOptions<WmsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<ReturnOrder> ReturnOrders => Set<ReturnOrder>();

    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();

    public DbSet<DispositionDecision> DispositionDecisions => Set<DispositionDecision>();

    public DbSet<HistoricalCaseView> HistoricalCaseViews => Set<HistoricalCaseView>();

    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();

    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();

    public DbSet<CommandDeduplication> CommandDeduplications => Set<CommandDeduplication>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<ReturnAttachment> ReturnAttachments => Set<ReturnAttachment>();

    public DbSet<SopDocument> SopDocuments => Set<SopDocument>();

    public DbSet<SopChunk> SopChunks => Set<SopChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalSubject).HasMaxLength(128);
            entity.Property(x => x.UserName).HasMaxLength(128);
            entity.Property(x => x.DisplayName).HasMaxLength(128);
            entity.HasIndex(x => x.ExternalSubject).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(128);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
        });

        modelBuilder.Entity<ReturnOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReturnNo).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<QualityInspection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Condition).HasMaxLength(64);
            entity.Property(x => x.Notes).HasMaxLength(512);
        });

        modelBuilder.Entity<DispositionDecision>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Outcome).HasMaxLength(64);
        });

        modelBuilder.Entity<HistoricalCaseView>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Condition).HasMaxLength(64);
            entity.Property(x => x.Outcome).HasMaxLength(64);
        });

        modelBuilder.Entity<ApprovalTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ApprovalType).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<ApprovalAction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(32);
            entity.Property(x => x.Actor).HasMaxLength(128);
        });

        modelBuilder.Entity<CommandDeduplication>(entity =>
        {
            entity.HasKey(x => x.IdempotencyKey);
            entity.Property(x => x.CommandName).HasMaxLength(64);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageType).HasMaxLength(128);
        });

        modelBuilder.Entity<ReturnAttachment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ObjectKey).HasMaxLength(256);
            entity.Property(x => x.ContentType).HasMaxLength(128);
            entity.Property(x => x.FileName).HasMaxLength(256);
        });

        modelBuilder.Entity<SopDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentCode).HasMaxLength(64);
            entity.Property(x => x.OperationCode).HasMaxLength(64);
            entity.Property(x => x.Version).HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasIndex(x => new { x.OperationCode, x.Status });
        });

        modelBuilder.Entity<SopChunk>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepCode).HasMaxLength(64);
            entity.Property(x => x.Content).HasMaxLength(4000);
            entity.HasIndex(x => new { x.DocumentId, x.StepCode, x.Sequence });
        });
    }
}
