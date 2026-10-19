using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Concilia.Tests.Embeddings;

// Without configuration the API uses the Fake embedder, so these tests need no model.
public class SimilarityEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Returns_the_similarity_of_two_texts()
    {
        var response = await _client.PostAsJsonAsync("/embeddings/similarity", new { a = "Reservar una sala", b = "reservar una sala." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SimilarityResponse>();
        Assert.Equal(1, body!.Similarity, precision: 6);
    }

    [Fact]
    public async Task Rejects_a_request_without_both_texts()
    {
        var response = await _client.PostAsJsonAsync("/embeddings/similarity", new { a = "solo uno" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record SimilarityResponse(double Similarity);
}
