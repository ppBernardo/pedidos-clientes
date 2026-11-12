using FluentValidation;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Pedidos;

/// <summary>Validação de formato do pedido (RN-01, 02, 06 e 07). Falhas viram 400.</summary>
public sealed class CriarPedidoValidator : AbstractValidator<CriarPedidoRequest>
{
    public CriarPedidoValidator()
    {
        RuleFor(p => p.ClienteId)
            .GreaterThan(0).WithMessage("Informe o cliente.");

        RuleFor(p => p.Itens)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O pedido deve ter ao menos um item.")
            .Must(itens => itens!.Count <= Pedido.MaximoDeItens)
            .WithMessage($"O pedido aceita no máximo {Pedido.MaximoDeItens} itens.")
            .Must(NaoRepetirProduto)
            .WithMessage("Cada produto deve aparecer uma única vez; para mais unidades, aumente a quantidade.");

        RuleForEach(p => p.Itens)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Item inválido.")
            .SetValidator(new ItemPedidoRequestValidator());
    }

    private static bool NaoRepetirProduto(IReadOnlyList<ItemPedidoRequest>? itens) =>
        itens is null || itens.Where(i => i is not null).GroupBy(i => i.ProdutoId).All(g => g.Count() == 1);
}

public sealed class ItemPedidoRequestValidator : AbstractValidator<ItemPedidoRequest>
{
    public ItemPedidoRequestValidator()
    {
        RuleFor(i => i.ProdutoId)
            .GreaterThan(0).WithMessage("Informe o produto.");

        RuleFor(i => i.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}
