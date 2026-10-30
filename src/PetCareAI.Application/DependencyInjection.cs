using Microsoft.Extensions.DependencyInjection;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Services;

namespace PetCareAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IConsultaService, ConsultaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAtividadeService, AtividadeService>();
        return services;
    }
}
