using Dockerizer.Api.Extensions;
using Dockerizer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
