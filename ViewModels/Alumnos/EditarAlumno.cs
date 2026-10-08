using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class EditarAlumno : FormularioAlumno
{
    [Required]
    public int Id { get; set; }
}
