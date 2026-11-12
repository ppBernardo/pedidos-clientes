using Microsoft.AspNetCore.Http.HttpResults;
using Pedidos.Api.Validation;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Relatorios;

namespace Pedidos.Api.Endpoints;

public static class RelatorioEndpoints
{
    public static IEndpointRouteBuilder MapRelatorioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/relatorios").WithTags("Relatórios");

        grupo.MapGet("/top-clientes", TopClientesAsync)
            .AddEndpointFilter<ValidationFilter<TopClientesQuery>>()
            .WithName("TopClientes")
            .WithSummary("Os 5 clientes com maior valor total de pedidos no período informado (spec 5.4)")
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<TopClienteResponse>>> TopClientesAsync(
        [AsParameters] TopClientesQuery query, IRelatorioConsultas consultas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await consultas.TopClientesAsync(query.Periodo, TopClientesQuery.Limite, cancellationToken));
}
