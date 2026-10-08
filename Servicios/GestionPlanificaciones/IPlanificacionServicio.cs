using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Planificaciones;

namespace Proyecto_Final.Servicios.GestionPlanificaciones;

public interface IPlanificacionServicio
{
    Task<List<PlanificacionLista>> ObtenerTodosAsync();
    Task<List<PlanificacionLista>> ObtenerHistorialAsync();

    Task PrepararAsync(FormularioPlanificacion m);

    Task<ResultadoOperacion> CrearAsync(
        CrearPlanificacion m,
        CancellationToken ct
    );

    Task<EditarPlanificacion?> ObtenerEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(
        EditarPlanificacion m,
        CancellationToken ct
    );

    Task<ResultadoOperacion> EnviarRevisionAsync(int id);

    Task<RevisarPlanificacion?> ObtenerRevisionAsync(int id);

    Task<ResultadoOperacion> RevisarAsync(
        RevisarPlanificacion m,
        string usuarioId
    );

    Task<int?> ObtenerArchivoPdfIdAsync(int id);
}
