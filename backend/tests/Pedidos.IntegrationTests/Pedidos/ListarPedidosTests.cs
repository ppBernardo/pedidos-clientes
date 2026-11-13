using System.Net;
using System.Net.Http.Json;
using Pedidos.Application.Comum;
using Pedidos.Application.Pedidos;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Pedidos;

public sealed class ListarPedidosTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    private static DateTime Utc(int ano, int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    private Task<PaginaResponse<PedidoResumoResponse>?> ListarAsync(string query = "") =>
        Api.GetFromJsonAsync<PaginaResponse<PedidoResumoResponse>>($"/pedidos{query}", Cancelamento);

    [Fact]
    [Trait("CA", "CA-09")]
    [Trait("RN", "RN-13")]
    public async Task Get_SemFiltros_DevolveAPrimeiraPaginaDoMaisRecenteParaOMaisAntigo()
    {
        var cliente = await Banco.CriarClienteAsync("Ana Souza");
        var produto = await Banco.CriarProdutoAsync("Mouse", 10m, estoque: 100);
        var ids = new List<int>();
        for (var dia = 1; dia <= 12; dia++)
        {
            ids.Add(await Banco.CriarPedidoAsync(cliente, Utc(2026, 3, dia), (produto, dia, 10m)));
        }

        var pagina = (await ListarAsync())!;

        pagina.Pagina.Should().Be(1);
        pagina.TamanhoPagina.Should().Be(10);
        pagina.TotalItens.Should().Be(12);
        pagina.TotalPaginas.Should().Be(2);
        pagina.Itens.Select(p => p.Id).Should().Equal(Enumerable.Reverse(ids).Take(10));

        var maisRecente = pagina.Itens[0];
        maisRecente.ClienteNome.Should().Be("Ana Souza");
        maisRecente.DataPedido.Should().Be(Utc(2026, 3, 12));
        maisRecente.QuantidadeItens.Should().Be(1);
        maisRecente.ValorTotal.Should().Be(120m);
    }

    [Fact]
    [Trait("CA", "CA-09")]
    [Trait("RN", "RN-13")]
    public async Task Get_PedidosNoMesmoInstante_DesempatamPeloIdDecrescente()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Mouse", 10m, estoque: 100);
        var primeiro = await Banco.CriarPedidoAsync(cliente, Utc(2026, 3, 1), (produto, 1, 10m));
        var segundo = await Banco.CriarPedidoAsync(cliente, Utc(2026, 3, 1), (produto, 1, 10m));

        var pagina = (await ListarAsync())!;

        pagina.Itens.Select(p => p.Id).Should().Equal(segundo, primeiro);
    }

    [Fact]
    [Trait("CA", "CA-10")]
    public async Task Get_FiltrandoPorCliente_DevolveSoOsPedidosDele()
    {
        var ana = await Banco.CriarClienteAsync("Ana");
        var bruno = await Banco.CriarClienteAsync("Bruno");
        var produto = await Banco.CriarProdutoAsync("Mouse", 10m, estoque: 100);
        await Banco.CriarPedidoAsync(ana, Utc(2026, 1, 1), (produto, 1, 10m));
        var doBruno = await Banco.CriarPedidoAsync(bruno, Utc(2026, 1, 2), (produto, 2, 10m));

        var pagina = (await ListarAsync($"?clienteId={bruno}"))!;

        pagina.TotalItens.Should().Be(1);
        pagina.Itens.Should().ContainSingle().Which.Id.Should().Be(doBruno);
    }

    [Fact]
    [Trait("CA", "CA-10")]
    [Trait("RN", "RN-12")]
    public async Task Get_ClienteSemPedidos_DevolveListaVaziaENaoErro()
    {
        var pagina = (await ListarAsync("?clienteId=999999"))!;

        pagina.Itens.Should().BeEmpty();
        pagina.TotalItens.Should().Be(0);
        pagina.TotalPaginas.Should().Be(0);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("RN", "RN-11")]
    public async Task Get_FiltrandoPorPeriodo_IncluiOUltimoSegundoDoDiaFinalEExcluiOsVizinhos()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Mouse", 10m, estoque: 100);
        await Banco.CriarPedidoAsync(cliente, Utc(2026, 1, 31, 23, 59, 59), (produto, 1, 10m));
        var primeiro = await Banco.CriarPedidoAsync(cliente, Utc(2026, 2, 1, 0, 0, 0), (produto, 1, 10m));
        var ultimo = await Banco.CriarPedidoAsync(cliente, Utc(2026, 2, 28, 23, 59, 59), (produto, 1, 10m));
        await Banco.CriarPedidoAsync(cliente, Utc(2026, 3, 1, 0, 0, 0), (produto, 1, 10m));

        var pagina = (await ListarAsync("?dataInicio=2026-02-01&dataFim=2026-02-28"))!;

        pagina.Itens.Select(p => p.Id).Should().Equal(ultimo, primeiro);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    public async Task Get_PaginaAlemDaUltima_DevolveVaziaComOsTotaisCorretos()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Mouse", 10m, estoque: 100);
        for (var dia = 1; dia <= 3; dia++)
        {
            await Banco.CriarPedidoAsync(cliente, Utc(2026, 4, dia), (produto, 1, 10m));
        }

        var pagina = (await ListarAsync("?pagina=5&tamanhoPagina=2"))!;

        pagina.Itens.Should().BeEmpty();
        pagina.Pagina.Should().Be(5);
        pagina.TotalItens.Should().Be(3);
        pagina.TotalPaginas.Should().Be(2);
    }

    [Theory]
    [Trait("CA", "CA-12")]
    [InlineData("?dataInicio=2026-03-01&dataFim=2026-02-01", "dataInicio")]
    [InlineData("?pagina=0", "pagina")]
    [InlineData("?tamanhoPagina=101", "tamanhoPagina")]
    [InlineData("?dataFim=9999-12-31", "dataFim")]
    public async Task Get_ParametroInvalido_Retorna400ApontandoOCampo(string query, string campo)
    {
        var resposta = await Api.GetAsync($"/pedidos{query}", Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.LerProblemaAsync(Cancelamento)).Errors.Should().ContainKey(campo);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    public async Task Get_ParametroQueNaoEhNumero_Retorna400ComProblemDetails()
    {
        var resposta = await Api.GetAsync("/pedidos?pagina=abc", Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.LerProblemaAsync(Cancelamento)).Title.Should().Be("Requisição inválida");
    }
}
