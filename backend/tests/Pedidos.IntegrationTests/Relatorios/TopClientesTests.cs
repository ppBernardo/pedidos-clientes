using System.Net;
using System.Net.Http.Json;
using Pedidos.Application.Relatorios;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Relatorios;

public sealed class TopClientesTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    private const string Ano2026 = "?dataInicio=2026-01-01&dataFim=2026-12-31";

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    private async Task<List<TopClienteResponse>> RankingAsync(string query = Ano2026) =>
        (await Api.GetFromJsonAsync<List<TopClienteResponse>>($"/relatorios/top-clientes{query}", Cancelamento))!;

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("RN", "RN-14")]
    public async Task Get_DevolveOsCincoMaioresDoMaiorParaOMenor()
    {
        var produto = await Banco.CriarProdutoAsync("Item", 1m, estoque: 10_000);
        var clientes = new List<int>();
        for (var i = 1; i <= 7; i++)
        {
            var cliente = await Banco.CriarClienteAsync($"Cliente {i}");
            clientes.Add(cliente);
            await Banco.CriarPedidoAsync(cliente, Utc(2026, 6, i), (produto, i * 100, 1m));
        }

        var ranking = await RankingAsync();

        ranking.Should().HaveCount(5);
        ranking.Select(r => r.ClienteId).Should().Equal(clientes[6], clientes[5], clientes[4], clientes[3], clientes[2]);
        ranking.Select(r => r.Posicao).Should().Equal(1, 2, 3, 4, 5);
        ranking[0].ClienteNome.Should().Be("Cliente 7");
        ranking[0].ValorTotal.Should().Be(700m);
        ranking[0].QuantidadePedidos.Should().Be(1);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    public async Task Get_ClientesHomonimosAparecemSeparados()
    {
        // Bug da query original (GROUP BY c.Nome): as duas Marias virariam uma só
        // com 500 e passariam o João.
        var produto = await Banco.CriarProdutoAsync("Item", 1m, estoque: 10_000);
        var maria1 = await Banco.CriarClienteAsync("Maria Silva");
        var maria2 = await Banco.CriarClienteAsync("Maria Silva");
        var joao = await Banco.CriarClienteAsync("João Pereira");
        await Banco.CriarPedidoAsync(maria1, Utc(2026, 2, 1), (produto, 300, 1m));
        await Banco.CriarPedidoAsync(maria2, Utc(2026, 3, 1), (produto, 200, 1m));
        await Banco.CriarPedidoAsync(joao, Utc(2026, 4, 1), (produto, 400, 1m));

        var ranking = await RankingAsync();

        ranking.Select(r => (r.ClienteId, r.ValorTotal)).Should().Equal((joao, 400m), (maria1, 300m), (maria2, 200m));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("RN", "RN-11")]
    public async Task Get_IgnoraPedidosForaDoPeriodoRespeitandoAsFronteiras()
    {
        var produto = await Banco.CriarProdutoAsync("Item", 1m, estoque: 10_000);
        var cliente = await Banco.CriarClienteAsync("Ana Souza");
        await Banco.CriarPedidoAsync(cliente, Utc(2025, 12, 31, 23, 59, 59), (produto, 1000, 1m)); // fora
        await Banco.CriarPedidoAsync(cliente, Utc(2026, 1, 1, 0, 0, 0), (produto, 10, 1m));        // dentro
        await Banco.CriarPedidoAsync(cliente, Utc(2026, 12, 31, 23, 59, 59), (produto, 20, 1m));   // dentro: BETWEEN '...-12-31' perderia
        await Banco.CriarPedidoAsync(cliente, Utc(2027, 1, 1, 0, 0, 0), (produto, 1000, 1m));     // fora

        var ranking = await RankingAsync();

        ranking.Should().ContainSingle().Which.Should().Be(new TopClienteResponse(1, cliente, "Ana Souza", 2, 30m));
    }

    [Fact]
    public async Task Get_QuantidadeDePedidosContaPedidosENaoItens()
    {
        var cliente = await Banco.CriarClienteAsync();
        var a = await Banco.CriarProdutoAsync("A", 10m, estoque: 100);
        var b = await Banco.CriarProdutoAsync("B", 20m, estoque: 100);
        var c = await Banco.CriarProdutoAsync("C", 30m, estoque: 100);
        await Banco.CriarPedidoAsync(cliente, Utc(2026, 5, 1), (a, 1, 10m), (b, 1, 20m), (c, 1, 30m));

        var linha = (await RankingAsync()).Should().ContainSingle().Which;

        linha.QuantidadePedidos.Should().Be(1, "o join com os itens repete o pedido; a contagem é COUNT(DISTINCT)");
        linha.ValorTotal.Should().Be(60m);
    }

    [Fact]
    [Trait("RN", "RN-14")]
    public async Task Get_EmpateNoValor_DesempataPeloMenorClienteId()
    {
        var produto = await Banco.CriarProdutoAsync("Item", 1m, estoque: 10_000);
        var primeiro = await Banco.CriarClienteAsync("Zélia");
        var segundo = await Banco.CriarClienteAsync("Abel");
        await Banco.CriarPedidoAsync(segundo, Utc(2026, 1, 10), (produto, 50, 1m));
        await Banco.CriarPedidoAsync(primeiro, Utc(2026, 1, 11), (produto, 50, 1m));

        var ranking = await RankingAsync();

        ranking.Select(r => r.ClienteId).Should().Equal(primeiro, segundo);
    }

    [Fact]
    public async Task Get_SemComprasNoPeriodo_DevolveListaVazia()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Item", 1m, estoque: 10);
        await Banco.CriarPedidoAsync(cliente, Utc(2025, 6, 1), (produto, 1, 1m));

        (await RankingAsync()).Should().BeEmpty();
    }

    [Theory]
    [Trait("CA", "CA-14")]
    [InlineData("", "dataInicio")]
    [InlineData("?dataInicio=2026-01-01", "dataFim")]
    [InlineData("?dataInicio=2026-12-31&dataFim=2026-01-01", "dataInicio")]
    public async Task Get_PeriodoAusenteOuInvertido_Retorna400(string query, string campo)
    {
        var resposta = await Api.GetAsync($"/relatorios/top-clientes{query}", Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.LerProblemaAsync(Cancelamento)).Errors.Should().ContainKey(campo);
    }
}
