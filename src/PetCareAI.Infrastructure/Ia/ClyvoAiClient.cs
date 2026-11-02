using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Exceptions;
using PetCareAI.Application.Interfaces;

namespace PetCareAI.Infrastructure.Ia;

/// <summary>Chama o endpoint POST /predict do serviço CLYVO AI (Flask).</summary>
public class ClyvoAiClient : IClyvoAiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ClyvoAiClient> _logger;

    public ClyvoAiClient(HttpClient http, ILogger<ClyvoAiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PrevisaoClyvoAi> PreverAsync(EntradaClyvoAi entrada, CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
            throw new ServicoIndisponivelException("Serviço de IA não configurado (ClyvoAi:Url).");

        // Mesmo formato das colunas do dataset usado no treino
        var corpo = new Dictionary<string, object>
        {
            ["AnimalName"] = entrada.AnimalName,
            ["Breed"] = entrada.Breed,
            ["Age"] = entrada.Age,
            ["Weight_kg"] = entrada.WeightKg,
            ["MedicalHistory"] = entrada.MedicalHistory
        };
        for (var i = 0; i < entrada.Symptoms.Count; i++)
            corpo[$"Symptom_{i + 1}"] = entrada.Symptoms[i];

        try
        {
            var response = await _http.PostAsJsonAsync("predict", corpo, ct);
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<RespostaPredict>(ct)
                ?? throw new ServicoIndisponivelException("Resposta vazia do serviço de IA.");
            return new PrevisaoClyvoAi(resultado.Priority, resultado.Confidence);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Falha ao chamar o CLYVO AI em {Url}", _http.BaseAddress);
            throw new ServicoIndisponivelException("O serviço de IA está indisponível no momento.", ex);
        }
    }

    private record RespostaPredict(
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("confidence")] Dictionary<string, double> Confidence);
}
