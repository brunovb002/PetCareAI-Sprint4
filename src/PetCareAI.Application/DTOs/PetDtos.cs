using System.ComponentModel.DataAnnotations;

namespace PetCareAI.Application.DTOs;

public class CreatePetDto
{
    /// <example>Thor</example>
    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <example>Cachorro</example>
    [Required, StringLength(50)]
    public string Especie { get; set; } = string.Empty;

    /// <example>Golden Retriever</example>
    [Required, StringLength(80)]
    public string Raca { get; set; } = string.Empty;

    /// <example>3</example>
    [Range(0, 40)]
    public int Idade { get; set; }

    /// <example>28.5</example>
    [Range(0.01, 200)]
    public double Peso { get; set; }
}

public class UpdatePetDto : CreatePetDto { }

public class PetResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raca { get; set; } = string.Empty;
    public int Idade { get; set; }
    public double Peso { get; set; }
    public int UsuarioId { get; set; }
}
