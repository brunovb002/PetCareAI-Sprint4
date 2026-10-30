namespace PetCareAI.Domain.Entities;

/// <summary>
/// Registro de auditoria de uma operação feita na API (quem fez o quê e quando).
/// Persistido no MongoDB, por ser um dado de log, sem relacionamentos e de esquema flexível.
/// </summary>
public class RegistroAtividade
{
    public const string AcaoCriacao = "Criacao";
    public const string AcaoAtualizacao = "Atualizacao";
    public const string AcaoRemocao = "Remocao";

    public RegistroAtividade(string entidade, string entidadeId, string acao, int? usuarioId, string descricao,
        string? id = null, DateTime? dataHora = null)
    {
        Id = id;
        Entidade = entidade;
        EntidadeId = entidadeId;
        Acao = acao;
        UsuarioId = usuarioId;
        Descricao = descricao;
        DataHora = dataHora ?? DateTime.UtcNow;
    }

    public string? Id { get; }
    public string Entidade { get; }
    public string EntidadeId { get; }
    public string Acao { get; }
    public int? UsuarioId { get; }
    public string Descricao { get; }
    public DateTime DataHora { get; }
}
