using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Domain.Produtos;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

internal sealed class ProdutoRepository(PedidosDbContext db) : IProdutoRepository
{
    /// <summary>
    /// Snapshot sem rastreamento: a baixa de estoque é um UPDATE direto no banco,
    /// fora do change tracker, e um produto rastreado ficaria com estoque obsoleto.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, Produto>> ObterPorIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await db.Produtos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
}
