using FluentValidation;
using Pedidos.Application.Comum;
using Pedidos.Domain.Comum;

namespace Pedidos.Application.Relatorios;

/// <summary>Parâmetros de <c>GET /relatorios/top-clientes</c> (spec 5.4). O período é obrigatório (RN-14).</summary>
public sealed record TopClientesQuery(DateOnly? DataInicio = null, DateOnly? DataFim = null)
{
    /// <summary>Quantidade de clientes no ranking.</summary>
    public const int Limite = 5;

    public Periodo Periodo => new(DataInicio, DataFim);
}

public sealed class TopClientesValidator : AbstractValidator<TopClientesQuery>
{
    public TopClientesValidator()
    {
        RuleFor(q => q.DataInicio)
            .NotNull().WithMessage("Informe a data inicial do período.")
            .DentroDaFaixaAceita()
            .NaoDepoisDe(q => q.DataFim);

        RuleFor(q => q.DataFim)
            .NotNull().WithMessage("Informe a data final do período.")
            .DentroDaFaixaAceita();
    }
}

public sealed record TopClienteResponse(int Posicao, int ClienteId, string ClienteNome, int QuantidadePedidos, decimal ValorTotal);
