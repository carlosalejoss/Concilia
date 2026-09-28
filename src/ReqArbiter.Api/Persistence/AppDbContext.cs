using Microsoft.EntityFrameworkCore;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Api.Persistence;

// Entry point to the database through EF Core: each DbSet is a table.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

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
            file.Property(f => f.ContentType).HasMaxLength(255);
            file.Property(f => f.Sha256).HasMaxLength(64).IsFixedLength();
        });
    }
}
