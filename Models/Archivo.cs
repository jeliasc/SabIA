
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Archivo
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string NombreOriginal { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ClaveAlmacenamiento { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string TipoMime { get; set; } = string.Empty;

    public long TamanoBytes { get; set; }

    [StringLength(64)]
    public string? HashSha256 { get; set; }

    [Required]
    public string SubidoPorUsuarioId { get; set; } = string.Empty;

    public Usuario SubidoPorUsuario { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }
}
