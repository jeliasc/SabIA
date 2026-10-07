
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

    [Range(1, int.MaxValue)]
    public int NumeroIntento { get; set; }

    public EstadoIntentoCuestionario Estado { get; set; } = EstadoIntentoCuestionario.EnCurso;

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }

    [Precision(9, 2)]
    public decimal? Calificacion { get; set; }

    public DateTime? FechaCalificacion { get; set; }

    public ICollection<RespuestaAlumno> Respuestas { get; set; } = new List<RespuestaAlumno>();
}
