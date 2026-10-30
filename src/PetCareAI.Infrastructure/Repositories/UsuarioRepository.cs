using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Persistence;

namespace PetCareAI.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> AdicionarAsync(Usuario usuario, CancellationToken ct = default)
    {
        await _context.Usuarios.AddAsync(usuario, ct);
        await _context.SaveChangesAsync(ct);
        return usuario;
    }

    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct = default) =>
        _context.Usuarios.AnyAsync(u => u.Email == email, ct);
}
