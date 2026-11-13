# Plano de implementação: Pedidos de Clientes

| | |
|---|---|
| **Spec** | [docs/spec.md](spec.md) (v1.0) |
| **Gerado em** | 2026-11-12, Claude Code em *plan mode* (Claude Opus 5.5) |
| **Status** | Aprovado e executado; desvios e retrospectiva nas seções 5 e 6 |

## Como este plano foi produzido

1. **Spec primeiro.** O rascunho v0.1 de `docs/spec.md` foi escrito e commitado antes de qualquer código.
2. **Plan mode.** Com o Claude em modo somente leitura:
   - versões atuais checadas direto no npm e no NuGet. Isso revelou que o .NET 11 e o TypeScript 7 tinham acabado de sair e que o template oficial do Vite trocou ESLint por oxlint;
   - formatos exatos de hooks, skills, rules e `.mcp.json` confirmados na documentação do Claude Code, por um subagente especializado (`claude-code-guide`).
3. **Revisão crítica independente.** Um subagente de planejamento recebeu os requisitos e as decisões provisórias e devolveu uma crítica sem partir das conclusões do primeiro rascunho. A seção 2 mostra o que foi aceito e o que foi recusado.
4. **Perguntas ao humano** nos pontos que eram decisão dele: versão do .NET, framework do frontend, diferenciais e conta do GitHub (seção 1).
5. **Aprovação** do plano final. Os ajustes que ele trouxe para a spec viraram a v1.0.

## 1. Decisões do revisor humano

| Tema | Opções apresentadas | Decisão | Motivo / consequência |
|---|---|---|---|
| Versão do .NET | .NET 8 (já instalado) ou .NET 10 | **.NET 10 LTS** | O .NET 8 saiu de suporte em 10/11/2026; o .NET 11 é STS. Exige instalar o SDK 10 na máquina de desenvolvimento. |
| Docker | Instalar agora ou seguir sem | **Instalar** | Sem Docker não há SQL Server, testes de integração nem compose para validar. |
| Frontend | React ou Angular | **React + TypeScript** | Mais enxuto para duas telas. Segue o template oficial do Vite (TS 7 + oxlint). |
| Diferenciais | Playwright, CI, painel de top clientes, deploy | **Só o CI** | O CA-15 (criar no modal e ver na listagem) virou verificação manual. Saíram o painel e o deploy. |
| Conta do GitHub | — | **ppBernardo**, pedida no meio do planejamento | O Credential Manager tinha duas contas. O Claude confirmou o ID do e-mail noreply pela API pública antes de configurar. O remote leva o usuário na URL para o push não usar a conta errada. |

## 2. Revisão crítica do rascunho

**Quem decidiu.**
- Os pontos vieram do subagente de planejamento.
- A triagem foi proposta pelo Claude: aceitar ou recusar cada ponto, com o motivo.
- O humano aprovou a triagem junto com o plano, sem alterações item a item.

As decisões que partiram do humano estão na seção 1.

**Aceito, e o plano mudou por causa disso**:

| Ponto levantado | Ajuste |
|---|---|
| Pedido de exemplo com data futura (31/12/2026) ficaria **acima** dos pedidos novos na listagem | Carga com datas fixas até 31/10/2026; fronteiras só nos testes e na Parte 4 |
| `datetime2(0)` **arredonda**: 23:59:59.6 vira o dia seguinte e o 201 diverge do que foi gravado | `DataPedido` truncada em segundos no domínio, com `TimeProvider` |
| `EnableRetryOnFailure` lança exceção com transação manual | Proibido no CLAUDE.md |
| `npx` no `.mcp.json` precisa de `cmd /c` no Windows e quebra no Mac/Linux | DBHub como serviço HTTP do compose (profile `dev`) |
| O "read-only" do DBHub no SQL Server é só um classificador de palavras-chave | A barreira real é o login `mcp_leitura`, com permissões mínimas |
| Leituras bloqueando atrás das transações de pedido | RCSI ligado de forma idempotente |
| Baixa de estoque: snapshot rastreado ficaria obsoleto; produto desativado depois do snapshot | Snapshot `AsNoTracking`; predicado `Ativo = 1 AND Estoque >= @q`; baixa em ordem de `ProdutoId` *antes* do INSERT do pedido |
| 4 projetos: o risco de exagero está *dentro* deles | Travas no CLAUDE.md: sem MediatR/AutoMapper, repositório genérico, barramento CQRS ou biblioteca de Result |
| `db_datawriter` é amplo demais | `SELECT` no schema + `INSERT` em 2 tabelas + `UPDATE (Estoque)`. Os testes rodam a API com esse login. |
| "Dados únicos por teste" não sustenta listagem sem filtro nem ranking global | Um container por assembly, reset das 4 tabelas por teste, o mesmo `init-db.sh` do compose |
| Hook com `npx` e matcher só `Bash` | Forma exec com `node`; trava de git cobre `Bash\|PowerShell` + `permissions.deny` |
| `dataFim=9999-12-31` e `pagina` enorme dão 500 por estouro | Faixas validadas (datas 2000–2100, página ≤ 1.000.000) |
| Parte 4: collation CI funde "ANA SOUZA" e "Ana Souza "; LEFT JOIN não é a causa da lentidão; variável local muda o plano | Diagnóstico corrigido; medição em 3 estados (A/B/C) com literais |
| Async suffix como warning + warnings como erro quebraria os testes xUnit | `.editorconfig` próprio em `backend/tests` |

**Recusado**, e por quê:

| Sugestão | Motivo da recusa |
|---|---|
| Mapear a `SqlException 547` (violação do CHECK de estoque) para 422 | Com a baixa condicional esse caminho não existe. Se o CHECK disparar, é bug, e um 500 com log é o sinal certo. |
| Plano com 78 commits | Granularidade excessiva. O alvo é ~55 commits, cada um compilando e com testes verdes. |
| Hook de `Stop` rodando os testes a cada resposta | Adiciona latência em toda interação. O build pós-edição e o CI já cobrem isso. |

## 3. Alternativas descartadas

| Decisão | Alternativa | Por que não |
|---|---|---|
| Baixa condicional item a item, em ordem de Id | Um único `UPDATE ... FROM OPENJSON(@itens)` | A ordem dos locks passa a depender do plano de execução, com risco de deadlock. Até 100 idas ao banco numa transação é aceitável. |
| Baixa condicional | `rowversion` + retry | Pior sob disputa: gera 409 falsos e retries. |
| FluentValidation | Validação nativa do .NET 10 (`AddValidation()`) | Regras como "produto repetido" ficam mais claras e testáveis em validadores próprios. |
| EF Core sobre o schema dos scripts | Migrations do EF ou Dapper | Os scripts SQL são a fonte da verdade (o desafio pede `database/`). EF dá LINQ seguro nas leituras, e a SQL crítica do relatório fica explícita via `SqlQuery`. |
| xUnit v3 + AwesomeAssertions + NSubstitute | FluentAssertions 8 / Moq | FluentAssertions 8 virou licença comercial; o Moq teve o episódio SponsorLink. |
| DBHub via HTTP no compose | `npx` em stdio; MCP oficial da Microsoft | Portabilidade entre sistemas operacionais e versão fixada em imagem; o MCP oficial exige clonar e compilar. |
| Reset simples das 4 tabelas | Respawn | Com 4 tabelas, um `DELETE` na ordem das FKs é mais simples e não adiciona dependência. |

---

## 4. Plano aprovado

### 4.1 Arquitetura

```
/
├─ CLAUDE.md                    regras gerais (< 200 linhas; aponta para docs/spec.md)
├─ .claude/
│  ├─ settings.json             hooks + permissões (allow para build e testes; deny para reescrita de histórico)
│  ├─ hooks/                    verificar-apos-edicao.mjs, bloquear-reescrita-git.mjs (+ testes com node --test)
│  ├─ rules/                    backend.md, frontend.md, banco.md, testes.md (frontmatter paths:)
│  ├─ skills/                   novo-endpoint/, analisar-query/
│  └─ agents/                   revisor.md (revisa o diff contra spec e CLAUDE.md antes do push)
├─ .mcp.json                    { "sqlserver": { "type": "http", "url": "http://localhost:8090/mcp" } }
├─ database/                    01-criar-banco, 02-tabelas, 03-indices, 04-seguranca, 05-carga-dados, init-db.sh, mcp/dbhub.toml
│  └─ perf/                     run-perf.sh, geração de volume (GENERATE_SERIES), cenário, queries original e corrigida
├─ backend/                     Pedidos.slnx, global.json, Directory.Build.props, Directory.Packages.props, Dockerfile, .dockerignore
│  ├─ src/                      Pedidos.Domain · Pedidos.Application · Pedidos.Infrastructure · Pedidos.Api
│  └─ tests/                    Pedidos.UnitTests · Pedidos.IntegrationTests
├─ frontend/                    Vite + React + TS; Dockerfile (build Node, depois nginx com proxy /api), nginx.conf
├─ docs/                        spec.md, plan.md, diario-claude.md, parte4/ (.sqlplan antes e depois)
├─ docker-compose.yml           sqlserver → db-init → backend → frontend (+ mcp-sqlserver no profile dev)
└─ .github/workflows/ci.yml     backend (build, unitários, integração) · frontend (lint, tsc, build) · compose (smoke)
```

| Projeto | Conteúdo |
|---|---|
| Domain | `Cliente`, `Produto`, `Pedido` (`Criar(...)`, `ValorTotal`), `ItemPedido` (`Subtotal`), `ItemSolicitado`, `Periodo` (`InicioUtc`, `FimExclusivoUtc`), `RegraNegocioException`, `CodigosErro`. Nenhuma dependência externa. |
| Application | `PedidoService`; validadores FluentValidation; DTOs `*Request`/`*Response`, `PaginaResponse<T>`; portas `I{Cliente,Produto,Pedido}Repository` e `I{Pedido,Relatorio,Catalogo}Consultas`; `TimeProvider`. |
| Infrastructure | `PedidosDbContext` sobre o schema dos scripts (**sem migrations**); conversor UTC; repositórios; consultas `AsNoTracking` com projeção direto para DTO. O relatório usa `Database.SqlQuery<T>` com a mesma SQL da Parte 4. |
| Api | Minimal APIs: `PedidoEndpoints`, `RelatorioEndpoints`, `CatalogoEndpoints`; `ValidationFilter<T>` → 400; `IExceptionHandler`: `RegraNegocioException` → 422 com `codigo`; OpenAPI + Swagger UI; `/health`. |

- **Frontend**:
  - TanStack Query, com a listagem invalidada após criar;
  - `fetch` tipado que converte ProblemDetails em `ApiError`;
  - `PedidosPage` (filtros, tabela, paginação) e `NovoPedidoModal` (`<dialog>` nativo);
  - chamadas sempre para `/api`: proxy do Vite em dev, nginx no Docker.
- **Docker**:
  - SQL Server 2022 com o CU fixado, a mesma imagem nos testes;
  - `db-init` reaproveita a imagem e roda `bash /database/init-db.sh`;
  - backend em `aspnet:10.0`, com usuário não root e healthcheck via curl;
  - senhas padrão alfanuméricas e `${VAR:-padrão}`.
- **Skills**: cada uma é criada logo depois que o código de referência existe e é **usada de fato**:
  - `novo-endpoint` gera o GET /pedidos e o relatório;
  - `analisar-query` conduz a Parte 4 pelo MCP.

### 4.2 Parte 4: diagnóstico prévio (a confirmar com planos reais)

| # | Problema | Efeito |
|---|---|---|
| 1 | `ORDER BY Total` ascendente | o "ranking" começa por quem comprou menos |
| 2 | `GROUP BY c.Nome` | funde homônimos e, pela collation CI_AS, variações de caixa e espaço final |
| 3 | 2º `LEFT JOIN` continua externo | pedido sem itens dá `Total` NULL, que vai para o topo no ASC |
| 4 | `YEAR(p.DataPedido) = 2026` | não é sargable e força scan |
| 5 | FK `ItensPedido.PedidoId` sem índice; agregação por `NVARCHAR(150)` | scan + hash join na maior tabela; agregação larga + sort |

Correção:
- pré-agregar por `ClienteId` num intervalo semiaberto, com INNER JOINs;
- juntar `Clientes` só no fim;
- ordenar por `Total DESC, c.Id`.

Evidências:
- uma massa em `PedidosPerf`, com estados A/B/C;
- `STATISTICS IO/TIME` e os `.sqlplan` versionados;
- Query Store e DMVs lidos pelo MCP.

### 4.3 Testes

- **Unitários** (xUnit v3, NSubstitute, AwesomeAssertions):
  - regras do `Pedido` e limites do `Periodo`;
  - validadores (CA-04/05/06/12/14);
  - `PedidoService` (cliente/produto inexistente, inativo, estoque, data truncada, corrida).
- **Integração** (Testcontainers.MsSql + `WebApplicationFactory`, scripts reais, login da API):
  - CA-01 a CA-14 e rollback;
  - concorrência do CA-08 (20 POSTs para estoque 5) e ordem cruzada sem deadlock;
  - mapeamento EF contra o schema, idempotência do init, acentos e ProblemDetails.
- **CI**: backend, frontend e smoke do compose.

### 4.4 Execução por marcos

| Marco | Conteúdo | Depende de |
|---|---|---|
| M0 | spec v1.0; este plano; diário de uso do Claude | — |
| M1 | CLAUDE.md; rules; hook que trava reescrita de git (+ teste); permissões | — |
| M2 | scripts SQL 01–05 + `init-db.sh` | Docker para validar |
| M3 | solução .NET (global.json, props centrais, 4 projetos + 2 de testes); hook de build pós-edição | SDK 10 |
| M4 | Domain + testes; Application + testes; CI do backend | SDK 10 |
| M5 | compose com sqlserver + db-init; MCP (serviço `dev` + `.mcp.json`); pausa para reiniciar a sessão e carregar o MCP | Docker |
| M6 | EF Core + repositórios; fixture Testcontainers; POST e GET /pedidos/{id}; ProblemDetails; integração (incl. concorrência) | SDK + Docker |
| M7 | skill `novo-endpoint` → demais endpoints + testes; `/health`; Swagger; subagente `revisor`; API no compose | — |
| M8 | frontend + nginx no compose; CI do frontend + smoke do compose | — |
| M9 | Parte 4 com a skill `analisar-query` e o MCP | MCP |
| M10 | README; rastreabilidade CA → teste; retrospectiva | — |

### 4.5 Verificação

| Marco | Como |
|---|---|
| M1 | `node --test .claude/hooks`; `git commit --amend` bloqueado via Bash e via PowerShell |
| M2/M5 | `docker compose down -v && docker compose up -d sqlserver db-init` duas vezes, sem duplicar dados; login da API sem DDL; login do MCP sem escrita; acentos íntegros; MCP conectado |
| M3/M4 | build com 0 warnings; erro proposital num `.cs` é devolvido pelo hook; unitários verdes local e no CI |
| M6/M7 | integração verde; CA-08 repetido sem instabilidade; payloads iguais à spec §5 |
| M8 | lint, tsc e build verdes; roteiro manual em `localhost:3000` |
| M9 | massa reproduzível; original × corrigida divergem no cenário; corrigida = endpoint |
| M10 | clone limpo sem `.env` → `docker compose up` funciona; CI verde |

---

## 5. Desvios durante a implementação

Comparação do plano aprovado com o que de fato aconteceu. O detalhe de cada episódio está no [diário](diario-claude.md).

| O plano previa | O que aconteceu | Por quê |
|---|---|---|
| Backend (M3–M7) antes do frontend (M8) | Scripts SQL e frontend feitos antes do backend | O SDK do .NET 10 e o Docker ainda não estavam na máquina. O próprio plano previa essa troca. |
| SDK do .NET 10 instalado pelo usuário | Instalado **por usuário** com o script oficial `dotnet-install`, sem admin; o hook de build foi ajustado para achar esse SDK | A instalação não tinha acontecido e o backend estava bloqueado |
| Testes em xUnit v3 (runner não definido) | Microsoft.Testing.Platform, ativado no `global.json` | O xUnit v3 4.x não roda no modo VSTest do SDK 10 |
| M2/M5/M6 validados localmente; pausa para reiniciar a sessão e usar o MCP | Integração (42 testes), `docker compose up` e MCP validados **no GitHub Actions**, com jobs de smoke test para o compose e o MCP. O **uso interativo do MCP numa sessão local não aconteceu**. Na revisão final, o DBHub também leu o banco da medição no `parte4.yml` (contagem e Query Store), por um cliente MCP em script | A máquina continuou sem Docker até o fim |
| Skills criadas e usadas em sequência | A `novo-endpoint` só pôde ser usada depois de um reinício da sessão | Skills e subagentes só carregam quando a sessão começa |
| A `novo-endpoint` gera o GET /pedidos e o relatório (§4.1) | Contratos, validadores e portas de listagem, relatório e catálogo saíram com a camada de aplicação (M4), antes da skill. Depois do reinício, ela terminou três fatias: endpoint e testes do GET /pedidos; consulta, endpoint e testes do relatório e do catálogo | A camada de aplicação foi escrita inteira de uma vez, e a skill só nasceu depois do POST /pedidos, como o plano mandava |
| `settings.json` com lista `allow` para build e testes (§4.1) | Só restrições: hooks e `permissions.deny` | O classificador do modo automático bloqueou o agente de se conceder permissões |
| Cenário e queries da Parte 4 em `database/perf/` (§4.1) | Em `database/consultas/`; `perf/` ficou só com a medição | O cenário de erros roda no `PedidosDb`, com a carga de exemplo, e não depende do volume |
| `global.json` em `backend/` (§4.1) | Na raiz do repositório | O `dotnet` procura o `global.json` a partir do diretório atual. Na raiz, o runner de testes vale para os comandos do README rodados de lá |
| `node --test .claude/hooks` (§4.5) | `node --test ".claude/hooks/**/*.test.mjs"` | No Node 24, um diretório passado ao `--test` é tratado como arquivo |
| Parte 4 investigada pelo MCP, em 3 estados (A/B/C), com os `.sqlplan` em `docs/parte4/` | Medição no workflow `parte4.yml`, em **4 estados**. Os números e os operadores dos planos estão no README; os `.sqlplan` ficam como artefato da execução, e `docs/parte4/` está no `.gitignore` | Sem Docker local. O estado extra (corrigida sem índices) mostrou que a reescrita sozinha não muda o custo |
| Diagnóstico nº 5: a FK `ItensPedido.PedidoId` sem índice causa scan + hash join (§4.2) | Com o índice, o join com os itens continua scan + hash join. O ganho veio do índice coberto, mais estreito (7.273 → 4.659 páginas), e do seek em `Pedidos` | Um terço dos itens é de 2026: 400 mil seeks custariam mais que o scan |
| Datas de fronteira nos testes e na massa da Parte 4 (§2) | Só nos testes de integração | A massa é gerada por aritmética modular. As fronteiras do período são exercitadas pelos testes da listagem e do relatório |
| Diagnóstico aceito na revisão: "o LEFT JOIN não é causa de lentidão; o otimizador o converte em INNER" | O plano real manteve o `Left Outer Join` e aplicou o `YEAR()` depois do join | A evidência derrubou a premissa, e o README foi corrigido |
| Frontend verificado em `localhost:3000` com a API real | Verificado no navegador contra um mock que segue o contrato da spec; o fluxo pela API real é coberto pelo smoke test do compose no CI | Sem Docker local |
| Cerca de 55 commits, cada um compilando e com testes verdes (§2) | Cerca de 100, contando a revisão final (o número exato está no `git log`). Duas exceções à regra: o `7a954ff` não compila sozinho, e a integração ficou vermelha em 28 commits (`c42bf97` a `df80e75`) | Correções em código já commitado viraram commits `fix:` próprios, o CI rendeu commits a mais e a revisão final acrescentou outro lote. Sem Docker local, a integração só rodou no CI depois do primeiro push; o histórico não foi reescrito |
| Subagente `revisor` antes de cada push | Usado antes do primeiro push, que concentrou quase todo o código, e antes do push da revisão final | Os pushes intermediários foram correções pequenas, validadas pelo CI |

## 6. Retrospectiva

**O plano acertou em:**
- Spec com RN e CA numerados. Ela guiou testes, endpoints e a rastreabilidade sem retrabalho.
- Ter um plano B para a falta de SDK e de Docker. A ordem mudou e o trabalho não parou.
- Usar os mesmos scripts SQL no compose e nos testes. A primeira execução real do banco no CI funcionou de primeira.
- A revisão crítica independente, que pegou problemas reais antes do código existir (seção 2).

**O plano errou ou subestimou:**
- **A dependência do ambiente.** Validar só no CI funcionou, mas deixou o MCP sem uso interativo, justamente um dos itens avaliados.
- **Premissas sobre o otimizador.** O plano tratou como fato o que só a medição podia confirmar.
- **Os testes de integração ficaram muito tempo sem rodar.** Eles foram escritos bem antes da primeira execução, e o bug do relógio de teste só apareceu no CI. Publicar no GitHub mais cedo teria antecipado isso.
- **Afirmações sobre o próprio processo.** O README chegou a dizer que cada correção virou commit `fix:` próprio, que todo commit compilava e que a skill criou quatro endpoints inteiros. A auditoria final conferiu com o `git log` e corrigiu o texto. Afirmação sobre o processo precisa da mesma evidência que afirmação sobre o código.
