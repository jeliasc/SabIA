using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.ResolucionesTraslados;

public sealed class ResolucionesTrasladosIndex
{
    public bool PuedeGestionar { get; set; }
    public bool PuedeAprobar { get; set; }
    public List<ResolucionTrasladoLista> Resoluciones { get; set; } = [];
    public List<ResolucionTrasladoPendiente> Pendientes { get; set; } = [];
}

public sealed class ResolucionTrasladoLista
{
    public int Id { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public TipoResolucionTraslado Tipo { get; set; }
    public EstadoResolucionTraslado Estado { get; set; }
    public bool Vigente { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
}

public sealed class ResolucionTrasladoPendiente
{
    public int InscripcionId { get; set; }
    public int CursoId { get; set; }
    public int PeriodoId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Secciones { get; set; } = string.Empty;
}

public sealed class CrearResolucionTraslado
{
    public int InscripcionId { get; set; }
    public int CursoId { get; set; }
    public int PeriodoId { get; set; }
    [Required(ErrorMessage = "Seleccione el tipo de resolución.")]
    public TipoResolucionTraslado? Tipo { get; set; }
    [Required, StringLength(2000, MinimumLength = 10, ErrorMessage = "Describa una justificación de 10 a 2000 caracteres.")]
    public string Justificacion { get; set; } = string.Empty;
    [Range(0, 999999, ErrorMessage = "La nota consolidada no puede ser negativa.")]
    public decimal? NotaConsolidadaPropuesta { get; set; }
    public decimal? EscalaMaximaConsolidada { get; set; }
    public List<int> HistorialTrasladoIds { get; set; } = [];
    public List<SelectListItem> Traslados { get; set; } = [];
    public string Alumno { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
}

public sealed class DetalleResolucionTraslado
{
    public int Id { get; set; }
    public int InscripcionId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public TipoResolucionTraslado Tipo { get; set; }
    public EstadoResolucionTraslado Estado { get; set; }
    public bool Vigente { get; set; }
    public string Justificacion { get; set; } = string.Empty;
    public string? ObservacionDecision { get; set; }
    public string CreadaPor { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaDecisionUtc { get; set; }
    public bool PeriodoCerrado { get; set; }
    public decimal? EscalaMaximaConsolidada { get; set; }
    public decimal? NotaConsolidadaPropuesta { get; set; }
    public decimal? NotaConsolidadaAprobada { get; set; }
    public List<FuenteResolucionTrasladoDetalle> Fuentes { get; set; } = [];
}

public sealed class FuenteResolucionTrasladoDetalle
{
    public string CursoSeccion { get; set; } = string.Empty;
    public string Traslado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Evidencia { get; set; } = string.Empty;
}

public sealed class DecidirResolucionTraslado
{
    public int Id { get; set; }
    public bool Aprobar { get; set; }
    [Required, StringLength(2000, MinimumLength = 5)]
    public string Observacion { get; set; } = string.Empty;
}
