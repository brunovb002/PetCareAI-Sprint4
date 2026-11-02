using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Domain.Common;

/// <summary>Validações reutilizadas pelas entidades para manter suas invariantes.</summary>
internal static class Guard
{
    public static string TextoObrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException($"{campo} é obrigatório.");

        var texto = valor.Trim();
        if (texto.Length > tamanhoMaximo)
            throw new DomainException($"{campo} deve ter no máximo {tamanhoMaximo} caracteres.");

        return texto;
    }

    public static string TextoOpcional(string? valor, string campo, int tamanhoMaximo)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length > tamanhoMaximo)
            throw new DomainException($"{campo} deve ter no máximo {tamanhoMaximo} caracteres.");

        return texto;
    }
}
