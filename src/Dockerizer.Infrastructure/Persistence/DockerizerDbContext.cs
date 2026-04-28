using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Infrastructure.Persistence;

public sealed class DockerizerDbContext(DbContextOptions<DockerizerDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobArtifact> JobArtifacts => Set<JobArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DockerizerDbContext).Assembly);
    }
}
