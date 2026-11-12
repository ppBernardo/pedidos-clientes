using System.Net.Http.Json;
using Pedidos.Application.Catalogo;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Catalogo;

public sealed class CatalogoTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    [Fact]
    public async Task GetClientes_DevolveTodosPorNomeComHomonimosEmOrdemEstavel()
    {
        var carlos = await Banco.CriarClienteAsync("Carlos Lima");
        var maria1 = await Banco.CriarClienteAsync("Maria Silva");
        var ana = await Banco.CriarClienteAsync("Ana Souza");
        var maria2 = await Banco.CriarClienteAsync("Maria Silva");

        var clientes = (await Api.GetFromJsonAsync<List<ClienteResponse>>("/clientes", Cancelamento))!;

        clientes.Select(c => c.Id).Should().Equal(ana, carlos, maria1, maria2);
        clientes.Should().OnlyContain(c => c.Email.EndsWith("@teste.com.br"));
    }

    [Fact]
    public async Task GetProdutos_DevolveSoOsAtivosPorNomeInclusiveSemEstoque()
    {
        var teclado = await Banco.CriarProdutoAsync("Teclado", 149.90m, estoque: 10);
        await Banco.CriarProdutoAsync("Projetor", 1899m, estoque: 5, ativo: false);
        var monitor = await Banco.CriarProdutoAsync("Monitor", 3299m, estoque: 0);

        var produtos = (await Api.GetFromJsonAsync<List<ProdutoResponse>>("/produtos", Cancelamento))!;

        produtos.Should().Equal(
            new ProdutoResponse(monitor, "Monitor", 3299m, 0),
            new ProdutoResponse(teclado, "Teclado", 149.90m, 10));
    }
}
