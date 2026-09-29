using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Roles;

public class EditarRol : FormularioRol
{
    [Required]
    public string Id { get; set; } = string.Empty;
}
