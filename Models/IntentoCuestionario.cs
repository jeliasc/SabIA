
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class IntentoCuestionario
{
    public int Id { get; set; }

    public int CuestionarioId { get; set; }
    public Cuestionario Cuestionario { get; set; } = null!;

    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public int? InscripcionId { get; set; }
    public Inscripcion? Inscripcion { get; set; }

    // Una autorización excepcional puede consumirse en un único intento.
    public int? ReaperturaEntregaId { get; set; }
    public ReaperturaEntrega? ReaperturaEntrega { get; set; }

    [Range(1, int.MaxValue)]
    public int NumeroIntento { get; set; }

    public EstadoIntentoCuestionario Estado { get; set; } = EstadoIntentoCuestionario.EnCurso;

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }

    // Momento máximo para responder (sin la tolerancia de red).
    // Nulo cuando el cuestionario no tiene límite de tiempo.
    public DateTime? FechaLimite { get; set; }

    // Identificadores de las preguntas, separados por comas, en el orden
    // en que se presentan en este intento. Se fija al iniciar el intento
    // para que recargar la página no cambie el orden.
    [Required]
    [StringLength(4000)]
    public string OrdenPreguntas { get; set; } = string.Empty;

    // Posición de la pregunta en curso cuando el alumno no puede navegar.
    [Range(0, int.MaxValue)]
    public int PreguntaActual { get; set; }

    [Precision(9, 2)]
    public decimal? Calificacion { get; set; }

    public DateTime? FechaCalificacion { get; set; }

    public ICollection<RespuestaAlumno> Respuestas { get; set; } = new List<RespuestaAlumno>();
}
