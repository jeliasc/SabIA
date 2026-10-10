namespace Proyecto_Final.Servicios.Auditoria;

public static class ZonaHorariaAuditoria
{
    public const string IdIana = "America/Guatemala";
    private const string IdWindows = "Central America Standard Time";

    private static readonly Lazy<TimeZoneInfo> ZonaGuatemala = new(ObtenerZonaGuatemala);

    public static DateTime ConvertirUtcAGuatemala(DateTime fechaUtc)
    {
        var utc = fechaUtc.Kind switch
        {
            DateTimeKind.Utc => fechaUtc,
            DateTimeKind.Local => fechaUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaGuatemala.Value);
    }

    public static DateTime InicioDiaGuatemalaEnUtc(DateOnly fecha) =>
        ConvertirHoraGuatemalaAUtc(fecha.ToDateTime(TimeOnly.MinValue));

    public static DateTime FinExclusivoDiaGuatemalaEnUtc(DateOnly fecha) =>
        ConvertirHoraGuatemalaAUtc(fecha.AddDays(1).ToDateTime(TimeOnly.MinValue));

    private static DateTime ConvertirHoraGuatemalaAUtc(DateTime fechaGuatemala) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(fechaGuatemala, DateTimeKind.Unspecified),
            ZonaGuatemala.Value);

    private static TimeZoneInfo ObtenerZonaGuatemala()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(IdIana, out var zonaIana))
            return zonaIana;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(IdWindows, out var zonaWindows))
            return zonaWindows;

        throw new TimeZoneNotFoundException(
            $"No se encontró la zona horaria {IdIana} ni su equivalente de Windows {IdWindows}.");
    }
}
