using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>API real em memória, conectada ao container com o login da aplicação e com relógio controlado.</summary>
public sealed class PedidosApiFactory(string connectionStringApp) : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 12, 15, 30, 45, TimeSpan.Zero);

    public RelogioDeTeste Relogio { get; } = new(Agora);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Pedidos", connectionStringApp);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Relogio);
        });
    }
}
