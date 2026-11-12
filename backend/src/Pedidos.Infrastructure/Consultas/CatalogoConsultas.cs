using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Catalogo;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Consultas;

/// <summary>
/// Listas de apoio ao frontend (spec 5.5). Devolvem tudo, sem paginação, o que é
/// adequado ao volume da demo; com milhares de registros, viraria busca paginada.
/// </summary>
internal sealed class CatalogoConsultas(PedidosDbContext db) : ICatalogoConsultas
{
    public async Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken) =>
        await db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Id) // homônimos em ordem estável
            .Select(c => new ClienteResponse(c.Id, c.Nome, c.Email))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAtivosAsync(CancellationToken cancellationToken) =>
        await db.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .ThenBy(p => p.Id)
            .Select(p => new ProdutoResponse(p.Id, p.Nome, p.Preco, p.Estoque))
            .ToListAsync(cancellationToken);
}
