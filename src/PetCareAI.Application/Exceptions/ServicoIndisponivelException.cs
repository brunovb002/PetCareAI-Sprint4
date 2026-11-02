namespace PetCareAI.Application.Exceptions;

/// <summary>Um serviço externo (ex.: CLYVO AI) não respondeu (mapeada para HTTP 503).</summary>
public class ServicoIndisponivelException : Exception
{
    public ServicoIndisponivelException(string message, Exception? inner = null) : base(message, inner) { }
}
