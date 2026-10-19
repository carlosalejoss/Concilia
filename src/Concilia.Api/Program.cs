using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Deliverables;
using Concilia.Api.Embeddings;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Projects;
using Concilia.Api.Questions;
using Concilia.Api.Reports;
using Concilia.Api.Runs;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration (in Docker, the environment
// variable ConnectionStrings__Default). It is only read when a request first
// needs the database, so the API can start without one (e.g. in the /health test).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Where uploaded files are stored (setting "Storage:RootPath").
builder.Services.AddSingleton<FileStorage>();

// The model runner: Fake by default, LiteLlm when Runner:Provider says so.
builder.Services.AddRunnerProvider(builder.Configuration);

// Embeddings to compare texts by meaning, computed in local (milestone 17):
// Fake by default, the ONNX model when Embeddings:Provider says so.
builder.Services.AddTextEmbedder(builder.Configuration);

// Jobs (model A and model B over the same input) are queued by the API and
// executed in the background by JobWorker; the progress events of each run
// are kept in memory for the SSE endpoint.
builder.Services.AddSingleton(builder.Configuration.GetSection("Catalog").Get<CatalogOptions>() ?? new CatalogOptions());
builder.Services.AddSingleton<JobQueue>();
// The console of each run is also saved in its folder when the run ends.
builder.Services.AddSingleton(services =>
    new RunEventStore(runId => Path.Combine(services.GetRequiredService<FileStorage>().RunFolder(runId), RunEventStore.FileName)));
builder.Services.AddSingleton<RunExecutor>();
builder.Services.AddSingleton<PhaseExecutor>();
builder.Services.AddSingleton<MockupExecutor>();
builder.Services.AddSingleton<JobRunner>();
builder.Services.AddHostedService<JobWorker>();

// Enums travel as text in JSON ("Done", not 2), easier to read in the web.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
app.MapProjectEndpoints();
app.MapProjectFileEndpoints();
app.MapRunEndpoints();
app.MapJobEndpoints();
app.MapActiveJobEndpoint();
app.MapRearbitrationEndpoints();
app.MapFlowEndpoints();
app.MapQuestionEndpoints();
app.MapDeliverableEndpoints();
app.MapMockupEndpoints();
app.MapReportEndpoints();
app.MapEmbeddingEndpoints();
app.MapMatchingEndpoints();

app.Run();

// Exposes Program to the test project (WebApplicationFactory<Program>).
public partial class Program { }
