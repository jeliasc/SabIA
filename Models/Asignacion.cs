
namespace Proyecto_Final.Models;

public class Asignacion
{
    public int Id { get; set; }

    public int SeccionId { get; set; }

    public Seccion Seccion { get; set; } = null!;

    public int CursoId { get; set; }

    public Curso Curso { get; set; } = null!;

    public int GradoId { get; set; }

    public int DocenteId { get; set; }

    public Docente Docente { get; set; } = null!;

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
