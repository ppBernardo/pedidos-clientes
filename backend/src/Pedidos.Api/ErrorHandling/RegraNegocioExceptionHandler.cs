using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Domain.Comum;

namespace Pedidos.Api.ErrorHandling;

/// <summary>Regra de negócio violada → 422 ProblemDetails com o <c>codigo</c> estável (spec 5.1).</summary>
internal sealed partial class RegraNegocioExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<RegraNegocioExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RegraNegocioException regra)
        {
            return false;
        }

        LogRegraViolada(logger, regra.Codigo, regra.Message);
        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Regra de negócio violada",
                Detail = regra.Message,
                Extensions = { ["codigo"] = regra.Codigo },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Regra de negócio violada ({Codigo}): {Mensagem}")]
    private static partial void LogRegraViolada(ILogger logger, string codigo, string mensagem);
}
