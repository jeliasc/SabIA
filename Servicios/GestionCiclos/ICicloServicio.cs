using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Servicios.GestionCiclos;

public interface ICicloServicio
{
    Task<List<CicloLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearCiclo modelo);
    Task<EditarCiclo?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarCiclo modelo);
    Task<ResultadoOperacion> ActivarAsync(int id);
}
