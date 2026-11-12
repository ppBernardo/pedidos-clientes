using FluentValidation;
using Pedidos.Application.Comum;
using Pedidos.Domain.Comum;

namespace Pedidos.Application.Pedidos;

/// <summary>Parâmetros de <c>GET /pedidos</c> (spec 5.3). Todos os filtros são opcionais (RN-12).</summary>
public sealed record ListarPedidosQuery(
    int? ClienteId = null,
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null,
    int Pagina = 1,
    int TamanhoPagina = 10)
{
    public const int MaiorPagina = 1_000_000;
    public const int MaiorTamanhoPagina = 100;

    public Periodo Periodo => new(DataInicio, DataFim);
}

public sealed class ListarPedidosValidator : AbstractValidator<ListarPedidosQuery>
{
    public ListarPedidosValidator()
    {
        RuleFor(q => q.ClienteId)
            .GreaterThan(0).When(q => q.ClienteId.HasValue).WithMessage("O cliente informado é inválido.");

        RuleFor(q => q.DataInicio).DentroDaFaixaAceita().NaoDepoisDe(q => q.DataFim);
        RuleFor(q => q.DataFim).DentroDaFaixaAceita();

        RuleFor(q => q.Pagina)
            .InclusiveBetween(1, ListarPedidosQuery.MaiorPagina)
            .WithMessage("A página deve estar entre 1 e 1.000.000.");

        RuleFor(q => q.TamanhoPagina)
            .InclusiveBetween(1, ListarPedidosQuery.MaiorTamanhoPagina)
            .WithMessage("O tamanho da página deve estar entre 1 e 100.");
    }
}
