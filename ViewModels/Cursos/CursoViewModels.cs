using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Cursos;

public class FormularioCurso
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un grado.")]
    [Display(Name = "Grado")]
    public int GradoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre del curso")]
    public string Nombre { get; set; } = string.Empty;
}
public sealed class CrearCurso : FormularioCurso { }
public sealed class EditarCurso : FormularioCurso { public int Id { get; set; } }
public sealed class CursoLista
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public string Carrera { get; init; } = string.Empty;
    public EstadoRegistro Estado { get; init; }
    public int Asignaciones { get; init; }
}
