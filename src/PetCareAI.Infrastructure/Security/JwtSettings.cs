namespace PetCareAI.Infrastructure.Security;

/// <summary>
/// Seção "Jwt" da configuração. A Key é um segredo e NÃO fica no appsettings.json:
/// localmente vem do user-secrets, na Azure vem das App Settings (Jwt__Key) alimentadas pelo pipeline/Key Vault.
/// </summary>
public class JwtSettings
{
    public const string Secao = "Jwt";
    public const int TamanhoMinimoChave = 32;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PetCareAI";
    public string Audience { get; set; } = "PetCareAI.Clientes";
    public int ExpiracaoMinutos { get; set; } = 120;
}
