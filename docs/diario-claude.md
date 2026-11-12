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
