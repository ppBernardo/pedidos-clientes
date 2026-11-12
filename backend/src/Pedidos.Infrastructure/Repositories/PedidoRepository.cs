using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Domain.Comum;
using Pedidos.Domain.Pedidos;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

internal sealed class PedidoRepository(PedidosDbContext db) : IPedidoRepository
{
    /// <summary>
    /// RN-09: grava o pedido e baixa o estoque numa única transação (READ COMMITTED,
    /// com RCSI no banco).
    /// <list type="number">
    /// <item>A baixa é condicional (<c>Ativo = 1 AND Estoque &gt;= @quantidade</c>). O UPDATE
    /// trava a linha e reavalia o predicado sobre o valor atual, então duas requisições
    /// nunca vendem a mesma unidade.</item>
    /// <item>Os itens são baixados em ordem crescente de ProdutoId, e antes de inserir o
    /// pedido: os locks são tomados sempre na mesma ordem, o que evita deadlock entre
    /// pedidos com os mesmos produtos em ordens diferentes.</item>
    /// <item>Se algum item não puder ser baixado, a transação é desfeita (dispose sem
    /// commit) e nada é gravado.</item>
    /// </list>
    /// </summary>
    public async Task AdicionarComBaixaDeEstoqueAsync(Pedido pedido, CancellationToken cancellationToken)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in pedido.Itens.OrderBy(i => i.ProdutoId))
        {
            var linhasAfetadas = await db.Produtos
                .Where(p => p.Id == item.ProdutoId && p.Ativo && p.Estoque >= item.Quantidade)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estoque, p => p.Estoque - item.Quantidade), cancellationToken);

            if (linhasAfetadas == 0)
            {
                throw await ExplicarFalhaNaBaixaAsync(item, cancellationToken);
            }
        }

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// O snapshot dizia que dava, mas o UPDATE não achou a linha: outra requisição levou
    /// o estoque, ou o produto foi desativado ou removido. Relê dentro da transação
    /// para responder com o código e a mensagem exatos.
    /// </summary>
    private async Task<RegraNegocioException> ExplicarFalhaNaBaixaAsync(ItemPedido item, CancellationToken cancellationToken)
    {
        var atual = await db.Produtos
            .AsNoTracking()
            .Where(p => p.Id == item.ProdutoId)
            .Select(p => new { p.Nome, p.Ativo, p.Estoque })
            .FirstOrDefaultAsync(cancellationToken);

        return atual switch
        {
            null => ErrosDePedido.ProdutosNaoEncontrados([item.ProdutoId]),
            { Ativo: false } => ErrosDePedido.ProdutosInativos([atual.Nome]),
            _ => ErrosDePedido.EstoqueInsuficiente([new EstoqueFaltante(atual.Nome, item.Quantidade, atual.Estoque)]),
        };
    }
}
