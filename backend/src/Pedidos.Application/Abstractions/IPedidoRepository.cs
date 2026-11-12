using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Abstractions;

public interface IPedidoRepository
{
    /// <summary>
    /// Grava o pedido e baixa o estoque de cada item numa única transação (RN-09).
    /// Se outra requisição consumiu o estoque entre a leitura do snapshot e a baixa,
    /// desfaz tudo e lança <c>RegraNegocioException</c> com <c>ESTOQUE_INSUFICIENTE</c>.
    /// Ao terminar, <see cref="Pedido.Id"/> está preenchido.
    /// </summary>
    Task AdicionarComBaixaDeEstoqueAsync(Pedido pedido, CancellationToken cancellationToken);
}
