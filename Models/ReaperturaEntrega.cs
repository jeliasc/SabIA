using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public sealed class ReaperturaEntrega
{
    public int Id { get; set; }

    public int EntregaId { get; set; }

    public Entrega Entrega { get; set; } = null!;

    public DateTime FechaReapertura { get; set; }

    public DateTime FechaLimite { get; set; }

    [Required]
    [StringLength(500)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public string ReabiertaPorUsuarioId { get; set; } = string.Empty;

    public Usuario ReabiertaPorUsuario { get; set; } = null!;
}
