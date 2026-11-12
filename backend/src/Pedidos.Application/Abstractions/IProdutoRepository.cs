using Pedidos.Domain.Produtos;

namespace Pedidos.Application.Abstractions;

public interface IProdutoRepository
{
    /// <summary>Snapshot dos produtos encontrados, por id. Ids inexistentes ficam de fora.</summary>
    Task<IReadOnlyDictionary<int, Produto>> ObterPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
