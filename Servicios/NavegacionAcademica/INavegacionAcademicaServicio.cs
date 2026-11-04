using Proyecto_Final.ViewModels.NavegacionAcademica;

namespace Proyecto_Final.Servicios.NavegacionAcademica;

public interface INavegacionAcademicaServicio
{
    Task<MisCursosIndice?> ObtenerMisCursosAsync();
    Task<EspacioCurso?> ObtenerEspacioAsync(int asignacionId);
}
