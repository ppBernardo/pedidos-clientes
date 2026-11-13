using System.Net;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Pedidos;

/// <summary>RN-09 sob carga real: requisições simultâneas contra o mesmo SQL Server.</summary>
public sealed class ConcorrenciaTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("RN", "RN-09")]
    public async Task Post_VinteRequisicoesSimultaneasParaEstoqueCinco_CriaExatamenteCinco()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Placa de vídeo RTX", 5499m, estoque: 5);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Api.CriarPedidoAsync(cliente, Cancelamento, (produto, 1))));

        respostas.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(5);
        respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity).Should().Be(15);
        (await Banco.EstoqueAsync(produto)).Should().Be(0);
        (await Banco.ContarAsync("Pedidos")).Should().Be(5);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("RN", "RN-09")]
    public async Task Post_PedidosSimultaneosComOsMesmosProdutosEmOrdemCruzada_NaoTemDeadlock()
    {
        var cliente = await Banco.CriarClienteAsync();
        var a = await Banco.CriarProdutoAsync("A", 10m, estoque: 100);
        var b = await Banco.CriarProdutoAsync("B", 10m, estoque: 100);

        // Metade pede [A, B] e metade [B, A]. Sem a baixa ordenada por ProdutoId,
        // isso é a receita clássica de deadlock.
        var respostas = await Task.WhenAll(Enumerable.Range(0, 30).Select(i => i % 2 == 0
            ? Api.CriarPedidoAsync(cliente, Cancelamento, (a, 1), (b, 1))
            : Api.CriarPedidoAsync(cliente, Cancelamento, (b, 1), (a, 1))));

        respostas.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);
        (await Banco.EstoqueAsync(a)).Should().Be(70);
        (await Banco.EstoqueAsync(b)).Should().Be(70);
    }
}
