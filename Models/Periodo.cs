
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Periodo
{
    public int Id { get; set; }

    public int CicloEscolarId { get; set; }

    public CicloEscolar CicloEscolar { get; set; } = null!;

    [Range(1, 4)]
    public int Numero { get; set; }

    [Required]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }
}
