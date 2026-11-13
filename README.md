# Pedidos de Clientes

[![CI](https://github.com/ppBernardo/pedidos-clientes/actions/workflows/ci.yml/badge.svg)](https://github.com/ppBernardo/pedidos-clientes/actions/workflows/ci.yml)

Cadastro e consulta de pedidos: **SQL Server 2022 + API .NET 10 + React 19**, tudo subindo com um único `docker compose up`. O projeto foi feito com **Claude Code** em fluxo spec-driven (SDD), e a configuração do Claude está versionada no repositório: rules, hooks, skills, subagente e MCP.

| Documento | Conteúdo |
|---|---|
| [docs/spec.md](docs/spec.md) | Especificação: regras (RN), contrato da API, critérios de aceite (CA) e rastreabilidade até os testes |
| [docs/plan.md](docs/plan.md) | Plano gerado em *plan mode*, com as decisões humanas, a revisão crítica e as alternativas descartadas |
| [docs/diario-claude.md](docs/diario-claude.md) | Diário cronológico do uso do Claude Code: o que funcionou e o que precisou ser corrigido |

## Sumário

1. [Como rodar com Docker](#como-rodar-com-docker)
2. [Como rodar os testes](#como-rodar-os-testes)
3. [Desenvolvimento local](#desenvolvimento-local)
4. [Arquitetura](#arquitetura)
5. [Principais decisões técnicas](#principais-decisões-técnicas)
6. [Como usei o Claude Code](#como-usei-o-claude-code)
7. [Parte 4: debug da query](#parte-4-debug-da-query)
8. [O que ficou de fora](#o-que-ficou-de-fora)

## Como rodar com Docker

Pré-requisito: Docker com Compose v2. No Windows, use o Docker Desktop ou o Rancher Desktop com o engine **dockerd (moby)**.

```bash
docker compose up --build
```

| Endereço | O que é |
|---|---|
| http://localhost:3000 | Frontend (listagem de pedidos e modal de criação) |
| http://localhost:8080/swagger | Swagger UI da API (documento OpenAPI em `/openapi/v1.json`) |
| `localhost:1433` | SQL Server (só no loopback). Banco `PedidosDb`, usuário `sa`, senha em [`.env.example`](.env.example) |

O compose sobe nesta ordem, garantida por healthchecks:
1. `sqlserver`.
2. `db-init`, que roda [`database/init-db.sh`](database/init-db.sh) e termina: cria o banco (com RCSI e Query Store), as tabelas, os índices, os logins de menor privilégio e a carga de exemplo (30 clientes, 25 produtos e 1.500 pedidos de jan/2025 a out/2026).
3. `backend`.
4. `frontend` (nginx, com proxy de `/api` para a API).

Os scripts são idempotentes, então rodar `up` de novo não duplica nada. Não precisa de `.env`: todas as variáveis têm padrão. Para mudar portas ou senhas, copie `.env.example` para `.env`.

**Roteiro de verificação manual (CA-15)**
1. Abra http://localhost:3000. A primeira página mostra os 10 pedidos mais recentes de um total de 1.500.
2. Filtre por um cliente e por um período, e use a paginação. Depois clique em **Limpar**.
3. Clique em **Novo pedido** e escolha um cliente e dois produtos.
   - Pedir mais do que o estoque de "Placa de vídeo RTX" (2 unidades) é barrado na própria tela, com a mensagem do item.
   - A API devolve 422 nesse mesmo caso, se o estoque mudar entre o carregamento da tela e o envio; o erro dela aparece no modal.
4. Crie o pedido. O modal fecha, aparece "Pedido #N criado" e o pedido novo fica **destacado no topo** da listagem.
   - Os filtros são mantidos depois de criar: com um filtro que exclua o pedido novo, só a confirmação aparece.

O roteiro foi executado duas vezes, pelo Claude no navegador.
- A primeira foi contra um mock da API que segue o contrato da spec, porque a máquina de desenvolvimento não tinha Docker.
- A segunda foi **contra a API real, num GitHub Codespace** (13/11/2026), e os quatro passos passaram:
  - o filtro por cliente e período trouxe os 60 pedidos da Ana Souza de janeiro a outubro de 2026, em 6 páginas;
  - pedir 3 placas de vídeo foi barrado na tela;
  - o pedido #1501 (teclado e mouse, R$ 579,80) apareceu destacado no topo.

  Pela API, o detalhe do pedido conferiu, o estoque do teclado e do mouse baixou uma unidade, e o da placa de vídeo continuou em 2.

  A mesma execução achou um defeito visual: a linha de item do modal passava da largura dele e criava rolagem horizontal, pior no celular. Ele foi corrigido em seguida.

A cada push, o job `compose` do CI faz o mesmo fluxo pela API (listar, criar e ver no topo), sem a tela.

**Problemas comuns**
- **Mudou `SA_PASSWORD` depois do primeiro `up`?** O volume guarda a senha antiga. Rode `docker compose down -v` para recriar o banco do zero.
- **Apple Silicon:** a imagem do SQL Server é só amd64 e roda emulada (`platform: linux/amd64`).
- **Porta ocupada:** ajuste `FRONTEND_PORT`, `API_PORT` ou `SQL_PORT` no `.env`.

## Como rodar os testes

Pré-requisitos: SDK do .NET 10 e, para os testes de integração, Docker rodando.

| Testes | Comando | O que cobre |
|---|---|---|
| Unitários | `dotnet test --project backend/tests/Pedidos.UnitTests` | Regras do domínio (`Pedido.Criar`, `Periodo`), validadores e `PedidoService` com dublês |
| Integração | `dotnet test --project backend/tests/Pedidos.IntegrationTests` | Os endpoints contra **SQL Server real** (Testcontainers), inclusive concorrência no estoque |
| Todo o backend | `dotnet test --solution backend/Pedidos.slnx` | Os dois acima |
| Frontend | `npm ci --prefix frontend`, depois `npm run lint --prefix frontend` e `npm run build --prefix frontend` | oxlint, typecheck (TypeScript 7) e build |
| Hooks do Claude | `node --test ".claude/hooks/**/*.test.mjs"` | Os scripts dos hooks |

O runner é o Microsoft.Testing.Platform (xUnit v3), ativado no [`global.json`](global.json), por isso o `dotnet test` usa `--project` ou `--solution`.

Como funcionam os **testes de integração**:
- Sobem um container com **a mesma imagem do compose** e o inicializam com **o mesmo `database/init-db.sh`**. Assim, os scripts SQL são testados também.
- A API em memória (`WebApplicationFactory`) conecta com o login **`pedidos_app`**: se faltar uma permissão, um teste quebra.
- O banco é limpo antes de cada teste, e cada teste cria os próprios dados.
- Destaques: 20 POSTs simultâneos para estoque 5 (exatamente 5 criados e estoque 0), pedidos em ordem cruzada sem deadlock, rollback no repositório, limites de período (23:59:59) e o ranking com homônimos.

**CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)): roda a cada push na `main` e em todo pull request, com estes jobs:

| Job | O que faz |
|---|---|
| hooks | Testes dos scripts dos hooks |
| backend | Build com warnings como erro, testes unitários e de integração |
| frontend | Lint e build |
| compose | Sobe tudo com `docker compose up --build`, sem `.env`, e faz um fluxo real pelo proxy do frontend: lista, cria, segue o `Location`, confere o topo, relatório e 422 |
| mcp | Sobe o banco e o DBHub (profile `dev`) e conversa com ele como o Claude Code: handshake, ferramentas, uma leitura e um `DELETE` recusado |

A Parte 4 tem um workflow próprio, [`parte4.yml`](.github/workflows/parte4.yml), que roda quando `database/` muda ou sob demanda: cenário de erros, medição em 4 estados e leitura do banco da medição pelo MCP.

A rastreabilidade de cada critério de aceite até o teste está na [seção 10 da spec](docs/spec.md#10-rastreabilidade-critério--teste).

## Desenvolvimento local

```bash
# banco + MCP do Claude (DBHub) em containers
docker compose --profile dev up -d sqlserver db-init mcp-sqlserver
# API em http://localhost:8080 (usa appsettings.Development.json)
dotnet run --project backend/src/Pedidos.Api
# frontend em http://localhost:5173 (proxy /api → localhost:8080)
npm ci --prefix frontend && npm run dev --prefix frontend
```

## Arquitetura

```mermaid
flowchart LR
    navegador[Navegador] -->|:3000| front["frontend<br/>nginx + SPA React"]
    front -->|/api → :8080| api["backend<br/>API .NET 10"]
    api -->|login pedidos_app| sql[("sqlserver<br/>SQL Server 2022")]
    init["db-init<br/>sqlcmd + database/*.sql"] -->|login sa| sql
    claude[Claude Code] -->|MCP via HTTP :8090| mcp["mcp-sqlserver<br/>DBHub (profile dev)"]
    mcp -->|login mcp_leitura| sql
```

| Pasta | Conteúdo |
|---|---|
| `backend/src/Pedidos.Domain` | Entidades e regras: `Pedido.Criar`, `Periodo`, erros com código. Sem dependências externas |
| `backend/src/Pedidos.Application` | `PedidoService`, validadores (FluentValidation), DTOs e portas (`I*Repository`, `I*Consultas`) |
| `backend/src/Pedidos.Infrastructure` | EF Core sobre o schema dos scripts, repositórios e consultas com projeção |
| `backend/src/Pedidos.Api` | Minimal APIs, ProblemDetails, filtro de validação, OpenAPI e `/health` |
| `backend/tests` | Testes unitários e de integração |
| `frontend/` | Vite + React + TypeScript + TanStack Query |
| `database/` | Scripts 01 a 05, `init-db.sh`, a Parte 4 (`consultas/` e `perf/`) e a configuração do MCP |
| `.claude/` e `CLAUDE.md` | Configuração do Claude Code (ver [Como usei o Claude Code](#como-usei-o-claude-code)) |

## Principais decisões técnicas

**Banco e modelagem**
- **.NET 10 LTS.** O .NET 8 saiu de suporte em 10/11/2026 e o .NET 11 é STS.
- **Os scripts SQL são a fonte da verdade do schema.** O EF Core só mapeia as tabelas, sem migrations, e os testes de integração rodam esses mesmos scripts.
- **Baixa de estoque segura sob concorrência (RN-09).** Numa transação, cada item faz `UPDATE ... SET Estoque = Estoque - @q WHERE Id = @id AND Ativo = 1 AND Estoque >= @q`, **em ordem de `ProdutoId`** e **antes** de inserir o pedido.
  - O UPDATE trava a linha e reavalia o predicado sobre o valor atual, então duas requisições nunca vendem a mesma unidade.
  - A ordem fixa evita deadlock entre pedidos com os mesmos produtos.
  - Zero linhas afetadas desfaz tudo e responde 422.
  - O `CHECK (Estoque >= 0)` é a última barreira.
  - Alternativas descartadas: `rowversion` + retry, que gera 409 falsos sob disputa, e um único `UPDATE ... FROM OPENJSON`, em que a ordem dos locks depende do plano.
- **RCSI ligado.** A listagem não bloqueia atrás das transações de pedido.
- **Total do pedido não armazenado.** Os itens são a fonte da verdade, e um índice coberto (`UX_ItensPedido_PedidoId_ProdutoId INCLUDE (Quantidade, PrecoUnitario)`) mantém a soma barata. O mesmo índice único garante a RN-07 no banco.
- **Datas em UTC, truncadas em segundos.** A coluna `datetime2(0)` *arredondaria*, e 23:59:59.6 viraria o dia seguinte. Os filtros de período são intervalos semiabertos `[início, fim + 1 dia)`, sargable.
- **Privilégio mínimo:**
  - a API conecta como `pedidos_app`, com `SELECT` no schema, `INSERT` em `Pedidos` e `ItensPedido` e `UPDATE` **só da coluna `Estoque`**;
  - o MCP conecta como `mcp_leitura`, que só lê;
  - os testes de integração cobrem as duas coisas.

**API**
- **Erros sempre em ProblemDetails (RFC 9457), com `traceId`:**
  - validação → 400, com campo no formato do JSON (`itens[1].quantidade`);
  - regra de negócio → 422, com `codigo` estável (`ESTOQUE_INSUFICIENTE`...);
  - binding inválido → 400 em qualquer ambiente.
- **4 projetos, mas com travas contra excesso:** sem MediatR e AutoMapper (comerciais desde 2025), sem repositório ou UnitOfWork genérico e sem biblioteca de Result. Leituras vão direto do banco para o DTO (CQRS leve); escritas passam pelo domínio.
- **Testes com xUnit v3, AwesomeAssertions e NSubstitute.** O FluentAssertions 8 virou licença comercial, e a AwesomeAssertions é o fork Apache 2.0 dele.

**Frontend**
- **Sempre chama `/api`.** Em dev o proxy é do Vite, no Docker é do nginx: não há CORS em nenhum ambiente.
- **TanStack Query** para cache, invalidação após criar e página anterior visível enquanto a próxima carrega.
- **Mesma toolchain do template oficial atual do Vite** (TypeScript 7 + oxlint), já que o `typescript-eslint` ainda não suporta o TS 7.

**Limitações conhecidas**
- Filtros por data em UTC, e não no fuso de negócio.
- Listas de clientes e produtos sem paginação, o que basta para o volume da demo.
- Sem autenticação, que está fora do escopo da spec.

## Como usei o Claude Code

### Fluxo

1. **Spec antes de tudo.** O `docs/spec.md` v0.1 foi commitado antes de qualquer código.
2. **Plan mode.**
   - O Claude checou as versões atuais no npm e no NuGet, em vez de confiar na memória.
   - Um subagente confirmou na documentação o formato de hooks, skills e MCP.
   - **Outro subagente fez uma revisão crítica independente do rascunho.** Dos 17 pontos dela registrados na seção 2 do plano, 14 mudaram o plano e 3 foram recusados com motivo. Entre eles havia problemas reais que o rascunho tinha deixado passar, como um pedido de exemplo com data futura, que apareceria acima dos pedidos novos, e o arredondamento do `datetime2(0)`.
3. **Decisões humanas registradas no plano**, com motivo: .NET 10, React, diferenciais escolhidos e conta do GitHub. A spec virou v1.0 com os ajustes.
4. **Implementação em commits pequenos**, em Conventional Commits em português.
   - A regra era cada commit compilar e ter testes verdes. Ficaram duas exceções conhecidas no histórico, que não foi reescrito:
     - o `7a954ff` não compila sozinho;
     - a suíte de integração, que só rodava no CI, ficou vermelha em 28 commits, de `c42bf97` até a correção em `df80e75`.
   - Correções em código já commitado viraram commits `fix:` próprios. Um deles saiu como `refactor:` (`8d2178b`).
   - Erros que o hook pegou antes do commit não geraram commit; ficaram registrados no [diário](docs/diario-claude.md).

### Configuração versionada (`CLAUDE.md` e `.claude/`)

| Peça | Arquivo | Para que serve e como foi usada |
|---|---|---|
| **Rules** | [`CLAUDE.md`](CLAUDE.md) e [`.claude/rules/`](.claude/rules) | O CLAUDE.md curto traz arquitetura, comandos, nomenclatura e armadilhas do ambiente. As regras por área (backend, frontend, banco e testes) carregam pelo caminho do arquivo editado (`paths:`). |
| **Hook de verificação** | [`verificar-apos-edicao.mjs`](.claude/hooks/verificar-apos-edicao.mjs) | `PostToolUse` em `Edit\|Write`: `dotnet build` do projeto editado, ou `tsc -b` + oxlint no frontend. O erro volta para o Claude na hora. Pegou erros reais de compilação no código gerado, inclusive o mesmo erro repetido duas vezes. |
| **Hook de histórico** | [`bloquear-reescrita-git.mjs`](.claude/hooks/bloquear-reescrita-git.mjs) | `PreToolUse` em `Bash\|PowerShell`: bloqueia rebase, amend, push forçado e reset que move o branch, porque o desafio exige histórico preservado. Tem 47 testes e foi testado ao vivo nas duas ferramentas. |
| **Skill `novo-endpoint`** | [`.claude/skills/novo-endpoint`](.claude/skills/novo-endpoint) | Roteiro do padrão do projeto (spec → contrato → porta → implementação → endpoint → testes), extraído do `POST /pedidos`. Usada depois de um reinício da sessão para terminar três fatias: `GET /pedidos` (faltavam o endpoint e os testes de integração), `GET /relatorios/top-clientes` e `GET /clientes` + `GET /produtos` (consulta, endpoint e testes). Os contratos, validadores e portas dessas fatias já existiam: foram escritos com a camada de aplicação, antes da skill. |
| **Skill `analisar-query`** | [`.claude/skills/analisar-query`](.claude/skills/analisar-query) | Roteiro de investigação de SQL: corretude, inspeção pelo MCP, medição com `sqlcmd` e comparação da original e da corrigida, com e sem índices. Traz consultas prontas de Query Store e DMVs. |
| **Subagente `revisor`** | [`.claude/agents/revisor.md`](.claude/agents/revisor.md) | Revisa os commits não publicados contra spec, CLAUDE.md e rules. Usado antes do primeiro push: 2 apontamentos viraram `fix:`, 2 foram **descartados depois de conferidos**, porque eram falsos, e 1 sugestão foi recusada porque não compila no xUnit 4. Usado de novo antes do push da revisão final: achou 1 problema real, e a correção que ele sugeriu não funcionaria. |
| **MCP SQL Server** | [`.mcp.json`](.mcp.json) e [`database/mcp/dbhub.toml`](database/mcp/dbhub.toml) | DBHub rodando como serviço HTTP do compose (profile `dev`). Fica igual em Windows, macOS e Linux, sem o `cmd /c npx` que o stdio exigiria no Windows. É somente leitura em duas camadas: o `readonly` do DBHub e o login `mcp_leitura`, que é a barreira real. |

**Como o MCP é usado.**
1. Suba o banco e o servidor: `docker compose --profile dev up -d sqlserver db-init mcp-sqlserver`.
2. Abra o Claude Code na raiz do repositório e aprove o servidor `sqlserver` do `.mcp.json` (confira em `/mcp`).
3. As ferramentas são:
   - `search_objects`: inspecionar tabelas, colunas e índices;
   - `execute_sql`: conferir a carga e ler planos e métricas do Query Store e das DMVs.

   - `explain_sql`: obter o plano estimado em XML.

   A skill `analisar-query` traz o roteiro e as consultas de diagnóstico prontas. Mudanças de schema nunca passam pelo MCP: vão por script versionado.

**MCP validado no CI.** O job `mcp` do workflow conversa com o DBHub como o Claude Code faria:
- handshake MCP via HTTP (protocolo `2025-06-18`);
- `tools/list` com as 3 ferramentas;
- uma leitura pelo `execute_sql` (os 1.500 pedidos da carga);
- um `DELETE`, **recusado pelo próprio DBHub** ("Read-only"), com a contagem conferida direto no banco depois.

**O MCP também leu o banco da Parte 4.** No workflow `parte4.yml`, depois da medição, o DBHub consultou o `PedidosPerf` com nomes de três partes. Ele usou o login `mcp_leitura` e estava conectado ao `PedidosDb` ([execução 43431232512](https://github.com/ppBernardo/pedidos-clientes/actions/runs/43431232512), no resumo da página).
- `COUNT(*)` em `PedidosPerf.dbo.Pedidos`: 400.000.
- O Query Store da medição trouxe as quatro queries, com média de 10.066, 10.066, 6.568 e 5.855 leituras lógicas. São os mesmos números do `STATISTICS IO` que o `sqlcmd` mediu na mesma execução: duas fontes independentes concordam.

**O que não foi possível nesta máquina:** usar o MCP interativamente numa sessão do Claude Code. Isso exige o banco rodando localmente, e não havia Docker. As conversas com o DBHub acima foram feitas por um cliente MCP em script, no CI, e não pelo Claude. Com Docker, os passos acima ligam o MCP na sessão.

### O que funcionou bem

- **Spec com RN e CA numerados.** Todo endpoint e todo teste aponta para uma regra (`[Trait("CA", ...)]`). A skill obriga a conferir a spec antes de codar.
- **Revisão crítica independente do plano.** Um agente revisando "a frio" achou problemas que o rascunho tinha deixado passar.
- **Hooks como rede de segurança.** O erro de compilação aparece no mesmo passo em que o código foi gerado, não minutos depois.
- **Pesquisa de versões ao vivo.** Evitou montar um frontend com ESLint em cima do TypeScript 7, que não é suportado, e usar o .NET 8 recém-descontinuado.

### O que precisei corrigir no código gerado

| Problema | Correção |
|---|---|
| Testes configurados para VSTest por hábito. O xUnit v3 4.x usa o Microsoft.Testing.Platform, e o SDK 10 recusou. | Runner novo no `global.json` e regra no CLAUDE.md |
| `new(...)` com tipo inferido como único argumento de `params` (CS8752), **repetido** num segundo arquivo. | O hook pegou as duas vezes; a correção definitiva foi trocar por *collection expression* |
| Teste "cliente inexistente" passando pelo motivo errado: o NSubstitute devolve `""`, e não `null`, para `string`. | Cenário configurado explicitamente no teste |
| `Program.cs` lia a connection string antes do `Build()`, e os testes não conseguiriam trocá-la. | Leitura na criação do `DbContext` |
| **1ª execução real no CI:** 37 de 42 testes de integração quebraram. Um teste adiantava o `FakeTimeProvider` e os seguintes tentavam voltar o relógio, o que ele proíbe. | `TimeProvider` mínimo e ajustável nos testes. Na 2ª execução, 42 de 42 passaram |
| OpenAPI documentando a query string em PascalCase, diferente do contrato. | Transformer na API, sem poluir a Application |
| Um commit intermediário não compila sozinho: o registro de DI entrou antes das consultas. | **Não** foi reescrito, porque o histórico é preservado; está registrado no diário |
| Subagente revisor com 2 falsos positivos. | Conferidos no código e descartados |
| Script de medição da Parte 4 com `sqlcmd -y 0 -h -1`, combinação que o sqlcmd recusa. A 1ª execução falhou. | Removido o `-h` |
| Modal de novo pedido com rolagem horizontal: a caixa de produto assumia a largura da opção mais longa. Foi achado ao rodar o CA-15 contra a API real. | `minmax(0, 1fr)` no grid e `fieldset` que encolhe; conferido em desktop e celular |
| **Afirmações minhas desmentidas pela evidência:** "o otimizador converte o LEFT JOIN em INNER" (o plano real mostrou que não), "o `explain_sql` não funciona no SQL Server" (funciona) e "o modal mostra o 422 ao pedir acima do estoque" (a tela barra antes). | README e diário corrigidos com o que foi medido e observado |
| **Afirmações do próprio README que o `git log` desmentiu**, achadas na revisão final: "cada correção virou um commit `fix:` próprio", "cada commit compila" e "a skill `novo-endpoint` criou os quatro endpoints". | Texto reescrito com o que os commits mostram |

**Limites do Claude Code encontrados.**
- O modo automático **bloqueou** a tentativa do agente de se dar permissões (uma lista `allow` no `settings.json`). Por isso o `settings.json` versionado só restringe.
- Skills e subagentes criados durante a sessão só ficam disponíveis depois de reiniciá-la.

## Parte 4: debug da query

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

### O que estava errado no resultado

| # | Problema | Efeito |
|---|---|---|
| 1 | `ORDER BY Total` é **ascendente** | O "ranking" começa por quem comprou **menos** |
| 2 | `GROUP BY c.Nome` | Nome não é chave: **homônimos viram uma pessoa só**, com os totais somados. Pior: na collation padrão (`SQL_Latin1_General_CP1_CI_AS`, case-insensitive e que ignora espaço final), `'ANA SOUZA '` e `'Ana Souza'` também se fundem. A carga de exemplo tem duas "Maria Silva" justamente para mostrar isso. |
| 3 | 2º `LEFT JOIN` (itens) | Pedido de 2026 **sem itens** dá `Total = NULL`, e no `ASC` o NULL vem **primeiro**. O banco não impede pedido sem itens; a API sim. Mover o filtro de data para o `ON` "corrigiria" o join, mas traria todos os clientes com NULL. O certo num ranking de compras é `INNER JOIN`. |
| 4 | 1º `LEFT JOIN` + `WHERE YEAR(p.DataPedido) = 2026` | O `WHERE` sobre `p` já descarta as linhas sem pedido, então o resultado é o de um INNER JOIN. Não é bug de resultado, mas esconde a intenção e, como o plano real mostrou, atrapalha o otimizador (ver abaixo). |
| 5 | Sem desempate | Clientes com o mesmo total saem em ordem instável |

O cenário [`database/consultas/cenario-erros.sql`](database/consultas/cenario-erros.sql) roda as duas queries sobre a carga de exemplo dentro de uma transação que termina em `ROLLBACK`. Dentro dela, cria uma cliente `'ANA SOUZA '` com um pedido de 2026 e um pedido de 2026 sem itens. Resultado real ([execução 43335181912 no CI](https://github.com/ppBernardo/pedidos-clientes/actions/runs/43335181912)):

| Query original (as primeiras e as últimas linhas) | Query corrigida |
|---|---|
| `Otávio Brandão NULL` ← **1ª linha**: pedido sem itens | Otávio não aparece: num ranking de compras só entra quem comprou |
| `Camila Ribeiro 54912.30` ... ordem **crescente** | `1 · Maria Silva (Id 4) 300307.40` |
| `Ana Souza 271588.30` ← Ana **+ 'ANA SOUZA '** (266.988,40 + 4.599,90) | `2 · João Pereira 299446.80` |
| `João Pereira 299446.80` | `5 · Ana Souza (Id 1) 266988.40` |
| `Maria Silva 401709.50` ← **as duas Marias fundidas** (300.307,40 + 101.402,10), aparecendo como a "maior" | `12 · Maria Silva (Id 19) 101402.10`, e `27 · ANA SOUZA (Id 31) 4599.90` |

### Por que era lenta (o que os planos reais mostraram)

- **`YEAR(p.DataPedido) = 2026` não é sargable.** Mesmo com `IX_Pedidos_DataPedido`, o plano da original faz **Index Scan dos 400 mil pedidos** e aplica a função linha a linha. A corrigida faz **Index Seek** só nos 132.964 de 2026.
- **O `YEAR()` também atrapalha as estimativas.** A original estimou 114.789 pedidos em 2026 (eram 132.964) e 120.000 itens (eram 398.919). A corrigida estimou 132.942 contra 132.964 reais.
- **O LEFT JOIN não virou INNER.** Eu esperava que o otimizador convertesse o join, já que o `WHERE` rejeita NULL. O plano real mostrou outra coisa: `Hash Match (Left Outer Join)` entre os 20 mil clientes e **todos** os 400 mil pedidos, e só depois o `Filter` do `YEAR()`. Com a função em volta da coluna, ele não empurrou o filtro para antes do join. Neste volume o custo extra foi pequeno, porque o que domina é ler as tabelas, mas a premissa não se confirmou.
- **Índices de suporte ausentes.** Sem os de `03-indices.sql`, as duas queries leem as tabelas pelo índice clusterizado inteiro.
  - Com eles, a leitura dos itens cai de 7.273 para 4.659 páginas, porque o índice coberto `UX_ItensPedido_PedidoId_ProdutoId` é mais estreito que a tabela.
  - O join com os itens continua *scan* + *hash join*: um terço dos itens é de 2026, e 400 mil seeks custariam mais.
  - O índice da FK `ItensPedido.PedidoId` serve para buscar os itens de um pedido (detalhe e listagem), não para este ranking anual.
- **Agrupamento por `NVARCHAR(150)`.** Além de errado, junta **20.000 clientes em 2.000 grupos** no `Hash Match (Aggregate)`, porque a massa repete os nomes.

### A query corrigida

[`database/consultas/ranking-2026-corrigida.sql`](database/consultas/ranking-2026-corrigida.sql)

```sql
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
```

- **Agrupa por `ClienteId`, ordena desc com desempate e usa `INNER JOIN`.** Resolve os erros de resultado.
- **Intervalo semiaberto em vez de `YEAR()`.**
  - O predicado vira *seek* em `IX_Pedidos_DataPedido (DataPedido) INCLUDE (ClienteId)`.
  - É à prova de hora: `BETWEEN '2026-01-01' AND '2026-12-31'` perderia o dia 31 depois da meia-noite.
- **Pré-agrega os itens por `ClienteId`** e só junta `Clientes`, para o nome, nas linhas do resultado.
  - Os itens são lidos pelo índice coberto `UX_ItensPedido_PedidoId_ProdutoId INCLUDE (Quantidade, PrecoUnitario)`, sem lookup.
  - A chave do agrupamento é um inteiro.
- O endpoint `GET /relatorios/top-clientes` usa a mesma estrutura (pré-agregação por `ClienteId` no intervalo semiaberto e `Clientes` só no fim), parametrizada, com `TOP (5)` e `COUNT(DISTINCT)` de pedidos.
  - A diferença está na posição: o endpoint numera de 1 a 5 em C#, com desempate pelo menor `ClienteId` (RN-14), e o script usa `RANK()`, que repete a posição em caso de empate.
  - Os testes de integração do endpoint reproduzem os cenários que quebram a query original.

### Como investiguei

Roteiro da skill [`analisar-query`](.claude/skills/analisar-query/SKILL.md):

1. **Corretude primeiro.** Hipóteses de erro conferidas com dados, no cenário com `ROLLBACK`.
2. **Volume para medir.** [`database/perf/run-perf.sh`](database/perf/run-perf.sh) cria o banco separado `PedidosPerf` com 20 mil clientes (nomes repetidos de propósito), 400 mil pedidos (2024–2026) e cerca de 1,2 milhão de itens.
3. **Medição em 4 estados**, em cache quente e com literais (variável local mudaria a estimativa e o plano):

   | Estado | Query | Índices |
   |---|---|---|
   | 1 | Original | Sem os de `03-indices.sql` |
   | 2 | Corrigida | Sem os de `03-indices.sql` |
   | 3 | Original | Com os de `03-indices.sql` |
   | 4 | Corrigida | Com os de `03-indices.sql` |

   Para cada estado ficam salvos `SET STATISTICS IO, TIME` e o **plano real** (`.sqlplan`). Os índices ficam num script separado (`03-indices.sql`) justamente para permitir essa comparação.
4. **Onde rodou.** A máquina de desenvolvimento não tinha Docker, então a medição roda no workflow [`parte4.yml`](.github/workflows/parte4.yml), num runner do GitHub Actions. O workflow publica na página da execução a tabela abaixo e os operadores de cada plano real, e guarda os `.sqlplan` como artefato.

**Resultado medido** ([execução 43335181912](https://github.com/ppBernardo/pedidos-clientes/actions/runs/43335181912), cache quente, 400 mil pedidos e 1,2 milhão de itens). Os tempos variam um pouco entre execuções, e as leituras lógicas quase nada: a [reexecução 43431232512](https://github.com/ppBernardo/pedidos-clientes/actions/runs/43431232512) deu 2 páginas a mais no índice dos itens nos estados 3 e 4 (6.568 e 5.855). Nessa reexecução, o Query Store lido pelo MCP confirmou as mesmas leituras médias.

| Estado | Leituras lógicas | CPU | Tempo | Acesso a `Pedidos` no plano real |
|---|---|---|---|---|
| 1 · original, sem índices | 10.066 (Itens 7.273 · Pedidos 1.934 · Clientes 859) | 643 ms | 185 ms | Clustered Index **Scan**, os 400 mil |
| 2 · corrigida, sem índices | 10.066 | 589 ms | 180 ms | Clustered Index **Scan**, com o predicado empurrado para o scan |
| 3 · original, com índices | 6.566 (Itens 4.659 · Pedidos 1.048 · Clientes 859) | 439 ms | 126 ms | Index **Scan** em `IX_Pedidos_DataPedido`, os 400 mil |
| 4 · corrigida, com índices | **5.853** (Itens 4.659 · Pedidos **335** · Clientes 859) | **353 ms** | 135 ms | Index **Seek** em `IX_Pedidos_DataPedido`, só os 132.964 de 2026 |

**Leitura honesta dos números**
- **Reescrever sem índice não muda o custo** (1 → 2): sargabilidade só ajuda quando existe um índice para buscar.
- **Com índices, só a query corrigida faz seek** (3 → 4): a leitura de `Pedidos` cai de 1.048 para 335 páginas. Do estado 1 ao 4, as leituras caem 42% e a CPU 45%.
- **O que sobra é ler os itens.** Um terço deles é de 2026, então o otimizador escolhe scan do índice coberto + hash join, que é mais barato que 400 mil seeks.
  - O próximo passo, se esse ranking fosse crítico, seria materializar o total, com uma view indexada por pedido e dia ou um `ValorTotal` em `Pedidos`. A spec optou por não fazer isso, para ter uma fonte única da verdade.
  - Hipótese **não medida**: com períodos menores (um mês, por exemplo), a vantagem do seek deve crescer, porque a parte lida de `Pedidos` encolhe junto com o período.
- **O ganho mais importante desta query é de corretude:** o ranking original estava errado.

Para reproduzir, com o compose no ar:

```bash
MSYS_NO_PATHCONV=1 docker compose run --rm --user root -v ./docs/parte4:/saida --entrypoint bash db-init /database/perf/run-perf.sh
```

## O que ficou de fora

- **Diferenciais não escolhidos:** testes E2E com Playwright, painel de top clientes no frontend e deploy online.
  - O CA-16 é coberto pelo smoke test do compose no CI.
  - O CA-15 tem roteiro manual, executado contra a API real num Codespace. Sem Playwright, a tela não é testada a cada push.
- **Fuso de negócio:** para operar em `America/Sao_Paulo`, os limites do período seriam convertidos a partir desse fuso.
- **Busca paginada** de clientes e produtos, se o volume crescer.
