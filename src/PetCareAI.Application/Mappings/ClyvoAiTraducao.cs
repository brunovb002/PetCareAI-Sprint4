using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PetCareAI.Application.DTOs;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Application.Mappings;

/// <summary>Sintoma reconhecido: termo do dataset (inglês) e gravidade (3 grave, 2 moderado, 1 leve), igual à usada no treino do modelo.</summary>
public record SintomaTraduzido(string Termo, int Gravidade);

/// <summary>Histórico médico: valor do dataset, descrição para o tutor e se aumenta o risco.</summary>
public record HistoricoTraduzido(string Termo, string Descricao, bool AumentaRisco);

/// <summary>
/// Traduz os dados do PetCareAI (em português) para os valores do dataset em que o modelo CLYVO AI foi treinado (em inglês).
/// </summary>
public static class ClyvoAiTraducao
{
    public const int GravidadeGrave = 3;
    public const int GravidadeModerada = 2;

    private static readonly Dictionary<string, string> Especies = new()
    {
        ["cachorro"] = "Dog", ["cao"] = "Dog", ["cadela"] = "Dog", ["dog"] = "Dog",
        ["gato"] = "cat", ["gata"] = "cat", ["cat"] = "cat"
    };

    private static readonly Dictionary<string, string> Racas = new()
    {
        ["srd"] = "Mixed Breed", ["vira-lata"] = "Mixed Breed", ["sem raca definida"] = "Mixed Breed", ["mestico"] = "Mixed Breed",
        ["siames"] = "Siamese", ["persa"] = "Persian", ["pastor alemao"] = "German Shepherd",
        ["labrador"] = "Labrador Retriever", ["buldogue"] = "Bulldog", ["yorkshire"] = "Yorkshire Terrier",
        ["fold escoces"] = "Scottish Fold", ["britanico de pelo curto"] = "British Shorthair"
    };

    private static readonly Dictionary<HistoricoMedico, HistoricoTraduzido> Historicos = new()
    {
        [HistoricoMedico.SemCondicoesPrevias] = new("No previous conditions", "sem condições prévias", false),
        [HistoricoMedico.Vacinado] = new("Vaccinated", "vacinado", false),
        [HistoricoMedico.NaoVacinado] = new("Not vaccinated", "vacinação não está em dia", true),
        [HistoricoMedico.Alergias] = new("Allergies", "alergias", false),
        [HistoricoMedico.ProblemasDentarios] = new("Dental issues", "problemas dentários", false),
        [HistoricoMedico.HistoricoDeParasitas] = new("Parasite history", "histórico de parasitas", false),
        [HistoricoMedico.HistoricoDePele] = new("Skin conditions history", "histórico de problemas de pele", false),
        [HistoricoMedico.CirurgiaRecente] = new("Recent surgery", "cirurgia recente", true),
        [HistoricoMedico.DoencaRenalPrevia] = new("Previous kidney disease", "doença renal prévia", true),
        [HistoricoMedico.CardiopatiaPrevia] = new("Previous heart condition", "cardiopatia prévia", true),
        [HistoricoMedico.DoencaCronica] = new("Chronic illness", "doença crônica", true)
    };

    private static readonly Dictionary<string, SintomaTraduzido> Sintomas = new()
    {
        ["dificuldade para respirar"] = new("Difficulty in breathing", 3), ["falta de ar"] = new("Difficulty in breathing", 3),
        ["convulsao"] = new("Seizures", 3), ["convulsoes"] = new("Seizures", 3),
        ["paralisia"] = new("Paralysis", 3), ["desmaio"] = new("Collapse", 3),
        ["febre"] = new("Fever", 2), ["vomito"] = new("Vomiting", 2), ["vomitos"] = new("Vomiting", 2),
        ["diarreia"] = new("Diarrhea", 2), ["fraqueza"] = new("Weakness", 2), ["letargia"] = new("Lethargy", 2),
        ["apatia"] = new("Lethargy", 2), ["dor"] = new("Pain", 2), ["sangramento"] = new("Bleeding", 2),
        ["inchaco"] = new("Swelling", 2), ["desidratacao"] = new("Dehydration", 2), ["mancando"] = new("Lameness", 2),
        ["claudicacao"] = new("Lameness", 2), ["anemia"] = new("Anemia", 2),
        ["tosse"] = new("Coughing", 1), ["espirros"] = new("Sneezing", 1), ["espirro"] = new("Sneezing", 1),
        ["perda de apetite"] = new("Loss of appetite", 1), ["falta de apetite"] = new("Loss of appetite", 1),
        ["perda de peso"] = new("Weight loss", 1), ["queda de pelo"] = new("Hair loss", 1), ["coriza"] = new("Nasal discharge", 1)
    };

    public static string Especie(string especie) =>
        Especies.TryGetValue(Normalizar(especie), out var valor)
            ? valor
            : throw new DomainException("A avaliação de risco da IA está disponível apenas para cães e gatos.");

    /// <summary>Raças fora do dicionário seguem como estão; o modelo trata raças desconhecidas sem falhar.</summary>
    public static string Raca(string raca) =>
        Racas.TryGetValue(Normalizar(raca), out var valor) ? valor : raca.Trim();

    public static HistoricoTraduzido Historico(HistoricoMedico historico) => Historicos[historico];

    /// <summary>Procura um termo conhecido dentro do texto (ex.: "febre alta" vira "Fever").</summary>
    public static SintomaTraduzido? Sintoma(string sintoma)
    {
        var texto = Normalizar(sintoma);
        return Sintomas
            .OrderByDescending(s => s.Key.Length)
            .Where(s => Regex.IsMatch(texto, $@"\b{Regex.Escape(s.Key)}\b"))
            .Select(s => s.Value)
            .FirstOrDefault();
    }

    /// <summary>Minúsculas e sem acentos, para comparar "Vômito" com "vomito".</summary>
    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var semAcento = decomposto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(semAcento.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
