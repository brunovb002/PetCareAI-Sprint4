using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PetCareAI.Application.DTOs;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>ID do usuário autenticado, lido do token JWT.</summary>
    public static int ObterUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var id)
            ? id
            : throw new UnauthorizedAccessException("Token sem identificação de usuário.");
    }

    /// <summary>ID e perfil (Tutor ou Veterinario) do usuário autenticado, usados nas regras de autorização.</summary>
    public static UsuarioAtual ObterUsuarioAtual(this ClaimsPrincipal user) =>
        new(user.ObterUsuarioId(), user.FindFirstValue(ClaimTypes.Role) ?? Usuario.PerfilTutor);
}
