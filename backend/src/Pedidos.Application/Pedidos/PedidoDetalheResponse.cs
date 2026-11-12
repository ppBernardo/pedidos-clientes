using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;

namespace Pedidos.Application.Pedidos;

/// <summary>Detalhe do pedido (spec 5.2), devolvido no 201 do POST e no GET /pedidos/{id}.</summary>
public sealed record PedidoDetalheResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    IReadOnlyList<ItemPedidoResponse> Itens)
{
    public static PedidoDetalheResponse De(Pedido pedido, string clienteNome, IReadOnlyDictionary<int, Produto> produtos) =>
        new(
            pedido.Id,
            pedido.ClienteId,
            clienteNome,
            pedido.DataPedido,
            pedido.ValorTotal,
            [.. pedido.Itens.Select(i => new ItemPedidoResponse(
                i.ProdutoId, produtos[i.ProdutoId].Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal))]);
}

public sealed record ItemPedidoResponse(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);
