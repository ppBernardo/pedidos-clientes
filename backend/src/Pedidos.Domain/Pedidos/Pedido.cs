using Pedidos.Domain.Produtos;

namespace Pedidos.Domain.Pedidos;

/// <summary>Pedido de um cliente. O total não é armazenado: vem dos itens.</summary>
public sealed class Pedido
{
    /// <summary>Limite de proteção da API (RN-02).</summary>
    public const int MaximoDeItens = 100;

    private readonly List<ItemPedido> _itens = [];

    private Pedido()
    {
    }

    public int Id { get; private set; }
    public int ClienteId { get; private set; }

    /// <summary>Instante da criação em UTC, truncado em segundos (RN-11).</summary>
    public DateTime DataPedido { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();
    public decimal ValorTotal => _itens.Sum(i => i.Subtotal);

    /// <summary>
    /// Cria o pedido garantindo as invariantes (RN-01, 02, 06, 07) e as regras que
    /// dependem do estado dos produtos (RN-04, 05, 08), avaliadas sobre o snapshot
    /// <paramref name="produtos"/>. O preço de cada item é o preço atual do produto
    /// (RN-10). A garantia final de estoque sob concorrência é a baixa condicional
    /// no banco (RN-09).
    /// </summary>
    public static Pedido Criar(
        int clienteId,
        IReadOnlyCollection<ItemSolicitado> itens,
        IReadOnlyDictionary<int, Produto> produtos,
        DateTime agoraUtc)
    {
        if (agoraUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A data do pedido deve estar em UTC.", nameof(agoraUtc));
        }

        ValidarInvariantes(itens);
        ValidarProdutos(itens, produtos);

        var pedido = new Pedido { ClienteId = clienteId, DataPedido = TruncarEmSegundos(agoraUtc) };
        foreach (var item in itens)
        {
            pedido._itens.Add(new ItemPedido(item.ProdutoId, item.Quantidade, produtos[item.ProdutoId].Preco));
        }

        return pedido;
    }

    private static void ValidarInvariantes(IReadOnlyCollection<ItemSolicitado> itens)
    {
        if (itens.Count == 0)
        {
            throw ErrosDePedido.PedidoInvalido("O pedido deve ter ao menos um item.");
        }

        if (itens.Count > MaximoDeItens)
        {
            throw ErrosDePedido.PedidoInvalido($"O pedido aceita no máximo {MaximoDeItens} itens.");
        }

        if (itens.Any(i => i.Quantidade <= 0))
        {
            throw ErrosDePedido.PedidoInvalido("A quantidade de cada item deve ser maior que zero.");
        }

        var repetido = itens.GroupBy(i => i.ProdutoId).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
        {
            throw ErrosDePedido.PedidoInvalido(
                $"O produto {repetido.Key} aparece mais de uma vez; aumente a quantidade em um único item.");
        }
    }

    private static void ValidarProdutos(IReadOnlyCollection<ItemSolicitado> itens, IReadOnlyDictionary<int, Produto> produtos)
    {
        var inexistentes = itens.Where(i => !produtos.ContainsKey(i.ProdutoId)).Select(i => i.ProdutoId).ToList();
        if (inexistentes.Count > 0)
        {
            throw ErrosDePedido.ProdutosNaoEncontrados(inexistentes);
        }

        var inativos = itens.Select(i => produtos[i.ProdutoId]).Where(p => !p.Ativo).Select(p => p.Nome).ToList();
        if (inativos.Count > 0)
        {
            throw ErrosDePedido.ProdutosInativos(inativos);
        }

        var faltas = itens
            .Select(i => (Item: i, Produto: produtos[i.ProdutoId]))
            .Where(x => x.Produto.Estoque < x.Item.Quantidade)
            .Select(x => new EstoqueFaltante(x.Produto.Nome, x.Item.Quantidade, x.Produto.Estoque))
            .ToList();
        if (faltas.Count > 0)
        {
            throw ErrosDePedido.EstoqueInsuficiente(faltas);
        }
    }

    /// <summary><c>datetime2(0)</c> arredondaria os milissegundos; truncar mantém o dia e o que foi devolvido.</summary>
    private static DateTime TruncarEmSegundos(DateTime data) =>
        new(data.Ticks - (data.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
}
