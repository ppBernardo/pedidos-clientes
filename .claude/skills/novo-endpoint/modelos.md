# Modelos de referência (extraídos do código real)

## Endpoint de leitura com query string validada

```csharp
// backend/src/Pedidos.Api/Endpoints/<Recurso>Endpoints.cs
public static class RecursoEndpoints
{
    public static IEndpointRouteBuilder MapRecursoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/recursos").WithTags("Recursos");

        grupo.MapGet("/", ListarAsync)
            .AddEndpointFilter<ValidationFilter<ListarRecursosQuery>>()
            .WithName("ListarRecursos")
            .WithSummary("O que faz (spec 5.x)")
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Ok<PaginaResponse<RecursoResponse>>> ListarAsync(
        [AsParameters] ListarRecursosQuery query, IRecursoConsultas consultas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await consultas.ListarAsync(query, cancellationToken));
}
```

## Query com valores padrão e validador

```csharp
// backend/src/Pedidos.Application/<Funcionalidade>/ListarRecursosQuery.cs
public sealed record ListarRecursosQuery(DateOnly? DataInicio = null, DateOnly? DataFim = null, int Pagina = 1)
{
    public Periodo Periodo => new(DataInicio, DataFim);
}

public sealed class ListarRecursosValidator : AbstractValidator<ListarRecursosQuery>
{
    public ListarRecursosValidator()
    {
        RuleFor(q => q.DataInicio).DentroDaFaixaAceita().NaoDepoisDe(q => q.DataFim);
        RuleFor(q => q.DataFim).DentroDaFaixaAceita();
    }
}
```

## Consulta (Infrastructure)

```csharp
internal sealed class RecursoConsultas(PedidosDbContext db) : IRecursoConsultas
{
    public async Task<IReadOnlyList<RecursoResponse>> ListarAsync(ListarRecursosQuery query, CancellationToken cancellationToken)
    {
        var dados = db.Pedidos.AsNoTracking();
        if (query.Periodo.InicioUtc is { } inicio) dados = dados.Where(p => p.DataPedido >= inicio);        // sargable
        if (query.Periodo.FimExclusivoUtc is { } fim) dados = dados.Where(p => p.DataPedido < fim);         // semiaberto
        return await dados.Select(p => new RecursoResponse(p.Id /* ... */)).ToListAsync(cancellationToken);  // projeção
    }
}
```

## Teste de integração

```csharp
public sealed class RecursoTests(AmbienteDeTeste ambiente) : TesteDeIntegracao(ambiente)
{
    [Fact]
    [Trait("CA", "CA-xx")]
    public async Task Get_Cenario_Resultado()
    {
        var cliente = await Banco.CriarClienteAsync("Ana Souza");            // dados próprios do teste
        // ... arrange com Banco.CriarProdutoAsync / Banco.CriarPedidoAsync(data controlada)

        var resposta = await Api.GetAsync("/recursos?dataInicio=2026-01-01&dataFim=2026-12-31", Cancelamento);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        // ... asserts sobre o JSON e, se for escrita, sobre o estado do banco
    }
}
```
