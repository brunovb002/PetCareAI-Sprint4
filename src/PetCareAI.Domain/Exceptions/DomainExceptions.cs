namespace PetCareAI.Domain.Exceptions;

/// <summary>Violação de uma regra de negócio (mapeada para HTTP 400).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Recurso não encontrado (mapeada para HTTP 404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string recurso, object id)
        : base($"{recurso} com ID {id} não foi encontrado(a).") { }
}

/// <summary>Conflito com o estado atual, ex.: e-mail já cadastrado (mapeada para HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Credenciais de login inválidas (mapeada para HTTP 401).</summary>
public class CredenciaisInvalidasException : Exception
{
    public CredenciaisInvalidasException() : base("E-mail ou senha inválidos.") { }
}
