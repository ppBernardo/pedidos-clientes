namespace Pedidos.Domain.Comum;

/// <summary>
/// Violação de regra de negócio. A API devolve 422 com <see cref="Codigo"/>, que é
/// estável e serve para o cliente da API tratar o erro sem interpretar a mensagem.
/// </summary>
public sealed class RegraNegocioException(string codigo, string mensagem) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
}
