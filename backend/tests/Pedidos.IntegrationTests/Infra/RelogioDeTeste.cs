namespace Pedidos.IntegrationTests.Infra;

/// <summary>
/// Relógio parado e ajustável a qualquer momento, inclusive para trás. O
/// FakeTimeProvider proíbe voltar no tempo, e aqui cada teste precisa partir do
/// mesmo "agora", mesmo que um teste anterior tenha adiantado o relógio.
/// </summary>
public sealed class RelogioDeTeste(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora;
}
