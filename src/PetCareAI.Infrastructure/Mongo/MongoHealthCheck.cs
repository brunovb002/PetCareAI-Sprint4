using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PetCareAI.Infrastructure.Mongo;

/// <summary>Verifica se o MongoDB responde a um comando "ping".</summary>
public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
            return HealthCheckResult.Healthy("MongoDB respondeu ao ping.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB indisponível.", ex);
        }
    }
}
