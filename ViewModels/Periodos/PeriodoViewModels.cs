
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.ViewModels.Periodos;

public class FormularioPeriodo
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un ciclo escolar.")]
    [Display(Name = "Ciclo escolar")]
    public int CicloEscolarId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria."), Display(Name = "Fecha de inicio")]
    public DateOnly FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de finalización es obligatoria."), Display(Name = "Fecha de finalización")]
    public DateOnly FechaFin { get; set; }
}

public sealed class CrearPeriodo : FormularioPeriodo
{
    public CrearPeriodo()
    {
        FechaInicio = DateOnly.MinValue;
        FechaFin = DateOnly.MinValue;
    }
}

public sealed class EditarPeriodo : FormularioPeriodo
{
    public int Id { get; set; }

    public int Numero { get; set; }
}

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
