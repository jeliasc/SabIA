
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Grado
{
    public int Id { get; set; }

    public NivelEducativo Nivel { get; set; }

    [Required]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Carrera { get; set; } = string.Empty;

    public int Orden { get; set; }

    public ICollection<Seccion> Secciones { get; set; } = new List<Seccion>();

    public ICollection<Curso> Cursos { get; set; } = new List<Curso>();
}
