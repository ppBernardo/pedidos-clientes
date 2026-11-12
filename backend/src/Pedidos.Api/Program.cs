using System.Diagnostics;
using Pedidos.Api.Endpoints;
using Pedidos.Api.ErrorHandling;
using Pedidos.Api.OpenApi;
using Pedidos.Application;
using Pedidos.Infrastructure;
using Pedidos.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(provider =>
    provider.GetRequiredService<IConfiguration>().GetConnectionString("Pedidos")
    ?? throw new InvalidOperationException("Configure a connection string 'Pedidos' (variável ConnectionStrings__Pedidos)."));

// Erros sempre em ProblemDetails (RFC 9457), com traceId para correlacionar com o log.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = contexto =>
    contexto.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<RegraNegocioExceptionHandler>();
builder.Services.AddExceptionHandler<RequisicaoInvalidaExceptionHandler>();

// Falha de binding vira exceção em qualquer ambiente, para responder com o mesmo
// ProblemDetails. Sem isso, em produção ela devolveria um 400 sem corpo.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddOpenApi(options => options.AddOperationTransformer<QueryEmCamelCaseTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<PedidosDbContext>("sqlserver");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Pedidos API");
    options.RoutePrefix = "swagger";
});

app.MapHealthChecks("/health");
app.MapPedidoEndpoints();

app.Run();
