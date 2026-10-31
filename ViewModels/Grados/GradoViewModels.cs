using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Grados;

public class FormularioGrado
{
    [Required(ErrorMessage = "Seleccione un nivel educativo."), Display(Name = "Nivel educativo")]
    public NivelEducativo Nivel { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria."), StringLength(150, ErrorMessage = "La carrera no puede superar los 150 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor que cero.")]
    public int Orden { get; set; } = 1;
}
public sealed class CrearGrado : FormularioGrado { }
public sealed class EditarGrado : FormularioGrado { public int Id { get; set; } }
public sealed class GradoLista
{
    public int Id { get; init; }
    public NivelEducativo Nivel { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Carrera { get; init; } = string.Empty;
    public int Orden { get; init; }
    public int Secciones { get; init; }
    public int Asignaciones { get; init; }
}
