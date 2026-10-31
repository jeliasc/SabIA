using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Roles;

public class EditarRol : FormularioRol
{
    [Required(ErrorMessage = "El identificador del rol es obligatorio.")]
    public string Id { get; set; } = string.Empty;
}
