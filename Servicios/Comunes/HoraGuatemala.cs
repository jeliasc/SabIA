namespace Proyecto_Final.Servicios.Comunes;

// Conversión entre la hora que escriben los usuarios en los formularios
// (hora de Guatemala, sin zona) y la hora UTC con la que se guardan las
// fechas en PostgreSQL ("timestamp with time zone" solo acepta UTC).
// No depende de la zona horaria configurada en el servidor.
public static class HoraGuatemala
{
    private const string IdIana = "America/Guatemala";
    private const string IdWindows = "Central America Standard Time";

    private static readonly Lazy<TimeZoneInfo> Zona = new(ObtenerZona);

    // Fecha escrita en un formulario -> UTC para guardar.
    public static DateTime? ParaGuardar(DateTime? valor) =>
        valor.HasValue ? ParaGuardar(valor.Value) : null;

    public static DateTime ParaGuardar(DateTime valor) =>
        valor.Kind switch
        {
            DateTimeKind.Utc => valor,
            DateTimeKind.Local => valor.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(valor, Zona.Value)
        };

    // Fecha guardada en UTC -> hora de Guatemala para mostrarla en un
    // campo datetime-local. Se recorta a minutos para que el campo no
    // muestre segundos ni milisegundos.
    public static DateTime? ParaFormulario(DateTime? valor) =>
        valor.HasValue ? ParaFormulario(valor.Value) : null;

    public static DateTime ParaFormulario(DateTime valor)
    {
        var utc = valor.Kind switch
        {
            DateTimeKind.Utc => valor,
            DateTimeKind.Local => valor.ToUniversalTime(),
            _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
        };

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Zona.Value);
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, DateTimeKind.Unspecified);
    }

    // Hora actual de Guatemala para valores predeterminados de formularios.
    public static DateTime Ahora() => ParaFormulario(DateTime.UtcNow);

    private static TimeZoneInfo ObtenerZona()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(IdIana, out var zonaIana))
            return zonaIana;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(IdWindows, out var zonaWindows))
            return zonaWindows;

        // Guatemala no aplica horario de verano: UTC-6 fijo como respaldo.
        return TimeZoneInfo.CreateCustomTimeZone(IdIana, TimeSpan.FromHours(-6), "Guatemala", "Guatemala");
    }
}
