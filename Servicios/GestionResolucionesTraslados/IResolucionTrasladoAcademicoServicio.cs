using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.ResolucionesTraslados;

namespace Proyecto_Final.Servicios.GestionResolucionesTraslados;

public interface IResolucionTrasladoAcademicoServicio
{
    Task<ResolucionesTrasladosIndex> ObtenerIndexAsync(bool puedeGestionar, bool puedeAprobar);
    Task<CrearResolucionTraslado?> PrepararCreacionAsync(int inscripcionId, int cursoId, int periodoId);
    Task<ResultadoOperacion<int>> CrearAsync(CrearResolucionTraslado modelo);
    Task<DetalleResolucionTraslado?> ObtenerDetalleAsync(int id);
    Task<ResultadoConsultaDetalleResolucionTraslado> ObtenerDetalleParaNavegacionAsync(int id);
    Task<ResultadoOperacion> DecidirAsync(DecidirResolucionTraslado modelo);
}

public enum EstadoConsultaDetalleResolucionTraslado
{
    NoDisponible,
    Disponible,
    FueraDeCicloAutorizado
}

public sealed class ResultadoConsultaDetalleResolucionTraslado
{
    private ResultadoConsultaDetalleResolucionTraslado(
        EstadoConsultaDetalleResolucionTraslado estado,
        DetalleResolucionTraslado? detalle = null)
    {
        Estado = estado;
        Detalle = detalle;
    }

    public EstadoConsultaDetalleResolucionTraslado Estado { get; }

    public DetalleResolucionTraslado? Detalle { get; }

    public static ResultadoConsultaDetalleResolucionTraslado NoDisponible() =>
        new(EstadoConsultaDetalleResolucionTraslado.NoDisponible);

    public static ResultadoConsultaDetalleResolucionTraslado Disponible(
        DetalleResolucionTraslado detalle) =>
        new(EstadoConsultaDetalleResolucionTraslado.Disponible, detalle);

    public static ResultadoConsultaDetalleResolucionTraslado FueraDeCicloAutorizado() =>
        new(EstadoConsultaDetalleResolucionTraslado.FueraDeCicloAutorizado);
}
