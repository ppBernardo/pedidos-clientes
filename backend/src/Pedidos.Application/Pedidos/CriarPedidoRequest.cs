namespace Pedidos.Application.Pedidos;

/// <summary>Corpo de <c>POST /pedidos</c> (spec 5.1). O cliente da API não envia preço nem data.</summary>
public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoRequest(int ProdutoId, int Quantidade);
