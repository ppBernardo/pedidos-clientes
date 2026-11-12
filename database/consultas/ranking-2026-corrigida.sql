-- Parte 4: ranking de quem mais comprou em 2026, corrigido e otimizado.
--
-- O que mudou em relação à original (ranking-2026-original.sql):
--  1. Agrupa por c.Id, e não por c.Nome. O nome não é chave: homônimos se
--     fundiriam, e a collation CI_AS ainda junta variações de caixa e de espaço
--     final ('ANA SOUZA ' = 'Ana Souza').
--  2. ORDER BY Total DESC, com desempate por Id. O original era ASC e
--     começava por quem comprou menos.
--  3. INNER JOIN nos dois joins. O WHERE sobre p já anulava o 1º LEFT JOIN, e o
--     2º LEFT JOIN deixava pedido sem itens com Total NULL, que no ASC ia para
--     o topo. Num ranking de compras só entra quem comprou.
--  4. Intervalo semiaberto em vez de YEAR(): a coluna fica "limpa" e o
--     otimizador consegue fazer seek em IX_Pedidos_DataPedido. É também à prova
--     de hora (BETWEEN '2026-01-01' AND '2026-12-31' perderia o dia 31 depois da
--     meia-noite).
--  5. Pré-agrega os itens por ClienteId (só inteiros na chave do agrupamento) e
--     junta Clientes só para os clientes do resultado.
-- Literais e não variáveis locais, para medir o mesmo plano que a aplicação usa
-- com parâmetros.
WITH Totais AS (
    SELECT p.ClienteId,
           SUM(i.Quantidade * i.PrecoUnitario) AS Total
    FROM dbo.Pedidos AS p
    INNER JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
    WHERE p.DataPedido >= '2026-01-01'
      AND p.DataPedido <  '2027-01-01'
    GROUP BY p.ClienteId
)
SELECT RANK() OVER (ORDER BY t.Total DESC) AS Posicao,
       c.Id AS ClienteId,
       c.Nome,
       t.Total
FROM Totais AS t
INNER JOIN dbo.Clientes AS c ON c.Id = t.ClienteId
ORDER BY t.Total DESC, c.Id;
