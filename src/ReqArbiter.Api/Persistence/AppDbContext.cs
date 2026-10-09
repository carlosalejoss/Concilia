using Microsoft.EntityFrameworkCore;
using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Questions;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Persistence;

// Entry point to the database through EF Core: each DbSet is a table.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

    public DbSet<Run> Runs => Set<Run>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Deliverable> Deliverables => Set<Deliverable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(ProjectSettings.MaxNameLength);
            project.Property(p => p.SystemName).HasMaxLength(ProjectSettings.MaxSystemNameLength);
            project.Property(p => p.Language).HasMaxLength(2);
        });

        modelBuilder.Entity<ProjectFile>(file =>
        {
            // Deleting a project also deletes its file records.
            file.HasOne<Project>()
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            file.Property(f => f.FileName).HasMaxLength(255);
            file.Property(f => f.StoredName).HasMaxLength(100);
            file.Property(f => f.TextStoredName).HasMaxLength(100);
            file.Property(f => f.ContentType).HasMaxLength(255);
            file.Property(f => f.Sha256).HasMaxLength(64).IsFixedLength();
        });

        modelBuilder.Entity<Run>(run =>
        {
            run.HasOne<Project>()
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Stored as text ("Done", "Failed"...) so the table is readable.
            run.Property(r => r.State).HasConversion<string>().HasMaxLength(20);
            run.Property(r => r.Provider).HasMaxLength(50);
            run.Property(r => r.Model).HasMaxLength(100);
            run.Property(r => r.VerdictState).HasMaxLength(20);
            run.HasIndex(r => r.RetryOfRunId);
            // "A", "B", "ARB" (arbiter) or "MOCK" (mockup).
            run.Property(r => r.Slot).HasMaxLength(10);

            // Deleting a job also deletes its runs.
            run.HasOne<Job>()
                .WithMany()
                .HasForeignKey(r => r.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Job>(job =>
        {
            job.HasOne<Project>()
                .WithMany()
                .HasForeignKey(j => j.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            job.Property(j => j.Kind).HasMaxLength(20);
        });

        modelBuilder.Entity<Question>(question =>
        {
            question.HasOne<Project>()
                .WithMany()
                .HasForeignKey(q => q.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            // The round where it was asked.
            question.HasOne<Job>()
                .WithMany()
                .HasForeignKey(q => q.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // P-001… is unique within a project.
            question.HasIndex(q => new { q.ProjectId, q.Code }).IsUnique();
            question.Property(q => q.Code).HasMaxLength(20);
            question.Property(q => q.Origin).HasMaxLength(10);
            // Stored as text ("Pending", "Answered", "Discarded") so the table is readable.
            question.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Deliverable>(deliverable =>
        {
            deliverable.HasOne<Project>()
                .WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            // The job that generated it.
            deliverable.HasOne<Job>()
                .WithMany()
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // One current deliverable per kind and project.
            deliverable.HasIndex(d => new { d.ProjectId, d.Kind }).IsUnique();
            // Stored as text ("Catalog", "Draft"...) so the table is readable.
            deliverable.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
            deliverable.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
