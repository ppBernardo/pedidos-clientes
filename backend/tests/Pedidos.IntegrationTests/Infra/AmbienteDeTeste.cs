using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(Pedidos.IntegrationTests.Infra.AmbienteDeTeste))]

// Um único banco para todos os testes, limpo antes de cada um: os testes não podem rodar em paralelo.
[assembly: Parallelization(Mode = ParallelMode.None)]

namespace Pedidos.IntegrationTests.Infra;

/// <summary>
/// SQL Server real num container (mesma imagem do docker-compose.yml), inicializado
/// pelo mesmo <c>database/init-db.sh</c> do compose. A API de teste conecta com o login
/// de menor privilégio <c>pedidos_app</c>: se faltar permissão, um teste quebra.
/// </summary>
public sealed class AmbienteDeTeste : IAsyncLifetime
{
    public const string ImagemSqlServer = "mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04";

    private const string SenhaSa = "Pedidos2026Sa";
    private const string SenhaApp = "Pedidos2026App";
    private const string SenhaMcp = "Pedidos2026Mcp";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder(ImagemSqlServer)
        .WithPassword(SenhaSa)
        .WithResourceMapping(new DirectoryInfo(Repositorio.Pasta("database")), "/database/")
        .Build();

    public string ConnectionStringSa { get; private set; } = string.Empty;
    public string ConnectionStringApp { get; private set; } = string.Empty;
    public string ConnectionStringMcp { get; private set; } = string.Empty;

    public BancoDeTeste Banco { get; private set; } = null!;
    public PedidosApiFactory Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync();
        await ExecutarInitDbAsync();

        var baseConexao = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString()) { InitialCatalog = "PedidosDb" };
        ConnectionStringSa = baseConexao.ConnectionString;
        ConnectionStringApp = ComLogin(baseConexao, "pedidos_app", SenhaApp);
        ConnectionStringMcp = ComLogin(baseConexao, "mcp_leitura", SenhaMcp);

        Banco = new BancoDeTeste(ConnectionStringSa);
        Api = new PedidosApiFactory(ConnectionStringApp);
    }

    /// <summary>Roda database/init-db.sh dentro do container, como o serviço db-init do compose.</summary>
    public async Task<string> ExecutarInitDbAsync()
    {
        var resultado = await _sqlServer.ExecAsync(
        [
            "bash", "-c",
            $"DB_HOST=localhost SA_PASSWORD={SenhaSa} APP_DB_PASSWORD={SenhaApp} MCP_DB_PASSWORD={SenhaMcp} bash /database/init-db.sh",
        ]);

        if (resultado.ExitCode != 0)
        {
            throw new InvalidOperationException($"init-db.sh falhou (código {resultado.ExitCode}):\n{resultado.Stdout}\n{resultado.Stderr}");
        }

        return resultado.Stdout;
    }

    public async ValueTask DisposeAsync()
    {
        if (Api is not null)
        {
            await Api.DisposeAsync();
        }

        await _sqlServer.DisposeAsync();
    }

    private static string ComLogin(SqlConnectionStringBuilder baseConexao, string usuario, string senha) =>
        new SqlConnectionStringBuilder(baseConexao.ConnectionString) { UserID = usuario, Password = senha }.ConnectionString;
}

internal static class Repositorio
{
    /// <summary>Sobe a partir da pasta do assembly até achar a raiz do repositório.</summary>
    public static string Pasta(string relativa)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "database", "init-db.sh")))
            {
                return Path.Combine(dir.FullName, relativa);
            }
        }

        throw new DirectoryNotFoundException("Raiz do repositório (com database/init-db.sh) não encontrada.");
    }
}
