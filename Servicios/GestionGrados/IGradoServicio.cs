using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Grados;

namespace Proyecto_Final.Servicios.GestionGrados;

public interface IGradoServicio
{
    Task<List<GradoLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearGrado modelo);
    Task<EditarGrado?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarGrado modelo);
}
