-- Cria o banco da aplicação. Idempotente.
-- Executado em master: sqlcmd -d master -v DB_NAME=PedidosDb -i 01-criar-banco.sql

IF DB_ID(N'$(DB_NAME)') IS NULL
BEGIN
    CREATE DATABASE [$(DB_NAME)];
END;
GO

-- RCSI (READ_COMMITTED_SNAPSHOT): leituras como a listagem e o relatório leem a
-- última versão confirmada e não ficam bloqueadas atrás das transações de criação
-- de pedido. A baixa de estoque continua segura: o UPDATE condicional lê a linha
-- atual com bloqueio. Só altera se estiver desligado, porque o
-- ROLLBACK IMMEDIATE derruba conexões abertas.
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$(DB_NAME)' AND is_read_committed_snapshot_on = 0)
BEGIN
    ALTER DATABASE [$(DB_NAME)] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
END;
GO

-- Query Store: histórico de planos e métricas de execução. É usado na
-- investigação da Parte 4, inclusive pelo MCP somente leitura.
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$(DB_NAME)' AND is_query_store_on = 0)
BEGIN
    ALTER DATABASE [$(DB_NAME)] SET QUERY_STORE = ON (OPERATION_MODE = READ_WRITE);
END;
GO
