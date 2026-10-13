
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class RevisionPlanificacion
{
    public int Id { get; set; }

    public int PlanificacionId { get; set; }

    public Planificacion Planificacion { get; set; } = null!;

    public AccionRevisionPlanificacion Accion { get; set; }

    [Required]
    public string RealizadoPorUsuarioId { get; set; } = string.Empty;

    public Usuario RealizadoPorUsuario { get; set; } = null!;

    [StringLength(2000)]
    public string? Observaciones { get; set; }

    public DateTime Fecha { get; set; }

    public Guid? OperacionLoteId { get; set; }
}
