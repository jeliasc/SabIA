using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class PermisoSistema
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Modulo { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}