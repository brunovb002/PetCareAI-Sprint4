using System.ComponentModel.DataAnnotations;

namespace PetCareAI.Application.DTOs;

public class RegistrarUsuarioDto
{
    /// <example>Bruno Vinicius</example>
    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <example>bruno@petcare.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <example>Senha@123</example>
    [Required, StringLength(100, MinimumLength = 6)]
    public string Senha { get; set; } = string.Empty;

    /// <summary>"Tutor" (padrão) ou "Veterinario".</summary>
    /// <example>Tutor</example>
    public string? Perfil { get; set; }
}

public class LoginDto
{
    /// <example>bruno@petcare.com</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Senha@123</example>
    [Required]
    public string Senha { get; set; } = string.Empty;
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Bearer";
    public DateTime ExpiraEm { get; set; }
    public UsuarioResponseDto Usuario { get; set; } = new();
}

/// <summary>Token gerado pela infraestrutura (JWT).</summary>
public record TokenGerado(string Token, DateTime ExpiraEm);
