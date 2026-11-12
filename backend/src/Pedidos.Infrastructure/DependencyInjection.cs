using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application.Abstractions;
using Pedidos.Infrastructure.Consultas;
using Pedidos.Infrastructure.Persistence;
using Pedidos.Infrastructure.Repositories;

namespace Pedidos.Infrastructure;

public static class DependencyInjection
{
    /// <param name="connectionString">
    /// Lida quando o DbContext é criado, e não no startup: assim os testes de
    /// integração conseguem trocar a configuração pelo WebApplicationFactory.
    /// </param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        // Sem EnableRetryOnFailure: ele é incompatível com a transação manual da
        // criação de pedido (o EF lança exceção). O compose só sobe a API depois
        // que o banco está pronto.
        services.AddDbContext<PedidosDbContext>((provider, options) => options.UseSqlServer(connectionString(provider)));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IPedidoConsultas, PedidoConsultas>();
        services.AddScoped<IRelatorioConsultas, RelatorioConsultas>();
        services.AddScoped<ICatalogoConsultas, CatalogoConsultas>();
        return services;
    }
}
