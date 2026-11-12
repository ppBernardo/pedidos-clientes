namespace Pedidos.Application.Comum;

/// <summary>Página de resultados (spec 5.3). Página além da última vem vazia, com os totais corretos.</summary>
public sealed record PaginaResponse<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TotalItens == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
