using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Domain.Entities;

/// <summary>Resultado de uma avaliação de prioridade de cuidado feita pela CLYVO AI, guardado para acompanhar a evolução do pet.</summary>
public class AvaliacaoRisco
{
    public const string PrioridadeAlta = "Alta";
    public const string PrioridadeMedia = "Media";
    public const string PrioridadeBaixa = "Baixa";

    /// <summary>Separador usado para guardar listas (sintomas e fatores) em uma coluna de texto.</summary>
    public const char Separador = '|';

    public int Id { get; private set; }
    public int PetId { get; private set; }
    public Pet? Pet { get; private set; }

    public string Prioridade { get; private set; } = string.Empty;
    public double Confianca { get; private set; }
    public string HistoricoMedico { get; private set; } = string.Empty;
    public string Sintomas { get; private set; } = string.Empty;
    public string Fatores { get; private set; } = string.Empty;
    public string Recomendacao { get; private set; } = string.Empty;
    public int UsuarioId { get; private set; }
    public DateTime AvaliadoEm { get; private set; }

    // Usado pelo Entity Framework
    private AvaliacaoRisco() { }

    public AvaliacaoRisco(int petId, string prioridade, double confianca, string historicoMedico,
        IEnumerable<string> sintomas, IEnumerable<string> fatores, string recomendacao, int usuarioId)
    {
        if (prioridade is not (PrioridadeAlta or PrioridadeMedia or PrioridadeBaixa))
            throw new DomainException($"Prioridade inválida: '{prioridade}'.");
        if (confianca is < 0 or > 1)
            throw new DomainException("Confiança deve estar entre 0 e 1.");

        PetId = petId;
        Prioridade = prioridade;
        Confianca = confianca;
        HistoricoMedico = historicoMedico;
        Sintomas = string.Join(Separador, sintomas);
        Fatores = string.Join(Separador, fatores);
        Recomendacao = recomendacao;
        UsuarioId = usuarioId;
        AvaliadoEm = DateTime.UtcNow;
    }

    public IReadOnlyList<string> ListaDeSintomas => Dividir(Sintomas);
    public IReadOnlyList<string> ListaDeFatores => Dividir(Fatores);

    private static string[] Dividir(string texto) =>
        texto.Split(Separador, StringSplitOptions.RemoveEmptyEntries);
}
