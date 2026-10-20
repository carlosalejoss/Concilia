using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Concilia.Api.OpenApi;

namespace Concilia.Tests;

// The OpenAPI document is generated from the endpoints, so a new endpoint
// always appears in it. These tests check that it also appears documented:
// in a group, with a summary and with what it answers. An endpoint added
// without WithTags, WithSummary or Produces makes them fail.
public class OpenApiDocumentTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] Methods = ["get", "put", "post", "delete", "patch"];

    [Fact]
    public async Task Every_operation_is_documented()
    {
        var document = await DocumentAsync();
        var tags = document["tags"]!.AsArray().Select(t => t!["name"]!.GetValue<string>()).ToHashSet();
        var problems = new List<string>();

        foreach (var (name, operation) in Operations(document))
        {
            var operationTags = operation["tags"]?.AsArray().Select(t => t!.GetValue<string>()).ToList() ?? [];
            if (operationTags.Count == 0)
                problems.Add($"{name}: sin grupo (WithTags)");
            problems.AddRange(operationTags.Where(t => !tags.Contains(t)).Select(t => $"{name}: grupo «{t}» que no está en ApiTags.All"));
            if (string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()))
                problems.Add($"{name}: sin resumen (WithSummary)");

            var success = operation["responses"]!.AsObject().Where(r => r.Key.StartsWith('2')).ToList();
            if (success.Count == 0)
                problems.Add($"{name}: sin respuesta 2xx (Produces)");
            // Every success answer says what it returns, except 204.
            problems.AddRange(success
                .Where(r => r.Key != "204" && r.Value!["content"] is null)
                .Select(r => $"{name}: la respuesta {r.Key} no dice qué devuelve (Produces<T>)"));
        }

        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }

    [Fact]
    public async Task Every_endpoint_of_the_api_is_in_the_document()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription != true)
            .SelectMany(e => e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])
            .Count();

        Assert.Equal(endpoints, Operations(await DocumentAsync()).Count());
    }

    [Fact]
    public async Task Every_group_has_a_description_and_is_used()
    {
        var document = await DocumentAsync();
        var used = Operations(document).SelectMany(o => o.Operation["tags"]!.AsArray().Select(t => t!.GetValue<string>())).ToHashSet();

        foreach (var tag in document["tags"]!.AsArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(tag!["description"]?.GetValue<string>()));
            Assert.Contains(tag["name"]!.GetValue<string>(), used);
        }
    }

    // The copy in docs/api/openapi.json is written by each Debug build of the
    // API. CI builds in Release and does not write it, so this catches a
    // change of the API committed without its copy.
    [Fact]
    public async Task The_copy_in_docs_matches_the_api()
    {
        var copy = Path.Combine(RepositoryRoot(), "docs", "api", "openapi.json");

        var document = await DocumentAsync();

        Assert.True(JsonNode.DeepEquals(document, JsonNode.Parse(await File.ReadAllTextAsync(copy))),
            "docs/api/openapi.json no está al día: compila la API en Debug (dotnet build) y súbelo con el cambio.");
    }

    [Fact]
    public async Task Swagger_ui_shows_the_document_with_the_look_of_concilia()
    {
        var client = factory.CreateClient();

        var page = await client.GetAsync($"/{ApiDocumentation.UiPath}/index.html");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Concilia", html);
        Assert.Contains("--accent: #0e6e5c", html);
    }

    private async Task<JsonNode> DocumentAsync()
    {
        var response = await factory.CreateClient().GetAsync(ApiDocumentation.DocumentPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    // The folder with docs/, going up from the tests' bin folder.
    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "docs", "api", "openapi.json")))
            folder = folder.Parent;
        return folder?.FullName ?? throw new InvalidOperationException("No se encuentra docs/api/openapi.json.");
    }

    // Each operation of the document, named "GET /projects/{projectId}".
    private static IEnumerable<(string Name, JsonNode Operation)> Operations(JsonNode document) =>
        document["paths"]!.AsObject().SelectMany(path => Methods
            .Where(method => path.Value![method] is not null)
            .Select(method => ($"{method.ToUpperInvariant()} {path.Key}", path.Value![method]!)));
}
