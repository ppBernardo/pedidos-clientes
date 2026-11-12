namespace Pedidos.Domain.Produtos;

/// <summary>
/// Produto do catálogo. A baixa de estoque não acontece em memória: é um UPDATE
/// condicional no banco (RN-09), para ser segura com requisições concorrentes.
/// </summary>
public sealed class Produto
{
    private Produto()
    {
    }

    /// <summary>Usado pela carga e pelos testes: o cadastro de produtos está fora do escopo.</summary>
    public Produto(int id, string nome, decimal preco, int estoque, bool ativo = true)
    {
        Id = id;
        Nome = nome;
        Preco = preco;
        Estoque = estoque;
        Ativo = ativo;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public int Estoque { get; private set; }
    public bool Ativo { get; private set; }
}
