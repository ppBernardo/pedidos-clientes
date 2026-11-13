using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Infrastructure.Persistence;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests.Banco;

/// <summary>Os scripts de database/ e as permissões, exercitados no SQL Server real.</summary>
public sealed class BancoTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    [Fact]
    public async Task InitDb_RodandoDeNovo_EhIdempotenteEAplicaACargaComAcentos()
    {
        // A base limpou o banco, então a carga roda de novo aqui.
        await Ambiente.ExecutarInitDbAsync();
        var clientes = await Banco.ContarAsync("Clientes");
        var pedidos = await Banco.ContarAsync("Pedidos");

        var saida = await Ambiente.ExecutarInitDbAsync();

        saida.Should().Contain("Carga de exemplo já aplicada");
        clientes.Should().Be(30);
        pedidos.Should().Be(1500);
        (await Banco.ContarAsync("Clientes")).Should().Be(clientes);
        (await Banco.ContarAsync("Pedidos")).Should().Be(pedidos);
        (await Banco.EscalarAsync<string>("SELECT Nome FROM dbo.Clientes WHERE Id = 2;")).Should().Be("João Pereira");
        (await Banco.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Clientes WHERE Nome = N'Maria Silva';")).Should().Be(2);
    }

    [Fact]
    public async Task CargaDeExemplo_NaoTemPedidoNoFuturoNemPedidoSemItens()
    {
        await Ambiente.ExecutarInitDbAsync();

        (await Banco.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE DataPedido >= '2026-11-01';")).Should().Be(0);
        (await Banco.EscalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Pedidos AS p WHERE NOT EXISTS (SELECT 1 FROM dbo.ItensPedido AS i WHERE i.PedidoId = p.Id);"))
            .Should().Be(0);
    }

    [Fact]
    public async Task LoginDaApi_NaoTemDdlNemDeleteNemAlteraPreco()
    {
        var comoApi = new BancoDeTeste(Ambiente.ConnectionStringApp);

        await FluentActions.Awaiting(() => comoApi.ExecutarAsync("CREATE TABLE dbo.Intrusa (Id INT);")).Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => comoApi.ExecutarAsync("DELETE FROM dbo.Pedidos;")).Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => comoApi.ExecutarAsync("UPDATE dbo.Produtos SET Preco = 0;")).Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task LoginDoMcp_SoLe()
    {
        var produto = await Banco.CriarProdutoAsync("Teclado", 100m, estoque: 3);
        var comoMcp = new BancoDeTeste(Ambiente.ConnectionStringMcp);

        (await comoMcp.EstoqueAsync(produto)).Should().Be(3);
        await FluentActions.Awaiting(() => comoMcp.ExecutarAsync("UPDATE dbo.Produtos SET Estoque = 0;")).Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => comoMcp.ExecutarAsync("INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'x', N'x@x');"))
            .Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public void ImagemDoContainer_EhAMesmaDoCompose()
    {
        var compose = File.ReadAllText(Repositorio.Pasta("docker-compose.yml"));

        compose.Should().Contain($"&sqlserver-imagem {AmbienteDeTeste.ImagemSqlServer}",
            "os testes precisam rodar na mesma versão do SQL Server que o docker compose sobe");
    }

    [Fact]
    public async Task ModeloDoEf_ConsultaTodasAsTabelasDoSchemaRealComOLoginDaApi()
    {
        var cliente = await Banco.CriarClienteAsync("Ana Souza");
        var produto = await Banco.CriarProdutoAsync("Mouse", 90m, estoque: 10);
        await Banco.CriarPedidoAsync(cliente, new DateTime(2026, 5, 10, 14, 0, 0, DateTimeKind.Utc), (produto, 2, 90m));

        await using var escopo = Ambiente.Api.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();

        (await db.Clientes.ToListAsync(Cancelamento)).Should().ContainSingle().Which.Nome.Should().Be("Ana Souza");
        (await db.Produtos.ToListAsync(Cancelamento)).Should().ContainSingle().Which.Preco.Should().Be(90m);
        var pedido = (await db.Pedidos.Include(p => p.Itens).ToListAsync(Cancelamento)).Should().ContainSingle().Which;
        pedido.DataPedido.Kind.Should().Be(DateTimeKind.Utc);
        pedido.ValorTotal.Should().Be(180m);
    }
}
