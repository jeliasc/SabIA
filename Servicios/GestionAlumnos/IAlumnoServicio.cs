using System.Security.Claims;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Alumnos;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.GestionAlumnos;

public interface IAlumnoServicio
{
    Task<List<AlumnoLista>> ObtenerTodosAsync(ClaimsPrincipal usuarioActual);
    Task<ResultadoOperacion<AlumnoDetalle>> ObtenerDetalleAsync(int id, ClaimsPrincipal usuarioActual);
    Task PrepararCreacionAsync(CrearAlumno modelo);
    Task<ResultadoOperacion<ResultadoContrasenaTemporal>> CrearAsync(CrearAlumno modelo);
    Task<EditarAlumno?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarAlumno modelo);
    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo);
}
