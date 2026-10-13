
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class RespuestaAlumno
{
    public int Id { get; set; }

    public int IntentoCuestionarioId { get; set; }

    public IntentoCuestionario IntentoCuestionario { get; set; } =
        null!;

    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;

    public string? TextoRespuesta { get; set; }

    public DateTime FechaPresentacion { get; set; }
    public DateTime? FechaRespuesta { get; set; }

    [Precision(9, 2)]
    public decimal? PunteoObtenido { get; set; }

    public bool RequiereRevisionManual { get; set; }

    public string? CalificadoPorUsuarioId { get; set; }
    public Usuario? CalificadoPorUsuario { get; set; }

    public DateTime? FechaCalificacion { get; set; }

    public ICollection<RespuestaAlumnoOpcion> OpcionesSeleccionadas { get; set; } = new List<RespuestaAlumnoOpcion>();
}
