using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Persistence;

namespace PetCareAI.Tests.Integration;

/// <summary>
/// Sobe a API completa em memória, trocando apenas o que é externo:
/// Azure SQL -> EF Core InMemory e MongoDB -> repositório em memória.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _nomeBanco = $"PetCareTests-{Guid.NewGuid()}";

    public InMemoryAtividadeRepository Atividades { get; } = new();
    public FakeClyvoAiClient ClyvoAi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "chave-de-teste-integracao-com-mais-de-32-caracteres"
        }));

        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_nomeBanco));

            services.RemoveAll<IRegistroAtividadeRepository>();
            services.AddSingleton<IRegistroAtividadeRepository>(Atividades);

            services.RemoveAll<IClyvoAiClient>();
            services.AddSingleton<IClyvoAiClient>(ClyvoAi);
        });
    }

    /// <summary>Cria um usuário novo (Tutor ou Veterinario), faz login e devolve um HttpClient já com o token JWT.</summary>
    public async Task<HttpClient> CriarClienteAutenticadoAsync(string perfil = "Tutor")
    {
        var client = CreateClient();
        var email = $"tutor-{Guid.NewGuid():N}@petcare.com";

        var registro = await client.PostAsJsonAsync("/api/auth/registrar",
            new { nome = "Tutor Teste", email, senha = "Senha@123", perfil });
        registro.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha@123" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

/// <summary>Substitui o serviço Python CLYVO AI: "high" se houver dificuldade respiratória, senão "low".</summary>
public class FakeClyvoAiClient : IClyvoAiClient
{
    public EntradaClyvoAi? UltimaEntrada { get; private set; }

    public Task<PrevisaoClyvoAi> PreverAsync(EntradaClyvoAi entrada, CancellationToken ct = default)
    {
        UltimaEntrada = entrada;
        var prioridade = entrada.Symptoms.Any(s => s.Contains("breathing")) ? "high" : "low";
        return Task.FromResult(new PrevisaoClyvoAi(prioridade, new Dictionary<string, double> { [prioridade] = 0.9 }));
    }
}

/// <summary>Substitui o MongoDB nos testes, guardando as atividades em memória.</summary>
public class InMemoryAtividadeRepository : IRegistroAtividadeRepository
{
    private readonly ConcurrentQueue<RegistroAtividade> _registros = new();

    public IReadOnlyCollection<RegistroAtividade> Registros => _registros.ToArray();

    public Task RegistrarAsync(RegistroAtividade registro, CancellationToken ct = default)
    {
        _registros.Enqueue(new RegistroAtividade(registro.Entidade, registro.EntidadeId, registro.Acao,
            registro.UsuarioId, registro.Descricao, Guid.NewGuid().ToString("N"), registro.DataHora));
        return Task.CompletedTask;
    }

    public Task<PagedResult<RegistroAtividade>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default)
    {
        var query = _registros
            .Where(r => filtro.Entidade is null || r.Entidade == filtro.Entidade)
            .Where(r => filtro.Acao is null || r.Acao == filtro.Acao)
            .Where(r => filtro.EntidadeId is null || r.EntidadeId == filtro.EntidadeId)
            .OrderByDescending(r => r.DataHora)
            .ToList();

        var itens = query.Skip(filtro.Saltar).Take(filtro.TamanhoPagina).ToList();
        return Task.FromResult(new PagedResult<RegistroAtividade>(itens, query.Count, filtro.Pagina, filtro.TamanhoPagina));
    }
}
