using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Persistence;

namespace PetCareAI.Infrastructure.Repositories;

public class PetRepository : IPetRepository
{
    private readonly AppDbContext _context;

    public PetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Pet> AdicionarAsync(Pet pet, CancellationToken ct = default)
    {
        await _context.Pets.AddAsync(pet, ct);
        await _context.SaveChangesAsync(ct);
        return pet;
    }

    public Task<Pet?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _context.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PagedResult<Pet>> ListarAsync(PetFiltro filtro, CancellationToken ct = default)
    {
        var query = _context.Pets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Nome))
            query = query.Where(p => p.Nome.Contains(filtro.Nome));
        if (!string.IsNullOrWhiteSpace(filtro.Especie))
            query = query.Where(p => p.Especie == filtro.Especie);
        if (filtro.UsuarioId.HasValue)
            query = query.Where(p => p.UsuarioId == filtro.UsuarioId);

        query = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "nome" => query.OrdenarPor(p => p.Nome, filtro.Descendente),
            "especie" => query.OrdenarPor(p => p.Especie, filtro.Descendente),
            "idade" => query.OrdenarPor(p => p.Idade, filtro.Descendente),
            "peso" => query.OrdenarPor(p => p.Peso, filtro.Descendente),
            _ => query.OrdenarPor(p => p.Id, filtro.Descendente)
        };

        return query.PaginarAsync(filtro, ct);
    }

    public Task AtualizarAsync(Pet pet, CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public async Task RemoverAsync(Pet pet, CancellationToken ct = default)
    {
        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync(ct);
    }
}
