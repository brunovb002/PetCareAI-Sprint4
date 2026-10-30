using Microsoft.Extensions.Logging;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Infrastructure.Mongo;

/// <summary>
/// Usado quando MongoDb:ConnectionString não está configurada (ex.: rodando localmente sem MongoDB).
/// A API continua funcionando, mas o histórico de atividades fica vazio e um aviso vai para o log.
/// </summary>
public class AtividadeDesabilitadaRepository : IRegistroAtividadeRepository
{
    private readonly ILogger<AtividadeDesabilitadaRepository> _logger;

    public AtividadeDesabilitadaRepository(ILogger<AtividadeDesabilitadaRepository> logger)
    {
        _logger = logger;
    }

    public Task RegistrarAsync(RegistroAtividade registro, CancellationToken ct = default)
    {
        _logger.LogWarning("MongoDB não configurado: atividade {Acao} de {Entidade} {EntidadeId} não foi registrada",
            registro.Acao, registro.Entidade, registro.EntidadeId);
        return Task.CompletedTask;
    }

    public Task<PagedResult<RegistroAtividade>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default) =>
        Task.FromResult(new PagedResult<RegistroAtividade>([], 0, filtro.Pagina, filtro.TamanhoPagina));
}
