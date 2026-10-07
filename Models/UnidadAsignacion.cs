
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class UnidadAsignacion
{
    public int Id { get; set; }

    public int UnidadId { get; set; }

    public Unidad Unidad { get; set; } = null!;

    public int AsignacionId { get; set; }

    public Asignacion Asignacion { get; set; } = null!;

    public int? PeriodoId { get; set; }

    public Periodo? Periodo { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public EstadoPublicacionUnidad Estado { get; set; } = EstadoPublicacionUnidad.Borrador;

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaCierreAcceso { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    [Required]
    public string AsignadoPorUsuarioId { get; set; } = string.Empty;

    public Usuario AsignadoPorUsuario { get; set; } = null!;
}
