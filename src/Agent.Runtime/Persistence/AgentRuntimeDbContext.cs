using Microsoft.EntityFrameworkCore;

namespace Agent.Runtime.Persistence;

public sealed class AgentRuntimeDbContext(DbContextOptions<AgentRuntimeDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    public DbSet<WorkflowCheckpoint> WorkflowCheckpoints => Set<WorkflowCheckpoint>();

    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();

    public DbSet<AgentMessage> AgentMessages => Set<AgentMessage>();

    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();

    public DbSet<ToolInvocation> ToolInvocations => Set<ToolInvocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("agent_runtime");

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("workflow_instances");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WorkflowCode).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasMany(x => x.Checkpoints)
                .WithOne()
                .HasForeignKey(x => x.WorkflowInstanceId);
        });

        modelBuilder.Entity<WorkflowCheckpoint>(entity =>
        {
            entity.ToTable("workflow_checkpoints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(80);
            entity.Property(x => x.StateJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.ToTable("agent_runs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AgentName).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.ModelProfileCode).HasMaxLength(80);
        });

        modelBuilder.Entity<AgentMessage>(entity =>
        {
            entity.ToTable("agent_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AgentName).HasMaxLength(100);
            entity.Property(x => x.Role).HasMaxLength(30);
            entity.Property(x => x.Content).HasColumnType("text");
        });

        modelBuilder.Entity<ConversationSummary>(entity =>
        {
            entity.ToTable("conversation_summaries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SummaryText).HasColumnType("text");
        });

        modelBuilder.Entity<ToolInvocation>(entity =>
        {
            entity.ToTable("tool_invocations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ToolName).HasMaxLength(100);
            entity.Property(x => x.TraceId).HasMaxLength(80);
            entity.Property(x => x.InputSummary).HasColumnType("text");
            entity.Property(x => x.OutputSummary).HasColumnType("text");
            entity.Property(x => x.Status).HasMaxLength(30);
        });
    }
}
