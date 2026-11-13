-- Parte 4: volume para medir a query de ranking (rodar no banco PedidosPerf).
-- 20.000 clientes, 500 produtos, 400.000 pedidos de 2024 a 2026 e ~1,2 milhão
-- de itens. Determinístico (aritmética modular) e idempotente.
-- Os nomes se repetem de propósito: 2.000 nomes para 20.000 clientes. No mundo
-- real homônimos são comuns, e é isso que o GROUP BY c.Nome erra.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    PRINT N'Volume já gerado; nada a fazer.';
    RETURN;
END;

ALTER DATABASE CURRENT SET RECOVERY SIMPLE;

INSERT INTO dbo.Clientes (Nome, Email, DataCadastro)
SELECT CONCAT(N'Cliente ', 1 + g.value % 2000),
       CONCAT(N'cliente', g.value, N'@exemplo.com.br'),
       DATEADD(DAY, -(g.value % 1000), CAST('2024-01-01' AS DATETIME2(0)))
FROM GENERATE_SERIES(1, 20000) AS g;

INSERT INTO dbo.Produtos (Nome, Preco, Estoque, Ativo)
SELECT CONCAT(N'Produto ', g.value), 10 + (g.value * 37) % 4990, 1000, 1
FROM GENERATE_SERIES(1, 500) AS g;

-- 94.608.000 s = 1.095 dias a partir de 2024-01-01. Como 2024 é bissexto, a massa
-- vai até 30/12/2026 23:59:59: não há pedidos em 31/12/2026. Isso não muda a
-- comparação entre as queries, e o valor fica como está para os números medidos
-- no README continuarem reproduzíveis.
INSERT INTO dbo.Pedidos (ClienteId, DataPedido)
SELECT 1 + (CAST(g.value AS BIGINT) * 7919) % 20000,
       DATEADD(SECOND,
               CAST((CAST(g.value AS BIGINT) * 2654435761) % 94608000 AS INT),
               CAST('2024-01-01' AS DATETIME2(0)))
FROM GENERATE_SERIES(1, 400000) AS g;

-- 1 a 5 itens por pedido, produtos distintos (deslocamentos múltiplos de 101 módulo 500).
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT p.Id, prod.Id, 1 + (p.Id + j.value) % 5, prod.Preco
FROM dbo.Pedidos AS p
CROSS JOIN GENERATE_SERIES(0, 4) AS j
JOIN dbo.Produtos AS prod ON prod.Id = 1 + (p.Id * 13 + j.value * 101) % 500
WHERE j.value <= p.Id % 5;

UPDATE STATISTICS dbo.Clientes WITH FULLSCAN;
UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;

DECLARE @pedidos INT = (SELECT COUNT(*) FROM dbo.Pedidos),
        @pedidos2026 INT = (SELECT COUNT(*) FROM dbo.Pedidos WHERE DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'),
        @itens INT = (SELECT COUNT(*) FROM dbo.ItensPedido);
PRINT CONCAT(N'Volume gerado: ', @pedidos, N' pedidos (', @pedidos2026, N' em 2026) e ', @itens, N' itens.');
