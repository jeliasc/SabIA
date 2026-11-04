using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Calificaciones;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

public interface ICalificacionServicio
{
    Task<CalificacionesIndex> ObtenerIndexAsync(bool puedeConfigurar);
    Task<CalificacionesCursoContextual?> ObtenerContextoCursoAsync(int asignacionId);
    Task<ControlTareas?> ObtenerControlTareasAsync(int asignacionId, int? periodoId = null);
    Task<ConfigurarEvaluacion?> PrepararConfiguracionAsync(
        int asignacionId,
        int periodoId,
        int? plantillaId = null);
    Task PrepararOpcionesAsync(ConfigurarEvaluacion modelo);
    Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(ConfigurarEvaluacion modelo);
    Task<LibroCalificaciones?> ObtenerLibroAsync(
        int configuracionId,
        bool permisoRegistrar,
        bool permisoCerrar,
        bool usarResultadosCerrados = true);
    Task<ResultadoConsultaLibroCalificaciones> ObtenerLibroParaNavegacionAsync(
        int configuracionId,
        bool permisoRegistrar,
        bool permisoCerrar);
    Task<ResultadoOperacion> GuardarLibroAsync(GuardarLibroCalificaciones modelo);
    Task<ResultadoOperacion> CerrarAsync(int configuracionId);
    Task<ResumenAbacus?> ObtenerResumenAbacusAsync(int configuracionId);
    Task<ResultadoConsultaResumenAbacus> ObtenerResumenAbacusParaNavegacionAsync(
        int configuracionId);
    Task<CorreccionesCalificaciones> ObtenerCorreccionesAsync(bool puedeRevisar);
    Task<ResultadoOperacion> SolicitarCorreccionAsync(SolicitarCorreccionCalificacion modelo);
    Task<ResultadoOperacion> RevisarCorreccionAsync(RevisarCorreccionCalificacion modelo);
    Task<ResultadoOperacion> AplicarCorreccionAsync(AplicarCorreccionCalificacion modelo);
}

public enum EstadoConsultaLibroCalificaciones
{
    Disponible,
    FueraDeCicloAutorizado,
    NoDisponible
}

public sealed class ResultadoConsultaLibroCalificaciones
{
    private ResultadoConsultaLibroCalificaciones(
        EstadoConsultaLibroCalificaciones estado,
        LibroCalificaciones? libro = null)
    {
        Estado = estado;
        Libro = libro;
    }

    public EstadoConsultaLibroCalificaciones Estado { get; }

    public LibroCalificaciones? Libro { get; }

    public static ResultadoConsultaLibroCalificaciones Disponible(LibroCalificaciones libro) =>
        new(EstadoConsultaLibroCalificaciones.Disponible, libro);

    public static ResultadoConsultaLibroCalificaciones FueraDeCicloAutorizado() =>
        new(EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado);

    public static ResultadoConsultaLibroCalificaciones NoDisponible() =>
        new(EstadoConsultaLibroCalificaciones.NoDisponible);
}

public sealed class ResultadoConsultaResumenAbacus
{
    private ResultadoConsultaResumenAbacus(
        EstadoConsultaLibroCalificaciones estado,
        ResumenAbacus? resumen = null)
    {
        Estado = estado;
        Resumen = resumen;
    }

    public EstadoConsultaLibroCalificaciones Estado { get; }

    public ResumenAbacus? Resumen { get; }

    public static ResultadoConsultaResumenAbacus Disponible(ResumenAbacus resumen) =>
        new(EstadoConsultaLibroCalificaciones.Disponible, resumen);

    public static ResultadoConsultaResumenAbacus FueraDeCicloAutorizado() =>
        new(EstadoConsultaLibroCalificaciones.FueraDeCicloAutorizado);

    public static ResultadoConsultaResumenAbacus NoDisponible() =>
        new(EstadoConsultaLibroCalificaciones.NoDisponible);
}
