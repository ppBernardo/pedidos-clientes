namespace Pedidos.Application.Pedidos;

/// <summary>Linha da listagem (spec 5.3). <see cref="QuantidadeItens"/> é o número de linhas de item.</summary>
public sealed record PedidoResumoResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    int QuantidadeItens,
    decimal ValorTotal);
