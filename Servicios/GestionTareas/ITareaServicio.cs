using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Servicios.GestionTareas;

public interface ITareaServicio
{
    Task<List<TareaLista>> ObtenerTodosAsync();
    Task<List<TareaLista>> ObtenerHistorialAsync();

    Task PrepararAsync(FormularioTarea m);

    Task<ResultadoOperacion> CrearAsync(
        CrearTarea m,
        CancellationToken ct
    );

    Task<EditarTarea?> ObtenerEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(
        EditarTarea m,
        CancellationToken ct
    );

    Task<ResultadoOperacion> PublicarAsync(int id);

    Task<ResultadoOperacion> CerrarAsync(int id);
}
