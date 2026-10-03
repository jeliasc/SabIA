using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Proyecto_Final.Models;

public class Usuario : IdentityUser
{
    [Required]
    [StringLength(100)]
    [Column("Primer_Nombre")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("Segundo_Nombre")]
    public string? SegundoNombre { get; set; }

    [StringLength(100)]
    [Column("Tercer_Nombre")]
    public string? TercerNombre { get; set; }

    [Required]
    [StringLength(100)]
    [Column("Primer_Apellido")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("Segundo_Apellido")]
    public string? SegundoApellido { get; set; }

    [Column("Activo")]
    public bool Activo { get; set; } = true;

    [Column("Cambiar_Contrasena")]
    public bool CambiarContrasena { get; set; } = false;

    [Column("Fecha_Creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("Fecha_Ultimo_Cambio_Contrasena")]
    public DateTime? FechaUltimoCambioContrasena { get; set; }
}