using Microsoft.EntityFrameworkCore;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Persistence;

// Entry point to the database through EF Core: each DbSet is a table.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

    public DbSet<Run> Runs => Set<Run>();

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(200);
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
            run.Property(r => r.Slot).HasMaxLength(1);

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
    }
}
