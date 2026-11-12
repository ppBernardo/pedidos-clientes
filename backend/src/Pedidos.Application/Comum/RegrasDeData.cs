using System.Globalization;
using FluentValidation;
using Pedidos.Domain.Comum;

namespace Pedidos.Application.Comum;

internal static class RegrasDeData
{
    private static readonly string MensagemFaixa = string.Create(
        CultureInfo.InvariantCulture,
        $"A data deve estar entre {Periodo.MenorDataAceita:yyyy-MM-dd} e {Periodo.MaiorDataAceita:yyyy-MM-dd}.");

    /// <summary>RN-11: datas fora de 2000–2100 são rejeitadas (evita estouro no "+1 dia").</summary>
    public static IRuleBuilderOptions<T, DateOnly?> DentroDaFaixaAceita<T>(this IRuleBuilder<T, DateOnly?> regra) =>
        regra.Must(data => data is null || (data >= Periodo.MenorDataAceita && data <= Periodo.MaiorDataAceita))
            .WithMessage(MensagemFaixa);

    /// <summary>RN-12/RN-14: a data inicial não pode ser depois da final.</summary>
    public static IRuleBuilderOptions<T, DateOnly?> NaoDepoisDe<T>(this IRuleBuilder<T, DateOnly?> regra, Func<T, DateOnly?> fim) =>
        regra.Must((objeto, inicio) => inicio is null || fim(objeto) is not { } dataFim || inicio <= dataFim)
            .WithMessage("A data inicial não pode ser maior que a data final.");
}
