using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.ResolucionesTraslados;

namespace Proyecto_Final.Servicios.GestionResolucionesTraslados;

public interface IResolucionTrasladoAcademicoServicio
{
    Task<ResolucionesTrasladosIndex> ObtenerIndexAsync(bool puedeGestionar, bool puedeAprobar);
    Task<CrearResolucionTraslado?> PrepararCreacionAsync(int inscripcionId, int cursoId, int periodoId);
    Task<ResultadoOperacion<int>> CrearAsync(CrearResolucionTraslado modelo);
    Task<DetalleResolucionTraslado?> ObtenerDetalleAsync(int id);
    Task<ResultadoOperacion> DecidirAsync(DecidirResolucionTraslado modelo);
}
