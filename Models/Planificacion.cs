
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Planificacion
{
    public int Id { get; set; }

    public Guid GrupoVersionId { get; set; }

    [Range(1, int.MaxValue)]
    public int NumeroVersion { get; set; } = 1;

    public int? PlanificacionAnteriorId { get; set; }

    public Planificacion? PlanificacionAnterior { get; set; }

    public int DocenteId { get; set; }

    public Docente Docente { get; set; } = null!;

    public TipoPlanificacion Tipo { get; set; }

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Objetivos { get; set; }

    [StringLength(2000)]
    public string? Descripcion { get; set; }

    public EstadoPlanificacion Estado { get; set; } = EstadoPlanificacion.Borrador;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaEnvioRevision { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public string? AprobadoPorUsuarioId { get; set; }

    public Usuario? AprobadoPorUsuario { get; set; }

    public ICollection<PlanificacionDetalle> Detalles { get; set; } = new List<PlanificacionDetalle>();

    public ICollection<PlanificacionAsignacion> Asignaciones { get; set; } = new List<PlanificacionAsignacion>();

    public ICollection<PlanificacionArchivo> Archivos { get; set; } = new List<PlanificacionArchivo>();

    public ICollection<RevisionPlanificacion> Revisiones { get; set; } = new List<RevisionPlanificacion>();
}
