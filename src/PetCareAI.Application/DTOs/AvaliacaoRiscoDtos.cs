using System.ComponentModel.DataAnnotations;

namespace PetCareAI.Application.DTOs;

/// <summary>Histórico médico do pet, nas categorias que o modelo CLYVO AI conhece.</summary>
public enum HistoricoMedico
{
    SemCondicoesPrevias,
    Vacinado,
    NaoVacinado,
    Alergias,
    ProblemasDentarios,
    HistoricoDeParasitas,
    HistoricoDePele,
    CirurgiaRecente,
    DoencaRenalPrevia,
    CardiopatiaPrevia,
    DoencaCronica
}

public class AvaliacaoRiscoRequestDto
{
    /// <example>DoencaCronica</example>
    [Required]
    public HistoricoMedico? HistoricoMedico { get; set; }

    /// <summary>De 1 a 5 sintomas relatados pelo tutor, em português.</summary>
    /// <example>["dificuldade para respirar", "febre", "fraqueza"]</example>
    [Required, MinLength(1), MaxLength(5)]
    public List<string> Sintomas { get; set; } = [];
}

public class AvaliacaoRiscoResponseDto
{
    public int Id { get; set; }
    public int PetId { get; set; }

    /// <summary>Alta, Media ou Baixa.</summary>
    public string Prioridade { get; set; } = string.Empty;

    /// <summary>Probabilidade (0 a 1) da prioridade prevista.</summary>
    public double Confianca { get; set; }

    public string Recomendacao { get; set; } = string.Empty;

    /// <summary>O que pesou na avaliação (sinais graves ou moderados, idade, histórico de risco).</summary>
    public List<string> Fatores { get; set; } = [];

    public string HistoricoMedico { get; set; } = string.Empty;
    public List<string> Sintomas { get; set; } = [];

    /// <summary>Sintomas que não estão no dicionário e foram enviados ao modelo como digitados (contam como leves).</summary>
    public List<string> SintomasNaoReconhecidos { get; set; } = [];

    public DateTime AvaliadoEm { get; set; }
}

/// <summary>Dados enviados ao serviço CLYVO AI, já no formato do modelo (dataset em inglês).</summary>
public record EntradaClyvoAi(string AnimalName, string Breed, int Age, double WeightKg, string MedicalHistory, IReadOnlyList<string> Symptoms);

/// <summary>Resposta do serviço CLYVO AI: prioridade (low/medium/high) e probabilidade de cada classe.</summary>
public record PrevisaoClyvoAi(string Priority, IReadOnlyDictionary<string, double> Confidence);
