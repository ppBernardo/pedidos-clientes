-- Logins com privilégio mínimo. Idempotente; as senhas vêm de variáveis do sqlcmd:
--   sqlcmd -v DB_NAME=PedidosDb APP_DB_PASSWORD=... MCP_DB_PASSWORD=... -i 04-seguranca.sql
--
-- pedidos_app : usado pela API. Lê tudo, insere pedidos e itens e só pode
--               alterar a coluna Estoque. Sem DDL e sem DELETE.
-- mcp_leitura : usado pelo MCP do Claude Code. Só leitura, mais o que é preciso
--               para inspecionar planos de execução, Query Store e DMVs.

-- Permissões de servidor só podem ser concedidas a partir de master.
USE [master];
GO

IF SUSER_ID(N'pedidos_app') IS NULL
    CREATE LOGIN pedidos_app WITH PASSWORD = N'$(APP_DB_PASSWORD)', DEFAULT_DATABASE = [$(DB_NAME)];
ELSE
    ALTER LOGIN pedidos_app WITH PASSWORD = N'$(APP_DB_PASSWORD)'; -- ressincroniza com a variável de ambiente
GO

IF SUSER_ID(N'mcp_leitura') IS NULL
    CREATE LOGIN mcp_leitura WITH PASSWORD = N'$(MCP_DB_PASSWORD)', DEFAULT_DATABASE = [$(DB_NAME)];
ELSE
    ALTER LOGIN mcp_leitura WITH PASSWORD = N'$(MCP_DB_PASSWORD)';
GO

-- Plano em cache e índices ausentes (sys.dm_exec_*, sys.dm_db_missing_index_*).
-- Permissão granular do SQL Server 2022, mais restrita que VIEW SERVER STATE.
GRANT VIEW SERVER PERFORMANCE STATE TO mcp_leitura;
GO

USE [$(DB_NAME)];
GO

IF USER_ID(N'pedidos_app') IS NULL
    CREATE USER pedidos_app FOR LOGIN pedidos_app;
GO

GRANT SELECT ON SCHEMA::dbo TO pedidos_app;
GRANT INSERT ON OBJECT::dbo.Pedidos TO pedidos_app;
GRANT INSERT ON OBJECT::dbo.ItensPedido TO pedidos_app;
GRANT UPDATE (Estoque) ON OBJECT::dbo.Produtos TO pedidos_app; -- baixa de estoque, e nada mais
GO

IF USER_ID(N'mcp_leitura') IS NULL
    CREATE USER mcp_leitura FOR LOGIN mcp_leitura;
GO

GRANT SELECT ON SCHEMA::dbo TO mcp_leitura;
GRANT VIEW DEFINITION ON SCHEMA::dbo TO mcp_leitura;    -- metadados de tabelas e índices
GRANT SHOWPLAN TO mcp_leitura;                          -- planos estimados
GRANT VIEW DATABASE PERFORMANCE STATE TO mcp_leitura;   -- Query Store e DMVs do banco
GO
