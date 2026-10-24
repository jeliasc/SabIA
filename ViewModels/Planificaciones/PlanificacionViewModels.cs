using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Planificaciones;

public class FormularioPlanificacion
{
    public TipoPlanificacion Tipo { get; set; }

    [Required(ErrorMessage = "El título es obligatorio."), StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Objetivos { get; set; }

    [StringLength(2000, ErrorMessage = "Las observaciones no pueden superar los 2000 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Seleccione una asignación.")]
    public string Contenido { get; set; } = string.Empty;

    public string? Actividades { get; set; }

    public string? Recursos { get; set; }

    public string? EstrategiaEvaluacion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una asignación.")]
    public int AsignacionId { get; set; }

    public int? PeriodoId { get; set; }

    public int? UnidadAsignacionId { get; set; }

    public DateOnly FechaInicio { get; set; } =
        DateOnly.FromDateTime(DateTime.Today);

    public DateOnly FechaFin { get; set; } =
        DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    public IFormFile? Archivo { get; set; }

    public List<OpcionSeleccion> Asignaciones { get; set; } = [];

    public List<OpcionSeleccion> Periodos { get; set; } = [];

    public List<OpcionSeleccion> Unidades { get; set; } = [];
}

public sealed class CrearPlanificacion : FormularioPlanificacion
{
}

public sealed class EditarPlanificacion : FormularioPlanificacion
{
    public int Id { get; set; }

    public string? ArchivoActual { get; set; }
}

public sealed class PlanificacionLista
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public TipoPlanificacion Tipo { get; set; }

    public string Docente { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public EstadoPlanificacion Estado { get; set; }

    public int Version { get; set; }

    public DateTime FechaCreacion { get; set; }
    public bool TienePdf { get; set; }
}

public sealed class RevisarPlanificacion
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Docente { get; set; } = string.Empty;

    public string Contenido { get; set; } = string.Empty;
    public bool TienePdf { get; set; }

    [StringLength(2000, ErrorMessage = "El comentario no puede superar los 2000 caracteres.")]
    public string? Observaciones { get; set; }

    public bool Aprobar { get; set; }
}
