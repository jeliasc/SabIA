using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Ciclos;

public class FormularioCiclo
{
    [Range(2000, 9999, ErrorMessage = "Ingrese un año válido.")]
    [Display(Name = "Año")]
    public int Anio { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [Display(Name = "Fecha de inicio")]
    public DateOnly FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [Display(Name = "Fecha de finalización")]
    public DateOnly FechaFin { get; set; }
}
public sealed class CrearCiclo : FormularioCiclo { }
public sealed class EditarCiclo : FormularioCiclo { public int Id { get; set; } }
public sealed class CicloLista
{
    public int Id { get; init; }
    public int Anio { get; init; }
    public DateOnly Inicio { get; init; }
    public DateOnly Fin { get; init; }
    public bool Activo { get; init; }
    public int Periodos { get; init; }
    public int Secciones { get; init; }
}
