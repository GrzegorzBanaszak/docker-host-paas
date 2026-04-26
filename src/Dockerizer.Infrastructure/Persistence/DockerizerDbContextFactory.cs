using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dockerizer.Infrastructure.Persistence;

public sealed class DockerizerDbContextFactory : IDesignTimeDbContextFactory<DockerizerDbContext>
{
    public DockerizerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DockerizerDbContext>();
        const string connectionString = "Host=localhost;Port=5432;Database=dockerizer;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new DockerizerDbContext(optionsBuilder.Options);
    }
}
