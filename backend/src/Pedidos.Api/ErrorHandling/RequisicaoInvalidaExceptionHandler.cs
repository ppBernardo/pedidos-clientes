using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Pedidos.Api.ErrorHandling;

/// <summary>
/// Falha de binding (JSON malformado, <c>pagina=abc</c>, corpo ausente) → 400
/// ProblemDetails. Para isso, <c>RouteHandlerOptions.ThrowOnBadRequest</c> fica
/// ligado em todos os ambientes (ver Program.cs).
/// </summary>
internal sealed class RequisicaoInvalidaExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException requisicaoInvalida)
        {
            return false;
        }

        httpContext.Response.StatusCode = requisicaoInvalida.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = requisicaoInvalida.StatusCode,
                Title = "Requisição inválida",
                Detail = requisicaoInvalida.Message,
            },
        });
    }
}
