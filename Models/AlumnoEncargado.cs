
namespace Proyecto_Final.Models;

public class AlumnoEncargado
{
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    public int EncargadoId { get; set; }
    public Encargado Encargado { get; set; } = null!;

    public TipoParentesco Parentesco { get; set; }
}
