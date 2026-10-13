using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

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
public sealed class ReabrirCiclo
{
    public int Id { get; set; }
    public int Anio { get; set; }

    [Required(ErrorMessage = "La justificación es obligatoria.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "La justificación debe contener entre 10 y 1000 caracteres.")]
    [Display(Name = "Justificación")]
    public string Justificacion { get; set; } = string.Empty;
}

public sealed class CicloLista
{
    public int Id { get; init; }
    public int Anio { get; init; }
    public DateOnly Inicio { get; init; }
    public DateOnly Fin { get; init; }
    public bool Activo { get; init; }
    public EstadoCicloEscolar Estado { get; init; }
    public int Periodos { get; init; }
    public int Secciones { get; init; }
}

public sealed class RevisionCierreCiclo
{
    public int CicloId { get; init; }
    public int Anio { get; init; }
    public EstadoCicloEscolar Estado { get; init; }
    public IReadOnlyList<PendienteCierreCiclo> Pendientes { get; init; } = [];
    public int AsignacionesEvaluables { get; init; }
    public int AsignacionesSinObligaciones { get; init; }
    public bool PuedeCerrar => Estado == EstadoCicloEscolar.Activo && Pendientes.Count == 0;
}

public sealed class PendienteCierreCiclo
{
    public string Docente { get; init; } = string.Empty;
    public string Curso { get; init; } = string.Empty;
    public string Grado { get; init; } = string.Empty;
    public string Carrera { get; init; } = string.Empty;
    public string Seccion { get; init; } = string.Empty;
    public string Periodo { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public sealed class MovimientoCicloLista
{
    public long Id { get; init; }
    public int Anio { get; init; }
    public TipoMovimientoCicloEscolar Tipo { get; init; }
    public EstadoCicloEscolar EstadoAnterior { get; init; }
    public EstadoCicloEscolar EstadoNuevo { get; init; }
    public DateTime FechaUtc { get; init; }
    public string Responsable { get; init; } = string.Empty;
    public string? Justificacion { get; init; }
}
