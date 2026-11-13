using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Pedidos;
using Pedidos.Domain.Comum;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Pedidos;

public sealed class CriarPedidoTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    [Fact]
    [Trait("CA", "CA-01")]
    public async Task Post_PedidoValido_Retorna201ComLocationTotalEBaixaOEstoque()
    {
        var cliente = await Banco.CriarClienteAsync("Ana Souza");
        var teclado = await Banco.CriarProdutoAsync("Teclado mecânico", 149.90m, estoque: 10);
        var mouse = await Banco.CriarProdutoAsync("Mouse sem fio", 90.00m, estoque: 3);

        var resposta = await Api.CriarPedidoAsync(cliente, Cancelamento, (teclado, 2), (mouse, 1));

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedido = (await resposta.Content.ReadFromJsonAsync<PedidoDetalheResponse>(Cancelamento))!;
        resposta.Headers.Location!.OriginalString.Should().Be($"/pedidos/{pedido.Id}");
        pedido.ClienteNome.Should().Be("Ana Souza");
        pedido.DataPedido.Should().Be(PedidosApiFactory.Agora.UtcDateTime);
        pedido.ValorTotal.Should().Be(389.80m);
        pedido.Itens.Should().ContainSingle(i => i.ProdutoId == teclado && i.PrecoUnitario == 149.90m && i.Subtotal == 299.80m);

        (await Banco.EstoqueAsync(teclado)).Should().Be(8);
        (await Banco.EstoqueAsync(mouse)).Should().Be(2);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("RN", "RN-11")]
    public async Task GetDoLocation_DevolveOMesmoPedidoComDataEmUtcETruncada()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Monitor", 1000m, estoque: 5);
        Ambiente.Api.Relogio.Agora = new DateTimeOffset(2026, 12, 31, 23, 59, 59, 900, TimeSpan.Zero);

        var criado = await Api.CriarPedidoAsync(cliente, Cancelamento, (produto, 1));
        var json = await Api.GetStringAsync(criado.Headers.Location, Cancelamento);

        // 23:59:59.900 truncado: continua no mesmo dia (arredondar levaria para 2027).
        json.Should().Contain("\"dataPedido\":\"2026-12-31T23:59:59Z\"");
    }

    [Fact]
    [Trait("CA", "CA-02")]
    public async Task Post_ClienteInexistente_Retorna422ENaoGravaNada()
    {
        var produto = await Banco.CriarProdutoAsync("Teclado", 100m, estoque: 10);

        var resposta = await Api.CriarPedidoAsync(clienteId: 999_999, Cancelamento, (produto, 1));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problema = await resposta.LerProblemaAsync(Cancelamento);
        problema.Codigo.Should().Be(CodigosErro.ClienteNaoEncontrado);
        problema.TraceId.Should().NotBeNullOrEmpty();
        (await Banco.ContarAsync("Pedidos")).Should().Be(0);
        (await Banco.EstoqueAsync(produto)).Should().Be(10);
    }

    [Fact]
    [Trait("CA", "CA-03")]
    public async Task Post_UmItemSemEstoque_Retorna422ENaoAlteraNenhumEstoque()
    {
        var cliente = await Banco.CriarClienteAsync();
        var comEstoque = await Banco.CriarProdutoAsync("Teclado", 100m, estoque: 10);
        var semEstoque = await Banco.CriarProdutoAsync("Placa de vídeo", 5000m, estoque: 2);

        var resposta = await Api.CriarPedidoAsync(cliente, Cancelamento, (comEstoque, 1), (semEstoque, 5));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problema = await resposta.LerProblemaAsync(Cancelamento);
        problema.Codigo.Should().Be(CodigosErro.EstoqueInsuficiente);
        problema.Detail.Should().Contain("'Placa de vídeo' (solicitado: 5, disponível: 2)");
        (await Banco.EstoqueAsync(comEstoque)).Should().Be(10);
        (await Banco.EstoqueAsync(semEstoque)).Should().Be(2);
        (await Banco.ContarAsync("Pedidos")).Should().Be(0);
    }

    [Theory]
    [Trait("CA", "CA-04")]
    [InlineData("""{"clienteId": 1, "itens": []}""")]
    [InlineData("""{"clienteId": 1}""")]
    public async Task Post_SemItens_Retorna400ComErroEmItens(string corpo)
    {
        var resposta = await Api.PostAsync("/pedidos", new StringContent(corpo, Encoding.UTF8, "application/json"), Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.LerProblemaAsync(Cancelamento)).Errors.Should().ContainKey("itens");
    }

    [Fact]
    [Trait("CA", "CA-05")]
    public async Task Post_QuantidadeZero_Retorna400ApontandoOCampoDoItem()
    {
        var resposta = await Api.CriarPedidoAsync(1, Cancelamento, (1, 1), (2, 0));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problema = await resposta.LerProblemaAsync(Cancelamento);
        problema.Errors.Should().ContainKey("itens[1].quantidade");
        problema.TraceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("RN", "RN-07")]
    public async Task Post_ProdutoRepetido_Retorna400SemGravarNada()
    {
        var cliente = await Banco.CriarClienteAsync();
        var produto = await Banco.CriarProdutoAsync("Teclado", 100m, estoque: 10);

        var resposta = await Api.CriarPedidoAsync(cliente, Cancelamento, (produto, 1), (produto, 2));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.LerProblemaAsync(Cancelamento)).Errors.Should().ContainKey("itens");
        (await Banco.EstoqueAsync(produto)).Should().Be(10);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    public async Task Post_ProdutoInexistente_Retorna422ProdutoNaoEncontrado()
    {
        var cliente = await Banco.CriarClienteAsync();

        var resposta = await Api.CriarPedidoAsync(cliente, Cancelamento, (999_999, 1));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.LerProblemaAsync(Cancelamento)).Codigo.Should().Be(CodigosErro.ProdutoNaoEncontrado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    public async Task Post_ProdutoInativo_Retorna422ProdutoInativo()
    {
        var cliente = await Banco.CriarClienteAsync();
        var inativo = await Banco.CriarProdutoAsync("Projetor", 1899m, estoque: 10, ativo: false);

        var resposta = await Api.CriarPedidoAsync(cliente, Cancelamento, (inativo, 1));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.LerProblemaAsync(Cancelamento)).Codigo.Should().Be(CodigosErro.ProdutoInativo);
        (await Banco.EstoqueAsync(inativo)).Should().Be(10);
    }

    [Fact]
    public async Task Post_JsonMalformado_Retorna400ComProblemDetails()
    {
        var resposta = await Api.PostAsync("/pedidos", new StringContent("""{"clienteId": 1, "itens": [""", Encoding.UTF8, "application/json"), Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problema = await resposta.LerProblemaAsync(Cancelamento);
        problema.Title.Should().Be("Requisição inválida");
        problema.TraceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_PedidoInexistente_Retorna404ComProblemDetails()
    {
        var resposta = await Api.GetAsync("/pedidos/999999", Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await resposta.LerProblemaAsync(Cancelamento)).TraceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("CA", "CA-03")]
    [Trait("RN", "RN-09")]
    public async Task Repositorio_QuandoABaixaDeUmItemFalha_DesfazAsBaixasAnteriores()
    {
        var cliente = await Banco.CriarClienteAsync();
        var primeiro = await Banco.CriarProdutoAsync("A", 10m, estoque: 10);
        var segundo = await Banco.CriarProdutoAsync("B", 10m, estoque: 1);

        // Snapshot desatualizado: diz que B tem 5 unidades, como se outra requisição
        // tivesse levado o estoque entre a leitura e a baixa.
        var snapshot = new Dictionary<int, Produto>
        {
            [primeiro] = new(primeiro, "A", 10m, estoque: 10),
            [segundo] = new(segundo, "B", 10m, estoque: 5),
        };
        var pedido = Pedido.Criar(cliente, [new ItemSolicitado(primeiro, 2), new ItemSolicitado(segundo, 3)], snapshot, DateTime.UtcNow);

        await using var escopo = Ambiente.Api.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();
        var acao = () => repositorio.AdicionarComBaixaDeEstoqueAsync(pedido, Cancelamento);

        (await acao.Should().ThrowAsync<RegraNegocioException>())
            .Which.Message.Should().Contain("'B' (solicitado: 3, disponível: 1)");
        (await Banco.EstoqueAsync(primeiro)).Should().Be(10, "a baixa de A precisa ser desfeita junto");
        (await Banco.EstoqueAsync(segundo)).Should().Be(1);
        (await Banco.ContarAsync("Pedidos")).Should().Be(0);
    }
}
