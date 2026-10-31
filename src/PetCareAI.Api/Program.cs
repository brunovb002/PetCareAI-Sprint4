using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PetCareAI.Api.Extensions;
using PetCareAI.Api.Middleware;
using PetCareAI.Application;
using PetCareAI.Infrastructure;
using PetCareAI.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging estruturado (Serilog): console legível + arquivo em JSON
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Aplicacao", "PetCareAI.Api")
    .Enrich.WithProperty("Ambiente", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/petcareai-.json",
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

// 2. Camadas (Clean Architecture) via injeção de dependência
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// 3. Segurança, API e documentação
builder.Services.AddJwtAuthentication();
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// O App Service termina o HTTPS no proxy; isso garante links HATEOAS com https://
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 4. Health checks: /health/live (a API está de pé) e /health (inclui bancos SQL e MongoDB)
builder.Services.AddHealthChecks()
    .AddCheck("api", () => HealthCheckResult.Healthy("API PetCareAI operacional."), tags: ["live"])
    .AddInfrastructureHealthChecks(builder.Configuration);

// 5. Observabilidade (OpenTelemetry): métricas em /metrics e tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("PetCareAI.Api")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment())
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Swagger habilitado também em produção: a entrega exige demonstrar os endpoints no app publicado
app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "PetCareAI API - CLYVO VET");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Falha ao aplicar as migrations. Confira a connection string 'DefaultConnection'.");
        throw;
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapControllers();

app.Run();

public partial class Program { }
