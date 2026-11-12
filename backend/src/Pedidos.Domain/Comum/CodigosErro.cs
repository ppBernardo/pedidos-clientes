namespace Pedidos.Domain.Comum;

/// <summary>Códigos estáveis dos erros de regra de negócio (seção 4 da spec).</summary>
public static class CodigosErro
{
    /// <summary>Invariante do pedido violada (RN-01, 02, 06, 07). Normalmente barrada antes pela validação (400).</summary>
    public const string PedidoInvalido = "PEDIDO_INVALIDO";

    public const string ClienteNaoEncontrado = "CLIENTE_NAO_ENCONTRADO";
    public const string ProdutoNaoEncontrado = "PRODUTO_NAO_ENCONTRADO";
    public const string ProdutoInativo = "PRODUTO_INATIVO";
    public const string EstoqueInsuficiente = "ESTOQUE_INSUFICIENTE";
}
