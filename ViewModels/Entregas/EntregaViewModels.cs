using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.Extensions;

namespace Proyecto_Final.ViewModels.Entregas;

public sealed class EntregaLista
{
    public int Id { get; set; }

    // Identifica el contexto académico, no un intento histórico aislado.
    public int TareaId { get; set; }
    public int AlumnoId { get; set; }
    public int? InscripcionId { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public DateTime? FechaLimite { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public EstadoEntrega Estado { get; set; }

    public decimal? Calificacion { get; set; }

    public OrigenCalificacionAdministrativa? OrigenCalificacionAdministrativa { get; set; }

    public decimal PunteoMaximo { get; set; }

    public int NumeroEnvio { get; set; }

    public int CantidadIntentos { get; set; }

    public int? IntentosPermitidos { get; set; }

    public int OportunidadesAdicionales { get; set; }

    // Las autorizaciones extraordinarias no forman parte del cupo ordinario.
    // Se exponen por separado para que las vistas puedan indicar si siguen
    // disponibles, ya se utilizaron o vencieron.
    public int OportunidadesAdicionalesConsumidas { get; set; }
    public int OportunidadesAdicionalesVigentes { get; set; }
    public int OportunidadesAdicionalesVencidas { get; set; }

    public DateTime? FechaLimiteIndividual { get; set; }

    public bool PuedeReabrirse { get; set; }

    public bool PuedeCalificarse { get; set; }

    public bool PuedeCalificarseConCero { get; set; }

    // Entrega enviada más antigua aún pendiente de calificación dentro del
    // contexto agrupado; no necesariamente coincide con el intento vigente.
    public int? EntregaCalificableId { get; set; }

    public int? EntregaCalificableConCeroId { get; set; }

    public bool PuedeEntregarse { get; set; }

    public bool TieneIntentoPendienteEvaluacion { get; set; }

    // La nota efectiva puede pertenecer a un intento anterior. Se conserva
    // separada del estado del intento vigente para que una reapertura no haga
    // parecer que la actividad nunca fue calificada.
    public bool TieneCalificacionPrevia { get; set; }

    public bool EsCeroAdministrativoSinEntrega { get; set; }

    public bool CalificadoSinEntrega =>
        EsCeroAdministrativoSinEntrega ||
        (Estado == EstadoEntrega.Calificada && !FechaEntrega.HasValue && Calificacion == 0);

    public string EstadoIntento => PresentacionEstadoEntrega.ObtenerTexto(
        Estado,
        TieneIntentoPendienteEvaluacion,
        TieneCalificacionPrevia,
        CalificadoSinEntrega);

    public string ClaseEstadoIntento => PresentacionEstadoEntrega.ObtenerClase(Estado);
    // ESTADO ACADÉMICO DE LA ACTIVIDAD
    public string EstadoActividad
    {
        get
        {
            if (CalificadoSinEntrega)
                return "Calificado con cero (sin entregar)";

            if (Calificacion.HasValue)
                return "Calificada";

            return EstadoIntento;
        }
    }

    // ESTILO DEL ESTADO ACADÉMICO
    public string ClaseEstadoActividad =>
        Calificacion.HasValue
            ? "app-badge-success"
            : ClaseEstadoIntento;

    // INFORMACIÓN DEL ÚLTIMO INTENTO
    public string? ObservacionEstadoActividad
    {
        get
        {
            if (!Calificacion.HasValue)
                return null;

            if (TieneIntentoPendienteEvaluacion)
                return "Nuevo intento pendiente de evaluación.";

            if (Estado == EstadoEntrega.Pendiente)
                return "Oportunidad pendiente.";

            return null;
        }
    }

    // Si la tarea es un cuestionario: el alumno lo resuelve en lugar de
    // subir un archivo y el docente puede ver sus respuestas.
    public int? CuestionarioId { get; set; }
}

public sealed class EntregarTarea
{
    public int Id { get; set; }

    public int TareaId { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    public DateTime? FechaLimite { get; set; }

    [StringLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres.")]
    public string? Comentario { get; set; }

    public IFormFile? Archivo { get; set; }

    public List<ArchivoTareaApoyo> ArchivosApoyo { get; set; } = [];
}

public sealed class ArchivoTareaApoyo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
}

public sealed class DetalleEntrega
{
    public int EntregaSeleccionadaId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string? Instrucciones { get; set; }
    public TipoTarea Tipo { get; set; }
    public int? CuestionarioId { get; set; }
    public EstadoTarea EstadoTarea { get; set; }
    public DateTime? FechaDisponibilidad { get; set; }
    public DateTime? FechaLimiteEfectiva { get; set; }
    public decimal PunteoMaximo { get; set; }
    public decimal? CalificacionEfectiva { get; set; }
    public bool TieneIntentoPendienteEvaluacion { get; set; }
    public EstadoEntrega EstadoActual { get; set; }
    public bool TieneCalificacionPrevia { get; set; }
    public bool CalificadoSinEntrega { get; set; }
    public string EstadoIntentoActual => PresentacionEstadoEntrega.ObtenerTexto(
        EstadoActual,
        TieneIntentoPendienteEvaluacion,
        TieneCalificacionPrevia,
        CalificadoSinEntrega);
    public string ClaseEstadoIntentoActual => PresentacionEstadoEntrega.ObtenerClase(
        EstadoActual);
    public int? IntentosPermitidos { get; set; }
    // Sólo intentos que consumen el máximo ordinario. Una oportunidad
    // extraordinaria utilizada nunca se suma a este contador.
    public int IntentosOrdinariosUsados { get; set; }
    public int OportunidadesAdicionales { get; set; }
    public int OportunidadesAdicionalesConsumidas { get; set; }
    public int OportunidadesAdicionalesVigentes { get; set; }
    public int OportunidadesAdicionalesVencidas { get; set; }
    public bool PuedeEntregarse { get; set; }
    public bool PuedeCalificarse { get; set; }
    public bool PuedeReabrirse { get; set; }
    public bool PuedeCalificarseConCero { get; set; }
    public int? EntregaCalificableConCeroId { get; set; }
    public bool PuedeVerRespuestasCuestionario { get; set; }
    public List<ArchivoTareaApoyo> ArchivosApoyo { get; set; } = [];
    public List<IntentoEntregaDetalle> Intentos { get; set; } = [];
    public List<IntentoCuestionarioDetalle> IntentosCuestionario { get; set; } = [];
}

public sealed class IntentoEntregaDetalle
{
    public int Id { get; set; }
    public int NumeroEnvio { get; set; }
    public EstadoEntrega Estado { get; set; }
    public DateTime? FechaEntrega { get; set; }
    // Es el plazo específico del intento; cuando existe prevalece sobre el
    // plazo general de la tarea, incluidas las reaperturas posteriores.
    public DateTime? FechaLimiteIndividual { get; set; }
    public decimal? Calificacion { get; set; }
    public OrigenCalificacionAdministrativa? OrigenCalificacionAdministrativa { get; set; }
    public bool PuedeRectificarseCero { get; set; }
    public string? ComentarioAlumno { get; set; }
    public string? Retroalimentacion { get; set; }
    public string? MotivoReapertura { get; set; }
    public bool PuedeCalificarse { get; set; }
    public List<ArchivoEntregaDetalle> Archivos { get; set; } = [];
}

public sealed class ArchivoEntregaDetalle
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public bool PuedeVisualizar { get; set; }
}

public static class PresentacionEstadoEntrega
{
    public static string ObtenerTexto(
        EstadoEntrega estado,
        bool tieneIntentoPendienteEvaluacion,
        bool tieneCalificacionPrevia,
        bool calificadoSinEntrega = false) =>
        calificadoSinEntrega
            ? "Calificado con cero (sin entregar)"
            : estado == EstadoEntrega.Pendiente
            ? tieneCalificacionPrevia
                ? "Nuevo intento pendiente"
                : "Pendiente"
            : tieneIntentoPendienteEvaluacion &&
              estado is EstadoEntrega.Enviada or EstadoEntrega.Tardia
                ? "Evaluación pendiente"
                : estado.GetDisplayName();

    public static string ObtenerClase(EstadoEntrega estado) =>
        estado == EstadoEntrega.Calificada
            ? "app-badge-success"
            : estado == EstadoEntrega.Devuelta
                ? "app-badge-danger"
                : estado == EstadoEntrega.Pendiente
                    ? "app-badge-secondary"
                    : "app-badge-info";
}

public sealed class IntentoCuestionarioDetalle
{
    public int Id { get; set; }
    public int NumeroIntento { get; set; }
    public EstadoIntentoCuestionario Estado { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public decimal? Calificacion { get; set; }
    public bool EnCurso => Estado == EstadoIntentoCuestionario.EnCurso;
}

public sealed class CalificarEntrega
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public decimal PunteoMaximo { get; set; }

    public string? ComentarioAlumno { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public decimal Calificacion { get; set; }

    [StringLength(4000, ErrorMessage = "La retroalimentación no puede superar los 4000 caracteres.")]
    public string? Retroalimentacion { get; set; }

    public bool Devolver { get; set; }

    public List<IntentoSeleccionCalificar> Intentos { get; set; } = [];

    public int TareaId { get; set; }
    public int AlumnoId { get; set; }
    public int? InscripcionId { get; set; }
}

public sealed class IntentoSeleccionCalificar
{
    public int Id { get; set; }
    public int NumeroEnvio { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public EstadoEntrega Estado { get; set; }
}

public sealed class CalificarCeroEntrega
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public TipoTarea Tipo { get; set; }

    // No se usa Range sobre bool: la validación no intrusiva de jQuery
    // interpreta sus límites como números y rechaza incluso el valor "true".
    // La confirmación se valida en CalificarCeroAsync, antes de cualquier
    // escritura, para que también quede protegida ante POST manipulados.
    public bool Confirmado { get; set; }
}

public sealed class ReabrirEntrega
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public int NumeroEnvioActual { get; set; }

    public EstadoEntrega EstadoActual { get; set; }

    [Required(ErrorMessage = "Seleccione una entrega.")]
    public DateTime FechaLimite { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio."), StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe contener entre 5 y 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class RectificarCeroAdministrativo
{
    public int Id { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public OrigenCalificacionAdministrativa Origen { get; set; }

    [Required(ErrorMessage = "El motivo de la rectificación es obligatorio."),
     StringLength(1000, MinimumLength = 10,
         ErrorMessage = "El motivo debe contener entre 10 y 1000 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
