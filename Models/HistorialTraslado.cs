
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class HistorialTraslado
{
    public int Id { get; set; }

    public int InscripcionId { get; set; }

    public int CicloEscolarId { get; set; }

    public Inscripcion Inscripcion { get; set; } = null!;

    public int SeccionOrigenId { get; set; }

    public Seccion SeccionOrigen { get; set; } = null!;

    public int SeccionDestinoId { get; set; }

    public Seccion SeccionDestino { get; set; } = null!;

    public DateTime FechaTraslado { get; set; }

    [Required]
    [StringLength(500)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public string RealizadoPorUsuarioId { get; set; } = string.Empty;

    public Usuario RealizadoPorUsuario { get; set; } = null!;
}
