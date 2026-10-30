using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Interfaces;
using PetCareAI.Infrastructure.Mongo;
using PetCareAI.Infrastructure.Persistence;
using PetCareAI.Infrastructure.Repositories;
using PetCareAI.Infrastructure.Security;

namespace PetCareAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Relacional (Azure SQL / SQL Server). Connection string vem de variável de ambiente / App Service.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // NoSQL (MongoDB) para o histórico de atividades
        var mongo = configuration.GetSection(MongoDbSettings.Secao).Get<MongoDbSettings>() ?? new MongoDbSettings();
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.Secao));
        if (mongo.Configurado)
        {
            services.AddSingleton<IMongoClient>(_ =>
            {
                var settings = MongoClientSettings.FromConnectionString(mongo.ConnectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                return new MongoClient(settings);
            });
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongo.Database));
            services.AddScoped<IRegistroAtividadeRepository, MongoRegistroAtividadeRepository>();
        }
        else
        {
            services.AddScoped<IRegistroAtividadeRepository, AtividadeDesabilitadaRepository>();
        }

        // Segurança (JWT + hash de senha)
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.Secao))
            .Validate(s => s.Key.Length >= JwtSettings.TamanhoMinimoChave,
                $"Configure 'Jwt:Key' com pelo menos {JwtSettings.TamanhoMinimoChave} caracteres (user-secrets ou variável de ambiente Jwt__Key).")
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISenhaHasher, SenhaHasher>();

        return services;
    }

    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        builder.AddDbContextCheck<AppDbContext>("sqlserver", tags: ["ready"]);

        var mongo = configuration.GetSection(MongoDbSettings.Secao).Get<MongoDbSettings>();
        if (mongo?.Configurado == true)
            builder.AddCheck<MongoHealthCheck>("mongodb", tags: ["ready"]);

        return builder;
    }
}
