using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Abstractions;
using Pedidos.Application.Relatorios;
using Pedidos.Domain.Comum;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Consultas;

internal sealed class RelatorioConsultas(PedidosDbContext db) : IRelatorioConsultas
{
    /// <summary>
    /// RN-14, com a mesma SQL da correção da Parte 4 (ver README):
    /// <list type="bullet">
    /// <item>pré-agrega os itens dos pedidos do período <b>por ClienteId</b>, nunca por nome,
    /// para não fundir homônimos;</item>
    /// <item>período como intervalo semiaberto sobre a coluna (sargable: seek em IX_Pedidos_DataPedido);</item>
    /// <item><c>COUNT(DISTINCT p.Id)</c>: o join com os itens repete o pedido;</item>
    /// <item>só depois junta Clientes para o nome; ordem por valor desc com desempate por Id.</item>
    /// </list>
    /// SQL interpolada: o EF transforma cada valor em parâmetro.
    /// </summary>
    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(Periodo periodo, int limite, CancellationToken cancellationToken)
    {
        var inicio = periodo.InicioUtc ?? throw new ArgumentException("O período precisa de data inicial.", nameof(periodo));
        var fimExclusivo = periodo.FimExclusivoUtc ?? throw new ArgumentException("O período precisa de data final.", nameof(periodo));

        var linhas = await db.Database.SqlQuery<LinhaRanking>($"""
            WITH Totais AS (
                SELECT p.ClienteId,
                       COUNT(DISTINCT p.Id)                AS QuantidadePedidos,
                       SUM(i.Quantidade * i.PrecoUnitario) AS ValorTotal
                FROM dbo.Pedidos AS p
                INNER JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
                WHERE p.DataPedido >= {inicio}
                  AND p.DataPedido <  {fimExclusivo}
                GROUP BY p.ClienteId
            )
            SELECT TOP ({limite})
                   t.ClienteId,
                   c.Nome AS ClienteNome,
                   t.QuantidadePedidos,
                   t.ValorTotal
            FROM Totais AS t
            INNER JOIN dbo.Clientes AS c ON c.Id = t.ClienteId
            ORDER BY t.ValorTotal DESC, t.ClienteId
            """).ToListAsync(cancellationToken);

        return [.. linhas.Select((linha, indice) => new TopClienteResponse(
            indice + 1, linha.ClienteId, linha.ClienteNome, linha.QuantidadePedidos, linha.ValorTotal))];
    }

    private sealed record LinhaRanking(int ClienteId, string ClienteNome, int QuantidadePedidos, decimal ValorTotal);
}
