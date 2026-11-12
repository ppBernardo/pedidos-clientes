-- Parte 4: prova os erros de RESULTADO da query original, sobre a carga de
-- exemplo de PedidosDb, sem sujar o banco (tudo termina em ROLLBACK).
--
--   MSYS_NO_PATHCONV=1 docker compose run --rm --entrypoint bash db-init -c \
--     '/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -C -f 65001 -d PedidosDb \
--      -v CONSULTAS=/database/consultas -i /database/consultas/cenario-erros.sql'
--
-- A carga já tem duas clientes "Maria Silva" (Ids 4 e 19). Dentro da transação:
--   * uma cliente 'ANA SOUZA ' (caixa alta e espaço final), que a collation
--     CI_AS considera igual a 'Ana Souza';
--   * um pedido de 2026 sem itens (o banco não impede; a API sim).
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @anaMaiuscula INT;
INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'ANA SOUZA ', N'ana.souza.maiuscula@exemplo.com.br');
SET @anaMaiuscula = SCOPE_IDENTITY();

DECLARE @pedido INT;
INSERT INTO dbo.Pedidos (ClienteId, DataPedido) VALUES (@anaMaiuscula, '2026-05-20T15:00:00');
SET @pedido = SCOPE_IDENTITY();
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedido, 1, 1, 4599.90);

INSERT INTO dbo.Pedidos (ClienteId, DataPedido) VALUES (30, '2026-03-15T10:00:00'); -- sem itens

PRINT N'===== QUERY ORIGINAL =====';
PRINT N'Repare: NULL no topo (pedido sem itens), ordem crescente, uma linha só para "Maria Silva" e uma só para "Ana Souza".';
:r $(CONSULTAS)/ranking-2026-original.sql

PRINT N'===== QUERY CORRIGIDA =====';
PRINT N'Uma linha por cliente (Id), do maior para o menor, sem NULL.';
:r $(CONSULTAS)/ranking-2026-corrigida.sql

ROLLBACK TRANSACTION;
PRINT N'Cenário desfeito (ROLLBACK).';
