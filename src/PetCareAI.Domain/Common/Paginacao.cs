namespace PetCareAI.Domain.Common;

/// <summary>Parâmetros comuns de paginação e ordenação das consultas.</summary>
public abstract class FiltroPaginado
{
    public const int TamanhoPaginaMaximo = 50;

    private int _pagina = 1;
    private int _tamanhoPagina = 10;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanhoPagina
    {
        get => _tamanhoPagina;
        set => _tamanhoPagina = Math.Clamp(value, 1, TamanhoPaginaMaximo);
    }

    /// <summary>Campo usado na ordenação (cada filtro define os campos aceitos).</summary>
    public string? OrdenarPor { get; set; }

    /// <summary>"asc" (padrão) ou "desc".</summary>
    public string? Direcao { get; set; }

    public bool Descendente => string.Equals(Direcao, "desc", StringComparison.OrdinalIgnoreCase);

    public int Saltar => (Pagina - 1) * TamanhoPagina;
}

/// <summary>Uma página de resultados e os metadados para navegar entre páginas.</summary>
public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> itens, int totalItens, int pagina, int tamanhoPagina)
    {
        Itens = itens;
        TotalItens = totalItens;
        Pagina = pagina;
        TamanhoPagina = tamanhoPagina;
    }

    public IReadOnlyList<T> Itens { get; }
    public int TotalItens { get; }
    public int Pagina { get; }
    public int TamanhoPagina { get; }

    public int TotalPaginas => TotalItens == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
    public bool TemAnterior => Pagina > 1;
    public bool TemProxima => Pagina < TotalPaginas;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> conversor) =>
        new(Itens.Select(conversor).ToList(), TotalItens, Pagina, TamanhoPagina);
}
