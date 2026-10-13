using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Encargados;

namespace Proyecto_Final.Servicios.GestionEncargados;

public interface IEncargadoServicio
{
    Task<List<EncargadoLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearEncargado modelo);
    Task<EditarEncargado?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarEncargado modelo);
}
