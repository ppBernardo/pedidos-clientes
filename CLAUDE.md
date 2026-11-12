# Pedidos de Clientes

Cadastro e consulta de pedidos: SQL Server + API .NET 10 + React. A especificação (regras RN-xx e critérios CA-xx) está em `docs/spec.md` e o plano em `docs/plan.md`. **Leia a spec antes de mudar comportamento.**

## Fluxo de trabalho (SDD)

1. Mudança de comportamento começa na spec: ajuste ou crie a RN/CA em `docs/spec.md`.
2. Mudança com mais de um arquivo: planeje antes (plan mode) e confira o plano com o humano.
3. Código e teste andam juntos: regra de negócio leva teste unitário, endpoint leva teste de integração.
4. Commits pequenos, cada um compilando e com testes verdes, em Conventional Commits em português: `feat(api): adiciona ...`, `fix(db): corrige ...`, `test(integration): cobre ...`.
5. **Nunca reescreva o histórico** (rebase, amend, push --force, reset --hard). O hook `bloquear-reescrita-git` bloqueia isso. Para corrigir, faça um novo commit.
6. Correção de código gerado pelo Claude vira um commit `fix:` próprio e uma linha em `docs/diario-claude.md`.

## Estrutura

```
backend/   Pedidos.slnx: src/{Domain,Application,Infrastructure,Api} e tests/{UnitTests,IntegrationTests}
frontend/  Vite + React + TypeScript (TanStack Query); chama sempre /api
database/  scripts 01..05 (fonte da verdade do schema) + init-db.sh + perf/ (Parte 4)
docs/      spec.md, plan.md, diario-claude.md, parte4/
.claude/   settings.json (hooks e permissões), hooks/, rules/ (regras por pasta), skills/, agents/
```

## Comandos

| Objetivo | Comando |
|---|---|
| Subir tudo | `docker compose up --build` (front em :3000, API em :8080, SQL em :1433) |
| Banco + MCP para desenvolvimento | `docker compose --profile dev up -d sqlserver db-init mcp-sqlserver` |
| Build do backend | `dotnet build backend/Pedidos.slnx` |
| Testes unitários | `dotnet test backend/tests/Pedidos.UnitTests` |
| Testes de integração (precisam de Docker) | `dotnet test backend/tests/Pedidos.IntegrationTests` |
| API local (banco do compose) | `dotnet run --project backend/src/Pedidos.Api` |
| Frontend | `npm ci --prefix frontend`, depois `npm run dev --prefix frontend` (lint: `npm run lint --prefix frontend`) |
| Testes dos hooks | `node --test .claude/hooks` |

## Arquitetura do backend

- **Dependências**: `Api → Application → Domain` e `Infrastructure → Application`. O Domain não depende de nada externo. A `Api` só conhece a `Infrastructure` para registrar a injeção de dependência.
- **Escrita**: endpoint → `ValidationFilter<T>` (FluentValidation) → `PedidoService` → domínio (`Pedido.Criar`) → repositório. A transação e a baixa de estoque ficam em `PedidoRepository.AdicionarComBaixaDeEstoqueAsync`.
- **Leitura**: endpoint → interfaces `*Consultas` → EF Core `AsNoTracking`, com projeção direto para DTO.
- **Erros** (ProblemDetails, RFC 9457, sempre com `traceId`):
  - 400 para validação (`errors` por campo, ex.: `itens[1].quantidade`);
  - 422 para `RegraNegocioException`, com `codigo` estável;
  - 404 via `TypedResults.NotFound()`.
- **Não use**:
  - MediatR, AutoMapper, repositório genérico, UnitOfWork genérico, barramento CQRS ou biblioteca de Result;
  - migrations do EF: o schema vem dos scripts em `database/`;
  - `EnableRetryOnFailure`, que é incompatível com a transação manual da criação de pedido.
- **Datas**: em UTC; "agora" vem de `TimeProvider`. `DataPedido` é truncada em segundos, porque a coluna é `datetime2(0)`. Períodos usam intervalo semiaberto (`Periodo`).

## Nomenclatura

- Termos de negócio em português (`Pedido`, `ItemPedido`, `Cliente`, `Produto`) e sufixos técnicos em inglês (`PedidoService`, `IPedidoRepository`, `PedidoEndpoints`, `CriarPedidoValidator`, `CriarPedidoRequest`, `PedidoDetalheResponse`).
- C#: métodos assíncronos com sufixo `Async`, campos privados `_camelCase`, namespaces por arquivo, `record` para DTOs.
- Testes: `Metodo_Cenario_Resultado` em português, ex.: `Criar_ComEstoqueInsuficiente_LancaRegraNegocio`. O critério da spec coberto vai em `[Trait("CA", "CA-03")]`.
- HTTP: rotas no plural e minúsculas (`/pedidos`, `/relatorios/top-clientes`); JSON em camelCase.
- SQL: tabelas no plural (`Clientes`, `ItensPedido`). Constraints como `PK_Tabela`, `FK_Tabela_Referencia`, `UX_` (índice único), `IX_` e `CK_Tabela_Coluna`.

## Banco e MCP

- O MCP `sqlserver` (DBHub via HTTP, login somente leitura `mcp_leitura`) serve para inspecionar schema e índices, ver planos de execução e consultar Query Store e DMVs. **Nunca mude o banco pelo MCP**: DDL e dados vão por script versionado em `database/`.
- Para medir com `SET STATISTICS IO/TIME`, use sqlcmd como admin. O MCP é só leitura.

## Armadilhas do ambiente (Windows)

- Git Bash converte caminhos Linux em `docker exec`; prefixe com `MSYS_NO_PATHCONV=1`.
- O bit de execução não é versionado a partir do Windows: rode scripts com `bash script.sh`.
- Crie arquivos com as ferramentas Write/Edit. Redirecionamento do PowerShell grava BOM ou UTF-16 e quebra scripts SQL e shell.
- `sqlcmd` dentro dos containers fica em `/opt/mssql-tools18/bin/sqlcmd` e precisa de `-C` (certificado autoassinado).
- `dotnet build` falha com MSB3027 se um `dotnet run` estiver segurando `bin/`: pare a API antes.

## Regras detalhadas

Regras por área em `.claude/rules/`, carregadas pelo caminho do arquivo: `backend.md`, `frontend.md`, `banco.md` e `testes.md`.
