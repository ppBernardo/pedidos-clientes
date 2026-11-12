using FluentValidation.TestHelper;
using Pedidos.Application.Pedidos;

namespace Pedidos.UnitTests.Application;

public sealed class CriarPedidoValidatorTests
{
    private readonly CriarPedidoValidator _validador = new();

    private static CriarPedidoRequest Pedido(params ItemPedidoRequest[] itens) => new(ClienteId: 1, itens);

    [Fact]
    public void Validar_PedidoCorreto_NaoTemErros()
    {
        var resultado = _validador.TestValidate(Pedido(new(1, 2), new(2, 1)));

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validar_SemCliente_TemErroEmClienteId()
    {
        var resultado = _validador.TestValidate(new CriarPedidoRequest(0, [new(1, 1)]));

        resultado.ShouldHaveValidationErrorFor(p => p.ClienteId);
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("RN", "RN-01")]
    public void Validar_ItensAusentes_TemErroEmItens()
    {
        var resultado = _validador.TestValidate(new CriarPedidoRequest(1, null));

        resultado.ShouldHaveValidationErrorFor(p => p.Itens)
            .WithErrorMessage("O pedido deve ter ao menos um item.");
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("RN", "RN-01")]
    public void Validar_ItensVazios_TemErroEmItens()
    {
        var resultado = _validador.TestValidate(Pedido());

        resultado.ShouldHaveValidationErrorFor(p => p.Itens);
    }

    [Fact]
    [Trait("RN", "RN-02")]
    public void Validar_MaisDe100Itens_TemErroEmItens()
    {
        var itens = Enumerable.Range(1, 101).Select(id => new ItemPedidoRequest(id, 1)).ToArray();

        var resultado = _validador.TestValidate(Pedido(itens));

        resultado.ShouldHaveValidationErrorFor(p => p.Itens)
            .WithErrorMessage("O pedido aceita no máximo 100 itens.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [Trait("CA", "CA-05")]
    [Trait("RN", "RN-06")]
    public void Validar_QuantidadeInvalida_ApontaOCampoDoItem(int quantidade)
    {
        var resultado = _validador.TestValidate(Pedido(new(1, 1), new(2, quantidade)));

        resultado.ShouldHaveValidationErrorFor("Itens[1].Quantidade")
            .WithErrorMessage("A quantidade deve ser maior que zero.");
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("RN", "RN-07")]
    public void Validar_ProdutoRepetido_TemErroEmItens()
    {
        var resultado = _validador.TestValidate(Pedido(new(1, 1), new(1, 2)));

        resultado.ShouldHaveValidationErrorFor(p => p.Itens)
            .WithErrorMessage("Cada produto deve aparecer uma única vez; para mais unidades, aumente a quantidade.");
    }

    [Fact]
    public void Validar_ItemNulo_TemErroSemExcecao()
    {
        var resultado = _validador.TestValidate(new CriarPedidoRequest(1, [null!]));

        resultado.ShouldHaveValidationErrorFor("Itens[0]");
    }

    [Fact]
    public void Validar_ProdutoNaoInformado_ApontaOCampoDoItem()
    {
        var resultado = _validador.TestValidate(Pedido(new ItemPedidoRequest(0, 1)));

        resultado.ShouldHaveValidationErrorFor("Itens[0].ProdutoId");
    }
}
