namespace Proyecto_Final.Models;

public class RespuestaAlumnoOpcion
{
    public int RespuestaAlumnoId { get; set; }

    public int PreguntaId { get; set; }

    public RespuestaAlumno RespuestaAlumno { get; set; } = null!;

    public int OpcionPreguntaId { get; set; }

    public OpcionPregunta OpcionPregunta { get; set; } = null!;
}