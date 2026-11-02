using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Application.DTOs;

/// <summary>Usuário autenticado (lido do token JWT) e as regras de autorização sobre os pets.</summary>
public record UsuarioAtual(int Id, string Perfil)
{
    public bool EhVeterinario => Perfil == Usuario.PerfilVeterinario;

    /// <summary>O tutor gerencia só os próprios pets; o veterinário gerencia os de todos.</summary>
    public void GarantirAcessoAoPet(Pet pet)
    {
        if (!EhVeterinario && pet.UsuarioId != Id)
            throw new AcessoNegadoException("Apenas o tutor do pet ou um veterinário pode realizar esta operação.");
    }
}
