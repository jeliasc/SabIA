using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class SolicitudCorreccionCalificacion
{
    public int Id { get; set; }

    public int ResultadoCalificacionPeriodoId { get; set; }
    public ResultadoCalificacionPeriodo ResultadoCalificacionPeriodo { get; set; } = null!;

    public int ActividadEvaluableId { get; set; }
    public ActividadEvaluable ActividadEvaluable { get; set; } = null!;

    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;

    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    [Precision(9, 2)]
    public decimal NotaAnterior { get; set; }

    [Precision(9, 2)]
    public decimal NotaPropuesta { get; set; }

    [Required, StringLength(1000)]
    public string MotivoSolicitud { get; set; } = string.Empty;

    public EstadoSolicitudCorreccion Estado { get; set; } =
        EstadoSolicitudCorreccion.Pendiente;

    [Required]
    public string SolicitadaPorUsuarioId { get; set; } = string.Empty;
    public Usuario SolicitadaPorUsuario { get; set; } = null!;
    public DateTime FechaSolicitud { get; set; }

    public string? RevisadaPorUsuarioId { get; set; }
    public Usuario? RevisadaPorUsuario { get; set; }
    public DateTime? FechaRevision { get; set; }

    [StringLength(1000)]
    public string? ObservacionRevision { get; set; }

    public string? AplicadaPorUsuarioId { get; set; }
    public Usuario? AplicadaPorUsuario { get; set; }
    public DateTime? FechaAplicacion { get; set; }

    public Guid VersionConcurrencia { get; set; } = Guid.NewGuid();
}
