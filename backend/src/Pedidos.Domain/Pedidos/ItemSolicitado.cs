namespace Pedidos.Domain.Pedidos;

/// <summary>Item como pedido pelo cliente da API: só produto e quantidade, nunca preço.</summary>
public sealed record ItemSolicitado(int ProdutoId, int Quantidade);
