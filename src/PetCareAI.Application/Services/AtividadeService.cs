using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

public class AtividadeService : IAtividadeService
{
    private readonly IRegistroAtividadeRepository _repository;

    public AtividadeService(IRegistroAtividadeRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<AtividadeResponseDto>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _repository.ListarAsync(filtro, ct);
        return pagina.Map(a => a.ToDto());
    }
}
