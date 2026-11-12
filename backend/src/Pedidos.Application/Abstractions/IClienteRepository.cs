namespace Pedidos.Application.Abstractions;

public interface IClienteRepository
{
    /// <summary>Nome do cliente, ou <c>null</c> se ele não existir.</summary>
    Task<string?> ObterNomeAsync(int clienteId, CancellationToken cancellationToken);
}
