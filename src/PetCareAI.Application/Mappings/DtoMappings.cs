using PetCareAI.Application.DTOs;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Application.Mappings;

public static class DtoMappings
{
    public static PetResponseDto ToDto(this Pet pet) => new()
    {
        Id = pet.Id,
        Nome = pet.Nome,
        Especie = pet.Especie,
        Raca = pet.Raca,
        Idade = pet.Idade,
        Peso = pet.Peso,
        UsuarioId = pet.UsuarioId
    };

    public static ConsultaResponseDto ToDto(this Consulta consulta) => new()
    {
        Id = consulta.Id,
        PetId = consulta.PetId,
        DataConsulta = consulta.DataConsulta,
        Veterinario = consulta.Veterinario,
        Motivo = consulta.Motivo,
        Diagnostico = consulta.Diagnostico
    };

    public static UsuarioResponseDto ToDto(this Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        Perfil = usuario.Perfil
    };

    public static AtividadeResponseDto ToDto(this RegistroAtividade registro) => new()
    {
        Id = registro.Id,
        Entidade = registro.Entidade,
        EntidadeId = registro.EntidadeId,
        Acao = registro.Acao,
        UsuarioId = registro.UsuarioId,
        Descricao = registro.Descricao,
        DataHora = registro.DataHora
    };
}
