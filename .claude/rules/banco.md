---
paths:
  - "database/**"
---

# Regras de SQL (SQL Server 2022)

## Scripts

- **Idempotentes**: o `db-init` roda a cada `docker compose up`. Use `IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS (SELECT 1 FROM sys.indexes ...)` e faça a carga só com a tabela vazia.
- **Sem `USE`** dentro dos scripts: o banco vem por `sqlcmd -d $(DB_NAME)`, o que permite reaproveitar os scripts no banco da Parte 4.
- Arquivo em UTF-8 sem BOM, executado com `sqlcmd -f 65001`. Literais com acento sempre como `N'...'`.
- Segredos chegam por variável do sqlcmd (`$(APP_DB_PASSWORD)`), nunca escritos no script.
- Ordem: `01-criar-banco`, `02-tabelas`, `03-indices`, `04-seguranca`, `05-carga-dados`. Índice novo vai em `03-indices.sql`.

## Modelagem

- Nomes: `PK_Tabela`, `FK_Tabela_Referencia`, `UX_Tabela_Colunas` (índice único), `IX_Tabela_Colunas`, `CK_Tabela_Coluna`.
- **Toda FK tem índice**, porque o SQL Server não cria sozinho.
- Dinheiro `DECIMAL(18,2)`; datas `DATETIME2(0)` em UTC; texto `NVARCHAR`.
- Regras que o banco consegue garantir viram constraint (`CHECK`, `UNIQUE`). A aplicação valida antes para dar mensagem boa.

## Consultas

- Predicados *sargable*: nada de função sobre coluna indexada no `WHERE` (`YEAR(DataPedido) = 2026` força scan).
- Datas com intervalo semiaberto: `>= '2026-01-01' AND < '2027-01-01'`. Nunca `BETWEEN` com datetime.
- Agrupe pela chave (`ClienteId`), não por descrição (`Nome`): homônimos se fundiriam.
- Desempate determinístico no `ORDER BY` (ex.: `Total DESC, ClienteId`).
- Para medir: `SET STATISTICS IO, TIME ON` + plano real, em cache quente, com **literais ou `sp_executesql`**. Variável local muda a estimativa e o plano.

## Segurança e MCP

- A API usa `pedidos_app`: `SELECT` no schema, `INSERT` em Pedidos e ItensPedido, `UPDATE (Estoque)` em Produtos. Se precisar de mais, mude `04-seguranca.sql` e os testes de integração vão confirmar.
- O MCP usa `mcp_leitura`, que só lê. **Nunca altere schema ou dados pelo MCP**; mudanças passam por script versionado e revisão.
