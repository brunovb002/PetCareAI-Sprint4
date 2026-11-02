using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Infrastructure.Mongo;

/// <summary>Formato do documento na coleção do MongoDB (mantém o Domínio livre de atributos do driver).</summary>
public class RegistroAtividadeDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Entidade { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public string Descricao { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DataHora { get; set; }

    public static RegistroAtividadeDocument DeEntidade(RegistroAtividade r) => new()
    {
        Entidade = r.Entidade,
        EntidadeId = r.EntidadeId,
        Acao = r.Acao,
        UsuarioId = r.UsuarioId,
        Descricao = r.Descricao,
        DataHora = r.DataHora
    };

    public RegistroAtividade ParaEntidade() =>
        new(Entidade, EntidadeId, Acao, UsuarioId, Descricao, Id, DataHora);
}
