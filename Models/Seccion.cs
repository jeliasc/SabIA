
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Seccion
{
    public int Id { get; set; }

    public int CicloEscolarId { get; set; }

    public CicloEscolar CicloEscolar { get; set; } = null!;

    public int GradoId { get; set; }

    public Grado Grado { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string Nombre { get; set; } = string.Empty;

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
