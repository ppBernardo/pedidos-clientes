using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

internal sealed class ClienteRepository(PedidosDbContext db) : IClienteRepository
{
    public Task<string?> ObterNomeAsync(int clienteId, CancellationToken cancellationToken) =>
        db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => c.Nome)
            .FirstOrDefaultAsync(cancellationToken);
}
