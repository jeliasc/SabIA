using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class HistorialCalificacion
{
    public int Id { get; set; }

    public TipoOrigenHistorialCalificacion TipoOrigen { get; set; }

    public int? CalificacionManualId { get; set; }

    public CalificacionManual? CalificacionManual { get; set; }

    public int? EntregaId { get; set; }

    public Entrega? Entrega { get; set; }

    public int? IntentoCuestionarioId { get; set; }

    public IntentoCuestionario? IntentoCuestionario { get; set; }

    public int? RespuestaAlumnoId { get; set; }

    public RespuestaAlumno? RespuestaAlumno { get; set; }

    [Precision(9, 2)]
    public decimal? NotaAnterior { get; set; }

    [Precision(9, 2)]
    public decimal? NotaNueva { get; set; }

    [Required, StringLength(1000)]
    public string Motivo { get; set; } = string.Empty;

    public DateTime FechaCambio { get; set; }

    [Required]
    public string ModificadoPorUsuarioId { get; set; } = string.Empty;

    public Usuario ModificadoPorUsuario { get; set; } = null!;
}