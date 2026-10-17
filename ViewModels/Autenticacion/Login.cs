using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Autenticacion;

public class Login
{
    [Required(ErrorMessage = "Ingrese su nombre de usuario.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }
}
