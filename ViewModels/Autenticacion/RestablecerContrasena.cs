using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Autenticacion;

public class RestablecerContrasena
{
    public string UsuarioId { get; set; } =
        string.Empty;

    public string Token { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "La nueva contraseña es obligatoria."
    )]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Contrasena { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "Debe confirmar la contraseña."
    )]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(
        nameof(Contrasena),
        ErrorMessage = "Las contraseñas no coinciden."
    )]
    public string ConfirmarContrasena { get; set; } =
        string.Empty;
}