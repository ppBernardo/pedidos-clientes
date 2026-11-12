namespace Pedidos.Domain.Clientes;

/// <summary>Cliente. Fora do escopo de cadastro: os dados vêm da carga de exemplo.</summary>
public sealed class Cliente
{
    private Cliente()
    {
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime DataCadastro { get; private set; }
}
