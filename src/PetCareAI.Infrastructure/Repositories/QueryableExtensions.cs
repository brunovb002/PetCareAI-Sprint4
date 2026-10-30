using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Common;

namespace PetCareAI.Infrastructure.Repositories;

internal static class QueryableExtensions
{
    public static IQueryable<T> OrdenarPor<T, TChave>(this IQueryable<T> query, Expression<Func<T, TChave>> chave, bool descendente) =>
        descendente ? query.OrderByDescending(chave) : query.OrderBy(chave);

    public static async Task<PagedResult<T>> PaginarAsync<T>(this IQueryable<T> query, FiltroPaginado filtro, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var itens = await query.Skip(filtro.Saltar).Take(filtro.TamanhoPagina).ToListAsync(ct);
        return new PagedResult<T>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
    }
}
