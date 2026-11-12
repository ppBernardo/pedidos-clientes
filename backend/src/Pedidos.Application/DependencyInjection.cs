using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pedidos.Application.Pedidos;

namespace Pedidos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PedidoService>();
        services.AddValidatorsFromAssemblyContaining<CriarPedidoValidator>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
