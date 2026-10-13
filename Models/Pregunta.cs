
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class Pregunta
{
    public int Id { get; set; }

    public int CuestionarioId { get; set; }
    public Cuestionario Cuestionario { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public TipoPregunta Tipo { get; set; }

    [Required]
    public string Enunciado { get; set; } = string.Empty;

    [Precision(9, 2)]
    public decimal Punteo { get; set; }

    // Tiempo propio de la pregunta. Si es nulo se usa el tiempo
    // predeterminado del cuestionario (Cuestionario.TiempoPorPreguntaSegundos).
    public int? TiempoLimiteSegundos { get; set; }

    // Explicación que se muestra al alumno al revisar el resultado,
    // cuando el docente habilita mostrar las respuestas correctas.
    [StringLength(2000)]
    public string? Retroalimentacion { get; set; }

    public ICollection<OpcionPregunta> Opciones { get; set; } = new List<OpcionPregunta>();

    public ICollection<RespuestaAceptada> RespuestasAceptadas { get; set; } = new List<RespuestaAceptada>();
}
