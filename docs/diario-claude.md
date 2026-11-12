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
