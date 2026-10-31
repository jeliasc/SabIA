using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

/// <summary>
/// Decisión institucional independiente de libros y notas. Una aprobada vigente es la única
/// fuente oficial declarada para una inscripción, curso y período; no calcula ni copia notas.
/// </summary>
public sealed class ResolucionTrasladoAcademico
{
    public int Id { get; set; }
    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;
    public int CicloEscolarId { get; set; }
    public CicloEscolar CicloEscolar { get; set; } = null!;
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
    public TipoResolucionTraslado Tipo { get; set; }
    public EstadoResolucionTraslado Estado { get; set; } = EstadoResolucionTraslado.Pendiente;
    public bool Vigente { get; set; } = true;

    // Solo aplica a consolidación y preserva tanto la propuesta como la
    // decisión aprobada, sin tocar las evidencias originales.
    [Precision(9, 4)]
    public decimal? EscalaMaximaConsolidada { get; set; }
    [Precision(9, 4)]
    public decimal? NotaConsolidadaPropuesta { get; set; }
    [Precision(9, 4)]
    public decimal? NotaConsolidadaAprobada { get; set; }

    [Required, StringLength(2000)]
    public string Justificacion { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? ObservacionDecision { get; set; }
    [Required, StringLength(450)]
    public string CreadaPorUsuarioId { get; set; } = string.Empty;
    public Usuario CreadaPorUsuario { get; set; } = null!;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)]
    public string? DecididaPorUsuarioId { get; set; }
    public Usuario? DecididaPorUsuario { get; set; }
    public DateTime? FechaDecisionUtc { get; set; }
    public int? SustituyeResolucionId { get; set; }
    public ResolucionTrasladoAcademico? SustituyeResolucion { get; set; }
    public List<FuenteResolucionTrasladoAcademico> Fuentes { get; set; } = [];
}
