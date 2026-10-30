namespace PetCareAI.Domain.Common;

/// <summary>Filtros de pets. OrdenarPor aceita: id, nome, especie, idade, peso.</summary>
public class PetFiltro : FiltroPaginado
{
    public string? Nome { get; set; }
    public string? Especie { get; set; }
    public int? UsuarioId { get; set; }
}

/// <summary>Filtros de consultas. OrdenarPor aceita: id, data, veterinario.</summary>
public class ConsultaFiltro : FiltroPaginado
{
    public int? PetId { get; set; }
    public string? Veterinario { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

/// <summary>Filtros do histórico de atividades (MongoDB). Sempre ordenado da mais recente para a mais antiga.</summary>
public class AtividadeFiltro : FiltroPaginado
{
    public string? Entidade { get; set; }
    public string? Acao { get; set; }
    public string? EntidadeId { get; set; }
}
