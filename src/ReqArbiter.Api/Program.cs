var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposes Program to the test project (WebApplicationFactory<Program>).
public partial class Program { }
