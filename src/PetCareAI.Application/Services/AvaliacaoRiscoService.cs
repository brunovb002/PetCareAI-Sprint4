using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

/// <summary>
/// Junta os dados do pet (Azure SQL) com os sintomas relatados pelo tutor, pede a prioridade ao CLYVO AI,
/// explica o resultado com os fatores que pesaram e guarda a avaliação para acompanhar a evolução do pet.
/// </summary>
public class AvaliacaoRiscoService : IAvaliacaoRiscoService
{
    private const int IdadeSenior = 8;

    private readonly IPetRepository _petRepository;
    private readonly IAvaliacaoRiscoRepository _avaliacoes;
    private readonly IClyvoAiClient _clyvoAi;
    private readonly IRegistroAtividadeRepository _atividades;

    public AvaliacaoRiscoService(IPetRepository petRepository, IAvaliacaoRiscoRepository avaliacoes,
        IClyvoAiClient clyvoAi, IRegistroAtividadeRepository atividades)
    {
        _petRepository = petRepository;
        _avaliacoes = avaliacoes;
        _clyvoAi = clyvoAi;
        _atividades = atividades;
    }

    public async Task<AvaliacaoRiscoResponseDto> AvaliarAsync(int petId, AvaliacaoRiscoRequestDto dto, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = await ObterPetOuFalharAsync(petId, ct);
        usuario.GarantirAcessoAoPet(pet);

        var sintomas = dto.Sintomas.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (sintomas.Count is < 1 or > 5)
            throw new DomainException("Informe de 1 a 5 sintomas.");

        var traduzidos = sintomas.Select(s => (Texto: s, Traducao: ClyvoAiTraducao.Sintoma(s))).ToList();
        var historico = ClyvoAiTraducao.Historico(dto.HistoricoMedico!.Value);

        var entrada = new EntradaClyvoAi(
            ClyvoAiTraducao.Especie(pet.Especie),
            ClyvoAiTraducao.Raca(pet.Raca),
            pet.Idade,
            pet.Peso,
            historico.Termo,
            traduzidos.Select(s => s.Traducao?.Termo ?? s.Texto).ToList());

        var previsao = await _clyvoAi.PreverAsync(entrada, ct);
        var prioridade = ConverterPrioridade(previsao.Priority);
        var confianca = previsao.Confidence.GetValueOrDefault(previsao.Priority);

        var graves = traduzidos.Where(s => s.Traducao?.Gravidade == ClyvoAiTraducao.GravidadeGrave).Select(s => s.Texto).ToList();
        var moderados = traduzidos.Where(s => s.Traducao?.Gravidade == ClyvoAiTraducao.GravidadeModerada).Select(s => s.Texto).ToList();

        var fatores = new List<string>();
        fatores.AddRange(graves.Select(s => $"Sinal grave: {s}"));
        fatores.AddRange(moderados.Select(s => $"Sinal moderado: {s}"));
        if (pet.Idade >= IdadeSenior)
            fatores.Add($"Pet sênior: {pet.Idade} anos");
        if (historico.AumentaRisco)
            fatores.Add($"Histórico: {historico.Descricao}");

        var recomendacao = MontarRecomendacao(prioridade, graves, dto.HistoricoMedico == HistoricoMedico.NaoVacinado);

        var avaliacao = await _avaliacoes.AdicionarAsync(new AvaliacaoRisco(petId, prioridade, confianca,
            dto.HistoricoMedico.Value.ToString(), sintomas, fatores, recomendacao, usuario.Id), ct);

        await _atividades.RegistrarAsync(new RegistroAtividade(nameof(Pet), pet.Id.ToString(), RegistroAtividade.AcaoAvaliacaoRisco,
            usuario.Id, $"CLYVO AI avaliou '{pet.Nome}' com prioridade {prioridade} ({confianca:P0})."), ct);

        var resposta = avaliacao.ToDto();
        resposta.SintomasNaoReconhecidos = traduzidos.Where(s => s.Traducao is null).Select(s => s.Texto).ToList();
        return resposta;
    }

    public async Task<PagedResult<AvaliacaoRiscoResponseDto>> ListarPorPetAsync(int petId, AvaliacaoFiltro filtro, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = await ObterPetOuFalharAsync(petId, ct);
        usuario.GarantirAcessoAoPet(pet);

        var pagina = await _avaliacoes.ListarPorPetAsync(petId, filtro, ct);
        return pagina.Map(a => a.ToDto());
    }

    private async Task<Pet> ObterPetOuFalharAsync(int petId, CancellationToken ct) =>
        await _petRepository.ObterPorIdAsync(petId, ct) ?? throw new NotFoundException(nameof(Pet), petId);

    private static string ConverterPrioridade(string prioridade) => prioridade switch
    {
        "high" => AvaliacaoRisco.PrioridadeAlta,
        "medium" => AvaliacaoRisco.PrioridadeMedia,
        "low" => AvaliacaoRisco.PrioridadeBaixa,
        _ => throw new InvalidOperationException($"Prioridade desconhecida retornada pela IA: '{prioridade}'.")
    };

    private static string MontarRecomendacao(string prioridade, List<string> sinaisGraves, bool naoVacinado)
    {
        var partes = new List<string>
        {
            prioridade switch
            {
                AvaliacaoRisco.PrioridadeAlta => "Procure atendimento veterinário o quanto antes.",
                AvaliacaoRisco.PrioridadeMedia => "Agende uma consulta nos próximos dias e acompanhe a evolução dos sintomas.",
                _ => "Mantenha os cuidados de rotina e observe o comportamento do pet."
            }
        };

        // Segurança: mesmo com prioridade baixa, um sinal grave não pode passar sem alerta
        if (prioridade != AvaliacaoRisco.PrioridadeAlta && sinaisGraves.Count > 0)
            partes.Add($"Atenção: {string.Join(" e ", sinaisGraves)} " +
                       (sinaisGraves.Count == 1 ? "é um sinal grave" : "são sinais graves") +
                       "; se piorar ou não melhorar, procure um veterinário imediatamente.");

        if (naoVacinado)
            partes.Add("Coloque a vacinação do pet em dia.");

        return string.Join(" ", partes);
    }
}
