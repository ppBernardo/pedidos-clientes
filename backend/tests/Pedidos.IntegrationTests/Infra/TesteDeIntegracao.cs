namespace Pedidos.IntegrationTests.Infra;

/// <summary>Base dos testes de integração: limpa o banco e volta o relógio antes de cada teste.</summary>
public abstract class TesteDeIntegracao(AmbienteDeTeste ambiente) : IAsyncLifetime
{
    protected AmbienteDeTeste Ambiente { get; } = ambiente;
    protected BancoDeTeste Banco => Ambiente.Banco;
    protected HttpClient Api { get; private set; } = null!;
    protected static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        await Banco.LimparAsync();
        Ambiente.Api.Relogio.SetUtcNow(PedidosApiFactory.Agora);
        Api = Ambiente.Api.CreateClient();
    }

    public virtual ValueTask DisposeAsync()
    {
        Api.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
