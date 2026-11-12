using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Comum;
using Pedidos.Application.Pedidos;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Consultas;

internal sealed class PedidoConsultas(PedidosDbContext db) : IPedidoConsultas
{
    public async Task<PedidoDetalheResponse?> ObterDetalheAsync(int pedidoId, CancellationToken cancellationToken)
    {
        var cabecalho = await (
                from p in db.Pedidos.AsNoTracking()
                join c in db.Clientes on p.ClienteId equals c.Id
                where p.Id == pedidoId
                select new { p.Id, p.ClienteId, ClienteNome = c.Nome, p.DataPedido })
            .FirstOrDefaultAsync(cancellationToken);

        if (cabecalho is null)
        {
            return null;
        }

        var itens = await (
                from i in db.ItensPedido.AsNoTracking()
                join pr in db.Produtos on i.ProdutoId equals pr.Id
                where i.PedidoId == pedidoId
                orderby i.Id
                select new ItemPedidoResponse(i.ProdutoId, pr.Nome, i.Quantidade, i.PrecoUnitario, i.Quantidade * i.PrecoUnitario))
            .ToListAsync(cancellationToken);

        return new PedidoDetalheResponse(
            cabecalho.Id, cabecalho.ClienteId, cabecalho.ClienteNome, cabecalho.DataPedido, itens.Sum(i => i.Subtotal), itens);
    }

    /// <summary>
    /// Filtros só são aplicados quando informados, e o período vira intervalo
    /// semiaberto sobre a coluna, sem função (sargable). A paginação fica no banco
    /// (OFFSET/FETCH). A ordem é estável: data desc, Id desc.
    /// </summary>
    public async Task<PaginaResponse<PedidoResumoResponse>> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken)
    {
        var pedidos = db.Pedidos.AsNoTracking();

        if (query.ClienteId is { } clienteId)
        {
            pedidos = pedidos.Where(p => p.ClienteId == clienteId);
        }

        if (query.Periodo.InicioUtc is { } inicio)
        {
            pedidos = pedidos.Where(p => p.DataPedido >= inicio);
        }

        if (query.Periodo.FimExclusivoUtc is { } fimExclusivo)
        {
            pedidos = pedidos.Where(p => p.DataPedido < fimExclusivo);
        }

        var totalItens = await pedidos.CountAsync(cancellationToken);

        var pagina = await (
                from p in pedidos
                join c in db.Clientes on p.ClienteId equals c.Id
                orderby p.DataPedido descending, p.Id descending
                select new PedidoResumoResponse(
                    p.Id,
                    p.ClienteId,
                    c.Nome,
                    p.DataPedido,
                    p.Itens.Count,
                    p.Itens.Sum(i => i.Quantidade * i.PrecoUnitario)))
            .Skip((query.Pagina - 1) * query.TamanhoPagina)
            .Take(query.TamanhoPagina)
            .ToListAsync(cancellationToken);

        return new PaginaResponse<PedidoResumoResponse>(pagina, query.Pagina, query.TamanhoPagina, totalItens);
    }
}
