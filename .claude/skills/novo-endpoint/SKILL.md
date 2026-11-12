---
name: novo-endpoint
description: Cria ou altera um endpoint HTTP da API de pedidos seguindo o padrão do projeto (spec → contrato e validador → porta → implementação → endpoint → testes unitários e de integração → commits pequenos). Use sempre que for adicionar ou mudar uma rota em backend/src/Pedidos.Api.
argument-hint: "<MÉTODO> <rota> <o que o endpoint faz>"
---

# Novo endpoint: $ARGUMENTS

Siga o padrão já usado por `POST /pedidos` e `GET /pedidos/{id}`. Os trechos de referência estão em [modelos.md](modelos.md).

## 1. Antes de codar: a spec manda

1. Leia `docs/spec.md`: regras (seção 4), contrato da API (seção 5) e critérios de aceite (seção 8).
2. Se a rota **não estiver** na seção 5, ou se o contrato mudar, **pare**. Proponha antes o ajuste na spec (JSON de entrada e saída, status codes e RN/CA novas) e espere a confirmação.
3. Classifique o endpoint:
   - **Escrita** passa pelo domínio: serviço em `Application` → entidade/regra em `Domain` → `I*Repository`.
   - **Leitura** vai direto do banco para o DTO: porta `I*Consultas` → implementação com `AsNoTracking` + projeção.

## 2. Passos (um commit pequeno por passo lógico)

| # | Onde | O quê |
|---|---|---|
| 1 | `backend/src/Pedidos.Application/<Funcionalidade>/` | Request/Query e Response como `sealed record`, mais o validador `<Nome>Validator : AbstractValidator<T>` na mesma pasta (o registro é automático). Query string: valores padrão no construtor do record. |
| 2 | `Application/Abstractions/` | Porta: método numa interface `I*Consultas` (leitura) ou `I*Repository` + serviço (escrita). |
| 3 | `backend/src/Pedidos.Infrastructure/Consultas/` ou `Repositories/` | Implementação `internal sealed`, registrada em `Infrastructure/DependencyInjection.cs`. SQL crítica via `db.Database.SqlQuery<T>($"...")`, interpolado e portanto parametrizado. |
| 4 | `backend/src/Pedidos.Api/Endpoints/<Recurso>Endpoints.cs` | `MapGroup` + `WithTags`, handler `private static async` com `TypedResults`, `.AddEndpointFilter<ValidationFilter<T>>()`, `.WithName/.WithSummary/.Produces...`. Registre o `Map<Recurso>Endpoints()` em `Program.cs`. |
| 5 | `backend/tests/Pedidos.UnitTests/Application/` | Testes do validador: uma violação e um sucesso por regra, com `[Trait("CA", "CA-xx")]`. |
| 6 | `backend/tests/Pedidos.IntegrationTests/<Funcionalidade>/` | Testes de integração herdando `TesteDeIntegracao`. Dados criados com `BancoDeTeste` e **nunca** dependentes da carga de exemplo. Cubra o sucesso, o 400 e, quando houver, o 404/422. |
| 7 | `docs/spec.md` e `frontend/src/api/types.ts` | Mantenha spec, contrato e tipos do frontend em sincronia. |

## 3. Verificação

```bash
dotnet build backend/Pedidos.slnx
dotnet test --solution backend/Pedidos.slnx
```

O build precisa terminar com 0 warnings (warnings são erros). O hook pós-edição já compila a cada arquivo, então corrija o que ele apontar antes de seguir.

## 4. Checklist antes do commit

- [ ] Rota no plural e minúscula; JSON em camelCase; datas em UTC com `Z`.
- [ ] Erros só em ProblemDetails: 400 pelo `ValidationFilter`, 422 por `RegraNegocioException` e 404 por `TypedResults.NotFound()`. Nunca `Results.BadRequest("texto")`.
- [ ] Nenhuma entidade de domínio na resposta.
- [ ] Período via `Periodo` (intervalo semiaberto em UTC), sem função sobre coluna indexada.
- [ ] Sem MediatR, AutoMapper, repositório genérico ou `EnableRetryOnFailure`.
- [ ] Commits `feat(api): ...` e `test(...): ...`, citando no corpo que o endpoint foi gerado com a skill `novo-endpoint`.
