using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Persistence;

namespace PetCareAI.Infrastructure.Repositories;

public class ConsultaRepository : IConsultaRepository
{
    private readonly AppDbContext _context;

    public ConsultaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Consulta> AdicionarAsync(Consulta consulta, CancellationToken ct = default)
    {
        await _context.Consultas.AddAsync(consulta, ct);
        await _context.SaveChangesAsync(ct);
        return consulta;
    }

    public Task<Consulta?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _context.Consultas.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<PagedResult<Consulta>> ListarAsync(ConsultaFiltro filtro, CancellationToken ct = default)
    {
        var query = _context.Consultas.AsNoTracking();

        if (filtro.PetId.HasValue)
            query = query.Where(c => c.PetId == filtro.PetId);
        if (!string.IsNullOrWhiteSpace(filtro.Veterinario))
            query = query.Where(c => c.Veterinario.Contains(filtro.Veterinario));
        if (filtro.DataInicio.HasValue)
            query = query.Where(c => c.DataConsulta >= filtro.DataInicio);
        if (filtro.DataFim.HasValue)
            query = query.Where(c => c.DataConsulta <= filtro.DataFim);

        query = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "data" => query.OrdenarPor(c => c.DataConsulta, filtro.Descendente),
            "veterinario" => query.OrdenarPor(c => c.Veterinario, filtro.Descendente),
            _ => query.OrdenarPor(c => c.Id, filtro.Descendente)
        };

        return query.PaginarAsync(filtro, ct);
    }

    public Task AtualizarAsync(Consulta consulta, CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public async Task RemoverAsync(Consulta consulta, CancellationToken ct = default)
    {
        _context.Consultas.Remove(consulta);
        await _context.SaveChangesAsync(ct);
    }
}
