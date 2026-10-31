using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PetCareAI.Application.DTOs;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Application.Mappings;

/// <summary>
/// Traduz os dados do PetCareAI (em português) para os valores do dataset em que o modelo CLYVO AI foi treinado (em inglês).
/// </summary>
public static class ClyvoAiTraducao
{
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

    private static readonly Dictionary<HistoricoMedico, string> Historicos = new()
    {
        [HistoricoMedico.SemCondicoesPrevias] = "No previous conditions",
        [HistoricoMedico.Vacinado] = "Vaccinated",
        [HistoricoMedico.NaoVacinado] = "Not vaccinated",
        [HistoricoMedico.Alergias] = "Allergies",
        [HistoricoMedico.ProblemasDentarios] = "Dental issues",
        [HistoricoMedico.HistoricoDeParasitas] = "Parasite history",
        [HistoricoMedico.HistoricoDePele] = "Skin conditions history",
        [HistoricoMedico.CirurgiaRecente] = "Recent surgery",
        [HistoricoMedico.DoencaRenalPrevia] = "Previous kidney disease",
        [HistoricoMedico.CardiopatiaPrevia] = "Previous heart condition",
        [HistoricoMedico.DoencaCronica] = "Chronic illness"
    };

    private static readonly Dictionary<string, string> Sintomas = new()
    {
        ["dificuldade para respirar"] = "Difficulty in breathing", ["falta de ar"] = "Difficulty in breathing",
        ["convulsao"] = "Seizures", ["convulsoes"] = "Seizures", ["paralisia"] = "Paralysis", ["desmaio"] = "Collapse",
        ["febre"] = "Fever", ["vomito"] = "Vomiting", ["vomitos"] = "Vomiting", ["diarreia"] = "Diarrhea",
        ["fraqueza"] = "Weakness", ["letargia"] = "Lethargy", ["apatia"] = "Lethargy", ["dor"] = "Pain",
        ["sangramento"] = "Bleeding", ["inchaco"] = "Swelling", ["desidratacao"] = "Dehydration",
        ["mancando"] = "Lameness", ["claudicacao"] = "Lameness", ["anemia"] = "Anemia",
        ["tosse"] = "Coughing", ["espirros"] = "Sneezing", ["espirro"] = "Sneezing",
        ["perda de apetite"] = "Loss of appetite", ["falta de apetite"] = "Loss of appetite",
        ["perda de peso"] = "Weight loss", ["queda de pelo"] = "Hair loss", ["coriza"] = "Nasal discharge"
    };

    public static string Especie(string especie) =>
        Especies.TryGetValue(Normalizar(especie), out var valor)
            ? valor
            : throw new DomainException("A avaliação de risco da IA está disponível apenas para cães e gatos.");

    /// <summary>Raças fora do dicionário seguem como estão; o modelo trata raças desconhecidas sem falhar.</summary>
    public static string Raca(string raca) =>
        Racas.TryGetValue(Normalizar(raca), out var valor) ? valor : raca.Trim();

    public static string Historico(HistoricoMedico historico) => Historicos[historico];

    /// <summary>Procura um termo conhecido dentro do texto (ex.: "febre alta" vira "Fever").</summary>
    public static string? Sintoma(string sintoma)
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
