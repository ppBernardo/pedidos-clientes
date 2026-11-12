using Pedidos.Domain.Comum;

namespace Pedidos.UnitTests.Domain;

public sealed class PeriodoTests
{
    private static readonly Periodo Ano2026 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    private static bool Contem(Periodo periodo, DateTime instanteUtc) =>
        instanteUtc >= periodo.InicioUtc && instanteUtc < periodo.FimExclusivoUtc;

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

    [Theory]
    [Trait("CA", "CA-11")]
    [InlineData("2026-01-01T00:00:00", true)]
    [InlineData("2026-12-31T23:59:59", true)]
    [InlineData("2025-12-31T23:59:59", false)]
    [InlineData("2027-01-01T00:00:00", false)]
    public void Contem_IncluiOUltimoSegundoDoUltimoDiaEExcluiOsVizinhos(string instante, bool esperado)
    {
        var instanteUtc = DateTime.SpecifyKind(DateTime.Parse(instante, System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);

        Contem(Ano2026, instanteUtc).Should().Be(esperado);
    }

    [Fact]
    public void SemDatas_NaoLimitaNenhumaPonta()
    {
        var periodo = new Periodo(null, null);

        periodo.InicioUtc.Should().BeNull();
        periodo.FimExclusivoUtc.Should().BeNull();
    }
}
