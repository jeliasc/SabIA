using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Usuarios;

public class EditarUsuario : FormularioUsuario
{
    [Required]
    public string Id { get; set; } = string.Empty;
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;
}