using Microsoft.EntityFrameworkCore;
using Pedidos.Domain.Clientes;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;

namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// Mapeia o schema criado pelos scripts de <c>database/</c>, que são a fonte da
/// verdade. Por isso não há migrations do EF.
/// </summary>
public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PedidosDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        // Todas as datas do banco estão em UTC: ao ler, Kind = Utc, e a API serializa com "Z".
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
