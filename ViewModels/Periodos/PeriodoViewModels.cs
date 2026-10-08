using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Periodos;

public class FormularioPeriodo
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un ciclo escolar.")]
    [Display(Name = "Ciclo escolar")]
    public int CicloEscolarId { get; set; }

    [Range(1, 4, ErrorMessage = "El número de periodo debe estar entre 1 y 4.")]
    public int Numero { get; set; }

    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required, Display(Name = "Fecha de inicio")]
    public DateOnly FechaInicio { get; set; }

    [Required, Display(Name = "Fecha de finalización")]
    public DateOnly FechaFin { get; set; }
}
public sealed class CrearPeriodo : FormularioPeriodo { }
public sealed class EditarPeriodo : FormularioPeriodo { public int Id { get; set; } }
public sealed class PeriodoLista
{
    public int Id { get; init; }
    public int CicloId { get; init; }
    public int Ciclo { get; init; }
    public int Numero { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateOnly Inicio { get; init; }
    public DateOnly Fin { get; init; }
}
