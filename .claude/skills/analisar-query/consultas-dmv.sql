-- Consultas de diagnóstico para rodar pelo MCP (execute_sql), todas somente leitura.
-- O login mcp_leitura tem VIEW DATABASE PERFORMANCE STATE (Query Store) e
-- VIEW SERVER PERFORMANCE STATE (DMVs de plano e de índices ausentes).

-- 1) Consultas que mais leem, segundo o Query Store (troque o filtro de texto).
SELECT TOP (10)
    q.query_id,
    p.plan_id,
    rs.count_executions                      AS execucoes,
    CAST(rs.avg_logical_io_reads AS BIGINT)  AS leituras_logicas_media,
    CAST(rs.avg_cpu_time / 1000.0 AS DECIMAL(12, 2)) AS cpu_ms_media,
    CAST(rs.avg_duration / 1000.0 AS DECIMAL(12, 2)) AS duracao_ms_media,
    LEFT(qt.query_sql_text, 200)             AS consulta
FROM sys.query_store_query_text AS qt
JOIN sys.query_store_query AS q ON q.query_text_id = qt.query_text_id
JOIN sys.query_store_plan AS p ON p.query_id = q.query_id
JOIN sys.query_store_runtime_stats AS rs ON rs.plan_id = p.plan_id
WHERE qt.query_sql_text LIKE N'%ItensPedido%'
ORDER BY rs.avg_logical_io_reads DESC;

-- 2) Plano (XML) de um plan_id do Query Store: procure Index Scan x Index Seek,
--    Hash Match x Nested Loops/Stream Aggregate e "Estimated vs Actual Rows".
SELECT TRY_CAST(query_plan AS XML) AS plano
FROM sys.query_store_plan
WHERE plan_id = 1; -- troque pelo plan_id da consulta 1

-- 3) Índices ausentes sugeridos pelo otimizador neste banco.
SELECT
    d.statement           AS tabela,
    d.equality_columns    AS colunas_igualdade,
    d.inequality_columns  AS colunas_desigualdade,
    d.included_columns    AS colunas_incluidas,
    s.user_seeks          AS buscas,
    s.avg_user_impact     AS impacto_estimado_pct
FROM sys.dm_db_missing_index_details AS d
JOIN sys.dm_db_missing_index_groups AS g ON g.index_handle = d.index_handle
JOIN sys.dm_db_missing_index_group_stats AS s ON s.group_handle = g.index_group_handle
WHERE d.database_id = DB_ID()
ORDER BY s.avg_user_impact DESC;

-- 4) Estrutura dos índices de uma tabela (chave e colunas incluídas).
SELECT
    i.name                 AS indice,
    i.type_desc            AS tipo,
    i.is_unique            AS unico,
    c.name                 AS coluna,
    ic.key_ordinal         AS posicao_na_chave,
    ic.is_included_column  AS incluida
FROM sys.indexes AS i
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.Pedidos') -- troque a tabela
ORDER BY i.name, ic.is_included_column, ic.key_ordinal;
