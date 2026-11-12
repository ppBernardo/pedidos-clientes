using Microsoft.AspNetCore.Http.HttpResults;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Catalogo;

namespace Pedidos.Api.Endpoints;

/// <summary>Endpoints de apoio ao frontend (spec 5.5): filtro por cliente e formulário de pedido.</summary>
public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clientes", ListarClientesAsync)
            .WithTags("Catálogo")
            .WithName("ListarClientes")
            .WithSummary("Clientes ordenados por nome (spec 5.5)");

        app.MapGet("/produtos", ListarProdutosAsync)
            .WithTags("Catálogo")
            .WithName("ListarProdutos")
            .WithSummary("Produtos ativos ordenados por nome, com preço e estoque (spec 5.5)");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ClienteResponse>>> ListarClientesAsync(
        ICatalogoConsultas consultas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await consultas.ListarClientesAsync(cancellationToken));

    private static async Task<Ok<IReadOnlyList<ProdutoResponse>>> ListarProdutosAsync(
        ICatalogoConsultas consultas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await consultas.ListarProdutosAtivosAsync(cancellationToken));
}
