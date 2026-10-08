using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

public interface ICuestionarioServicio
{
    Task<List<CuestionarioLista>> ObtenerTodosAsync();
    Task<List<CuestionarioLista>> ObtenerHistorialAsync();
    Task<ConfigurarCuestionario?> ObtenerConfiguracionAsync(int tareaId);
    Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(ConfigurarCuestionario m);
    Task<CrearPregunta?> PrepararPreguntaAsync(int cuestionarioId);
    Task<ResultadoOperacion> AgregarPreguntaAsync(CrearPregunta m);
    Task<ResolverCuestionario?> ObtenerParaResolverAsync(int id);
    Task<ResultadoOperacion<ResultadoIntento>> ResolverAsync(ResolverCuestionario m);
}
