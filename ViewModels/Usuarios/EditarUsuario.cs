using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Usuarios;

public class EditarUsuario : FormularioUsuario
{
    [Required(ErrorMessage = "El identificador del usuario es obligatorio.")]
    public string Id { get; set; } = string.Empty;
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre de usuario no puede superar los 100 caracteres.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    public string? AdvertenciaConfiguracionRol { get; set; }
}
