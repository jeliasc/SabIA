namespace Proyecto_Final.Models;

// Registra la primera vez que un alumno abre una actividad publicada. La
// actividad es la tarea; un cuestionario no genera una novedad adicional.
public sealed class VisualizacionActividad
{
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    public DateTime FechaVisualizacionUtc { get; set; }
}
