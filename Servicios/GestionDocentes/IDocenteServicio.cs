using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Docentes;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.GestionDocentes;

public interface IDocenteServicio
{
    Task<List<DocenteLista>> ObtenerTodosAsync();

    Task<DetalleDocente?> ObtenerDetalleAsync(int id);

    Task<ResultadoOperacion<ResultadoContrasenaTemporal>> CrearAsync(
        CrearDocente modelo);

    Task<EditarDocente?> ObtenerParaEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(
        EditarDocente modelo);

    Task<ResultadoOperacion> CambiarEstadoAsync(
        int id,
        bool activo);
}
