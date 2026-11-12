using Pedidos.Application.Abstractions;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Pedidos;

/// <summary>Caso de uso de criação de pedido. O formato já chega validado (CriarPedidoValidator).</summary>
public sealed class PedidoService(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IPedidoRepository pedidos,
    TimeProvider relogio)
{
    public async Task<PedidoDetalheResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken)
    {
        // RN-03: o cliente precisa existir.
        var clienteNome = await clientes.ObterNomeAsync(request.ClienteId, cancellationToken)
            ?? throw ErrosDePedido.ClienteNaoEncontrado(request.ClienteId);

        var itens = (request.Itens ?? []).Select(i => new ItemSolicitado(i.ProdutoId, i.Quantidade)).ToList();
        var snapshot = await produtos.ObterPorIdsAsync([.. itens.Select(i => i.ProdutoId).Distinct()], cancellationToken);

        // RN-04 a RN-08 e RN-10/11 ficam no domínio, avaliadas sobre o snapshot.
        var pedido = Pedido.Criar(request.ClienteId, itens, snapshot, relogio.GetUtcNow().UtcDateTime);

        // RN-09: gravação + baixa condicional de estoque numa transação.
        await pedidos.AdicionarComBaixaDeEstoqueAsync(pedido, cancellationToken);

        return PedidoDetalheResponse.De(pedido, clienteNome, snapshot);
    }
}
