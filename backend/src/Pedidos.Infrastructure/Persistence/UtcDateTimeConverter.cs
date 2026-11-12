using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// O SQL Server não guarda o "Kind" do DateTime. Sem este conversor, a data volta
/// como Unspecified, é serializada sem "Z" e o navegador a interpreta como hora local.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    paraBanco => paraBanco.Kind == DateTimeKind.Local ? paraBanco.ToUniversalTime() : paraBanco,
    doBanco => DateTime.SpecifyKind(doBanco, DateTimeKind.Utc));
