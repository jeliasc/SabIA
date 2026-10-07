
namespace Proyecto_Final.Models;

public class Inscripcion
{
    public int Id { get; set; }

    public int AlumnoId { get; set; }

    public Alumno Alumno { get; set; } = null!;

    public int SeccionId { get; set; }

    public Seccion Seccion { get; set; } = null!;

    public int CicloEscolarId { get; set; }

    public DateOnly Fecha { get; set; }

    public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Activa;
}
