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
- O MCP conecta no `PedidosDb`. Para o banco da Parte 4, use nomes de três partes:
  - `PedidosPerf.dbo.Pedidos`, `PedidosPerf.sys.query_store_plan`;
  - `DB_ID(N'PedidosPerf')` no lugar de `DB_ID()`.

  O `run-perf.sh` cria o usuário `mcp_leitura` lá também. O `search_objects` só enxerga o `PedidosDb`, que tem o mesmo schema.
- O MCP **nunca** altera o banco (o login `mcp_leitura` nem consegue). Índice ou correção vai em script versionado e passa por revisão.

## 3. Medir (sqlcmd como admin, cache quente)

A medição completa (4 estados, estatísticas e planos reais em `docs/parte4/`) é o `run-perf.sh`:

```bash
MSYS_NO_PATHCONV=1 docker compose run --rm --user root -v ./docs/parte4:/saida --entrypoint bash db-init /database/perf/run-perf.sh
```

Para um script avulso, use o serviço `db-init`, que já tem o sqlcmd, a pasta `database/` montada e o `SA_PASSWORD` no ambiente. O serviço `sqlserver` não monta `database/`, e no host a variável não existe. As aspas simples fazem a variável ser expandida dentro do container:

```bash
MSYS_NO_PATHCONV=1 docker compose run --rm --entrypoint bash db-init -c \
  '/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -C -b -f 65001 -d PedidosPerf -i /database/perf/<script>.sql'
```

- `SET STATISTICS IO, TIME ON;` e plano real (`SET STATISTICS XML ON;`). Os `.sqlplan` vão para `docs/parte4/`, que é saída local e não é versionada. No CI, eles viram artefato do `parte4.yml`.
- Use **literais ou `sp_executesql`**, nunca variável local. Com variável local o otimizador estima por densidade média e escolhe outro plano.
- Rode duas vezes e registre a segunda (cache quente): leituras lógicas por tabela, CPU e tempo decorrido.

## 4. Corrigir e comparar

- Predicados *sargable*: intervalo semiaberto, sem função sobre coluna indexada.
- Agregue pela chave e só depois junte as descrições (pré-agregação).
- Use desempate determinístico.
- Compare 4 estados, separando o efeito da reescrita do efeito dos índices:
  1. original sem índices;
  2. corrigida sem índices;
  3. original com índices;
  4. corrigida com índices.

  Compare o resultado (explicando cada diferença) e o custo (leituras, CPU, tempo e operadores do plano).
  - Na Parte 4, o estado 2 mostrou que a reescrita sozinha **não muda** as leituras: sargabilidade só ajuda quando há índice para buscar.
- **Desconfie das próprias premissas.** "O otimizador converte esse LEFT JOIN em INNER" parecia certo e o plano real mostrou o contrário. Confirme no plano antes de escrever.
- Registre no README (seção da Parte 4) **só números medidos**, nunca estimados de cabeça.
- Sem Docker local, rode a medição no workflow `parte4.yml`, que publica o resumo na página da execução.
