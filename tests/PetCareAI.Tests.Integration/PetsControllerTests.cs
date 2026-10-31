using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Tests.Integration;

public class PetsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PetsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static object NovoPet(string nome, string especie = "Cachorro", int idade = 3) =>
        new { nome, especie, raca = "SRD", idade, peso = 12.5 };

    private static IEnumerable<string?> Rels(JsonElement links) =>
        links.EnumerateArray().Select(l => l.GetProperty("rel").GetString());

    [Fact]
    public async Task CrudCompleto_DeveCriarConsultarAtualizarERemoverPet()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act + Assert: CREATE
        var criar = await client.PostAsJsonAsync("/api/pets", NovoPet("Thor"));
        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        criar.Headers.Location.Should().NotBeNull();
        var criado = await criar.Content.ReadFromJsonAsync<JsonElement>();
        var id = criado.GetProperty("dados").GetProperty("id").GetInt32();
        Rels(criado.GetProperty("links")).Should().BeEquivalentTo("self", "atualizar", "remover", "consultas", "avaliacao-risco");

        // READ
        var lido = await client.GetFromJsonAsync<JsonElement>($"/api/pets/{id}");
        lido.GetProperty("dados").GetProperty("nome").GetString().Should().Be("Thor");

        // UPDATE
        var atualizar = await client.PutAsJsonAsync($"/api/pets/{id}", NovoPet("Thor", idade: 4));
        atualizar.StatusCode.Should().Be(HttpStatusCode.OK);
        (await atualizar.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("dados").GetProperty("idade").GetInt32().Should().Be(4);

        // DELETE
        var remover = await client.DeleteAsync($"/api/pets/{id}");
        remover.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var depois = await client.GetAsync($"/api/pets/{id}");
        depois.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Obter_PetInexistente_DeveRetornar404ComProblemDetails()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();

        // Act
        var response = await client.GetAsync("/api/pets/987654");
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        corpo.GetProperty("status").GetInt32().Should().Be(404);
        corpo.GetProperty("detail").GetString().Should().Contain("987654");
    }

    [Fact]
    public async Task Criar_ComDadosInvalidos_DeveRetornar400()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var invalido = new { nome = "", especie = "Gato", raca = "SRD", idade = 2, peso = 4 };

        // Act
        var response = await client.PostAsJsonAsync("/api/pets", invalido);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_ComFiltroOrdenacaoEPaginacao_DeveRetornarPaginaComLinksDeNavegacao()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var prefixo = $"Pag{Guid.NewGuid():N}"[..12];
        foreach (var sufixo in new[] { "C", "A", "B" })
            (await client.PostAsJsonAsync("/api/pets", NovoPet(prefixo + sufixo, especie: "Gato"))).EnsureSuccessStatusCode();

        // Act
        var corpo = await client.GetFromJsonAsync<JsonElement>(
            $"/api/pets?nome={prefixo}&especie=Gato&ordenarPor=nome&direcao=asc&pagina=1&tamanhoPagina=2");

        // Assert
        var nomes = corpo.GetProperty("itens").EnumerateArray()
            .Select(i => i.GetProperty("dados").GetProperty("nome").GetString()).ToList();
        nomes.Should().Equal(prefixo + "A", prefixo + "B");

        var paginacao = corpo.GetProperty("paginacao");
        paginacao.GetProperty("totalItens").GetInt32().Should().Be(3);
        paginacao.GetProperty("totalPaginas").GetInt32().Should().Be(2);

        var links = corpo.GetProperty("links");
        Rels(links).Should().Contain(["self", "primeira", "proxima", "ultima"]).And.NotContain("anterior");
        links.EnumerateArray().Single(l => l.GetProperty("rel").GetString() == "proxima")
            .GetProperty("href").GetString().Should().Contain("pagina=2").And.Contain($"nome={prefixo}");
    }

    [Fact]
    public async Task Criar_DeveVincularPetAoUsuarioLogadoERegistrarAtividadeNoHistorico()
    {
        // Arrange
        var client = await _factory.CriarClienteAutenticadoAsync();
        var eu = await client.GetFromJsonAsync<JsonElement>("/api/auth/eu");

        // Act
        var criado = await (await client.PostAsJsonAsync("/api/pets", NovoPet("Rex"))).Content.ReadFromJsonAsync<JsonElement>();
        var id = criado.GetProperty("dados").GetProperty("id").GetInt32();
        var historico = await client.GetFromJsonAsync<JsonElement>($"/api/atividades?entidade=Pet&entidadeId={id}");

        // Assert
        criado.GetProperty("dados").GetProperty("usuarioId").GetInt32().Should().Be(eu.GetProperty("id").GetInt32());
        var atividade = historico.GetProperty("itens").EnumerateArray().Single().GetProperty("dados");
        atividade.GetProperty("acao").GetString().Should().Be(RegistroAtividade.AcaoCriacao);
        atividade.GetProperty("usuarioId").GetInt32().Should().Be(eu.GetProperty("id").GetInt32());
    }
}
