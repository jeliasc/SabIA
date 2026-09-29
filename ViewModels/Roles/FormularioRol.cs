using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Roles;

public class FormularioRol
{
    [Required(
        ErrorMessage = "El nombre del rol es obligatorio."
    )]
    [StringLength(
        100,
        ErrorMessage =
            "El nombre del rol no puede superar los 100 caracteres."
    )]
    [Display(Name = "Nombre del rol")]
    public string Nombre { get; set; } = string.Empty;

    public List<PermisoRol> Permisos { get; set; } = [];
}