
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class PlanificacionDetalle
{
    public int Id { get; set; }

    public int PlanificacionId { get; set; }

    public Planificacion Planificacion { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    [StringLength(2000)]
    public string? Competencia { get; set; }

    [StringLength(2000)]
    public string? IndicadorLogro { get; set; }

    [Required]
    public string Contenido { get; set; } = string.Empty;

    public string? Actividades { get; set; }

    public string? Recursos { get; set; }

    public string? EstrategiaEvaluacion { get; set; }
}
