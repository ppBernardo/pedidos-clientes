namespace Pedidos.Domain.Comum;

/// <summary>
/// Período com datas inclusivas nas duas pontas (RN-11), convertido para o
/// intervalo semiaberto em UTC usado nas consultas: <c>[InicioUtc, FimExclusivoUtc)</c>.
/// Assim um pedido às 23:59:59 do último dia entra, e a consulta compara a coluna
/// direto, sem função sobre ela (predicado sargable). As pontas são opcionais na
/// listagem e obrigatórias no relatório (validado na aplicação).
/// </summary>
public sealed record Periodo(DateOnly? Inicio, DateOnly? Fim)
{
    /// <summary>Faixa aceita: evita estouro no "+1 dia" (9999-12-31) e datas sem sentido.</summary>
    public static readonly DateOnly MenorDataAceita = new(2000, 1, 1);

    public static readonly DateOnly MaiorDataAceita = new(2100, 12, 31);

    public DateTime? InicioUtc => Inicio?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public DateTime? FimExclusivoUtc => Fim?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
