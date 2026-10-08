using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Asignaciones;

namespace Proyecto_Final.Servicios.GestionAsignaciones;

public interface IAsignacionServicio
{
    Task<List<AsignacionLista>> ObtenerTodosAsync();

    Task PrepararAsync(FormularioAsignacion m);

    Task<ResultadoOperacion> CrearAsync(CrearAsignacion m);

    Task<EditarAsignacion?> ObtenerEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(EditarAsignacion m);

    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo);
}