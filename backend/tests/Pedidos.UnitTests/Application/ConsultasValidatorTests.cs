using FluentValidation.TestHelper;
using Pedidos.Application.Comum;
using Pedidos.Application.Pedidos;
using Pedidos.Application.Relatorios;

namespace Pedidos.UnitTests.Application;

public sealed class ListarPedidosValidatorTests
{
    private readonly ListarPedidosValidator _validador = new();

    [Fact]
    public void Validar_SemFiltros_UsaPadroesValidos()
    {
        var query = new ListarPedidosQuery();

        _validador.TestValidate(query).ShouldNotHaveAnyValidationErrors();
        query.Pagina.Should().Be(1);
        query.TamanhoPagina.Should().Be(10);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("RN", "RN-12")]
    public void Validar_DataInicialDepoisDaFinal_TemErroNaDataInicial()
    {
        var query = new ListarPedidosQuery(DataInicio: new DateOnly(2026, 3, 1), DataFim: new DateOnly(2026, 2, 1));

        _validador.TestValidate(query).ShouldHaveValidationErrorFor(q => q.DataInicio)
            .WithErrorMessage("A data inicial não pode ser maior que a data final.");
    }

    [Fact]
    public void Validar_MesmaDataNoInicioENoFim_EhValido()
    {
        var dia = new DateOnly(2026, 2, 1);

        _validador.TestValidate(new ListarPedidosQuery(DataInicio: dia, DataFim: dia)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("CA", "CA-12")]
    [Trait("RN", "RN-13")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Validar_PaginaForaDaFaixa_TemErro(int pagina)
    {
        _validador.TestValidate(new ListarPedidosQuery(Pagina: pagina)).ShouldHaveValidationErrorFor(q => q.Pagina);
    }

    [Theory]
    [Trait("CA", "CA-12")]
    [Trait("RN", "RN-13")]
    [InlineData(0)]
    [InlineData(101)]
    public void Validar_TamanhoDePaginaForaDaFaixa_TemErro(int tamanho)
    {
        _validador.TestValidate(new ListarPedidosQuery(TamanhoPagina: tamanho)).ShouldHaveValidationErrorFor(q => q.TamanhoPagina);
    }

    [Theory]
    [Trait("RN", "RN-11")]
    [InlineData("1999-12-31")]
    [InlineData("2101-01-01")]
    [InlineData("9999-12-31")]
    public void Validar_DataForaDaFaixaAceita_TemErro(string data)
    {
        var query = new ListarPedidosQuery(DataFim: DateOnly.Parse(data, System.Globalization.CultureInfo.InvariantCulture));

        _validador.TestValidate(query).ShouldHaveValidationErrorFor(q => q.DataFim);
    }

    [Fact]
    public void Validar_ClienteInvalido_TemErro()
    {
        _validador.TestValidate(new ListarPedidosQuery(ClienteId: 0)).ShouldHaveValidationErrorFor(q => q.ClienteId);
    }
}

public sealed class TopClientesValidatorTests
{
    private readonly TopClientesValidator _validador = new();

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("RN", "RN-14")]
    public void Validar_SemPeriodo_TemErroNasDuasDatas()
    {
        var resultado = _validador.TestValidate(new TopClientesQuery());

        resultado.ShouldHaveValidationErrorFor(q => q.DataInicio);
        resultado.ShouldHaveValidationErrorFor(q => q.DataFim);
    }

    [Fact]
    [Trait("RN", "RN-14")]
    public void Validar_DataInicialDepoisDaFinal_TemErro()
    {
        var query = new TopClientesQuery(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1));

        _validador.TestValidate(query).ShouldHaveValidationErrorFor(q => q.DataInicio);
    }

    [Fact]
    public void Validar_PeriodoCompleto_EhValido()
    {
        var query = new TopClientesQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        _validador.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class PaginaResponseTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(57, 10, 6)]
    public void TotalPaginas_ArredondaParaCima(int totalItens, int tamanho, int esperado)
    {
        new PaginaResponse<int>([], 1, tamanho, totalItens).TotalPaginas.Should().Be(esperado);
    }
}
