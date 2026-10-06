using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Personas;

public abstract class FormularioPersona
{
    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Primer nombre")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Segundo nombre")]
    public string? SegundoNombre { get; set; }

    [StringLength(100)]
    [Display(Name = "Tercer nombre")]
    public string? TercerNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Primer apellido")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Segundo apellido")]
    public string? SegundoApellido { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(256)]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;
}