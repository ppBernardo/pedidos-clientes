---
paths:
  - "frontend/**"
---

# Regras do frontend (React 19 + TypeScript 7)

- Componentes funcionais, um por arquivo `PascalCase.tsx`, com export nomeado. Hooks `useAlgo` ficam junto de quem usa ou em `src/hooks/`.
- **HTTP só via `src/api/client.ts`** (base `/api`). Componente nunca chama `fetch` direto. Erros chegam como `ApiError`, com o ProblemDetails tipado.
- **Tipos da API** em `src/api/types.ts`, espelhando a seção 5 de `docs/spec.md`. Mudou o contrato? Atualize os dois juntos.
- Dados com TanStack Query: chaves centralizadas em `src/api/queryKeys.ts`, e toda mutation invalida as queries afetadas.
- `strict` ligado e sem `any` (use `unknown` + narrowing).
- Textos de tela em português. Moeda com `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`. A API devolve datas em UTC (ISO com `Z`), e a tela mostra no fuso do navegador.
- Acessibilidade: todo campo tem `<label>`, modal é `<dialog>`, botões têm `type`, e erros são anunciados com `role="alert"`.
- Estilo: CSS próprio em `src/styles.css` com variáveis CSS, sem biblioteca de UI.
- Verificação: `npm run lint` (oxlint) e `npm run build` (inclui `tsc -b`). O hook pós-edição roda o typecheck sozinho.
