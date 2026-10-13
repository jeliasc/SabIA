using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class MovimientoCierreCalificaciones
{
    public int Id { get; set; }

    public int CierreCalificacionesId { get; set; }

    public CierreCalificaciones CierreCalificaciones { get; set; } =
        null!;

    public EstadoCierreCalificaciones EstadoAnterior { get; set; }

    public EstadoCierreCalificaciones EstadoNuevo { get; set; }

    [Required, StringLength(1000)]
    public string Motivo { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    [Required]
    public string RealizadoPorUsuarioId { get; set; } =
        string.Empty;

    public Usuario RealizadoPorUsuario { get; set; } = null!;
}