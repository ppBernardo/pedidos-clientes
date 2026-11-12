using Pedidos.Application.Catalogo;
using Pedidos.Application.Comum;
using Pedidos.Application.Pedidos;
using Pedidos.Application.Relatorios;
using Pedidos.Domain.Comum;

namespace Pedidos.Application.Abstractions;

// Lado de leitura: as consultas devolvem DTOs direto do banco, sem passar pelo
// domínio (que só existe para proteger as regras de escrita).

public interface IPedidoConsultas
{
    Task<PedidoDetalheResponse?> ObterDetalheAsync(int pedidoId, CancellationToken cancellationToken);

    Task<PaginaResponse<PedidoResumoResponse>> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken);
}

public interface IRelatorioConsultas
{
    /// <summary>Clientes com maior valor comprado no período, agrupando por Id (RN-14).</summary>
    Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(Periodo periodo, int limite, CancellationToken cancellationToken);
}

public interface ICatalogoConsultas
{
    Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAtivosAsync(CancellationToken cancellationToken);
}
