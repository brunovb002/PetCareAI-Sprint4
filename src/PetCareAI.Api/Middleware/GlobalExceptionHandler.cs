using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetCareAI.Application.Exceptions;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Api.Middleware;

/// <summary>
/// Tratamento global de exceções: converte qualquer exceção em uma resposta ProblemDetails (RFC 7807)
/// com o status HTTP correto, sem try/catch espalhado pelos controllers.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Regra de negócio violada"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito"),
            CredenciaisInvalidasException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            AcessoNegadoException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            ServicoIndisponivelException => (StatusCodes.Status503ServiceUnavailable, "Serviço indisponível"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor")
        };

        if (status >= 500)
            _logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("Requisição {Metodo} {Caminho} retornou {Status}: {Mensagem}",
                context.Request.Method, context.Request.Path, status, exception.Message);

        context.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                // Em erro 500 não expomos detalhes internos ao cliente
                Detail = status >= 500 ? "Ocorreu um erro inesperado. Tente novamente mais tarde." : exception.Message,
                Instance = context.Request.Path
            }
        });
    }
}
