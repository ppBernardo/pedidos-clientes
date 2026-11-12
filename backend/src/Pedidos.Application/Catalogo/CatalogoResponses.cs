namespace Pedidos.Application.Catalogo;

/// <summary>Item de <c>GET /clientes</c> (spec 5.5).</summary>
public sealed record ClienteResponse(int Id, string Nome, string Email);

/// <summary>Item de <c>GET /produtos</c> (spec 5.5): só produtos ativos.</summary>
public sealed record ProdutoResponse(int Id, string Nome, decimal Preco, int Estoque);
