using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Tests.Integration;

public class AvaliacaoRiscoTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AvaliacaoRiscoTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CriarPetAsync(HttpClient client, string especie = "Cachorro")
    {
        var response = await client.PostAsJsonAsync("/api/pets",
            new { nome = "Thor", especie, raca = "SRD", idade = 11, peso = 28.5 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dados").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Avaliar_ComSintomasGraves_DeveRetornarPrioridadeAltaERegistrarNoHistorico()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var petId = await CriarPetAsync(client);
        var dto = new { historicoMedico = "DoencaCronica", sintomas = new[] { "Dificuldade para respirar", "febre" } };

        // Act
        var response = await client.PostAsJsonAsync($"/api/pets/{petId}/avaliacao-risco", dto);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();
        var historico = await client.GetFromJsonAsync<JsonElement>($"/api/atividades?acao=AvaliacaoRisco&entidadeId={petId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("dados").GetProperty("prioridade").GetString().Should().Be("Alta");
        corpo.GetProperty("dados").GetProperty("recomendacao").GetString().Should().NotBeNullOrWhiteSpace();
        historico.GetProperty("itens").GetArrayLength().Should().Be(1);

        // A API enviou à IA os dados do banco já traduzidos para o formato do modelo
        var entrada = _factory.ClyvoAi.UltimaEntrada!;
        entrada.AnimalName.Should().Be("Dog");
        entrada.Breed.Should().Be("Mixed Breed");
        entrada.Age.Should().Be(11);
        entrada.MedicalHistory.Should().Be("Chronic illness");
        entrada.Symptoms.Should().Equal("Difficulty in breathing", "Fever");
    }

    [Fact]
    public async Task Avaliar_SemSintomas_DeveRetornar400()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var petId = await CriarPetAsync(client);

        // Act
        var response = await client.PostAsJsonAsync($"/api/pets/{petId}/avaliacao-risco",
            new { historicoMedico = "Vacinado", sintomas = Array.Empty<string>() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Avaliar_PetQueNaoECaoNemGato_DeveRetornar400()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var petId = await CriarPetAsync(client, especie: "Calopsita");

        // Act
        var response = await client.PostAsJsonAsync($"/api/pets/{petId}/avaliacao-risco",
            new { historicoMedico = "Vacinado", sintomas = new[] { "tosse" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Avaliar_PetInexistente_DeveRetornar404()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/pets/999999/avaliacao-risco",
            new { historicoMedico = "Vacinado", sintomas = new[] { "tosse" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
