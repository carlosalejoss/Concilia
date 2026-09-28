using Microsoft.EntityFrameworkCore;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Api.Persistence;

// Entry point to the database through EF Core: each DbSet is a table.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(200);
        });
    }
}
