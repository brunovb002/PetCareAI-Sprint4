using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PetCareAI.Tests.Integration;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task GetHealth_ComApiEmExecucao_DeveRetornarHealthy(string url)
    {
        // Arrange (API sobe pela factory)

        // Act
        var response = await _client.GetAsync(url);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_DeveIncluirVerificacaoDoBancoRelacional()
    {
        // Arrange
        const string url = "/health";

        // Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>(url);

        // Assert
        corpo.GetProperty("entries").TryGetProperty("sqlserver", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetSwagger_DeveExporDocumentacaoOpenApi()
    {
        // Arrange
        const string url = "/swagger/v1/swagger.json";

        // Act
        var response = await _client.GetAsync(url);
        var corpo = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.Should().Contain("/api/pets").And.Contain("Bearer");
    }
}
