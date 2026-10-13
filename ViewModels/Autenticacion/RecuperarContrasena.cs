using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Autenticacion;

public class RecuperarContrasena
{
    [Required(
        ErrorMessage = "El correo electrónico es obligatorio."
    )]
    [EmailAddress(
        ErrorMessage = "Ingrese un correo electrónico válido."
    )]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } =
        string.Empty;
}