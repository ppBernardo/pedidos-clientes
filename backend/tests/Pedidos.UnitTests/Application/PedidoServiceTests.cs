using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Pedidos;
using Pedidos.Domain.Comum;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;

namespace Pedidos.UnitTests.Application;

public sealed class PedidoServiceTests
{
    private const int ClienteExistente = 7;

    private static readonly Dictionary<int, Produto> Catalogo = new()
    {
        [1] = new Produto(1, "Teclado mecânico", 149.90m, estoque: 10),
        [2] = new Produto(2, "Mouse sem fio", 90.00m, estoque: 3),
    };

    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 12, 19, 4, 11, 678, TimeSpan.Zero));
    private readonly PedidoService _servico;

    public PedidoServiceTests()
    {
        _servico = new PedidoService(_clientes, _produtos, _pedidos, _relogio);

        _clientes.ObterNomeAsync(ClienteExistente, Arg.Any<CancellationToken>()).Returns("Ana Souza");
        _produtos.ObterPorIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(chamada =>
            {
                var ids = chamada.Arg<IReadOnlyCollection<int>>();
                return (IReadOnlyDictionary<int, Produto>)Catalogo.Where(p => ids.Contains(p.Key)).ToDictionary();
            });
    }

    private static CriarPedidoRequest Pedido(int clienteId, ItemPedidoRequest[] itens) => new(clienteId, itens);

    [Fact]
    [Trait("CA", "CA-01")]
    public async Task CriarAsync_PedidoValido_GravaComDataDoServidorERetornaDetalhe()
    {
        var resposta = await _servico.CriarAsync(
            Pedido(ClienteExistente, [new(1, 2), new(2, 1)]), TestContext.Current.CancellationToken);

        await _pedidos.Received(1).AdicionarComBaixaDeEstoqueAsync(
            Arg.Is<Pedido>(p => p.ClienteId == ClienteExistente && p.Itens.Count == 2),
            Arg.Any<CancellationToken>());

        resposta.ClienteNome.Should().Be("Ana Souza");
        resposta.DataPedido.Should().Be(new DateTime(2026, 11, 12, 19, 4, 11, DateTimeKind.Utc));
        resposta.ValorTotal.Should().Be(389.80m);
        resposta.Itens.Should().BeEquivalentTo(
        [
            new ItemPedidoResponse(1, "Teclado mecânico", 2, 149.90m, 299.80m),
            new ItemPedidoResponse(2, "Mouse sem fio", 1, 90.00m, 90.00m),
        ]);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-03")]
    public async Task CriarAsync_ClienteInexistente_LancaClienteNaoEncontradoENaoGrava()
    {
        // Explícito: sem configuração, o NSubstitute devolveria "" (e não null) para string.
        _clientes.ObterNomeAsync(999, Arg.Any<CancellationToken>()).Returns((string?)null);

        var acao = () => _servico.CriarAsync(Pedido(999, [new(1, 1)]), TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<RegraNegocioException>())
            .Which.Codigo.Should().Be(CodigosErro.ClienteNaoEncontrado);
        await _pedidos.DidNotReceive().AdicionarComBaixaDeEstoqueAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
        await _produtos.DidNotReceive().ObterPorIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("RN", "RN-04")]
    public async Task CriarAsync_ProdutoInexistente_LancaProdutoNaoEncontradoENaoGrava()
    {
        var acao = () => _servico.CriarAsync(Pedido(ClienteExistente, [new(1, 1), new(42, 1)]), TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<RegraNegocioException>())
            .Which.Codigo.Should().Be(CodigosErro.ProdutoNaoEncontrado);
        await _pedidos.DidNotReceive().AdicionarComBaixaDeEstoqueAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("CA", "CA-03")]
    [Trait("RN", "RN-08")]
    public async Task CriarAsync_EstoqueInsuficienteNoSnapshot_LancaEstoqueInsuficienteENaoGrava()
    {
        var acao = () => _servico.CriarAsync(Pedido(ClienteExistente, [new(1, 1), new(2, 4)]), TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<RegraNegocioException>())
            .Which.Codigo.Should().Be(CodigosErro.EstoqueInsuficiente);
        await _pedidos.DidNotReceive().AdicionarComBaixaDeEstoqueAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN-09")]
    public async Task CriarAsync_EstoqueConsumidoPorOutraRequisicaoNaBaixa_PropagaEstoqueInsuficiente()
    {
        // O snapshot tinha estoque, mas outra requisição levou as unidades antes da
        // baixa condicional: o repositório desfaz a transação e sinaliza.
        _pedidos.AdicionarComBaixaDeEstoqueAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ErrosDePedido.EstoqueInsuficiente([new EstoqueFaltante("Mouse sem fio", 3, 1)]));

        var acao = () => _servico.CriarAsync(Pedido(ClienteExistente, [new(2, 3)]), TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<RegraNegocioException>())
            .Which.Codigo.Should().Be(CodigosErro.EstoqueInsuficiente);
    }

    [Fact]
    public async Task CriarAsync_ConsultaOSnapshotSoComOsProdutosDoPedido()
    {
        await _servico.CriarAsync(Pedido(ClienteExistente, [new(2, 1)]), TestContext.Current.CancellationToken);

        await _produtos.Received(1).ObterPorIdsAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 2 })),
            Arg.Any<CancellationToken>());
    }
}
