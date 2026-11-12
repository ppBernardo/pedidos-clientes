---
paths:
  - "backend/tests/**"
---

# Regras de testes

- xUnit v3 + AwesomeAssertions (`resultado.Should().Be(...)`). Dublês com NSubstitute, só nos testes unitários.
- Nome: `Metodo_Cenario_Resultado` em português. O critério coberto vai em `[Trait("CA", "CA-xx")]` (e `[Trait("RN", "RN-xx")]` quando fizer sentido).
- Estrutura Arrange / Act / Assert, com um comportamento por teste.

## Unitários (`Pedidos.UnitTests`)

- Só Domain e Application: sem banco, sem HTTP e sem relógio real (use `FakeTimeProvider`).
- Cada regra RN-xx tem pelo menos um teste de violação e um de sucesso.

## Integração (`Pedidos.IntegrationTests`)

- SQL Server real via Testcontainers (mesma imagem do compose), inicializado pelo **mesmo `database/init-db.sh`**.
- Um container para o assembly inteiro. Os testes rodam em série e o banco é limpo antes de cada teste (`BancoDeTeste.LimparAsync`).
- A API sobe com o login de menor privilégio `pedidos_app`: uma permissão faltando precisa quebrar um teste.
- Cada teste cria seus dados com os helpers de `BancoDeTeste`. **Nunca dependa da carga de exemplo.**
- Verifique o estado do banco quando importar: estoque baixado, nada gravado após falha.
- Concorrência com `Task.WhenAll`, sem `Task.Delay`/sleeps. O teste precisa ser repetível.
