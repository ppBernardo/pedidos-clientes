using Microsoft.AspNetCore.Http.HttpResults;
using Pedidos.Api.Validation;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Comum;
using Pedidos.Application.Pedidos;

namespace Pedidos.Api.Endpoints;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos").WithTags("Pedidos");

        grupo.MapPost("/", CriarAsync)
            .AddEndpointFilter<ValidationFilter<CriarPedidoRequest>>()
            .WithName("CriarPedido")
            .WithSummary("Cria um pedido com itens, validando cliente, produtos e estoque (spec 5.1)")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapGet("/{id:int}", ObterAsync)
            .WithName("ObterPedido")
            .WithSummary("Detalhe de um pedido (spec 5.2)");

        grupo.MapGet("/", ListarAsync)
            .AddEndpointFilter<ValidationFilter<ListarPedidosQuery>>()
            .WithName("ListarPedidos")
            .WithSummary("Lista pedidos com filtros opcionais por cliente e período, paginada (spec 5.3)")
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Created<PedidoDetalheResponse>> CriarAsync(
        CriarPedidoRequest pedido, PedidoService servico, CancellationToken cancellationToken)
    {
        var criado = await servico.CriarAsync(pedido, cancellationToken);
        return TypedResults.Created($"/pedidos/{criado.Id}", criado);
    }

    private static async Task<Results<Ok<PedidoDetalheResponse>, NotFound>> ObterAsync(
        int id, IPedidoConsultas consultas, CancellationToken cancellationToken) =>
        await consultas.ObterDetalheAsync(id, cancellationToken) is { } pedido
            ? TypedResults.Ok(pedido)
            : TypedResults.NotFound();

    private static async Task<Ok<PaginaResponse<PedidoResumoResponse>>> ListarAsync(
        [AsParameters] ListarPedidosQuery query, IPedidoConsultas consultas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await consultas.ListarAsync(query, cancellationToken));
}
