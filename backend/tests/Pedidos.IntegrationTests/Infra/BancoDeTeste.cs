using Microsoft.Data.SqlClient;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>Acesso direto ao banco (como sa) para preparar e conferir dados nos testes.</summary>
public sealed class BancoDeTeste(string connectionString)
{
    private int _sequencia;

    /// <summary>Apaga tudo, na ordem das FKs. Cada teste começa do zero e cria os próprios dados.</summary>
    public Task LimparAsync() => ExecutarAsync(
        "DELETE FROM dbo.ItensPedido; DELETE FROM dbo.Pedidos; DELETE FROM dbo.Produtos; DELETE FROM dbo.Clientes;");

    public Task<int> CriarClienteAsync(string nome = "Cliente de Teste") =>
        EscalarAsync<int>(
            "INSERT INTO dbo.Clientes (Nome, Email) OUTPUT INSERTED.Id VALUES (@nome, @email);",
            ("@nome", nome),
            ("@email", $"cliente{Interlocked.Increment(ref _sequencia)}@teste.com.br"));

    public Task<int> CriarProdutoAsync(string nome, decimal preco, int estoque, bool ativo = true) =>
        EscalarAsync<int>(
            "INSERT INTO dbo.Produtos (Nome, Preco, Estoque, Ativo) OUTPUT INSERTED.Id VALUES (@nome, @preco, @estoque, @ativo);",
            ("@nome", nome), ("@preco", preco), ("@estoque", estoque), ("@ativo", ativo));

    /// <summary>Pedido gravado direto no banco, com data controlada (para listagem e relatório).</summary>
    public async Task<int> CriarPedidoAsync(int clienteId, DateTime dataUtc, params (int ProdutoId, int Quantidade, decimal Preco)[] itens)
    {
        var pedidoId = await EscalarAsync<int>(
            "INSERT INTO dbo.Pedidos (ClienteId, DataPedido) OUTPUT INSERTED.Id VALUES (@cliente, @data);",
            ("@cliente", clienteId), ("@data", dataUtc));

        foreach (var (produtoId, quantidade, preco) in itens)
        {
            await ExecutarAsync(
                "INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedido, @produto, @qtd, @preco);",
                ("@pedido", pedidoId), ("@produto", produtoId), ("@qtd", quantidade), ("@preco", preco));
        }

        return pedidoId;
    }

    public Task<int> EstoqueAsync(int produtoId) =>
        EscalarAsync<int>("SELECT Estoque FROM dbo.Produtos WHERE Id = @id;", ("@id", produtoId));

    public Task<int> ContarAsync(string tabela) =>
        EscalarAsync<int>($"SELECT COUNT(*) FROM dbo.{tabela};");

    public async Task<T> EscalarAsync<T>(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = Criar(conexao, sql, parametros);
        return (T)(await comando.ExecuteScalarAsync())!;
    }

    public async Task ExecutarAsync(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = Criar(conexao, sql, parametros);
        await comando.ExecuteNonQueryAsync();
    }

    private static SqlCommand Criar(SqlConnection conexao, string sql, (string Nome, object Valor)[] parametros)
    {
        var comando = new SqlCommand(sql, conexao);
        foreach (var (nome, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nome, valor);
        }

        return comando;
    }
}
