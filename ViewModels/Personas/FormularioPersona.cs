using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Personas;

public abstract class FormularioPersona
{
    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Primer nombre")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Segundo nombre")]
    public string? SegundoNombre { get; set; }

    [StringLength(100, ErrorMessage = "El tercer nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Tercer nombre")]
    public string? TercerNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    [Display(Name = "Primer apellido")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    [Display(Name = "Segundo apellido")]
    public string? SegundoApellido { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(256, ErrorMessage = "El correo electrónico no puede superar los 256 caracteres.")]
    [RegularExpression(
        @"^[^@\s]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,63}$",
        ErrorMessage = "Ingrese un correo electrónico válido con un dominio completo."
    )]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;
}
