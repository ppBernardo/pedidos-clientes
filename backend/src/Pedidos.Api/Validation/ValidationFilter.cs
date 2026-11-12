using FluentValidation;

namespace Pedidos.Api.Validation;

/// <summary>
/// Roda o validador FluentValidation do argumento <typeparamref name="T"/> antes do
/// endpoint. Se houver erro, responde 400 ValidationProblemDetails com as chaves no
/// formato do JSON (ex.: <c>itens[1].quantidade</c>).
/// </summary>
internal sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argumento)
        {
            return await next(context);
        }

        var resultado = await validator.ValidateAsync(argumento, context.HttpContext.RequestAborted);
        if (resultado.IsValid)
        {
            return await next(context);
        }

        var erros = resultado.Errors
            .GroupBy(e => ParaCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return TypedResults.ValidationProblem(erros, title: "Requisição inválida");
    }

    /// <summary><c>Itens[1].Quantidade</c> → <c>itens[1].quantidade</c>.</summary>
    internal static string ParaCamelCase(string caminho) =>
        string.Join('.', caminho.Split('.').Select(parte =>
            parte.Length == 0 ? parte : string.Concat(char.ToLowerInvariant(parte[0]).ToString(), parte[1..])));
}
