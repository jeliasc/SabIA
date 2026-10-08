using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class CrearAlumno : FormularioAlumno
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sección.")]
    [Display(Name = "Sección")]
    public int SeccionId { get; set; }

    [Display(Name = "Encargado")]
    public int? EncargadoId { get; set; }

    [Display(Name = "Parentesco")]
    public TipoParentesco? Parentesco { get; set; }

    public List<OpcionSeleccion> Secciones { get; set; } = [];

    public List<OpcionSeleccion> Encargados { get; set; } = [];
}
