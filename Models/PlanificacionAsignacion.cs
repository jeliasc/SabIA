
namespace Proyecto_Final.Models;

public class PlanificacionAsignacion
{
    public int Id { get; set; }

    public int PlanificacionId { get; set; }

    public Planificacion Planificacion { get; set; } = null!;

    public int AsignacionId { get; set; }

    public Asignacion Asignacion { get; set; } = null!;

    public int? PeriodoId { get; set; }

    public Periodo? Periodo { get; set; }

    public int? UnidadAsignacionId { get; set; }

    public UnidadAsignacion? UnidadAsignacion { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }
}
