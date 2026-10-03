using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Autenticacion;

public class CambiarContrasena
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(
        nameof(NuevaContrasena),
        ErrorMessage = "Las contraseñas no coinciden."
    )]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}