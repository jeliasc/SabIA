using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Usuarios;

public class EditarUsuario : FormularioUsuario
{
    [Required]
    public string Id { get; set; } = string.Empty;
}