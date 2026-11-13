using Pedidos.Domain.Comum;

namespace Pedidos.UnitTests.Domain;

public sealed class PeriodoTests
{
    private static readonly Periodo Ano2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    [Fact]
    [Trait("RN", "RN-11")]
    public void InicioUtc_EhAMeiaNoiteUtcDoPrimeiroDia()
    {
        Ano2026.InicioUtc.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    [Trait("RN", "RN-11")]
    public void FimExclusivoUtc_EhAMeiaNoiteDoDiaSeguinte()
    {
        Ano2026.FimExclusivoUtc.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // As consultas filtram com DataPedido < FimExclusivoUtc. Com o fim na meia-noite
    // do dia seguinte, 23:59:59 do último dia entra e 00:00 do dia seguinte fica fora.
    // O predicado real é coberto na integração (Get_FiltrandoPorPeriodo_*).
    [Theory]
    [Trait("CA", "CA-11")]
    [Trait("RN", "RN-11")]
    [InlineData(2026, 2, 28, 2026, 3, 1)]
    [InlineData(2028, 2, 29, 2028, 3, 1)]
    [InlineData(2026, 4, 30, 2026, 5, 1)]
    [InlineData(2100, 12, 31, 2101, 1, 1)]
    public void FimExclusivoUtc_NoFimDeMesOuDeAno_EhAMeiaNoiteUtcDoDiaSeguinte(
        int ano, int mes, int dia, int anoSeguinte, int mesSeguinte, int diaSeguinte)
    {
        var fim = new Periodo(null, new DateOnly(ano, mes, dia)).FimExclusivoUtc!.Value;

        fim.Should().Be(new DateTime(anoSeguinte, mesSeguinte, diaSeguinte, 0, 0, 0, DateTimeKind.Utc));
        fim.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SemDatas_NaoLimitaNenhumaPonta()
    {
        var periodo = new Periodo(null, null);

        periodo.InicioUtc.Should().BeNull();
        periodo.FimExclusivoUtc.Should().BeNull();
    }
}
