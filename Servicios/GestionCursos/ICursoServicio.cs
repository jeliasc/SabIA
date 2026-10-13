using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Cursos;

namespace Proyecto_Final.Servicios.GestionCursos;

public interface ICursoServicio
{
    Task<List<CursoLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearCurso modelo);
    Task<EditarCurso?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarCurso modelo);
    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo);
}
