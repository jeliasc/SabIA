using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public sealed class MovimientoCicloEscolar
{
    public long Id { get; set; }
    public int CicloEscolarId { get; set; }
    public CicloEscolar CicloEscolar { get; set; } = null!;
    public TipoMovimientoCicloEscolar Tipo { get; set; }
    public EstadoCicloEscolar EstadoAnterior { get; set; }
    public EstadoCicloEscolar EstadoNuevo { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    [Required, StringLength(450)] public string RealizadoPorUsuarioId { get; set; } = string.Empty;
    public Usuario RealizadoPorUsuario { get; set; } = null!;
    [StringLength(1000)] public string? Justificacion { get; set; }
}
