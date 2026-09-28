using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration (in Docker, the environment
// variable ConnectionStrings__Default). It is only read when a request first
// needs the database, so the API can start without one (e.g. in the /health test).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Apply pending migrations on startup when asked to (docker-compose.yml sets
// Database__MigrateOnStartup=true). Simple enough for a single API instance.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposes Program to the test project (WebApplicationFactory<Program>).
public partial class Program { }
