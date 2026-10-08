using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Periodos;

namespace Proyecto_Final.Servicios.GestionPeriodos;

public interface IPeriodoServicio
{
    Task<List<PeriodoLista>> ObtenerTodosAsync();
    Task<ResultadoOperacion> CrearAsync(CrearPeriodo modelo);
    Task<EditarPeriodo?> ObtenerParaEditarAsync(int id);
    Task<ResultadoOperacion> EditarAsync(EditarPeriodo modelo);
}
