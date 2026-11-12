# Diário de uso do Claude Code

Registro cronológico de como o Claude Code foi usado neste projeto: o que funcionou, o que precisou de correção e as decisões tomadas por humano. O README resume este arquivo na seção "Como usei o Claude".

## 2026-11-12: Especificação e planejamento

**Checagem do ambiente antes de qualquer código.**
- O Claude verificou as ferramentas instaladas e encontrou **Docker ausente (sem WSL2)** e **só o SDK do .NET 8**, que saiu de suporte em 10/11/2026.
- Em vez de assumir, ele perguntou. Decisão humana: instalar o Docker e usar o **.NET 10 LTS**.

**Spec antes do plano.**
- `docs/spec.md` v0.1 foi escrita e commitada antes de entrar em plan mode.
- Ela já trazia regras numeradas (RN-xx) e critérios de aceite (CA-xx) com o nível de teste esperado, o que depois orienta os testes.

**Plan mode com pesquisa de versões ao vivo.**
- As versões foram consultadas no npm e no NuGet, não de memória:
  - o .NET 11 e o **TypeScript 7** (compilador nativo) tinham acabado de sair;
  - o `typescript-eslint` não suporta TS 7;
  - o **template oficial do Vite trocou ESLint por oxlint**.
- Seguir o template evitou montar uma toolchain incompatível.

**Subagentes no planejamento.**
- `claude-code-guide` confirmou na documentação oficial os formatos de hooks, skills, `.claude/rules` e `.mcp.json`. Achados importantes:
  - hook em "forma exec" para funcionar em qualquer sistema operacional;
  - `npx` no `.mcp.json` exige `cmd /c` no Windows.
- Um subagente de planejamento fez uma **revisão crítica independente** do rascunho e achou problemas reais que tinham passado:
  - pedido de exemplo com data futura, que ficaria acima dos pedidos novos;
  - arredondamento do `datetime2(0)`;
  - o modo "read-only" do DBHub, que é fraco no SQL Server;
  - duas imprecisões no diagnóstico da Parte 4.
- O resultado está em `docs/plan.md`, seção 2. Três sugestões dele foram **recusadas**, com motivo.

**Decisões humanas durante o plano.**
- React (e não Angular).
- Só o CI como diferencial: Playwright, painel extra e deploy ficaram de fora.
- **Troca da conta do GitHub** no meio do planejamento. O Claude:
  - encontrou duas contas no Git Credential Manager;
  - confirmou o ID do e-mail noreply pela API pública;
  - pôs o usuário na URL do remote, para o push não sair pela conta errada.

## 2026-11-12: CLAUDE.md, rules e hook de histórico (M1)

- **O CLAUDE.md ficou curto e o detalhe foi para `.claude/rules/`.** Regras de backend, frontend, banco e testes carregam pelo caminho do arquivo editado (`paths:`), para o contexto não inchar.
- **Hook `bloquear-reescrita-git`**, criado porque o desafio proíbe reescrever histórico:
  - a análise do comando é uma biblioteca pura com 47 testes (`node --test`). Os casos que não podem dar falso positivo têm teste próprio: mensagem de commit citando "git rebase", heredoc, here-string do PowerShell e `echo git push --force`;
  - **testado ao vivo na própria sessão**: o Claude tentou `git commit --amend` via Bash e `git push --force` via PowerShell, e os dois foram bloqueados, com o motivo devolvido para ele.
- **Correção**: o comando sugerido para rodar os testes dos hooks (`node --test .claude/hooks`) não funciona no Node 24, porque um diretório é tratado como arquivo. Trocado por um glob no CLAUDE.md.
- **Limite de segurança do Claude Code**: o classificador do modo automático **bloqueou** a primeira versão do `.claude/settings.json`. Ela trazia uma lista `allow`, ou seja, o próprio agente se concedendo permissões (auto-modificação). Foi versionada só a parte que *restringe* (hook + regras `deny`). Uma lista `allow` para reduzir confirmações fica a critério do humano.

## 2026-11-12: scripts SQL, frontend adiantado, hook de verificação e MCP

- **Ordem adaptada ao ambiente.** Sem o SDK do .NET 10 e sem Docker na máquina, o Claude seguiu o plano B do `plan.md`:
  - escreveu os scripts SQL (a validar quando o Docker chegar);
  - adiantou o frontend, guiado só pelo contrato da seção 5 da spec.
- **Falso alarme corrigido antes de virar código.**
  - A checagem de fim de linha com `grep -c $'\r'` no Git Bash acusou CRLF em todos os arquivos. Na verdade, esse comando conta **todas** as linhas, porque o CR do padrão se perde.
  - Antes de criar um hook para "converter", o Claude conferiu os bytes com `od` e com Node: os arquivos sempre estiveram em LF. Nenhum hook foi criado.
  - Lição registrada: confirmar a evidência antes de agir sobre ela.
- **Hook de verificação pós-edição** (`verificar-apos-edicao`):
  - roda `dotnet build` do projeto mais próximo para C#, e `tsc -b` + oxlint para TypeScript;
  - **testado ao vivo**: o Claude criou um arquivo com erro de tipo e recebeu o erro do `tsc` na mesma hora;
  - tem lock contra builds simultâneos de edições em paralelo.
- **MCP do SQL Server via HTTP.**
  - O DBHub roda como serviço do compose (profile `dev`). O `.mcp.json` não depende do sistema operacional.
  - O `explain_sql` do DBHub usa `EXPLAIN`, que o SQL Server 2022 não tem, então fica a validar. A alternativa já prevista é ler planos do Query Store e das DMVs com `execute_sql`.

## 2026-11-12: solução .NET, domínio e aplicação (M3 e M4)

- **SDK sem admin.** O SDK 10 foi instalado por usuário com o script oficial `dotnet-install`. O hook de build foi corrigido para achar esse SDK quando o `dotnet` do PATH só tem o 8, e para forçar a saída do CLI em inglês. Neste Windows em português, as mensagens localizadas quebravam a detecção.
- **Correções em código gerado pelo Claude**, cada uma em commit próprio:
  1. **Runner de testes.** O Claude configurou os projetos de teste para VSTest (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`), por hábito. O xUnit v3 4.x roda no Microsoft.Testing.Platform, e o SDK 10 recusou. Correção: ativar o novo `dotnet test` no `global.json`. A regra foi registrada no CLAUDE.md para não se repetir.
  2. **`new(...)` com `params`.** Um `new(0, 1)` com tipo inferido como único argumento de um `params` não compila (CS8752). O **hook pegou o erro na hora**. Mesmo assim o Claude **repetiu o erro** no arquivo seguinte, e o hook pegou de novo. A correção definitiva foi trocar o `params` por *collection expression* (`[new(1, 1)]`).
  3. **Analisador do xUnit (xUnit1051).** Ele recusou `default` como `CancellationToken` nas verificações do NSubstitute. Trocado por `Arg.Any<CancellationToken>()`.
  4. **Teste com falso positivo.** O teste "cliente inexistente" passava pelo motivo errado. O NSubstitute devolve `""`, e não `null`, para `string` não configurada, então o serviço "achava" o cliente. O código de produção estava certo; o teste agora configura o cenário explicitamente.
- **Resultado:** 55 testes unitários, build com warnings como erro (0 warnings) e CI com o job do backend.

## 2026-11-12: persistência, API e testes de integração (M6)

- **Baixa de estoque** com UPDATE condicional em ordem de `ProdutoId`, antes do INSERT do pedido. Os detalhes vieram da revisão independente do plano.
- **Smoke test sem banco.** Antes do commit da API, o Claude subiu a API e conferiu o que não depende do banco: OpenAPI 3.1, Swagger, JSON malformado → 400 com `traceId`, e validação → 400 com a chave `itens[1].quantidade`.
- **Ajuste de desenho pego antes de rodar.** O `Program.cs` lia a connection string antes do `Build()`, e o `WebApplicationFactory` não conseguiria trocá-la. O registro do `DbContext` passou a ler a configuração quando o contexto é criado.
- **API nova do xUnit 4.** `CollectionBehavior(DisableTestParallelization)` virou erro de compilação. O Claude confirmou a substituta (`Parallelization(Mode = ParallelMode.None)`) lendo o XML de documentação do pacote, em vez de chutar.
- **Ruído do hook em escrita paralela.** Quando vários arquivos interdependentes são escritos de uma vez, o hook do primeiro arquivo compila antes de os outros existirem e acusa erro transitório. O Claude confere com um build completo antes de agir.
- **Commit intermediário quebrado.** No commit dos repositórios entrou o `DependencyInjection.cs`, que já registrava as consultas, só commitadas no commit seguinte. Esse commit isolado não compila. **Não** foi reescrito: o histórico é preservado (e o próprio hook bloquearia o amend).
- **Testes de integração escritos, mas não executados aqui.** A máquina continua sem Docker. Eles compilam e entraram no CI, que roda num runner com Docker.

## 2026-11-12: skills e subagente (M7, parte 1)

- **Skill `novo-endpoint`** escrita a partir do padrão real do `POST /pedidos`, com modelos extraídos do código.
- **Limitação encontrada**: ao tentar invocar a skill logo depois de criá-la, a resposta foi `Unknown skill`. Skills e subagentes são descobertos quando a sessão começa. Em vez de criar os endpoints restantes "à mão" e dizer que foram feitos com a skill, o Claude escreveu também a skill `analisar-query` e o subagente `revisor` e pediu **um** reinício da sessão para usá-los de verdade.

## 2026-11-12: endpoints com a skill e revisão com o subagente (M7, parte 2)

- **`/novo-endpoint` usada de verdade** depois do reinício, três vezes: `GET /pedidos`, `GET /relatorios/top-clientes` e `GET /clientes` + `GET /produtos`. A skill tem o passo "a spec manda", então antes de cada endpoint o Claude conferiu a seção 5 e os CAs da spec. Os commits citam a skill.
- **Correção encontrada pelo roteiro da skill.** O smoke test previsto na verificação mostrou o OpenAPI com os parâmetros em PascalCase (`ClienteId`), diferente do contrato. O binding não diferencia maiúsculas, então só a documentação estava errada. A correção foi um transformer do OpenAPI na API, para não pôr atributos de ASP.NET na Application.
- **Subagente `revisor` antes do push.** Ele revisou o histórico inteiro. **Dois pontos foram conferidos no código e descartados por serem falsos:**
  - "falta teste de integração do CA-04": o teste existe e cobre os dois corpos;
  - "senha no `appsettings.json` base": a connection string está só no `appsettings.Development.json`.

  **Dois pontos foram aceitos** e viraram commits `fix:`:
  - porta do SQL publicada só no loopback;
  - senhas dos logins passadas ao `sqlcmd` por variável de ambiente, e não por `-v`. O comentário do script prometia isso, mas o código não cumpria.

  A sugestão de voltar para `CollectionBehavior(DisableTestParallelization)` foi recusada: ela não compila no xUnit 4. Lição: o relatório de um agente é uma hipótese, não prova. Cada item foi verificado antes de virar código.
