using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Docentes;

public class EditarDocente
    : FormularioDocente
{
    [Required(ErrorMessage = "El identificador del docente es obligatorio.")]
    public int Id { get; set; }
}
