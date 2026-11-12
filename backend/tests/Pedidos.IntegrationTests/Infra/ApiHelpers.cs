using System.Net.Http.Json;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>ProblemDetails como a API devolve (spec 5.1): <c>codigo</c> em 422, <c>errors</c> em 400.</summary>
public sealed record Problema(
    string? Title,
    int Status,
    string? Detail,
    string? Codigo,
    string? TraceId,
    Dictionary<string, string[]>? Errors);

public static class ApiHelpers
{
    public static Task<HttpResponseMessage> CriarPedidoAsync(
        this HttpClient api, int clienteId, CancellationToken cancellationToken, params (int ProdutoId, int Quantidade)[] itens) =>
        api.PostAsJsonAsync(
            "/pedidos",
            new { clienteId, itens = itens.Select(i => new { produtoId = i.ProdutoId, quantidade = i.Quantidade }) },
            cancellationToken);

    public static async Task<Problema> LerProblemaAsync(this HttpResponseMessage resposta, CancellationToken cancellationToken)
    {
        resposta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        return (await resposta.Content.ReadFromJsonAsync<Problema>(cancellationToken))!;
    }
}
