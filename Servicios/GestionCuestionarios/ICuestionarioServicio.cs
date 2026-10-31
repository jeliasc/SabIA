using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

public interface ICuestionarioServicio
{
    Task<List<CuestionarioLista>> ObtenerTodosAsync(int? asignacionId = null);
    Task<List<CuestionarioLista>> ObtenerHistorialAsync(int? asignacionId = null);
    Task<ConfigurarCuestionario?> ObtenerConfiguracionAsync(int tareaId);
    Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(ConfigurarCuestionario m);
    Task<CrearPregunta?> PrepararPreguntaAsync(int cuestionarioId);
    Task<ResultadoOperacion> AgregarPreguntaAsync(CrearPregunta m);

    Task<InicioCuestionario?> ObtenerInicioAsync(int id);
    Task<ResultadoOperacion<int>> IniciarAsync(int id);
    Task<VistaIntento> ObtenerIntentoAsync(int intentoId);
    Task<ResultadoOperacion<bool>> GuardarRespuestaAsync(int intentoId, RespuestaCuestionario respuesta);
    Task<ResultadoOperacion<bool>> ResponderAsync(ResponderPregunta m);
    Task<ResultadoOperacion<int>> FinalizarAsync(FinalizarIntento m);
    Task<ResultadoIntento?> ObtenerResultadoAsync(int intentoId);
    Task<int> FinalizarVencidosAsync(DateTime ahora, CancellationToken cancellationToken = default);

    Task<ResultadosCuestionario?> ObtenerResultadosAsync(int id);
    Task<RevisionIntento?> ObtenerRevisionIntentoAsync(int intentoId);
    Task<ResultadoOperacion<int>> AjustarRespuestaDirectaAsync(int respuestaId, bool aceptar);
    Task<AccesoRespuestasEntrega?> ObtenerRespuestasDeEntregaAsync(int entregaId);

    Task<List<Proyecto_Final.ViewModels.Comunes.OpcionSeleccion>> ObtenerTareasParaImportarAsync();
    Task<ResultadoOperacion<int>> ImportarPreguntasAsync(ImportarCuestionarioIa modelo);
}
