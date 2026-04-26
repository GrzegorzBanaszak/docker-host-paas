using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dockerizer.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DockerizerDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
