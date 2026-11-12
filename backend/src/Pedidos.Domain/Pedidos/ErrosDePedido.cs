using System.Globalization;
using Pedidos.Domain.Comum;

namespace Pedidos.Domain.Pedidos;

/// <summary>
/// Erros de negócio da criação de pedido, num lugar só: o domínio (que valida
/// pelo snapshot) e o repositório (que detecta a corrida na baixa de estoque)
/// produzem a mesma mensagem.
/// </summary>
public static class ErrosDePedido
{
    public static RegraNegocioException PedidoInvalido(string mensagem) =>
        new(CodigosErro.PedidoInvalido, mensagem);

    public static RegraNegocioException ClienteNaoEncontrado(int clienteId) =>
        new(CodigosErro.ClienteNaoEncontrado, $"Cliente {clienteId} não encontrado.");

    public static RegraNegocioException ProdutosNaoEncontrados(IEnumerable<int> produtoIds) =>
        new(CodigosErro.ProdutoNaoEncontrado, $"Produto(s) não encontrado(s): {string.Join(", ", produtoIds)}.");

    public static RegraNegocioException ProdutosInativos(IEnumerable<string> nomes) =>
        new(CodigosErro.ProdutoInativo, $"Produto(s) inativo(s), não podem ser vendidos: {Listar(nomes)}.");

    public static RegraNegocioException EstoqueInsuficiente(IEnumerable<EstoqueFaltante> faltas) =>
        new(
            CodigosErro.EstoqueInsuficiente,
            "Estoque insuficiente para " + string.Join("; ", faltas.Select(f =>
                string.Create(CultureInfo.InvariantCulture, $"'{f.Nome}' (solicitado: {f.Solicitado}, disponível: {f.Disponivel})")))
            + ".");

    private static string Listar(IEnumerable<string> nomes) => string.Join(", ", nomes.Select(n => $"'{n}'"));
}

public sealed record EstoqueFaltante(string Nome, int Solicitado, int Disponivel);
