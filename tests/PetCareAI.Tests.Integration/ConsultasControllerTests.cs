using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PetCareAI.Tests.Integration;

public class ConsultasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConsultasControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CriarPetAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/pets",
            new { nome = "Mimi", especie = "Gato", raca = "Siamês", idade = 2, peso = 4.2 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dados").GetProperty("id").GetInt32();
    }

    private static object NovaConsulta(int petId, string veterinario = "Dra. Carla Souza") => new
    {
        petId,
        dataConsulta = "2026-11-04T10:00:00",
        veterinario,
        motivo = "Consulta de rotina",
        diagnostico = "Saudável"
    };

    [Fact]
    public async Task CrudCompleto_DeveCriarConsultarAtualizarERemoverConsulta()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var petId = await CriarPetAsync(client);

        // Act + Assert: CREATE
        var criar = await client.PostAsJsonAsync("/api/consultas", NovaConsulta(petId));
        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        var criada = await criar.Content.ReadFromJsonAsync<JsonElement>();
        var id = criada.GetProperty("dados").GetProperty("id").GetInt32();
        criada.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString())
            .Should().Contain("pet");

        // READ (pela consulta e pela lista do pet)
        var doPet = await client.GetFromJsonAsync<JsonElement>($"/api/pets/{petId}/consultas");
        doPet.GetProperty("itens").GetArrayLength().Should().Be(1);

        // UPDATE
        var atualizar = await client.PutAsJsonAsync($"/api/consultas/{id}", new
        {
            dataConsulta = "2026-11-10T15:30:00",
            veterinario = "Dr. Marcos Lima",
            motivo = "Retorno",
            diagnostico = "Recuperado"
        });
        atualizar.StatusCode.Should().Be(HttpStatusCode.OK);
        var lida = await client.GetFromJsonAsync<JsonElement>($"/api/consultas/{id}");
        lida.GetProperty("dados").GetProperty("veterinario").GetString().Should().Be("Dr. Marcos Lima");

        // DELETE
        (await client.DeleteAsync($"/api/consultas/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/consultas/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Criar_ParaPetInexistente_DeveRetornar404()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/consultas", NovaConsulta(petId: 999999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_ComFiltroPorVeterinario_DeveRetornarSomenteConsultasDele()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var petId = await CriarPetAsync(client);
        var veterinario = $"Dr. Filtro {Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/consultas", NovaConsulta(petId, veterinario));
        await client.PostAsJsonAsync("/api/consultas", NovaConsulta(petId, "Outra Veterinária"));

        // Act
        var corpo = await client.GetFromJsonAsync<JsonElement>($"/api/consultas?veterinario={Uri.EscapeDataString(veterinario)}");

        // Assert
        corpo.GetProperty("itens").EnumerateArray()
            .Select(i => i.GetProperty("dados").GetProperty("veterinario").GetString())
            .Should().ContainSingle().Which.Should().Be(veterinario);
    }

    [Fact]
    public async Task Listar_ComPeriodoInvertido_DeveRetornar400()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act
        var response = await client.GetAsync("/api/consultas?dataInicio=2026-12-01&dataFim=2026-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
