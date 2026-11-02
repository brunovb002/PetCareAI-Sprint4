using System.ComponentModel.DataAnnotations;

namespace PetCareAI.Application.DTOs;

public class CreateConsultaDto
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int PetId { get; set; }

    /// <example>2026-11-04T10:00:00</example>
    [Required]
    public DateTime DataConsulta { get; set; }

    /// <example>Dra. Carla Souza</example>
    [Required, StringLength(120)]
    public string Veterinario { get; set; } = string.Empty;

    /// <example>Consulta de rotina</example>
    [Required, StringLength(200)]
    public string Motivo { get; set; } = string.Empty;

    /// <example>Animal saudável, vacinação em dia</example>
    [StringLength(400)]
    public string? Diagnostico { get; set; }
}

public class UpdateConsultaDto
{
    [Required]
    public DateTime DataConsulta { get; set; }

    [Required, StringLength(120)]
    public string Veterinario { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Diagnostico { get; set; }
}

public class ConsultaResponseDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public DateTime DataConsulta { get; set; }
    public string Veterinario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
}
