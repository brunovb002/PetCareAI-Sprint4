using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

/// <summary>
/// Junta os dados do pet (Azure SQL) com os sintomas relatados pelo tutor, pede a prioridade ao CLYVO AI
/// e registra o resultado no histórico de atividades (MongoDB).
/// </summary>
public class AvaliacaoRiscoService : IAvaliacaoRiscoService
{
    private readonly IPetRepository _petRepository;
    private readonly IClyvoAiClient _clyvoAi;
    private readonly IRegistroAtividadeRepository _atividades;

    public AvaliacaoRiscoService(IPetRepository petRepository, IClyvoAiClient clyvoAi, IRegistroAtividadeRepository atividades)
    {
        _petRepository = petRepository;
        _clyvoAi = clyvoAi;
        _atividades = atividades;
    }

    public async Task<AvaliacaoRiscoResponseDto> AvaliarAsync(int petId, AvaliacaoRiscoRequestDto dto, int usuarioId, CancellationToken ct = default)
    {
        var pet = await _petRepository.ObterPorIdAsync(petId, ct) ?? throw new NotFoundException(nameof(Pet), petId);

        var sintomas = dto.Sintomas.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (sintomas.Count is < 1 or > 5)
            throw new DomainException("Informe de 1 a 5 sintomas.");

        var naoReconhecidos = sintomas.Where(s => ClyvoAiTraducao.Sintoma(s) is null).ToList();
        var entrada = new EntradaClyvoAi(
            ClyvoAiTraducao.Especie(pet.Especie),
            ClyvoAiTraducao.Raca(pet.Raca),
            pet.Idade,
            pet.Peso,
            ClyvoAiTraducao.Historico(dto.HistoricoMedico!.Value),
            sintomas.Select(s => ClyvoAiTraducao.Sintoma(s) ?? s).ToList());

        var previsao = await _clyvoAi.PreverAsync(entrada, ct);
        var (prioridade, recomendacao) = Interpretar(previsao.Priority);
        var confianca = previsao.Confidence.GetValueOrDefault(previsao.Priority);

        await _atividades.RegistrarAsync(new RegistroAtividade(nameof(Pet), pet.Id.ToString(), RegistroAtividade.AcaoAvaliacaoRisco,
            usuarioId, $"CLYVO AI avaliou '{pet.Nome}' com prioridade {prioridade} ({confianca:P0})."), ct);

        return new AvaliacaoRiscoResponseDto
        {
            PetId = pet.Id,
            Prioridade = prioridade,
            Confianca = confianca,
            Recomendacao = recomendacao,
            SintomasNaoReconhecidos = naoReconhecidos,
            AvaliadoEm = DateTime.UtcNow
        };
    }

    private static (string Prioridade, string Recomendacao) Interpretar(string prioridade) => prioridade switch
    {
        "high" => ("Alta", "Procure atendimento veterinário o quanto antes."),
        "medium" => ("Media", "Agende uma consulta nos próximos dias e acompanhe a evolução dos sintomas."),
        "low" => ("Baixa", "Mantenha os cuidados de rotina e observe o comportamento do pet."),
        _ => throw new InvalidOperationException($"Prioridade desconhecida retornada pela IA: '{prioridade}'.")
    };
}
