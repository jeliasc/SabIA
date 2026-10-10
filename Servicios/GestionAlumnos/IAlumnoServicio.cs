using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Alumnos;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.GestionAlumnos;

public interface IAlumnoServicio
{
    Task<ConsultaIndiceAlumnos> ObtenerIndiceAsync();
    Task<ConsultaDetalleAlumno> ObtenerDetalleAutorizadoAsync(int id);
    Task PrepararCreacionAsync(CrearAlumno modelo);
    Task<ResultadoOperacion<ResultadoContrasenaTemporal>> CrearAsync(CrearAlumno modelo);
    Task<EditarAlumno?> ObtenerParaEditarAsync(int id);
    Task PrepararEdicionAsync(EditarAlumno modelo);
    Task PrepararOpcionesEncargadosAsync(EditarAlumno modelo);
    Task<ResultadoOperacion> EditarAsync(EditarAlumno modelo);
    Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo);
}
