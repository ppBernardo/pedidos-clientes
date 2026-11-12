using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Pedidos.Api.OpenApi;

/// <summary>
/// Com <c>[AsParameters]</c>, o OpenAPI documenta os parâmetros com o nome do record
/// (<c>ClienteId</c>), mas o contrato da spec usa camelCase na query string
/// (<c>clienteId</c>). O binding não diferencia maiúsculas, então este transformer
/// só alinha a documentação ao contrato, sem pôr atributos de ASP.NET na Application.
/// </summary>
internal sealed class QueryEmCamelCaseTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var parametro in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parametro.In == ParameterLocation.Query && !string.IsNullOrEmpty(parametro.Name))
            {
                parametro.Name = JsonNamingPolicy.CamelCase.ConvertName(parametro.Name);
            }
        }

        return Task.CompletedTask;
    }
}
