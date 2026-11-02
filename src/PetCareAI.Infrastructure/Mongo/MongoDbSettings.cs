namespace PetCareAI.Infrastructure.Mongo;

/// <summary>
/// Seção "MongoDb" da configuração. A ConnectionString é um segredo: em produção vem das
/// App Settings do Web App (MongoDb__ConnectionString), alimentadas pelo pipeline/Key Vault.
/// </summary>
public class MongoDbSettings
{
    public const string Secao = "MongoDb";

    public string? ConnectionString { get; set; }
    public string Database { get; set; } = "petcareai";
    public string ColecaoAtividades { get; set; } = "atividades";

    public bool Configurado => !string.IsNullOrWhiteSpace(ConnectionString);
}
