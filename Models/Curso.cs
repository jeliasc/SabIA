
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Curso
{
    public int Id { get; set; }

    public int GradoId { get; set; }

    public Grado Grado { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
