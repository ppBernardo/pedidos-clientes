---
paths:
  - "backend/**"
---

# Regras do backend (.NET 10)

## Endpoints (Pedidos.Api/Endpoints)

- Uma classe estática por recurso, `<Recurso>Endpoints`, com `Map<Recurso>Endpoints(this IEndpointRouteBuilder app)` e `MapGroup("/recurso").WithTags("...")`.
- Handlers são métodos `private static async Task<Results<...>>` com `TypedResults`. Sempre recebem `CancellationToken`.
- Body validado com `.AddEndpointFilter<ValidationFilter<TRequest>>()`; query string com `[AsParameters]` e o mesmo filtro.
- Documente os retornos: `.Produces<T>()`, `.ProducesValidationProblem()`, `.ProducesProblem(422)`.
- Entidade de domínio nunca sai da API: só DTOs (`*Response`).

## Application

- DTOs são `sealed record` em `Application/<Funcionalidade>/`, com sufixos `Request`/`Response`/`Query`.
- Validadores herdam `AbstractValidator<T>`, com mensagens em português. As chaves de erro saem em camelCase (`itens[0].quantidade`).
- Serviços dependem só das portas (`I*Repository`, `I*Consultas`) e de `TimeProvider`.
- Violação de regra de negócio: `throw new RegraNegocioException(CodigosErro.X, "mensagem clara")`.

## Domain

- Entidades com setters privados, construtor privado para o EF e métodos de fábrica (`Pedido.Criar`) que garantem as invariantes.
- Sem atributos do EF e sem referência a pacotes externos.

## Infrastructure (EF Core 10)

- Mapeamento por `IEntityTypeConfiguration<T>` em `Persistence/Configurations`, com nomes de tabela e coluna **iguais aos de `database/02-tabelas.sql`**.
- Dinheiro: `HasPrecision(18, 2)`. Datas: conversor UTC (`Kind = Utc` na leitura).
- Leituras: `AsNoTracking()` + `Select` direto para o DTO. Nada de `ToList()` antes de filtrar/paginar, e nada de lazy loading.
- Atualização em massa ou condicional: `ExecuteUpdateAsync`. A baixa de estoque é condicional e em ordem crescente de `ProdutoId`.
- Transação manual só em `PedidoRepository.AdicionarComBaixaDeEstoqueAsync`. Sem `EnableRetryOnFailure`.
- SQL crítica (relatório) via `Database.SqlQuery<T>($"...")`, interpolada e portanto parametrizada. Nunca concatene strings.

## Geral

- Versões de pacote só em `Directory.Packages.props`. Warnings são erros em `src/`.
- Log estruturado: `logger.LogWarning("Estoque insuficiente para {ProdutoId}", id)`, sem interpolação.
- Configuração: `ConnectionStrings:Pedidos`. Nenhum segredo real em `appsettings*.json`.
