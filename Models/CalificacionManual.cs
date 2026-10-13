using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class CalificacionManual
{
    public int Id { get; set; }

    public int ActividadEvaluableId { get; set; }

    public ActividadEvaluable ActividadEvaluable { get; set; } = null!;

    public int AlumnoId { get; set; }

    public Alumno Alumno { get; set; } = null!;

    public int? InscripcionId { get; set; }

    public Inscripcion? Inscripcion { get; set; }

    public EstadoCalificacionManual Estado { get; set; } = EstadoCalificacionManual.Pendiente;

    [Precision(9, 2)]
    public decimal? Nota { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    public DateTime? FechaCalificacion { get; set; }

    public string? CalificadoPorUsuarioId { get; set; }

    public Usuario? CalificadoPorUsuario { get; set; }
}
