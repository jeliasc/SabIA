
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Unidad
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    public int? UnidadOrigenId { get; set; }

    public Unidad? UnidadOrigen { get; set; }

    [Required]
    public string CreadoPorUsuarioId { get; set; } = string.Empty;

    public Usuario CreadoPorUsuario { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public ICollection<Material> Materiales { get; set; } = new List<Material>();

    public ICollection<UnidadAsignacion> UnidadAsignaciones { get; set; } = new List<UnidadAsignacion>();
}
