using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Clientes;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.Produtos;

namespace Pedidos.Infrastructure.Persistence.Configurations;

// Nomes de tabela e coluna iguais aos de database/02-tabelas.sql.

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.Property(c => c.DataCadastro).HasColumnType("datetime2(0)");
    }
}

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2);
    }
}

internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DataPedido).HasColumnType("datetime2(0)");
        builder.Ignore(p => p.ValorTotal); // calculado a partir dos itens

        builder.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId);

        // O domínio expõe Itens como somente leitura; o EF escreve no campo _itens.
        builder.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(i => i.Subtotal);

        builder.HasOne<Produto>().WithMany().HasForeignKey(i => i.ProdutoId);
    }
}
