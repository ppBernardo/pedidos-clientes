using Pedidos.Domain.Comum;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;

namespace Pedidos.UnitTests.Domain;

public sealed class PedidoTests
{
    private static readonly DateTime Agora = new(2026, 11, 12, 19, 4, 11, 678, DateTimeKind.Utc);

    private static readonly Dictionary<int, Produto> Catalogo = new()
    {
        [1] = new Produto(1, "Teclado mecânico", 149.90m, estoque: 10),
        [2] = new Produto(2, "Mouse sem fio", 90.00m, estoque: 3),
        [3] = new Produto(3, "Projetor portátil", 1899.00m, estoque: 5, ativo: false),
        [4] = new Produto(4, "Monitor ultrawide", 3299.00m, estoque: 0),
    };

    private static Pedido Criar(params ItemSolicitado[] itens) => Pedido.Criar(clienteId: 7, itens, Catalogo, Agora);

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("RN", "RN-10")]
    public void Criar_ComItensValidos_UsaPrecoAtualDoProdutoECalculaTotal()
    {
        var pedido = Criar(new ItemSolicitado(1, 2), new ItemSolicitado(2, 1));

        pedido.ClienteId.Should().Be(7);
        pedido.Itens.Should().HaveCount(2);
        pedido.Itens.Should().ContainSingle(i => i.ProdutoId == 1 && i.Quantidade == 2 && i.PrecoUnitario == 149.90m);
        pedido.ValorTotal.Should().Be(389.80m);
    }

    [Fact]
    [Trait("RN", "RN-11")]
    public void Criar_TruncaDataEmSegundosEMantemUtc()
    {
        var pedido = Criar(new ItemSolicitado(1, 1));

        pedido.DataPedido.Should().Be(new DateTime(2026, 11, 12, 19, 4, 11, DateTimeKind.Utc));
        pedido.DataPedido.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Criar_ComDataQueNaoEstaEmUtc_LancaArgumentException()
    {
        var agoraLocal = DateTime.SpecifyKind(Agora, DateTimeKind.Local);

        var acao = () => Pedido.Criar(7, [new ItemSolicitado(1, 1)], Catalogo, agoraLocal);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("RN", "RN-01")]
    public void Criar_SemItens_LancaPedidoInvalido()
    {
        var acao = () => Criar();

        acao.Should().Throw<RegraNegocioException>()
            .Which.Codigo.Should().Be(CodigosErro.PedidoInvalido);
    }

    [Fact]
    [Trait("RN", "RN-02")]
    public void Criar_ComMaisItensQueOLimite_LancaPedidoInvalido()
    {
        var itens = Enumerable.Range(1, Pedido.MaximoDeItens + 1).Select(id => new ItemSolicitado(id, 1)).ToArray();

        var acao = () => Criar(itens);

        acao.Should().Throw<RegraNegocioException>()
            .Which.Message.Should().Contain("no máximo 100 itens");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("CA", "CA-05")]
    [Trait("RN", "RN-06")]
    public void Criar_ComQuantidadeMenorOuIgualAZero_LancaPedidoInvalido(int quantidade)
    {
        var acao = () => Criar(new ItemSolicitado(1, quantidade));

        acao.Should().Throw<RegraNegocioException>()
            .Which.Codigo.Should().Be(CodigosErro.PedidoInvalido);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("RN", "RN-07")]
    public void Criar_ComProdutoRepetido_LancaPedidoInvalido()
    {
        var acao = () => Criar(new ItemSolicitado(1, 1), new ItemSolicitado(1, 2));

        acao.Should().Throw<RegraNegocioException>()
            .Which.Message.Should().Contain("aparece mais de uma vez");
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("RN", "RN-04")]
    public void Criar_ComProdutosInexistentes_LancaProdutoNaoEncontradoListandoTodos()
    {
        var acao = () => Criar(new ItemSolicitado(1, 1), new ItemSolicitado(98, 1), new ItemSolicitado(99, 1));

        var erro = acao.Should().Throw<RegraNegocioException>().Which;
        erro.Codigo.Should().Be(CodigosErro.ProdutoNaoEncontrado);
        erro.Message.Should().Contain("98, 99");
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("RN", "RN-05")]
    public void Criar_ComProdutoInativo_LancaProdutoInativo()
    {
        var acao = () => Criar(new ItemSolicitado(3, 1));

        var erro = acao.Should().Throw<RegraNegocioException>().Which;
        erro.Codigo.Should().Be(CodigosErro.ProdutoInativo);
        erro.Message.Should().Contain("Projetor portátil");
    }

    [Fact]
    [Trait("CA", "CA-03")]
    [Trait("RN", "RN-08")]
    public void Criar_ComEstoqueInsuficiente_LancaEstoqueInsuficienteListandoTodosOsItens()
    {
        var acao = () => Criar(new ItemSolicitado(1, 1), new ItemSolicitado(2, 5), new ItemSolicitado(4, 1));

        var erro = acao.Should().Throw<RegraNegocioException>().Which;
        erro.Codigo.Should().Be(CodigosErro.EstoqueInsuficiente);
        erro.Message.Should().Contain("'Mouse sem fio' (solicitado: 5, disponível: 3)")
            .And.Contain("'Monitor ultrawide' (solicitado: 1, disponível: 0)")
            .And.NotContain("Teclado");
    }

    [Fact]
    [Trait("RN", "RN-08")]
    public void Criar_ComQuantidadeIgualAoEstoque_Permite()
    {
        var pedido = Criar(new ItemSolicitado(2, 3));

        pedido.Itens.Should().ContainSingle().Which.Quantidade.Should().Be(3);
    }

    [Fact]
    public void Criar_ProdutoInexistenteTemPrioridadeSobreEstoque()
    {
        var acao = () => Criar(new ItemSolicitado(2, 50), new ItemSolicitado(99, 1));

        acao.Should().Throw<RegraNegocioException>()
            .Which.Codigo.Should().Be(CodigosErro.ProdutoNaoEncontrado);
    }
}
