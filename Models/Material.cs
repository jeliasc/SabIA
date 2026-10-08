
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Material
{
    public int Id { get; set; }

    public int UnidadId { get; set; }

    public Unidad Unidad { get; set; } = null!;

    public int? ArchivoId { get; set; }

    public Archivo? Archivo { get; set; }

    [StringLength(2048)]
    public string? UrlExterna { get; set; }

    public TipoMaterial Tipo { get; set; }

    public EstadoPublicacionMaterial Estado { get; set; } = EstadoPublicacionMaterial.Borrador;

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public bool Descargable { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
}
