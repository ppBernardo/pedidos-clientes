namespace Pedidos.Domain.Pedidos;

/// <summary>Item do pedido. Guarda o preço do produto no momento da compra (RN-10).</summary>
public sealed class ItemPedido
{
    private ItemPedido()
    {
    }

    internal ItemPedido(int produtoId, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public int Id { get; private set; }
    public int PedidoId { get; private set; }
    public int ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;
}
