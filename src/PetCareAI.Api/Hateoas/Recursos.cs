using PetCareAI.Domain.Common;

namespace PetCareAI.Api.Hateoas;

/// <summary>Link HATEOAS: para onde o cliente pode navegar a partir deste recurso.</summary>
public record LinkDto(string Href, string Rel, string Metodo);

/// <summary>Um recurso com seus dados e as ações disponíveis.</summary>
public class Recurso<T>
{
    public Recurso(T dados, IReadOnlyList<LinkDto> links)
    {
        Dados = dados;
        Links = links;
    }

    public T Dados { get; }
    public IReadOnlyList<LinkDto> Links { get; }
}

public class PaginacaoDto
{
    public int Pagina { get; init; }
    public int TamanhoPagina { get; init; }
    public int TotalItens { get; init; }
    public int TotalPaginas { get; init; }
}

/// <summary>Página de recursos com links de navegação (primeira, anterior, próxima, última).</summary>
public class RecursoPaginado<T>
{
    public IReadOnlyList<Recurso<T>> Itens { get; init; } = [];
    public PaginacaoDto Paginacao { get; init; } = new();
    public IReadOnlyList<LinkDto> Links { get; init; } = [];
}

public static class HateoasExtensions
{
    /// <param name="pagina">Resultado paginado vindo da camada de aplicação.</param>
    /// <param name="linksDoItem">Gera os links de cada item.</param>
    /// <param name="urlDaPagina">Gera a URL da listagem para um número de página, mantendo filtros e ordenação.</param>
    public static RecursoPaginado<T> ParaRecursoPaginado<T>(this PagedResult<T> pagina,
        Func<T, IReadOnlyList<LinkDto>> linksDoItem, Func<int, string> urlDaPagina)
    {
        var links = new List<LinkDto> { new(urlDaPagina(pagina.Pagina), "self", "GET") };

        if (pagina.TotalPaginas > 0)
            links.Add(new(urlDaPagina(1), "primeira", "GET"));
        if (pagina.TemAnterior)
            links.Add(new(urlDaPagina(pagina.Pagina - 1), "anterior", "GET"));
        if (pagina.TemProxima)
            links.Add(new(urlDaPagina(pagina.Pagina + 1), "proxima", "GET"));
        if (pagina.TotalPaginas > 0)
            links.Add(new(urlDaPagina(pagina.TotalPaginas), "ultima", "GET"));

        return new RecursoPaginado<T>
        {
            Itens = pagina.Itens.Select(i => new Recurso<T>(i, linksDoItem(i))).ToList(),
            Paginacao = new PaginacaoDto
            {
                Pagina = pagina.Pagina,
                TamanhoPagina = pagina.TamanhoPagina,
                TotalItens = pagina.TotalItens,
                TotalPaginas = pagina.TotalPaginas
            },
            Links = links
        };
    }
}
