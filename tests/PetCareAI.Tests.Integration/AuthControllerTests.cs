using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PetCareAI.Tests.Integration;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string EmailUnico() => $"user-{Guid.NewGuid():N}@petcare.com";

    [Fact]
    public async Task Registrar_ComDadosValidos_DeveRetornar201SemExporSenha()
    {
        // Arrange
        var dto = new { nome = "Carla", email = EmailUnico(), senha = "Senha@123", perfil = "Veterinario" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/registrar", dto);
        var corpo = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        corpo.Should().Contain("Veterinario").And.NotContain("Senha@123").And.NotContain("senhaHash");
    }

    [Fact]
    public async Task Registrar_ComEmailDuplicado_DeveRetornar409()
    {
        // Arrange
        var dto = new { nome = "Bruno", email = EmailUnico(), senha = "Senha@123" };
        await _client.PostAsJsonAsync("/api/auth/registrar", dto);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/registrar", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Registrar_ComSenhaCurta_DeveRetornar400()
    {
        // Arrange
        var dto = new { nome = "Bruno", email = EmailUnico(), senha = "123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/registrar", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornar401ComProblemDetails()
    {
        // Arrange
        var email = EmailUnico();
        await _client.PostAsJsonAsync("/api/auth/registrar", new { nome = "Bruno", email, senha = "Senha@123" });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha = "errada" });
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        corpo.GetProperty("detail").GetString().Should().Be("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task Eu_ComTokenValido_DeveRetornarUsuarioLogado()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act
        var corpo = await client.GetFromJsonAsync<JsonElement>("/api/auth/eu");

        // Assert
        corpo.GetProperty("nome").GetString().Should().Be("Tutor Teste");
        corpo.GetProperty("perfil").GetString().Should().Be("Tutor");
    }

    [Theory]
    [InlineData("/api/pets")]
    [InlineData("/api/consultas")]
    [InlineData("/api/atividades")]
    public async Task EndpointsProtegidos_SemToken_DevemRetornar401(string url)
    {
        // Arrange (client sem Authorization)

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
