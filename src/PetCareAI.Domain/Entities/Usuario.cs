using System.Net.Mail;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Domain.Entities;

public class Usuario
{
    public const int NomeMax = 100;
    public const int EmailMax = 150;

    public const string PerfilTutor = "Tutor";
    public const string PerfilVeterinario = "Veterinario";

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string Perfil { get; private set; } = PerfilTutor;
    public DateTime CriadoEm { get; private set; }

    // Usado pelo Entity Framework
    private Usuario() { }

    public Usuario(string nome, string email, string perfil)
    {
        Nome = Guard.TextoObrigatorio(nome, "Nome", NomeMax);
        Email = NormalizarEmail(email);
        Perfil = ValidarPerfil(perfil);
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>O hash é gerado fora do domínio (Infrastructure), aqui só garantimos que existe.</summary>
    public void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("Hash de senha inválido.");

        SenhaHash = senhaHash;
    }

    public static string NormalizarEmail(string? email)
    {
        var texto = Guard.TextoObrigatorio(email, "E-mail", EmailMax).ToLowerInvariant();
        if (!MailAddress.TryCreate(texto, out var endereco) || endereco.Address != texto)
            throw new DomainException("E-mail em formato inválido.");

        return texto;
    }

    private static string ValidarPerfil(string? perfil)
    {
        if (string.Equals(perfil, PerfilVeterinario, StringComparison.OrdinalIgnoreCase))
            return PerfilVeterinario;
        if (string.IsNullOrWhiteSpace(perfil) || string.Equals(perfil, PerfilTutor, StringComparison.OrdinalIgnoreCase))
            return PerfilTutor;

        throw new DomainException($"Perfil deve ser '{PerfilTutor}' ou '{PerfilVeterinario}'.");
    }
}
