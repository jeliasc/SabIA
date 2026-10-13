using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Docentes;

public class EditarDocente
    : FormularioDocente
{
    [Required]
    public int Id { get; set; }
}