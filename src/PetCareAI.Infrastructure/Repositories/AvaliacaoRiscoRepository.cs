using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Persistence;

namespace PetCareAI.Infrastructure.Repositories;

public class AvaliacaoRiscoRepository : IAvaliacaoRiscoRepository
{
    private readonly AppDbContext _context;

    public AvaliacaoRiscoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AvaliacaoRisco> AdicionarAsync(AvaliacaoRisco avaliacao, CancellationToken ct = default)
    {
        await _context.AvaliacoesRisco.AddAsync(avaliacao, ct);
        await _context.SaveChangesAsync(ct);
        return avaliacao;
    }

    public Task<PagedResult<AvaliacaoRisco>> ListarPorPetAsync(int petId, AvaliacaoFiltro filtro, CancellationToken ct = default) =>
        _context.AvaliacoesRisco.AsNoTracking()
            .Where(a => a.PetId == petId)
            .OrderByDescending(a => a.AvaliadoEm).ThenByDescending(a => a.Id)
            .PaginarAsync(filtro, ct);
}
