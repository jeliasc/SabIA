using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Secciones;

public class FormularioSeccion
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un ciclo escolar.")]
    [Display(Name = "Ciclo escolar")]
    public int CicloEscolarId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un grado.")]
    [Display(Name = "Grado")]
    public int GradoId { get; set; }

    [Required, StringLength(5)]
    [Display(Name = "Sección")]
    public string Nombre { get; set; } = string.Empty;
}
public sealed class CrearSeccion : FormularioSeccion { }
public sealed class EditarSeccion : FormularioSeccion { public int Id { get; set; } }
public sealed class SeccionLista
{
    public int Id { get; init; }
    public int Ciclo { get; init; }
    public string Grado { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public EstadoRegistro Estado { get; init; }
    public int Alumnos { get; init; }
    public int Asignaciones { get; init; }
}
