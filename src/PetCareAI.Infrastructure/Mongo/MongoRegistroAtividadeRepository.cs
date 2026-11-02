using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Infrastructure.Mongo;

public class MongoRegistroAtividadeRepository : IRegistroAtividadeRepository
{
    private readonly IMongoCollection<RegistroAtividadeDocument> _colecao;
    private readonly ILogger<MongoRegistroAtividadeRepository> _logger;

    public MongoRegistroAtividadeRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings,
        ILogger<MongoRegistroAtividadeRepository> logger)
    {
        _colecao = database.GetCollection<RegistroAtividadeDocument>(settings.Value.ColecaoAtividades);
        _logger = logger;
    }

    public async Task RegistrarAsync(RegistroAtividade registro, CancellationToken ct = default)
    {
        // A auditoria não pode derrubar a operação principal (já gravada no SQL):
        // se o MongoDB estiver indisponível, registramos o erro no log e seguimos.
        try
        {
            await _colecao.InsertOneAsync(RegistroAtividadeDocument.DeEntidade(registro), cancellationToken: ct);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            _logger.LogError(ex, "Falha ao gravar atividade {Acao} de {Entidade} {EntidadeId} no MongoDB",
                registro.Acao, registro.Entidade, registro.EntidadeId);
        }
    }

    public async Task<PagedResult<RegistroAtividade>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default)
    {
        var f = Builders<RegistroAtividadeDocument>.Filter;
        var filtroMongo = f.Empty;

        if (!string.IsNullOrWhiteSpace(filtro.Entidade))
            filtroMongo &= f.Eq(d => d.Entidade, filtro.Entidade);
        if (!string.IsNullOrWhiteSpace(filtro.Acao))
            filtroMongo &= f.Eq(d => d.Acao, filtro.Acao);
        if (!string.IsNullOrWhiteSpace(filtro.EntidadeId))
            filtroMongo &= f.Eq(d => d.EntidadeId, filtro.EntidadeId);

        var total = await _colecao.CountDocumentsAsync(filtroMongo, cancellationToken: ct);
        var documentos = await _colecao.Find(filtroMongo)
            .SortByDescending(d => d.DataHora)
            .Skip(filtro.Saltar)
            .Limit(filtro.TamanhoPagina)
            .ToListAsync(ct);

        return new PagedResult<RegistroAtividade>(
            documentos.Select(d => d.ParaEntidade()).ToList(), (int)total, filtro.Pagina, filtro.TamanhoPagina);
    }
}
