using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

/// <summary>Referencia inmutable a la asignación e historial que sustentaron la decisión.</summary>
public sealed class FuenteResolucionTrasladoAcademico
{
    public int Id { get; set; }
    public int ResolucionTrasladoAcademicoId { get; set; }
    public ResolucionTrasladoAcademico Resolucion { get; set; } = null!;
    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;
    public int HistorialTrasladoId { get; set; }
    public HistorialTraslado HistorialTraslado { get; set; } = null!;
    [Required, StringLength(24)]
    public string Rol { get; set; } = string.Empty;
    [Required, StringLength(2000)]
    public string Evidencia { get; set; } = string.Empty;
}
