---
name: analisar-query
description: Investiga a corretude e a performance de uma consulta SQL Server do projeto (plano de execução, STATISTICS IO/TIME, índices, Query Store e DMVs), usando o MCP sqlserver (somente leitura) para inspecionar e o sqlcmd do container para medir. Use para "a query está lenta", "o resultado está errado", revisão de SQL nova e a Parte 4.
argument-hint: "<arquivo .sql ou descrição da consulta>"
---

# Analisar consulta: $ARGUMENTS

**Pré-requisitos.**
- Banco e MCP no ar: `docker compose --profile dev up -d sqlserver db-init mcp-sqlserver`.
- O servidor `sqlserver` aparece conectado em `/mcp`.
- Para medir com volume, existe o banco `PedidosPerf`, criado por `database/perf/run-perf.sh`.

## 1. Corretude antes de performance

Leia a consulta e a regra de negócio correspondente em `docs/spec.md`. Depois confira as armadilhas mais comuns, uma a uma:

| Sintoma | Causa típica |
|---|---|
| Totais "somados" entre pessoas diferentes | `GROUP BY` por coluna descritiva (`Nome`): junta homônimos e, na collation CI_AS, variações de caixa e de espaço final |
| Ranking invertido ou instável | `ORDER BY` sem `DESC` ou sem desempate determinístico |
| Linhas com total NULL no topo | `LEFT JOIN` em detalhe inexistente + `ORDER BY ... ASC` (NULL vem primeiro) |
| `LEFT JOIN` que não traz os "sem correspondência" | `WHERE` sobre a tabela da direita, que transforma o join em INNER |
| Último dia do período faltando | `BETWEEN '...' AND 'yyyy-12-31'` com coluna que tem hora |
| Contagem inflada | `COUNT(*)` depois de join 1:N; use `COUNT(DISTINCT chave)` |

Prove cada hipótese com dados:
- pela carga existente, via MCP (`execute_sql`);
- ou com um cenário mínimo **dentro de uma transação com ROLLBACK**, via sqlcmd como admin, para não sujar o banco.

## 2. Inspecionar pelo MCP (somente leitura)

- `search_objects`: tabelas, colunas e índices envolvidos. Toda FK tem índice?
- `execute_sql` com as consultas prontas de [consultas-dmv.sql](consultas-dmv.sql):
  - planos e leituras médias no Query Store;
  - índices ausentes sugeridos pelo otimizador;
  - estrutura dos índices.
- O MCP **nunca** altera o banco (o login `mcp_leitura` nem consegue). Índice ou correção vai em script versionado e passa por revisão.

## 3. Medir (sqlcmd como admin, cache quente)

```bash
MSYS_NO_PATHCONV=1 docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C -d PedidosPerf -i /database/perf/<script>.sql
```

- `SET STATISTICS IO, TIME ON;` e plano real (`SET STATISTICS XML ON;`). Salve o `.sqlplan` em `docs/parte4/`.
- Use **literais ou `sp_executesql`**, nunca variável local. Com variável local o otimizador estima por densidade média e escolhe outro plano.
- Rode duas vezes e registre a segunda (cache quente): leituras lógicas por tabela, CPU e tempo decorrido.

## 4. Corrigir e comparar

- Predicados *sargable*: intervalo semiaberto, sem função sobre coluna indexada.
- Agregue pela chave e só depois junte as descrições (pré-agregação).
- Use desempate determinístico.
- Compare os estados:
  - **A**: original sem índices;
  - **B**: original com índices;
  - **C**: corrigida com índices.

  Compare o resultado (explicando cada diferença) e o custo (leituras, CPU, tempo e operadores do plano).
- Registre no README (seção da Parte 4) **só números medidos**, nunca estimados de cabeça.
