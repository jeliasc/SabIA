using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.PermisosSistema;

public class CrearPermiso
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(
        150,
        ErrorMessage = "El código no puede superar los 150 caracteres."
    )]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El módulo es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage = "El módulo no puede superar los 100 caracteres."
    )]
    [Display(Name = "Módulo")]
    public string Modulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(
        250,
        ErrorMessage = "La descripción no puede superar los 250 caracteres."
    )]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;
}