using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Cuestionarios;

public sealed class CuestionarioLista
{
    public int Id { get; set; }

    public int TareaId { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public int Preguntas { get; set; }

    public int MaximoIntentos { get; set; }

    public OrigenCuestionario Origen { get; set; }

    public EstadoTarea EstadoTarea { get; set; }

    // El docente o coordinación pueden revisar resultados; el alumno no.
    public bool PuedeVerResultados { get; set; }
}

public sealed class ConfigurarCuestionario
{
    public int TareaId { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public ModalidadTiempoCuestionario ModalidadTiempo { get; set; } =
        ModalidadTiempoCuestionario.SinLimite;

    [Range(1, 300, ErrorMessage = "El tiempo total debe estar entre 1 y 300 minutos.")]
    public int? TiempoGeneralMinutos { get; set; }

    [Range(10, 3600, ErrorMessage = "El tiempo por pregunta debe estar entre 10 y 3600 segundos.")]
    public int? TiempoPorPreguntaSegundos { get; set; }

    public bool PermitirNavegacion { get; set; }

    public bool OrdenAleatorio { get; set; }

    [Range(0, 100)]
    public int MaximoIntentos { get; set; } = 1;

    public CriterioCalificacionIntentos CriterioCalificacion { get; set; } =
        CriterioCalificacionIntentos.MejorNota;

    public bool MostrarResultadosAlFinalizar { get; set; } = true;

    public bool MostrarRespuestasCorrectas { get; set; }
}

public sealed class CrearPregunta
{
    public int CuestionarioId { get; set; }

    public string Cuestionario { get; set; } = string.Empty;

    public bool UsaTiempoPorPregunta { get; set; }

    public int? TiempoPredeterminadoSegundos { get; set; }

    [Range(1, 500)]
    public int Orden { get; set; } = 1;

    public TipoPregunta Tipo { get; set; } =
        TipoPregunta.SeleccionUnica;

    [Required]
    public string Enunciado { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999")]
    public decimal Punteo { get; set; } = 1;

    [Required]
    public string RespuestaCorrecta { get; set; } = string.Empty;

    public string? OpcionesSeparadas { get; set; }

    [Range(10, 3600, ErrorMessage = "El tiempo debe estar entre 10 y 3600 segundos.")]
    public int? TiempoLimiteSegundos { get; set; }

    [StringLength(2000)]
    public string? Retroalimentacion { get; set; }
}

// Pantalla previa al cuestionario: reglas, tiempo e intentos.
public sealed class InicioCuestionario
{
    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    public int CantidadPreguntas { get; set; }

    public int MaximoIntentos { get; set; }

    public int IntentosUsados { get; set; }

    public ModalidadTiempoCuestionario ModalidadTiempo { get; set; }

    public int? TiempoGeneralSegundos { get; set; }

    public int? TiempoPorPreguntaSegundos { get; set; }

    public bool PermitirNavegacion { get; set; }

    public int? IntentoEnCursoId { get; set; }

    public int? UltimoIntentoId { get; set; }

    public bool PlazoVencido { get; set; }

    public bool PuedeIniciar =>
        IntentoEnCursoId.HasValue ||
        (!PlazoVencido && CantidadPreguntas > 0 &&
         (MaximoIntentos == 0 || IntentosUsados < MaximoIntentos));
}

// Intento en curso. En modo navegable se envían todas las preguntas;
// en modo secuencial solo la pregunta actual.
public sealed class IntentoEnCurso
{
    public int IntentoId { get; set; }

    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public bool PermitirNavegacion { get; set; }

    public int TotalPreguntas { get; set; }

    // Segundos restantes de todo el intento; nulo si no hay límite general.
    public int? SegundosRestantes { get; set; }

    // Segundos restantes de la pregunta actual (solo tiempo por pregunta).
    public int? SegundosRestantesPregunta { get; set; }

    public List<PreguntaIntento> Preguntas { get; set; } = [];
}

public sealed class PreguntaIntento
{
    public int PreguntaId { get; set; }

    public int Numero { get; set; }

    public TipoPregunta Tipo { get; set; }

    public string Enunciado { get; set; } = string.Empty;

    public decimal Punteo { get; set; }

    public string? Texto { get; set; }

    public List<int> OpcionIds { get; set; } = [];

    public List<OpcionCuestionario> Opciones { get; set; } = [];
}

// Respuesta enviada por el alumno para una pregunta.
public sealed class RespuestaCuestionario
{
    public int PreguntaId { get; set; }

    public string? Texto { get; set; }

    public int? OpcionId { get; set; }

    public List<int> OpcionIds { get; set; } = [];
}

public sealed class OpcionCuestionario
{
    public int Id { get; set; }

    public string Texto { get; set; } = string.Empty;
}

public sealed class FinalizarIntento
{
    public int IntentoId { get; set; }

    public List<RespuestaCuestionario> Respuestas { get; set; } = [];
}

public sealed class ResponderPregunta
{
    public int IntentoId { get; set; }

    public RespuestaCuestionario Respuesta { get; set; } = new();
}

public sealed class ResultadoIntento
{
    public int IntentoId { get; set; }

    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public bool Expirado { get; set; }

    public decimal Calificacion { get; set; }

    public decimal PunteoObtenido { get; set; }

    public decimal PunteoTotal { get; set; }

    public decimal CalificacionAplicada { get; set; }

    public bool MostrarResultados { get; set; } = true;

    public bool MostrarRespuestasCorrectas { get; set; }

    public List<DetalleRespuestaIntento> Detalle { get; set; } = [];
}

public sealed class DetalleRespuestaIntento
{
    public int Numero { get; set; }

    public string Enunciado { get; set; } = string.Empty;

    public string RespuestaAlumno { get; set; } = string.Empty;

    public string RespuestaCorrecta { get; set; } = string.Empty;

    public bool Correcta { get; set; }

    public decimal PunteoObtenido { get; set; }

    public decimal Punteo { get; set; }

    public string? Retroalimentacion { get; set; }
}


// =====================================================================
// REVISIÓN DEL DOCENTE
// =====================================================================

public sealed class ResultadosCuestionario
{
    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string GradoSeccion { get; set; } = string.Empty;

    public EstadoTarea EstadoTarea { get; set; }

    public decimal PunteoMaximo { get; set; }

    public int MaximoIntentos { get; set; }

    public CriterioCalificacionIntentos CriterioCalificacion { get; set; }

    public int TotalAlumnos { get; set; }

    public int AlumnosConIntento { get; set; }

    public decimal? PromedioPorcentaje { get; set; }

    public List<EstadisticaPregunta> Preguntas { get; set; } = [];

    public List<ResultadoAlumnoCuestionario> Alumnos { get; set; } = [];
}

public sealed class EstadisticaPregunta
{
    public int Numero { get; set; }

    public string Enunciado { get; set; } = string.Empty;

    public TipoPregunta Tipo { get; set; }

    // Intentos considerados: el que define la nota de cada alumno.
    public int Evaluados { get; set; }

    public int SinResponder { get; set; }

    public int Correctas { get; set; }

    public decimal Porcentaje => Evaluados == 0 ? 0 : Math.Round(Correctas * 100m / Evaluados, 1);
}

public sealed class ResultadoAlumnoCuestionario
{
    public int AlumnoId { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string CodigoPersonal { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal? NotaEntrega { get; set; }

    public List<IntentoResumen> Intentos { get; set; } = [];
}

public sealed class IntentoResumen
{
    public int IntentoId { get; set; }

    public int Numero { get; set; }

    public EstadoIntentoCuestionario Estado { get; set; }

    public decimal? Calificacion { get; set; }

    public DateTime? Fecha { get; set; }
}

public sealed class RevisionIntento
{
    public int IntentoId { get; set; }

    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Alumno { get; set; } = string.Empty;

    public string CodigoPersonal { get; set; } = string.Empty;

    public int NumeroIntento { get; set; }

    public EstadoIntentoCuestionario Estado { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFinalizacion { get; set; }

    public decimal Calificacion { get; set; }

    public decimal PunteoObtenido { get; set; }

    public decimal PunteoTotal { get; set; }

    public decimal? NotaEntrega { get; set; }

    public decimal PunteoMaximoTarea { get; set; }

    public bool PuedeAjustar { get; set; }

    public List<DetalleRevision> Detalle { get; set; } = [];
}

public sealed class DetalleRevision
{
    public int? RespuestaId { get; set; }

    public int Numero { get; set; }

    public TipoPregunta Tipo { get; set; }

    public string Enunciado { get; set; } = string.Empty;

    public string RespuestaAlumno { get; set; } = string.Empty;

    public string RespuestaCorrecta { get; set; } = string.Empty;

    public bool SinResponder { get; set; }

    public bool Correcta { get; set; }

    public decimal PunteoObtenido { get; set; }

    public decimal Punteo { get; set; }

    public string? Retroalimentacion { get; set; }

    // Ajuste manual del docente en preguntas de respuesta directa.
    public bool AjusteManual { get; set; }

    public string? AjustadoPor { get; set; }

    public DateTime? FechaAjuste { get; set; }
}
