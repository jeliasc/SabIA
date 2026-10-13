using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Secciones;

namespace Proyecto_Final.Servicios.GestionSecciones;

public interface ISeccionServicio
{
    Task<List<SeccionLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearSeccion modelo);
    Task<EditarSeccion?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarSeccion modelo);
    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activa);
}
