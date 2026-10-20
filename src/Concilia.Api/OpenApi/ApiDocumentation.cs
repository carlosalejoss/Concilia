using System.Reflection;
using Concilia.Api.Runs;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Concilia.Api.OpenApi;

// The groups of endpoints in the documentation, in the order of the flow.
// Each endpoint file puts its routes under one of them (WithTags).
public static class ApiTags
{
    public const string Projects = "Proyectos";
    public const string Files = "Documentos";
    public const string Questions = "Preguntas";
    public const string Phases = "Catálogo y backlog";
    public const string Deliverables = "Entregables";
    public const string Mockup = "Mockup";
    public const string Jobs = "Trabajos";
    public const string Runs = "Ejecuciones";
    public const string Reports = "Informes y evaluación";
    public const string System = "Sistema";

    public static readonly IReadOnlyList<(string Name, string Description)> All =
    [
        (Projects, "Crear, editar y eliminar proyectos, y el estado de cada paso del flujo."),
        (Files, "Subir la documentación del proyecto (y la referencia de estilo del mockup, en `.zip`) y leer el texto extraído."),
        (Questions, "Rondas de preguntas al cliente (A ‖ B → árbitro), sugerencias de respuesta de la IA y respuestas del usuario."),
        (Phases, "Lanzar el catálogo de requisitos y el backlog (A ‖ B → árbitro). El resultado llega como borrador a «Entregables»."),
        (Deliverables, "El catálogo, el backlog y el mockup del proyecto: leer, editar, validar, reabrir y exportar."),
        (Mockup, "Generar, reanudar y ver el prototipo navegable a partir del backlog validado."),
        (Jobs, "Los trabajos lanzados (cada uno con sus ejecuciones), su resultado y los nuevos arbitrajes experimentales."),
        (Runs, "Cada ejecución de un modelo y su consola en directo (Server-Sent Events)."),
        (Reports, "Informe por trabajo, CSV del proyecto, emparejamiento de A y B por embeddings y similitud entre textos."),
        (System, "Estado de la API."),
    ];
}

public static class ApiDocumentation
{
    public const string DocumentName = "v1";
    public const string DocumentPath = "/openapi/v1.json";
    public const string UiPath = "swagger";

    // The OpenAPI document is generated from the endpoints themselves (routes,
    // parameters, request bodies and the metadata of WithSummary, Produces…)
    // each time it is requested, so it always matches the code: a new or
    // changed endpoint appears without touching anything here.
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(DocumentName, options => options
            .AddDocumentTransformer(DescribeDocument)
            .AddSchemaTransformer(NumbersAsNumbers));

    // The title, the front page and the groups, in the order of the flow.
    private static Task DescribeDocument(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Concilia API",
            Version = typeof(ApiDocumentation).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
            Description = Description,
            License = new OpenApiLicense { Name = "Trabajo de Fin de Grado" },
        };
        document.Tags = new HashSet<OpenApiTag>(ApiTags.All.Select(t => new OpenApiTag { Name = t.Name, Description = t.Description }));
        // No "servers": the generator would write the host it was asked
        // from, which behind nginx is the "api" container. Without it,
        // "Try it out" calls the same address the page was opened from.
        document.Servers = [];
        return Task.CompletedTask;
    }

    // ASP.NET Core also reads numbers written as text ("12"), so the generator
    // describes every number as "integer or string" with a pattern. The API
    // always writes them as numbers, and that is what the document says.
    private static Task NumbersAsNumbers(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema.Type is { } type && (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number)))
        {
            schema.Type = type & ~JsonSchemaType.String;
            schema.Pattern = null;
        }
        return Task.CompletedTask;
    }

    // GET /openapi/v1.json (the document) and GET /swagger (Swagger UI over it).
    public static void MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().ExcludeFromDescription();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = UiPath;
            options.SwaggerEndpoint(DocumentPath, "Concilia API");
            options.DocumentTitle = "Concilia · API";
            options.HeadContent = $"<style>{Stylesheet()}</style>";
            options.DocExpansion(DocExpansion.List);
            options.EnableDeepLinking();
            options.EnableFilter();
            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.DefaultModelsExpandDepth(1);
            options.DefaultModelRendering(ModelRendering.Model);
        });
    }

    // The answers of every endpoint that launches a task in the background:
    // 202 with the job and its runs, 400 if it cannot be launched now (the
    // step is not available, or another task is running) and 404.
    public static RouteHandlerBuilder LaunchesJob(this RouteHandlerBuilder endpoint) => endpoint
        .Produces<JobView>(StatusCodes.Status202Accepted)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

    // The look of Concilia (the palette of frontend/src/index.css) over Swagger UI.
    private static string Stylesheet()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Concilia.Api.OpenApi.swagger-ui.css")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // The front page of the documentation (Markdown, as Swagger UI shows it).
    private const string Description = """
        Plataforma que genera **catálogos de requisitos** (EARS/MASTeR), **backlogs ágiles** y un **mockup**
        con verificación cruzada: dos modelos de IA (A y B) trabajan cada tarea en paralelo y un **árbitro**
        los compara y fusiona.

        ### El flujo
        1. **Proyecto** → `POST /projects`
        2. **Documentos** → `POST /projects/{projectId}/files`
        3. **Rondas de preguntas** → `POST /projects/{projectId}/question-rounds` y respuestas en `/questions/{id}/…`
        4. **Catálogo** → `POST /projects/{projectId}/catalog-jobs`, se edita y se valida en `/deliverables/catalog`
        5. **Backlog** → `POST /projects/{projectId}/backlog-jobs`, se edita y se valida en `/deliverables/backlog`
        6. **Mockup** → `POST /projects/{projectId}/mockup-jobs`

        `GET /projects/{projectId}/flow` dice en qué estado está cada paso y si se puede lanzar.

        ### Convenciones
        - **Tareas en segundo plano.** Lanzar una tarea responde `202 Accepted` con el trabajo y sus ejecuciones;
          su avance se sigue con `GET /jobs/{jobId}` y la consola de cada ejecución con `GET /runs/{runId}/events` (SSE).
        - **Una tarea cada vez** en toda la plataforma: mientras hay una en curso, lanzar otra responde `400` con
          el mensaje de cuál se está ejecutando (`GET /jobs/active`).
        - **Errores.** Los de validación son `400` con un *ValidationProblem*: `errors` lleva, por campo, los mensajes
          en español que la web muestra tal cual. Un recurso que no existe es `404` sin cuerpo.
        - Los enumerados viajan como texto (`"Done"`, no `2`).
        - Sin autenticación: la API está pensada para un solo usuario, detrás de la web.
        """;
}
