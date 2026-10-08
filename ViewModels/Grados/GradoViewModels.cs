using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Grados;

public class FormularioGrado
{
    [Required, Display(Name = "Nivel educativo")]
    public NivelEducativo Nivel { get; set; }

    [Required, StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(150)]
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
    public int Cursos { get; init; }
}
