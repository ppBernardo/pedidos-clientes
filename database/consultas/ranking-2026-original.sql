-- Parte 4: a query ORIGINAL do desafio, sem nenhuma alteração.
-- Deveria trazer o ranking de quem mais comprou em 2026, mas é lenta e o
-- resultado está errado. Diagnóstico no README e em ranking-2026-corrigida.sql.
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
