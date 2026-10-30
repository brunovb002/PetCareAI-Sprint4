using Microsoft.AspNetCore.Identity;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Infrastructure.Security;

/// <summary>Usa o PasswordHasher do ASP.NET Core Identity (PBKDF2 com salt).</summary>
public class SenhaHasher : ISenhaHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string GerarHash(Usuario usuario, string senha) => _hasher.HashPassword(usuario, senha);

    public bool Verificar(Usuario usuario, string senha) =>
        _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) != PasswordVerificationResult.Failed;
}
