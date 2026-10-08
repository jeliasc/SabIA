using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Calificaciones;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

public interface ICalificacionServicio
{
    Task<CalificacionesIndex> ObtenerIndexAsync(bool puedeConfigurar);
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
    Task<ResultadoOperacion> GuardarLibroAsync(GuardarLibroCalificaciones modelo);
    Task<ResultadoOperacion> CerrarAsync(int configuracionId);
    Task<ResumenAbacus?> ObtenerResumenAbacusAsync(int configuracionId);
    Task<CorreccionesCalificaciones> ObtenerCorreccionesAsync(bool puedeRevisar);
    Task<ResultadoOperacion> SolicitarCorreccionAsync(SolicitarCorreccionCalificacion modelo);
    Task<ResultadoOperacion> RevisarCorreccionAsync(RevisarCorreccionCalificacion modelo);
    Task<ResultadoOperacion> AplicarCorreccionAsync(AplicarCorreccionCalificacion modelo);
}
